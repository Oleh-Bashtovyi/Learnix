using FluentValidation;
using Learnix.Domain.Constants;

namespace Learnix.Application.Reviews.Validation;

public static class ReviewRules
{
    public static IRuleBuilderOptions<T, int> ApplyReviewRatingRules<T>(this IRuleBuilder<T, int> ruleBuilder)
    {
        return ruleBuilder
            .InclusiveBetween(ReviewConstants.MinRating, ReviewConstants.MaxRating);
    }

    public static IRuleBuilderOptions<T, string?> ApplyReviewCommentRules<T>(this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .MaximumLength(ReviewConstants.CommentMaxLength);
    }
}
