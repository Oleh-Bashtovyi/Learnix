namespace Learnix.Application.Common.Settings;

public sealed class AiChatSettings
{
    public string Provider { get; init; } = "Anthropic";
    public int MessagesPerSessionCap { get; init; } = 50;
    public int ContextWindowSize { get; init; } = 20;
}
