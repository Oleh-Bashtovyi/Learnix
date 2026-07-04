using System.Runtime.CompilerServices;
using Learnix.Application.AiChat.Abstractions;
using Learnix.Application.AiChat.Abstractions.Models;
using Learnix.Application.AiChat.Tools;
using Learnix.Application.Common.Settings;
using Microsoft.Extensions.Options;

namespace Learnix.Application.AiChat.Services;

public sealed record SseEvent(string EventType, string Data);

public sealed class ChatStreamOrchestrator(
    IChatSessionRepository sessionRepository,
    IAiChatProvider provider,
    IEnumerable<IChatTool> tools,
    IOptions<AiChatSettings> aiChatOptions)
{
    private readonly IReadOnlyList<IChatTool> _tools = tools.ToList();
    private readonly int _contextWindowSize = aiChatOptions.Value.ContextWindowSize;
    private readonly int _messagesPerSessionCap = aiChatOptions.Value.MessagesPerSessionCap;

    public async IAsyncEnumerable<SseEvent> StreamAsync(
        Guid userId,
        string userMessage,
        [EnumeratorCancellation] CancellationToken ct)
    {
        // Load or create active session
        var session = await sessionRepository.GetActiveByUserIdAsync(userId, ct)
            ?? await sessionRepository.CreateAsync(userId, ct);

        var newUserMessage = new ChatMessage("user", userMessage, DateTime.UtcNow);
        var allMessages = new List<ChatMessage>(session.Messages) { newUserMessage };

        var toolDefinitions = _tools.Select(t => t.Definition).ToList();
        var toolMap = _tools.ToDictionary(t => t.Name);

        // Collect assistant messages to persist after streaming completes
        var assistantMessages = new List<ChatMessage>();
        string? errorMessage = null;

        await foreach (var evt in RunTurnLoopAsync(allMessages, toolDefinitions, toolMap, assistantMessages, ct))
        {
            if (evt.EventType == "error")
                errorMessage = evt.Data;
            yield return evt;
        }

        if (errorMessage is null)
        {
            // Persist user message + all assistant messages from this turn
            var toAppend = new List<ChatMessage> { newUserMessage };
            toAppend.AddRange(assistantMessages);
            await sessionRepository.AppendMessagesAsync(session.Id, toAppend, ct);

            // Check hard cap
            var totalMessages = session.Messages.Count + toAppend.Count;
            if (totalMessages >= _messagesPerSessionCap)
                await sessionRepository.CloseSessionAsync(session.Id, ct);

            var sessionCount = Math.Min(totalMessages, _messagesPerSessionCap);
            yield return new SseEvent("message_end", $"{{\"finishReason\":\"end_turn\",\"sessionMessageCount\":{sessionCount}}}");
        }
    }

    private async IAsyncEnumerable<SseEvent> RunTurnLoopAsync(
        List<ChatMessage> conversation,
        List<ToolDefinition> toolDefinitions,
        Dictionary<string, IChatTool> toolMap,
        List<ChatMessage> assistantMessages,
        [EnumeratorCancellation] CancellationToken ct)
    {
        const int maxToolTurns = 5;

        for (var turn = 0; turn < maxToolTurns; turn++)
        {
            var window = conversation.TakeLast(_contextWindowSize).ToList();
            var pendingToolCalls = new List<ToolCall>();
            var assistantTextBuffer = new System.Text.StringBuilder();
            var hasToolUse = false;
            var providerError = false;

            await foreach (var streamEvent in provider.StreamChatAsync(window, toolDefinitions, ct))
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
                        yield return new SseEvent("error", $"{{\"message\":{System.Text.Json.JsonSerializer.Serialize(error.Message)},\"code\":{System.Text.Json.JsonSerializer.Serialize(error.Code)}}}");
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
                    resultJson = await tool.ExecuteAsync(tc.ArgumentsJson, ct);
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

    private static int TryCountResults(string json)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                return doc.RootElement.GetArrayLength();
        }
        catch { }
        return 0;
    }
}
