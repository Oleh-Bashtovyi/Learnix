namespace Learnix.Application.AiChat.Abstractions.Models;

public sealed record ChatMessage(
    string Role,
    string Content,
    DateTime SentAt,
    IReadOnlyList<ToolCall>? ToolCalls = null);
