using Learnix.Application.AiChat.Abstractions.Models;

namespace Learnix.Application.AiChat.Abstractions;

public interface IChatSessionRepository
{
    Task<ChatSession?> GetActiveByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<ChatSession> CreateAsync(Guid userId, CancellationToken ct = default);
    Task AppendMessagesAsync(string sessionId, IEnumerable<ChatMessage> messages, CancellationToken ct = default);
    Task CloseSessionAsync(string sessionId, CancellationToken ct = default);
    Task DeleteOlderThanAsync(DateTime threshold, CancellationToken ct = default);
}
