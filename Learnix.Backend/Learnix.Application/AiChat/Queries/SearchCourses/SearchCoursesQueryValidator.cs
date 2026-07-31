using FluentValidation;
using Learnix.Application.Courses.Constants;

namespace Learnix.Application.AiChat.Queries.SearchCourses;

internal sealed class SearchCoursesQueryValidator : AbstractValidator<SearchCoursesQuery>
{
    public SearchCoursesQueryValidator()
    {
        // Reuses the catalog's own bound: both now run the same full-text search
        // (ADR-BACK-CATALOG-001), so a second AiChat-specific ceiling would just be the same rule
        // stated twice.
        RuleFor(x => x.Query)
            .NotEmpty()
            .MaximumLength(CourseValidationConstants.SearchMaxLength);
    }
}
