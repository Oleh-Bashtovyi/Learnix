namespace Learnix.Application.AiChat.Abstractions.Models;

public sealed class ChatSession
{
    public string Id { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public bool IsActive { get; init; }
    public List<ChatMessage> Messages { get; init; } = [];
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
