using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.AiChat.Specifications;

public sealed class AllCategoriesSpecification : Specification<Category>
{
    public AllCategoriesSpecification()
    {
        Query.OrderByDescending(c => c.CoursesCount).AsNoTracking();
    }
}
