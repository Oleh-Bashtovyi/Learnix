namespace Learnix.Application.AiChat.Abstractions.Models;

public abstract record ChatStreamEvent;

public sealed record TextDeltaEvent(string Content) : ChatStreamEvent;

public sealed record ToolUseStartEvent(string CallId, string ToolName) : ChatStreamEvent;

public sealed record ToolUseEndEvent(string CallId, string ToolName, string ArgumentsJson) : ChatStreamEvent;

public sealed record MessageEndEvent(string FinishReason) : ChatStreamEvent;

public sealed record ProviderErrorEvent(string Message, string Code) : ChatStreamEvent;
