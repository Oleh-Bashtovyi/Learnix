# Learnix — ADR: AI Chat

> Covers Phase 8: B-44 (MongoDB), B-45 (AI providers + SSE), B-46 (session persistence), B-46.5 (cleanup).

## Підсумок: що реалізовано

| Endpoint | Що робить |
|---|---|
| `GET /api/ai-chat/session` | Отримання активної сесії чату для поточного користувача |
| `POST /api/ai-chat/messages` | Відправка повідомлення та отримання стріму (SSE) з відповіддю AI |
| `DELETE /api/ai-chat/session` | Очищення поточної сесії чату |

---

## ADR-CHAT-001: `IAiChatProvider` Abstraction

**Decision:** The Application layer defines `IAiChatProvider` with a single method returning `IAsyncEnumerable<ChatStreamEvent>`. Infrastructure contains `AnthropicChatProvider` and `GeminiChatProvider`. The active provider is selected via `appsettings.json` → `AiChat:Provider = "Anthropic" | "Gemini"`. DI resolves the correct implementation based on that string at startup.

Streaming events are normalized into a shared model regardless of provider:

```
TextDeltaEvent(Content)
ToolUseStartEvent(CallId, ToolName)
ToolUseEndEvent(CallId, ToolName, ArgumentsJson)
MessageEndEvent(FinishReason)
ProviderErrorEvent(Message, Code)
```

The system prompt is defined once in `AiChatConstants.SystemPrompt` (Application layer, `AiChat/Abstractions/AiChatConstants.cs`) and referenced by both providers. Duplication in individual provider classes is avoided.

**Why:**
- Swapping providers requires changing one config value — the Application layer is untouched.
- The tool execution loop is written once in `ChatStreamOrchestrator` and not duplicated per provider.
- `IAsyncEnumerable` allows streaming events directly into SSE without buffering the full response.
- Single source of truth for the system prompt ensures consistent AI behavior regardless of which provider is active.

**Rejected alternatives:**
- Anthropic-only in v1 — simpler, but loses the ability to switch for cost optimization or fallback.
- Separate handlers per provider — massive duplication of the tool execution loop.
- Return `Stream` instead of `IAsyncEnumerable<ChatStreamEvent>` — cheaper abstraction, but SSE parsing would leak into the Application layer, violating Clean Architecture.

---

## ADR-CHAT-002: `Anthropic.SDK` Package over Manual HTTP

**Decision:** `AnthropicChatProvider` uses the `Anthropic.SDK` NuGet package (v5.x, by tghamm) instead of hand-rolled HTTP requests. The three manual files — `AnthropicRequestBuilder`, `AnthropicSseParser`, `AnthropicDtos` — are deleted.

Key SDK usage:
- `client.Messages.StreamClaudeMessageAsync(parameters, ct)` — handles HTTP, SSE parsing, and connection lifecycle internally.
- `new Function(name, description, JsonNode.Parse(schema))` — builds tool definitions from our existing JSON schema strings without a separate DTO layer.
- `new Message(outputs)` — reconstructs the full assistant message from accumulated streaming events; `ToolUseContent` blocks are extracted from it after streaming completes.
- `AnthropicClient` is registered as a singleton and injected into the scoped `AnthropicChatProvider`.

**Why:**
- Eliminates ~150 lines of fragile SSE parsing and HTTP plumbing with no business value.
- Built-in retry with exponential backoff on transient errors.
- `StreamClaudeMessageAsync` returns `IAsyncEnumerable<MessageResponse>` — maps cleanly onto our own `IAsyncEnumerable<ChatStreamEvent>`.
- Tool definitions are created directly from our `ToolDefinition.ParametersJsonSchema` string via `JsonNode.Parse` — no conversion layer needed.

**Rejected alternatives:**
- Official `Anthropic` NuGet package (v10+) — first-party, but was in beta at the time with breaking changes between minor versions.
- Keep manual HTTP — no benefit; the SDK handles auth headers, base URL, retries, and SSE framing.

---

## ADR-CHAT-003: MongoDB for AI Chat Sessions

**Decision:** AI chat sessions are stored in a MongoDB collection `chat_sessions`. One document = one session = list of messages.

Document schema:
```json
{
  "_id": "ObjectId",
  "userId": "Guid",
  "isActive": true,
  "messages": [
    {
      "role": "user|assistant|tool_result",
      "content": "...",
      "sentAt": "DateTime",
      "toolCalls": [
        { "callId": "...", "toolName": "...", "argumentsJson": "...", "resultJson": "..." }
      ]
    }
  ],
  "createdAt": "DateTime",
  "updatedAt": "DateTime",
  "closedAt": "DateTime|null"
}
```

Index: `{ userId: 1, isActive: 1 }` — for fast lookup of the active session. Created by `MongoIndexInitializer` (`IHostedService`) at startup.

**Why:**
- Document structure is natural for conversational data — messages are stored as an array inside the document, not in a separate table with a FK.
- MongoDB `$push` gives atomic message appends without race conditions on concurrent requests.
- Schema changes (e.g., adding new fields to messages like `toolCalls`) require no migrations.

**Rejected alternatives:**
- PostgreSQL JSONB — also supports document structure, but PostgreSQL is already used for relational data; mixing concerns without strong reason is avoided.
- Redis lists — too ephemeral; unsuitable for 30-day retention.

---

## ADR-CHAT-004: Single Active Session per User

**Decision:** Each user has at most one active session (`isActive: true`). The session is closed (`isActive: false`, `closedAt` set) when either the 50-message hard cap is reached or the user explicitly calls `DELETE /api/ai-chat/session`. The next message automatically creates a new session.

Closed sessions are **not returned to the UI** (GET returns only the active session) but are retained for 30 days for observability, then removed by `ChatSessionCleanupService`.

**Why:**
- Simplified UX — a student always has a "current chat" with no history navigation needed.
- Soft close instead of hard delete — data is preserved for debugging if the AI gave a wrong answer.
- The 50-message hard cap prevents unbounded document growth and signals a clean break to the user.

**Rejected alternatives:**
- Multiple sessions with UI switching — more appropriate for a full-featured product, but over-engineering for an LMS where AI is a supporting tool, not the primary feature.
- Unbounded session with no cap — risk of exponential document growth and provider context overflow.

---

## ADR-CHAT-005: Sliding Window Context (20 Messages)

**Decision:** Only the last 20 messages from the active session are sent to the AI provider. Older messages remain in MongoDB but are excluded from the provider's context window.

Implemented in `ChatStreamOrchestrator` before each provider call: `conversation.TakeLast(_contextWindowSize)`. The window size is read from `IOptions<AiChatSettings>` (`AiChat:ContextWindowSize`, default 20) — tunable without recompilation.

**Why:**
- Cost control: fewer tokens = lower cost per request. 20 messages ≈ 10 user/assistant pairs — sufficient for the typical "recommend a course" scenario.
- Latency: shorter prompt = faster first token.
- Externalizing the value to config allows tuning without a code change.

**Rejected alternatives:**
- Send all messages — correct for complex long conversations, but expensive and slow as sessions grow.
- Summarize older messages — effective in production AI assistants, but significantly more complex (requires a separate API call for summary generation).

---

## ADR-CHAT-006: Tool Use for Course Recommendations

**Decision:** The AI provider has access to two tools registered via `IChatTool`:

- `search_courses(query, category?, maxResults?)` — searches published courses by keyword and optional category slug; returns `{ courses: [...] }`.
- `get_categories()` — returns all available categories with name, slug, and course count. Called by the AI when the user mentions a subject area and the AI needs the correct slug before calling `search_courses`.

Implemented via the `IChatTool` interface in the Application layer. Both tools delegate to `IMediator.Send(...)` — preserving the FluentValidation and logging pipeline.

Tool execution loop in `ChatStreamOrchestrator`:
1. Receives `ToolUseEndEvent` from the provider.
2. Locates the matching `IChatTool` by name.
3. Calls `ExecuteAsync` — result as a JSON string.
4. Appends a `tool_result` message to the conversation.
5. Calls the provider again with the updated context.
6. Loops until `MessageEndEvent` with no tool calls, or a max of 5 turns as a safety guard.

**Tool result format — always a JSON object, never a bare array:**
Both tools return `{ "courses": [...] }` / `{ "categories": [...] }` (not a raw JSON array). Gemini's `FunctionResponse.Response` is typed as `IDictionary<string, object?>` — a JSON object. Passing a bare array would cause a `JsonException` when deserializing the stored `ResultJson` back into `Dictionary<string, object>` during conversation history replay in `GeminiChatProvider.MapContents`. Anthropic is unaffected (tool results are passed as text), but the object wrapper is applied uniformly for consistency.

**`CourseSearchResultDto` fields:**
`CategoryName` (human-readable string, e.g. `"Programming"`) is returned instead of `CategoryId` (a raw GUID). The handler resolves category names in a single batch query after fetching courses (`CategoriesByIdsSpecification`), not per-row. `Course` has no `Category` navigation property — the batch query is the correct join-free approach.

**Category filtering in `search_courses`:**
Resolved in two steps — the handler looks up the category `Guid` from the slug via `CategoryBySlugSpecification`, then passes the `Guid?` to `CourseSearchSpecification`. This avoids adding a navigation property to the `Course` entity.

**`CourseSearchSpecification` — full-text index strategy:**
The spec uses `c.Title.ToLower().Contains(normalized)` which EF Core + Npgsql translates to `LOWER("Title") LIKE '%q%'`. A migration (`AddCourseSearchTrigram`) enables the `pg_trgm` PostgreSQL extension and creates functional GIN indexes on `LOWER("Title")` and `LOWER("Description")`. PostgreSQL uses these indexes for `LOWER(col) LIKE '%q%'` patterns. `EF.Functions.ILike` (which would emit `ILIKE`) is not used because `Application` does not reference `Microsoft.EntityFrameworkCore` — the spec stays in the Application layer.

**`get_platform_info(section?)` — static platform knowledge:**
A third tool provides information about how the platform works without any DB calls. It holds hardcoded content for 10 sections: `overview`, `enrollment`, `lessons`, `tests`, `achievements`, `certificates`, `becoming_instructor`, `payment`, `chat`, `account`. When called without a section it returns an index of available sections; the AI then calls again with the relevant section. This is the only tool registered as `Singleton` (alongside `GeminiChatProvider`) because it has no mutable dependencies. Anthropic and Gemini providers receive it alongside the other tools — no provider-specific filtering needed.

The system prompt (`AiChatConstants.SystemPrompt`) explicitly lists all three tools and their purpose so the AI knows when to reach for each one.

**Why:**
- Tool use is the standard approach for grounded recommendations rather than hallucination.
- `IChatTool` instances are registered as `IEnumerable<IChatTool>` — new tools can be added without changing the orchestrator.
- `get_categories` lets the AI discover slugs dynamically instead of guessing them, avoiding empty results from malformed category filters.
- `get_platform_info` keeps the system prompt short (token cost per request) while still giving the AI access to full platform knowledge on demand.
- pg_trgm GIN indexes keep search fast without moving the spec to Infrastructure or coupling Application to EF Core.

**Rejected alternatives:**
- RAG (Retrieval-Augmented Generation) — far more powerful for semantic search, but requires an embedding model and vector store. Over-engineering for the current course volume.
- System prompt with a full course list — does not scale beyond a few dozen courses.
- Embedding all platform info in the system prompt — sent with every request regardless of whether the user asks about the platform; wastes tokens on pure course-search conversations.
- Direct DB access in the tool from Infrastructure — violates Clean Architecture and bypasses the validation pipeline.
- Bare JSON array as tool result — breaks Gemini's `FunctionResponse.Response` deserialization (see tool result format note above).
- `EF.Functions.ILike` in `CourseSearchSpecification` — requires adding `Microsoft.EntityFrameworkCore` to Application, violating layer boundaries.

---

## ADR-CHAT-007: Rate Limiting AI Chat (20 requests/hour per user)

**Decision:** A dedicated `RateLimiterPolicy` `"ai-chat"` is applied to `POST /api/ai-chat/messages`. Limit: 20 requests per hour, partition key = `userId` (from the JWT `sub` claim). Implemented via ASP.NET `FixedWindowLimiter` — the same mechanism as the `"auth-strict"` policy (see DECISIONS_AUTH.md).

On limit exceeded: HTTP 429 + `ProblemDetails` with `Retry-After` header via the unified `OnRejected` handler.

**Why:**
- Cost control: every request to Anthropic/Gemini is billable. 20/hour ≈ 1 request every 3 minutes — sufficient for learning use.
- Anti-abuse: without a limit, one user can exhaust the API quota for the entire product.
- Per-user (not per-IP) partition is more accurate for authenticated endpoints where IP may be shared (NAT, VPN).

**Rejected alternatives:**
- Sliding window — more even distribution, but marginally more complex. The difference is negligible at 20/hour.
- Token budget (limit by token count) — more precise cost control, but requires tracking tokens in the database.

---

## ADR-CHAT-008: SSE over WebSocket for AI Streaming

**Decision:** `POST /api/ai-chat/messages` returns `Content-Type: text/event-stream`. The controller writes SSE events directly to `Response.Body` without buffering. This endpoint is intentionally excluded from the MediatR pipeline — `ChatStreamOrchestrator` is called directly because SSE requires access to `HttpContext.Response`.

SSE event format:
```
event: text_delta
data: {"content":"Hello"}

event: tool_use_start
data: {"toolName":"search_courses","callId":"abc123"}

event: tool_use_end
data: {"callId":"abc123","resultsCount":5}

event: message_end
data: {"finishReason":"end_turn","sessionMessageCount":12}

event: error
data: {"message":"Provider unavailable","code":"PROVIDER_ERROR"}
```

Tool execution is entirely server-side. The client receives `tool_use_start`/`tool_use_end` for UX purposes (e.g., showing a "searching courses…" spinner) but does not execute tools itself.

**Why:**
- SSE is simpler than WebSocket for one-way server→client streaming: no upgrade handshake, proxy-friendly, HTTP/1.1 compatible.
- Excluding the SSE endpoint from MediatR is a deliberate and documented exception — a handler cannot stream into `HttpContext.Response`.

**Client-side consumption — `fetch` + `ReadableStream`, not `EventSource`:**
The frontend reads the SSE stream via `fetch` with a `ReadableStream` reader. The browser's native `EventSource` API is intentionally not used because it does not support custom request headers (e.g., `Authorization: Bearer <token>`). JWT auth would be impossible with `EventSource` without degrading to query-string tokens.

**Rejected alternatives:**
- WebSocket — appropriate if bi-directional streaming is needed (e.g., Student↔Instructor messaging). One-way is sufficient for AI responses.
- Polling — simpler server implementation, but higher first-token latency and unnecessary load.
- Long polling — a compromise between polling and SSE, but more complex with no advantage over SSE for streaming.
- `EventSource` on the client — cannot send `Authorization` header; would require passing the JWT in the query string, which is a security downgrade (tokens appear in server logs).

---

## ADR-CHAT-009: Closed Session Cleanup (30-day Retention)

**Decision:** `ChatSessionCleanupService` is a `BackgroundService` using `PeriodicTimer` with a 24-hour interval. It deletes documents where `isActive: false` and `updatedAt < UtcNow - 30d` via `IChatSessionRepository.DeleteOlderThanAsync`. Runs immediately on startup, then every 24 hours.

`IServiceScopeFactory` is used to resolve the scoped `IChatSessionRepository` from the singleton-hosted service.

**Why:**
- 30 days is sufficient for post-mortem investigation if a student complains about a wrong AI answer.
- `PeriodicTimer` + `BackgroundService` is the same pattern as `RefreshTokenCleanupHostedService` — consistent with the codebase.
- `IServiceScopeFactory` is mandatory for singleton-hosted services that depend on scoped services.

**Rejected alternatives:**
- MongoDB TTL index (`expireAfterSeconds`) — automatic, requires no code, but only works on a single `Date` field and deletes the document unconditionally after the TTL expires. Conditional deletion (`isActive: false AND updatedAt < threshold`) is not supported — a TTL index would delete active sessions too.
- External cron job — unnecessary complexity for a single operation that belongs to the service itself.

---

## ADR-CHAT-010: `Google.GenAI` Official Library for Gemini

**Decision:** `GeminiChatProvider` uses the official `Google.GenAI` NuGet package instead of manual HTTP requests to the Generative Language API. Key usage:

- Documentation: https://googleapis.github.io/dotnet-genai/
- `new Client(apiKey: ...)` — initializes the client; the library resolves the correct API endpoint (`https://generativelanguage.googleapis.com`) internally. No `BaseUrl` configuration is needed or exposed.
- `client.Models.GenerateContentStreamAsync(model, contents, config)` — handles HTTP, SSE framing, and response deserialization.
- `GenerateContentConfig.SystemInstruction` — system prompt passed as a `Content` object with a single `Part`.
- `GenerateContentConfig.Tools` — tool declarations built from `ToolDefinition.ParametersJsonSchema` via `JsonSerializer.Deserialize<Schema>(...)`.
- `FunctionCall.Args` / `FunctionResponse.Response` — both typed as `IDictionary<string, object?>`. Values are deserialized from stored JSON via `JsonSerializer.Deserialize<Dictionary<string, object>>` (STJ produces `JsonElement` values for nested structures). The library uses `System.Text.Json` internally, which handles `JsonElement` values transparently during re-serialization — this is the expected usage pattern.

**Conversation history mapping (`MapContents`):**
Gemini's multi-turn format differs from Anthropic's:
- User/assistant text → `Content { Role = "user"|"model", Parts = [Part { Text }] }`
- Assistant tool call (replaying history) → `Content { Role = "model", Parts = [Part { FunctionCall }] }`
- Tool result → `Content { Role = "user", Parts = [Part { FunctionResponse }] }`

The `tool_result` role used internally in `ChatMessage` is mapped to `"user"` in Gemini's format (Gemini requires function responses to come from the `user` turn, not a separate role).

**`GeminiChatProvider` is registered as `Singleton`** (vs `Scoped` for `AnthropicChatProvider`). The `Client` instance is thread-safe and is reused across requests. Both registrations are correct; the difference is intentional — the Google client benefits from connection pooling across requests.

**Why:**
- Eliminates manual SSE parsing and HTTP plumbing (same rationale as ADR-CHAT-002 for Anthropic).
- The official library handles API versioning, model routing, and error mapping.
- `GenerateContentStreamAsync` returns `IAsyncEnumerable<GenerateContentResponse>` — maps directly onto `IAsyncEnumerable<ChatStreamEvent>`.

**Rejected alternatives:**
- Manual HTTP + custom SSE parser — equivalent to what was replaced; ~200 lines of fragile plumbing with no business value.
- `Google.Ai.Generativelanguage.V1beta` (gRPC-based) — lower-level, more complex, no streaming SSE; overkill for this use case.
