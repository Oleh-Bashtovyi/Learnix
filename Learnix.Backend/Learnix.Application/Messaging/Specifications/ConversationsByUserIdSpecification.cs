using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Messaging.Specifications;

public sealed class ConversationsByUserIdSpecification : Specification<CourseConversation>
{
    public ConversationsByUserIdSpecification(Guid userId, int skip, int take, string? searchQuery)
    {
        Query
            .Where(c => c.StudentId == userId || c.InstructorId == userId)
            .Include(c => c.Course)
            .Include(c => c.Student)
            .Include(c => c.Instructor)
            .OrderByDescending(c => c.LastMessageAt)
            .Skip(skip)
            .Take(take)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var lowerSearch = searchQuery.ToLower();
            Query.Where(c =>
                c.Course!.Title.ToLower().Contains(lowerSearch) ||
                (c.StudentId == userId && (c.Instructor!.FirstName + " " + c.Instructor!.LastName).ToLower().Contains(lowerSearch)) ||
                (c.InstructorId == userId && (c.Student!.FirstName + " " + c.Student!.LastName).ToLower().Contains(lowerSearch))
            );
        }
    }
}
