using System.Runtime.CompilerServices;
using System.Text.Json;
using Learnix.Application.AiChat.Abstractions;
using Learnix.Application.AiChat.Abstractions.Models;
using Learnix.Application.AiChat.Constants;
using Learnix.Application.AiChat.Queries.GetCourseContextForAi;
using Learnix.Application.AiChat.Tools;
using Learnix.Application.Common.Options;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Learnix.Application.AiChat.Services;

public sealed record SseEvent(string EventType, string Data);

public sealed class ChatStreamOrchestrator(
    IChatSessionRepository sessionRepository,
    IAiChatProvider provider,
    IEnumerable<IChatTool> tools,
    IMediator mediator,
    IAiAvailabilityStore availability,
    IOptions<AiChatOptions> aiChatOptions,
    ILogger<ChatStreamOrchestrator> logger)
{
    private readonly IReadOnlyList<IChatTool> _tools = tools.ToList();
    private readonly int _contextWindowSize = aiChatOptions.Value.ContextWindowSize;
    private readonly int _storedMessagesLimit = aiChatOptions.Value.StoredMessagesLimit;

    public async IAsyncEnumerable<SseEvent> StreamAsync(
        Guid userId,
        ChatScope scope,
        Guid? lessonId,
        string userMessage,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var session = await sessionRepository.GetOrCreateAsync(userId, scope, cancellationToken);

        var newUserMessage = new ChatMessage(ChatMessageRoles.User, userMessage, DateTime.UtcNow, null, lessonId);
        var allMessages = new List<ChatMessage>(session.Messages) { newUserMessage };

        var scopedTools = _tools.Where(t => t.IsAvailableIn(scope.Type)).ToList();
        var toolDefinitions = scopedTools.Select(t => t.Definition).ToList();
        var toolMap = scopedTools.ToDictionary(t => t.Name);
        var systemPrompt = ChatSystemPrompt.For(scope, lessonId, await LoadCourseContextAsync(scope, lessonId, cancellationToken));

        // Collect assistant messages to persist after streaming completes
        var assistantMessages = new List<ChatMessage>();

        // The turn loop cannot return anything — it is an iterator — so the failure/finish reason it
        // saw come back here. finishReasons collects one entry per turn; the last one is the reason the
        // turn actually shown to the user ended on.
        var failures = new List<AiOutage>();
        var finishReasons = new List<string>();
        var toolContext = new ChatToolContext(scope.CourseId, lessonId);

        await foreach (var evt in RunTurnLoopAsync(
                           allMessages, toolDefinitions, toolMap, systemPrompt, toolContext, assistantMessages,
                           failures, finishReasons, cancellationToken))
        {
            yield return evt;
        }

        // This turn is the health check: it just called the provider for real (ADR-BACK-CHAT-014).
        if (failures.Count > 0)
            await availability.ReportOutageAsync(failures[0], cancellationToken);
        else
            await availability.ReportSuccessAsync(cancellationToken);

        if (failures.Count == 0)
        {
            // Persist user message + all assistant messages from this turn
            var toAppend = new List<ChatMessage> { newUserMessage };
            toAppend.AddRange(assistantMessages);

            // The repository trims to the newest N; the session itself is never closed.
            await sessionRepository.AppendMessagesAsync(session.Id, toAppend, _storedMessagesLimit, cancellationToken);

            var totalMessages = session.Messages.Count + toAppend.Count;
            var sessionCount = Math.Min(totalMessages, _storedMessagesLimit);
            var finishReason = finishReasons.Count > 0 ? finishReasons[^1] : ChatFinishReasons.EndTurn;
            var truncated = IsTruncated(finishReason);
            yield return new SseEvent(
                ChatSseEventTypes.MessageEnd,
                ToJson(new MessageEndPayload(finishReason, truncated, sessionCount)));
        }
    }

    // S107/S3776: this is an async iterator. Every branch of the switch yields an SSE event, and a
    // C# iterator cannot delegate a yield to a helper method, so the provider event loop has to stay
    // in one body. The parameters are the loop's state, carried across turns.
#pragma warning disable S107, S3776
    private async IAsyncEnumerable<SseEvent> RunTurnLoopAsync(
        List<ChatMessage> conversation,
        List<ToolDefinition> toolDefinitions,
        Dictionary<string, IChatTool> toolMap,
        string systemPrompt,
        ChatToolContext toolContext,
        List<ChatMessage> assistantMessages,
        List<AiOutage> failures,
        List<string> finishReasons,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        const int maxToolTurns = 5;

        // Stays true only if every one of the maxToolTurns iterations still wanted another tool call —
        // i.e. the safety guard tripped, not a natural "no more tools" completion. See the fallback
        // call after the loop.
        var turnLimitReached = true;

        for (var turn = 0; turn < maxToolTurns; turn++)
        {
            var window = ChatToolResultCompactor.Compact(
                ChatConversationWindow.TakeAlignedWindow(conversation, _contextWindowSize),
                toolContext.LessonId);

            var request = new ChatRequest(window, toolDefinitions, systemPrompt);
            var result = new ProviderTurnResult();

            await foreach (var evt in StreamProviderTurnAsync(request, result, failures, cancellationToken))
                yield return evt;

            if (result.ProviderError) yield break;
            if (result.FinishReason is not null) finishReasons.Add(result.FinishReason);

            // Save assistant message for this turn
            var assistantMsg = new ChatMessage(
                ChatMessageRoles.Assistant,
                result.AssistantTextBuffer.ToString(),
                DateTime.UtcNow,
                result.HasToolUse ? result.PendingToolCalls : null);
            assistantMessages.Add(assistantMsg);
            conversation.Add(assistantMsg);

            if (!result.HasToolUse)
            {
                turnLimitReached = false;
                break;
            }

            // Execute tools and add results to conversation
            var toolResults = new List<ToolCall>();
            foreach (var tc in result.PendingToolCalls)
            {
                string resultJson;
                if (toolMap.TryGetValue(tc.ToolName, out var tool))
                {
                    try
                    {
                        resultJson = await tool.ExecuteAsync(
                            new ChatToolInvocation(tc.ArgumentsJson, toolContext), cancellationToken);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        // A tool's handler throwing (a transient DB error, malformed stored data, …) must
                        // not take the whole SSE stream down with it — the model gets to react to a failed
                        // tool the same way it reacts to one that returned no results.
                        logger.LogError(ex, "Tool {ToolName} threw while handling call {CallId}", tc.ToolName, tc.CallId);
                        resultJson = JsonSerializer.Serialize(new { error = "Tool execution failed" });
                    }
                }
                else
                {
                    resultJson = JsonSerializer.Serialize(new { error = $"Tool '{tc.ToolName}' not found" });
                }

                toolResults.Add(tc with { ResultJson = resultJson });

                // Parse result count for SSE notification
                var resultsCount = TryCountResults(resultJson);
                yield return new SseEvent(ChatSseEventTypes.ToolUseEnd, ToJson(new ToolUseEndPayload(tc.CallId, resultsCount)));
            }

            // Add tool_result message to conversation
            var toolResultMsg = new ChatMessage(ChatMessageRoles.ToolResult, string.Empty, DateTime.UtcNow, toolResults);
            assistantMessages.Add(toolResultMsg);
            conversation.Add(toolResultMsg);
        }

        if (!turnLimitReached) yield break;

        // The safety guard tripped while the model still had tool results it never got to answer from —
        // without this, the turn ends on a bare tool_result with no assistant text, and the client shows
        // nothing at all. One more call, tools withheld so the model cannot ask for an sixth, forces a
        // text synthesis of whatever was already gathered instead of silently dropping the answer.
        var finalWindow = ChatToolResultCompactor.Compact(
            ChatConversationWindow.TakeAlignedWindow(conversation, _contextWindowSize),
            toolContext.LessonId);

        var finalRequest = new ChatRequest(finalWindow, [], systemPrompt);
        var finalResult = new ProviderTurnResult();

        await foreach (var evt in StreamProviderTurnAsync(finalRequest, finalResult, failures, cancellationToken))
            yield return evt;

        if (finalResult.ProviderError) yield break;
        if (finalResult.FinishReason is not null) finishReasons.Add(finalResult.FinishReason);

        var finalMsg = new ChatMessage(ChatMessageRoles.Assistant, finalResult.AssistantTextBuffer.ToString(), DateTime.UtcNow, null);
        assistantMessages.Add(finalMsg);
        conversation.Add(finalMsg);
    }
#pragma warning restore S107, S3776

    /// <summary>
    /// One provider call's worth of stream events, translated to SSE and collected into
    /// <paramref name="result"/>. Extracted so the in-loop call and the post-loop, tools-withheld
    /// fallback call (see <see cref="RunTurnLoopAsync"/>) share this instead of duplicating the switch.
    /// An iterator can <c>await foreach</c> another iterator and re-yield its events — what it cannot do
    /// is delegate a bare <c>yield</c> through an ordinary method, which is why this still has to be an
    /// iterator itself rather than returning a value.
    /// </summary>
    private async IAsyncEnumerable<SseEvent> StreamProviderTurnAsync(
        ChatRequest request,
        ProviderTurnResult result,
        List<AiOutage> failures,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var streamEvent in provider.StreamChatAsync(request, cancellationToken))
        {
            switch (streamEvent)
            {
                case TextDeltaEvent textDelta:
                    result.AssistantTextBuffer.Append(textDelta.Content);
                    yield return new SseEvent(ChatSseEventTypes.TextDelta, ToJson(new TextDeltaPayload(textDelta.Content)));
                    break;

                case ToolUseStartEvent toolStart:
                    result.HasToolUse = true;
                    yield return new SseEvent(ChatSseEventTypes.ToolUseStart, ToJson(new ToolUseStartPayload(toolStart.ToolName, toolStart.CallId)));
                    break;

                case ToolUseEndEvent toolEnd:
                    result.PendingToolCalls.Add(new ToolCall(toolEnd.CallId, toolEnd.ToolName, toolEnd.ArgumentsJson));
                    break;

                case MessageEndEvent messageEnd:
                    result.FinishReason = messageEnd.FinishReason;
                    break;

                case ProviderErrorEvent error:
                    result.ProviderError = true;
                    failures.Add(new AiOutage(error.Code, error.Message, error.RetryAtUtc));
                    yield return new SseEvent(ChatSseEventTypes.Error, ErrorPayload(error));
                    break;
            }
        }
    }

    private sealed class ProviderTurnResult
    {
        public List<ToolCall> PendingToolCalls { get; } = [];
        public System.Text.StringBuilder AssistantTextBuffer { get; } = new();
        public bool HasToolUse { get; set; }
        public bool ProviderError { get; set; }
        public string? FinishReason { get; set; }
    }

    // The shape behind each SseEvent.Data. useAiChat.ts on the client mirrors these fields by hand (no
    // shared type across languages), so a field added or renamed here has to be carried over there too.
    private sealed record TextDeltaPayload(string Content);

    private sealed record ToolUseStartPayload(string ToolName, string CallId);

    private sealed record ToolUseEndPayload(string CallId, int ResultsCount);

    private sealed record MessageEndPayload(string FinishReason, bool Truncated, int SessionMessageCount);

    private sealed record ErrorEventPayload(string Code, DateTime? RetryAtUtc);

    private static string ToJson<T>(T payload) => JsonSerializer.Serialize(payload, ChatToolJson.Write);

    /// <summary>
    /// Whether a turn ended because the provider ran out of output budget rather than because the
    /// model actually finished. The two providers spell this differently — Anthropic's raw stop_reason
    /// is "max_tokens", Gemini's C# enum renders as "MaxTokens" — so the comparison is normalised
    /// rather than listing both.
    /// </summary>
    private static bool IsTruncated(string finishReason) =>
        finishReason.Replace("_", "").Equals("maxtokens", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// What the client is told about a failed turn: only what a student can act on. The provider's own
    /// message never leaves the server — it can carry key fragments and endpoint detail — and the reason is
    /// narrowed to the public one, so a rejected key reads as "unavailable" and not as a status report on
    /// our credentials (ADR-BACK-CHAT-014).
    /// </summary>
    private static string ErrorPayload(ProviderErrorEvent error) =>
        ToJson(new ErrorEventPayload(AiOutageReasons.Public(error.Code), error.RetryAtUtc));

    /// <summary>
    /// The course behind a tutor session. A failure here is not worth killing the turn over: the tutor keeps
    /// its tools and simply answers without knowing which course it is in — which is where it was before
    /// ADR-BACK-CHAT-013.
    /// </summary>
    private async Task<CourseContextForAiDto?> LoadCourseContextAsync(
        ChatScope scope,
        Guid? lessonId,
        CancellationToken cancellationToken)
    {
        if (scope.Type != ChatScopeType.Course || scope.CourseId is null)
            return null;

        var result = await mediator.Send(new GetCourseContextForAiQuery(scope.CourseId.Value, lessonId), cancellationToken);

        return result.IsSuccess ? result.Value : null;
    }

    private static int TryCountResults(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
                return doc.RootElement.GetArrayLength();
        }
        catch (JsonException)
        {
            // A tool that answered with something other than JSON has no countable results.
            // This only feeds a telemetry counter, so an unparseable payload is not worth failing the turn over.
        }

        return 0;
    }
}
