namespace Learnix.Application.AiChat.Constants;

/// <summary>
/// The values <see cref="Abstractions.Models.ChatMessage.Role"/> takes — Learnix's own vocabulary for a
/// stored turn, not a provider's wire format. Anthropic and Gemini each translate these into their own
/// role types when building a request (<c>RoleType</c>, <c>"model"</c>/<c>"user"</c>).
/// </summary>
public static class ChatMessageRoles
{
    public const string User = "user";
    public const string Assistant = "assistant";
    public const string ToolResult = "tool_result";
}
