using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Messaging.Abstractions;

public interface IConversationRepository : IRepositoryBase<CourseConversation>
{
    Task<int> GetTotalUnreadAsync(Guid userId, bool isInstructor, CancellationToken ct = default);
}
