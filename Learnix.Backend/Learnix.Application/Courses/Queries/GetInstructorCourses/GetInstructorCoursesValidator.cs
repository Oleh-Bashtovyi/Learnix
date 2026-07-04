using FluentValidation;

namespace Learnix.Application.Courses.Queries.GetInstructorCourses;

public sealed class GetInstructorCoursesValidator : AbstractValidator<GetInstructorCoursesQuery>
{
    public GetInstructorCoursesValidator()
    {
        RuleFor(x => x.Skip).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Take)
            .InclusiveBetween(1, Learnix.Application.Common.Constants.PaginationConstants.MaxPageSize);
    }
}
