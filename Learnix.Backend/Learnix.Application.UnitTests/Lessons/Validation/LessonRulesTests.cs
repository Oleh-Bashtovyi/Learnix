using FluentValidation;
using FluentValidation.TestHelper;
using Learnix.Application.Lessons.Validation;
using Learnix.Domain.Constants;
using Learnix.Domain.Enums;

namespace Learnix.Application.UnitTests.Lessons.Validation;

public class LessonRulesTests
{
    private class DummyModel
    {
        public string Title { get; set; } = string.Empty;
        public string VideoBlobPath { get; set; } = string.Empty;
        public string PostContent { get; set; } = string.Empty;
        public int PassingThreshold { get; set; }
        public TestReviewMode ReviewMode { get; set; }
    }

    private class DummyValidator : AbstractValidator<DummyModel>
    {
        public DummyValidator()
        {
            RuleFor(x => x.Title).ApplyLessonTitleRules();
            RuleFor(x => x.VideoBlobPath).ApplyVideoBlobPathRules();
            RuleFor(x => x.PostContent).ApplyPostContentRules();
            RuleFor(x => x.PassingThreshold).ApplyTestPassingThresholdRules();
            RuleFor(x => x.ReviewMode).ApplyTestReviewModeRules();
        }
    }

    private readonly DummyValidator _validator = new();

    [Fact]
    public void Validate_WhenFieldsAreValid_ShouldPass()
    {
        var model = new DummyModel
        {
            Title = "Valid Title",
            VideoBlobPath = "path/to/video.mp4",
            PostContent = "Valid Content",
            PassingThreshold = 80,
            ReviewMode = TestReviewMode.FullReview
        };
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenTitleIsTooLong_ShouldFail()
    {
        var model = new DummyModel { Title = new string('a', LessonConstants.TitleMaxLength + 1) };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Validate_WhenPassingThresholdIsOutOfRange_ShouldFail()
    {
        var model = new DummyModel { PassingThreshold = LessonConstants.MaxPassingThreshold + 1 };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.PassingThreshold);
    }

    [Fact]
    public void Validate_WhenReviewModeIsInvalid_ShouldFail()
    {
        var model = new DummyModel { ReviewMode = (TestReviewMode)999 };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.ReviewMode);
    }
}
