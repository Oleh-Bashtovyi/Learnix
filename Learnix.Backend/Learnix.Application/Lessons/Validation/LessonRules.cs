using FluentValidation;
using Learnix.Domain.Constants;
using Learnix.Domain.Enums;

namespace Learnix.Application.Lessons.Validation;

public static class LessonRules
{
    public static IRuleBuilderOptions<T, string> ApplyLessonTitleRules<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .MaximumLength(LessonConstants.TitleMaxLength);
    }

    public static IRuleBuilderOptions<T, string> ApplyVideoBlobPathRules<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .MaximumLength(LessonConstants.VideoUrlMaxLength);
    }

    public static IRuleBuilderOptions<T, string?> ApplyVideoDescriptionRules<T>(this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .MaximumLength(LessonConstants.VideoDescriptionMaxLength);
    }

    public static IRuleBuilderOptions<T, string> ApplyPostContentRules<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .MaximumLength(LessonConstants.PostContentMaxLength);
    }

    public static IRuleBuilderOptions<T, string?> ApplyTestDescriptionRules<T>(this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .MaximumLength(LessonConstants.DescriptionMaxLength);
    }

    public static IRuleBuilderOptions<T, int> ApplyTestPassingThresholdRules<T>(this IRuleBuilder<T, int> ruleBuilder)
    {
        return ruleBuilder
            .InclusiveBetween(LessonConstants.MinPassingThreshold, LessonConstants.MaxPassingThreshold);
    }

    public static IRuleBuilderOptions<T, int?> ApplyTestAttemptLimitRules<T>(this IRuleBuilder<T, int?> ruleBuilder)
    {
        return ruleBuilder.GreaterThan(0);
    }

    public static IRuleBuilderOptions<T, int?> ApplyTestCooldownRules<T>(this IRuleBuilder<T, int?> ruleBuilder)
    {
        return ruleBuilder.GreaterThanOrEqualTo(0);
    }

    public static IRuleBuilderOptions<T, TestReviewMode> ApplyTestReviewModeRules<T>(this IRuleBuilder<T, TestReviewMode> ruleBuilder)
    {
        return ruleBuilder.IsInEnum();
    }

    public static IRuleBuilderOptions<T, TCollection> ApplyTestQuestionsRules<T, TCollection>(this IRuleBuilder<T, TCollection> ruleBuilder)
        where TCollection : System.Collections.IEnumerable
    {
        return ruleBuilder.NotEmpty();
    }
}
