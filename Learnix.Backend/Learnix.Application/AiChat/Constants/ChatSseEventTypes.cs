namespace Learnix.Application.AiChat.Constants;

/// <summary>
/// The <c>event:</c> names <see cref="Services.ChatStreamOrchestrator"/> puts on the wire and
/// <c>AiChatController</c> writes verbatim into the SSE frame. The client's <c>useAiChat</c> hook switches
/// on these same names — that side cannot share this constant (different language), so a rename there has
/// to be kept in sync by hand.
/// </summary>
public static class ChatSseEventTypes
{
    public const string TextDelta = "text_delta";
    public const string ToolUseStart = "tool_use_start";
    public const string ToolUseEnd = "tool_use_end";
    public const string MessageEnd = "message_end";
    public const string Error = "error";
}
