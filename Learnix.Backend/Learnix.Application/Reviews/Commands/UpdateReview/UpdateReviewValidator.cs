using FluentValidation;
using Learnix.Application.Reviews.Validation;

namespace Learnix.Application.Reviews.Commands.UpdateReview;

public sealed class UpdateReviewValidator : AbstractValidator<UpdateReviewCommand>
{
    public UpdateReviewValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
        RuleFor(x => x.ReviewId).NotEmpty();

        RuleFor(x => x.Rating).ApplyReviewRatingRules();

        RuleFor(x => x.Comment).ApplyReviewCommentRules();
    }
}
