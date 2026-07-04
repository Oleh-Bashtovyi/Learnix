using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Courses.Specifications;

public sealed class AdminCourseListCountSpecification : Specification<Course>
{
    public AdminCourseListCountSpecification(string? search, Guid? categoryId)
    {
        Query.IgnoreQueryFilters();

        if (categoryId.HasValue)
            Query.Where(c => c.CategoryId == categoryId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim().ToLower();
            Query.Where(c => c.Title.ToLower().Contains(normalized));
        }
    }
}
