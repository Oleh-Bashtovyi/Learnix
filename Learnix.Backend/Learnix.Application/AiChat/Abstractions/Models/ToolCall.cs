namespace Learnix.Application.AiChat.Abstractions.Models;

public sealed record ToolCall(
    string CallId,
    string ToolName,
    string ArgumentsJson,
    string? ResultJson = null);
