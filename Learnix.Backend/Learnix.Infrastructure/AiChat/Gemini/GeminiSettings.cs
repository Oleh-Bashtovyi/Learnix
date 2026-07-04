namespace Learnix.Infrastructure.AiChat.Gemini;

public sealed class GeminiSettings
{
    public string ApiKey { get; init; } = string.Empty;
    public string Model { get; init; } = "gemini-2.5-flash";
    public int MaxTokens { get; init; } = 1024;
}
