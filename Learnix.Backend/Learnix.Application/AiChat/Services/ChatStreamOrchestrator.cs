using System.Runtime.CompilerServices;
using Learnix.Application.AiChat.Abstractions;
using Learnix.Application.AiChat.Abstractions.Models;
using Learnix.Application.AiChat.Constants;
using Learnix.Application.AiChat.Queries.GetCourseContextForAi;
using Learnix.Application.AiChat.Tools;
using Learnix.Application.Common.Settings;
using MediatR;
using Microsoft.Extensions.Options;

namespace Learnix.Application.AiChat.Services;

public sealed record SseEvent(string EventType, string Data);

public sealed class ChatStreamOrchestrator(
    IChatSessionRepository sessionRepository,
    IAiChatProvider provider,
    IEnumerable<IChatTool> tools,
    IMediator mediator,
    IAiAvailabilityStore availability,
    IOptions<AiChatSettings> aiChatOptions)
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

        var newUserMessage = new ChatMessage("user", userMessage, DateTime.UtcNow, null, lessonId);
        var allMessages = new List<ChatMessage>(session.Messages) { newUserMessage };

        var scopedTools = _tools.Where(t => t.IsAvailableIn(scope.Type)).ToList();
        var toolDefinitions = scopedTools.Select(t => t.Definition).ToList();
        var toolMap = scopedTools.ToDictionary(t => t.Name);
        var systemPrompt = ChatSystemPrompt.For(scope, lessonId, await LoadCourseContextAsync(scope, lessonId, cancellationToken));

        // Collect assistant messages to persist after streaming completes
        var assistantMessages = new List<ChatMessage>();

        // The turn loop cannot return anything — it is an iterator — so the failure it saw comes back here.
        var failures = new List<AiOutage>();
        var toolContext = new ChatToolContext(scope.CourseId, lessonId);

        await foreach (var evt in RunTurnLoopAsync(
                           allMessages, toolDefinitions, toolMap, systemPrompt, toolContext, assistantMessages,
                           failures, cancellationToken))
        {
            yield return evt;
        }

        // This turn is the health check: it just called the provider for real (ADR-CHAT-014).
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
            yield return new SseEvent("message_end", $"{{\"finishReason\":\"end_turn\",\"sessionMessageCount\":{sessionCount}}}");
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
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        const int maxToolTurns = 5;

        for (var turn = 0; turn < maxToolTurns; turn++)
        {
            var window = ChatToolResultCompactor.Compact(
                ChatConversationWindow.TakeAlignedWindow(conversation, _contextWindowSize),
                toolContext.LessonId);

            var pendingToolCalls = new List<ToolCall>();
            var assistantTextBuffer = new System.Text.StringBuilder();
            var hasToolUse = false;
            var providerError = false;

            var request = new ChatRequest(window, toolDefinitions, systemPrompt);

            await foreach (var streamEvent in provider.StreamChatAsync(request, cancellationToken))
            {
                switch (streamEvent)
                {
                    case TextDeltaEvent textDelta:
                        assistantTextBuffer.Append(textDelta.Content);
                        yield return new SseEvent("text_delta", $"{{\"content\":{System.Text.Json.JsonSerializer.Serialize(textDelta.Content)}}}");
                        break;

                    case ToolUseStartEvent toolStart:
                        hasToolUse = true;
                        yield return new SseEvent("tool_use_start", $"{{\"toolName\":{System.Text.Json.JsonSerializer.Serialize(toolStart.ToolName)},\"callId\":{System.Text.Json.JsonSerializer.Serialize(toolStart.CallId)}}}");
                        break;

                    case ToolUseEndEvent toolEnd:
                        pendingToolCalls.Add(new ToolCall(toolEnd.CallId, toolEnd.ToolName, toolEnd.ArgumentsJson));
                        break;

                    case MessageEndEvent:
                        // handled after the loop
                        break;

                    case ProviderErrorEvent error:
                        providerError = true;
                        failures.Add(new AiOutage(error.Code, error.Message, error.RetryAtUtc));
                        yield return new SseEvent("error", ErrorPayload(error));
                        break;
                }
            }

            if (providerError) yield break;

            // Save assistant message for this turn
            var assistantText = assistantTextBuffer.ToString();
            var assistantMsg = new ChatMessage(
                "assistant",
                assistantText,
                DateTime.UtcNow,
                hasToolUse ? pendingToolCalls : null);
            assistantMessages.Add(assistantMsg);
            conversation.Add(assistantMsg);

            if (!hasToolUse) break;

            // Execute tools and add results to conversation
            var toolResults = new List<ToolCall>();
            foreach (var tc in pendingToolCalls)
            {
                string resultJson;
                if (toolMap.TryGetValue(tc.ToolName, out var tool))
                {
                    resultJson = await tool.ExecuteAsync(new ChatToolInvocation(tc.ArgumentsJson, toolContext), cancellationToken);
                }
                else
                {
                    resultJson = $"{{\"error\":\"Tool '{tc.ToolName}' not found\"}}";
                }

                toolResults.Add(tc with { ResultJson = resultJson });

                // Parse result count for SSE notification
                var resultsCount = TryCountResults(resultJson);
                yield return new SseEvent("tool_use_end", $"{{\"callId\":{System.Text.Json.JsonSerializer.Serialize(tc.CallId)},\"resultsCount\":{resultsCount}}}");
            }

            // Add tool_result message to conversation
            var toolResultMsg = new ChatMessage("tool_result", string.Empty, DateTime.UtcNow, toolResults);
            assistantMessages.Add(toolResultMsg);
            conversation.Add(toolResultMsg);
        }
    }
#pragma warning restore S107, S3776

    /// <summary>
    /// What the client is told about a failed turn: only what a student can act on. The provider's own
    /// message never leaves the server — it can carry key fragments and endpoint detail — and the reason is
    /// narrowed to the public one, so a rejected key reads as "unavailable" and not as a status report on
    /// our credentials (ADR-CHAT-014).
    /// </summary>
    private static string ErrorPayload(ProviderErrorEvent error)
    {
        var retryAt = error.RetryAtUtc is null
            ? "null"
            : System.Text.Json.JsonSerializer.Serialize(error.RetryAtUtc.Value);

        var code = System.Text.Json.JsonSerializer.Serialize(AiOutageReasons.Public(error.Code));

        return $"{{\"code\":{code},\"retryAtUtc\":{retryAt}}}";
    }

    /// <summary>
    /// The course behind a tutor session. A failure here is not worth killing the turn over: the tutor keeps
    /// its tools and simply answers without knowing which course it is in — which is where it was before
    /// ADR-CHAT-013.
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
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                return doc.RootElement.GetArrayLength();
        }
        catch (System.Text.Json.JsonException)
        {
            // A tool that answered with something other than JSON has no countable results.
            // This only feeds a telemetry counter, so an unparseable payload is not worth failing the turn over.
        }

        return 0;
    }
}
