using FluentValidation;

using Learnix.Domain.Constants;

namespace Learnix.Application.Reviews.Commands.UpdateReview;

public sealed class UpdateReviewValidator : AbstractValidator<UpdateReviewCommand>
{
    public UpdateReviewValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
        RuleFor(x => x.ReviewId).NotEmpty();

        RuleFor(x => x.Rating)
            .InclusiveBetween(ReviewConstants.MinRating, ReviewConstants.MaxRating);

        RuleFor(x => x.Comment)
            .MaximumLength(ReviewConstants.CommentMaxLength)
            .When(x => x.Comment is not null);
    }
}
