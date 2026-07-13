using FluentValidation;
using Learnix.Application.Reviews.Validation;

namespace Learnix.Application.Reviews.Commands.CreateReview;

public sealed class CreateReviewValidator : AbstractValidator<CreateReviewCommand>
{
    public CreateReviewValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();

        RuleFor(x => x.Rating).ApplyReviewRatingRules();

        RuleFor(x => x.Comment).ApplyReviewCommentRules();
    }
}
