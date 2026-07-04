using FluentValidation;

namespace Learnix.Application.Courses.Queries.GetAdminCourses;

public sealed class GetAdminCoursesValidator : AbstractValidator<GetAdminCoursesQuery>
{
    public GetAdminCoursesValidator()
    {
        RuleFor(x => x.Skip).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Take)
            .InclusiveBetween(1, Learnix.Application.Common.Constants.PaginationConstants.MaxPageSize);
    }
}
