using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Courses.Specifications;

public sealed class CourseListSpecification : Specification<Course>
{
    public CourseListSpecification(Guid? instructorId, string? search, Guid? categoryId, int skip, int take)
    {
        if (instructorId.HasValue)
        {
            Query.Where(c => c.InstructorId == instructorId.Value);
        }

        if (categoryId.HasValue)
        {
            Query.Where(c => c.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim().ToLower();
            Query.Where(c => c.Title.ToLower().Contains(normalized));
        }

        Query
            .OrderByDescending(c => c.UpdatedAt)
            .ThenBy(c => c.Title)
            .Skip(skip)
            .Take(take)
            .AsNoTracking();
    }
}
