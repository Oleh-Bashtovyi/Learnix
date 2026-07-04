using Ardalis.Specification.EntityFrameworkCore;
using Learnix.Application.Messaging.Abstractions;
using Learnix.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Learnix.Infrastructure.Persistence.EntityFramework.Repositories;

internal sealed class ConversationRepository(ApplicationDbContext context)
    : RepositoryBase<CourseConversation>(context), IConversationRepository
{
    public async Task<int> GetTotalUnreadAsync(Guid userId, bool isInstructor, CancellationToken ct = default)
    {
        return isInstructor
            ? await DbContext.Set<CourseConversation>()
                .Where(c => c.InstructorId == userId)
                .SumAsync(c => c.InstructorUnreadCount, ct)
            : await DbContext.Set<CourseConversation>()
                .Where(c => c.StudentId == userId)
                .SumAsync(c => c.StudentUnreadCount, ct);
    }
}
