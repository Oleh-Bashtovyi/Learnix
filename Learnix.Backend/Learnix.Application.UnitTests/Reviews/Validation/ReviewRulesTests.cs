using FluentValidation;
using FluentValidation.TestHelper;
using Learnix.Application.Reviews.Validation;
using Learnix.Domain.Constants;

namespace Learnix.Application.UnitTests.Reviews.Validation;

public class ReviewRulesTests
{
    private class DummyModel
    {
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }

    private class DummyValidator : AbstractValidator<DummyModel>
    {
        public DummyValidator()
        {
            RuleFor(x => x.Rating).ApplyReviewRatingRules();
            RuleFor(x => x.Comment).ApplyReviewCommentRules();
        }
    }

    private readonly DummyValidator _validator = new();

    [Fact]
    public void Validate_WhenFieldsAreValid_ShouldPass()
    {
        var model = new DummyModel { Rating = 5, Comment = "Great course!" };
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenRatingIsOutOfRange_ShouldFail()
    {
        var model = new DummyModel { Rating = ReviewConstants.MaxRating + 1 };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Rating);
    }

    [Fact]
    public void Validate_WhenCommentIsTooLong_ShouldFail()
    {
        var model = new DummyModel { Rating = 5, Comment = new string('a', ReviewConstants.CommentMaxLength + 1) };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Comment);
    }
}
