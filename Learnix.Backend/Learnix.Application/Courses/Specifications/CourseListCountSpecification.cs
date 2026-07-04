using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Courses.Specifications;

public sealed class CourseListCountSpecification : Specification<Course>
{
    public CourseListCountSpecification(Guid? instructorId, string? search, Guid? categoryId)
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

        Query.AsNoTracking();
    }
}
