using FluentValidation;

using Learnix.Domain.Constants;

namespace Learnix.Application.Reviews.Commands.CreateReview;

public sealed class CreateReviewValidator : AbstractValidator<CreateReviewCommand>
{
    public CreateReviewValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();

        RuleFor(x => x.Rating)
            .InclusiveBetween(ReviewConstants.MinRating, ReviewConstants.MaxRating);

        RuleFor(x => x.Comment)
            .MaximumLength(ReviewConstants.CommentMaxLength)
            .When(x => x.Comment is not null);
    }
}
