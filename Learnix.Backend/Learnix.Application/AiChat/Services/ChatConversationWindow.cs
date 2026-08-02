using Learnix.Application.AiChat.Abstractions.Models;
using Learnix.Application.AiChat.Constants;

namespace Learnix.Application.AiChat.Services;

/// <summary>
/// Slices the sliding context window (ADR-BACK-CHAT-005) on a turn boundary.
/// </summary>
public static class ChatConversationWindow
{
    /// <summary>
    /// Returns the last <paramref name="size"/> messages, moved forward to the first message that can
    /// legally open a provider request.
    /// <para>
    /// A blind <c>TakeLast</c> can start the window on a <c>tool_result</c> whose <c>assistant</c> tool call
    /// was cut away. Both providers reject that: Anthropic sees a <c>tool_result</c> block referencing an
    /// unknown <c>tool_use_id</c>, Gemini a <c>FunctionResponse</c> with no <c>FunctionCall</c>.
    /// </para>
    /// </summary>
    public static IReadOnlyList<ChatMessage> TakeAlignedWindow(IReadOnlyList<ChatMessage> conversation, int size)
    {
        if (conversation.Count == 0)
            return conversation;

        var start = Math.Max(0, conversation.Count - size);

        while (start < conversation.Count && !IsTurnStart(conversation[start]))
            start++;

        // No turn start survived the cut — only possible when the configured window is smaller than a
        // single tool-use turn. Widen back to the most recent one rather than send an unusable window.
        if (start == conversation.Count)
        {
            start = conversation.Count - 1;
            while (start > 0 && !IsTurnStart(conversation[start]))
                start--;
        }

        return start == 0 ? conversation : conversation.Skip(start).ToList();
    }

    /// <summary>
    /// A plain user message. Tool results also carry the user role downstream, but they arrive here
    /// tagged <c>tool_result</c> and always hold the calls they answer.
    /// </summary>
    private static bool IsTurnStart(ChatMessage message) =>
        message.Role == ChatMessageRoles.User && message.ToolCalls is null or { Count: 0 };
}
