using FluentValidation;
using Learnix.Domain.Constants;

namespace Learnix.Application.Courses.Validation;

public static class CourseRules
{
    public static IRuleBuilderOptions<T, string> ApplyCourseTitleRules<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .MaximumLength(CourseConstants.TitleMaxLength);
    }

    public static IRuleBuilderOptions<T, string> ApplyCourseDescriptionRules<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .MaximumLength(CourseConstants.DescriptionMaxLength);
    }

    public static IRuleBuilderOptions<T, decimal> ApplyCoursePriceRules<T>(this IRuleBuilder<T, decimal> ruleBuilder)
    {
        return ruleBuilder
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(CourseConstants.MaxPrice);
    }

    public static void ApplyCourseTagsRules<T>(this IRuleBuilderInitial<T, IEnumerable<string>> ruleBuilder)
    {
        ruleBuilder
            .Must(tags => tags.Count() <= CourseConstants.MaxTagsPerCourse)
            .WithMessage($"Cannot have more than {CourseConstants.MaxTagsPerCourse} tags.")
            .Must(HaveUniqueTags)
            .WithMessage("Tags must be unique (case-insensitive).");
    }

    public static IRuleBuilderOptions<T, string> ApplyCourseTagItemRules<T>(this IRuleBuilderInitialCollection<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .MaximumLength(CourseConstants.TagMaxLength);
    }

    public static IRuleBuilderOptions<T, string> ApplyCoverImageRules<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .MaximumLength(CourseConstants.CoverImageUrlMaxLength);
    }

    private static bool HaveUniqueTags(IEnumerable<string> tags)
    {
        var list = tags.ToList();
        return list.Select(t => t.Trim().ToLowerInvariant()).Distinct().Count() == list.Count;
    }
}
