using FluentValidation;

namespace Learnix.Application.Courses.Queries.GetPublicCourses;

public sealed class GetPublicCoursesValidator : AbstractValidator<GetPublicCoursesQuery>
{
    private static readonly string[] AllowedSortValues = ["popular", "newest", "rating"];

    public GetPublicCoursesValidator()
    {
        RuleFor(x => x.Skip).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Take)
            .InclusiveBetween(1, Learnix.Application.Common.Constants.PaginationConstants.MaxPageSize);
        RuleFor(x => x.SortBy)
            .Must(v => v is null || AllowedSortValues.Contains(v))
            .WithMessage($"sortBy must be one of: {string.Join(", ", AllowedSortValues)}");
        RuleFor(x => x.MinRating)
            .InclusiveBetween(0m, 5m)
            .When(x => x.MinRating.HasValue);
    }
}
