using FluentValidation;
using FluentValidation.TestHelper;
using Learnix.Application.Courses.Validation;
using Learnix.Domain.Constants;

namespace Learnix.Application.UnitTests.Courses.Validation;

public class CourseRulesTests
{
    private class DummyModel
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public List<string> Tags { get; set; } = new();
    }

    private class DummyValidator : AbstractValidator<DummyModel>
    {
        public DummyValidator()
        {
            RuleFor(x => x.Title).ApplyCourseTitleRules();
            RuleFor(x => x.Description).ApplyCourseDescriptionRules();
            RuleFor(x => x.Price).ApplyCoursePriceRules();
            RuleFor(x => x.Tags).ApplyCourseTagsRules();
            RuleForEach(x => x.Tags).ApplyCourseTagItemRules();
        }
    }

    private readonly DummyValidator _validator = new();

    [Fact]
    public void Validate_WhenFieldsAreValid_ShouldPass()
    {
        var model = new DummyModel
        {
            Title = "Valid Title",
            Description = "Valid Description",
            Price = 10m,
            Tags = new List<string> { "tag1" }
        };
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenTitleIsTooLong_ShouldFail()
    {
        var model = new DummyModel { Title = new string('a', CourseConstants.TitleMaxLength + 1), Description = "Valid" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Validate_WhenPriceIsNegative_ShouldFail()
    {
        var model = new DummyModel { Title = "Title", Description = "Desc", Price = -1m };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Price);
    }

    [Fact]
    public void Validate_WhenTooManyTags_ShouldFail()
    {
        var tags = Enumerable.Range(0, CourseConstants.MaxTagsPerCourse + 1).Select(i => $"tag{i}").ToList();
        var model = new DummyModel { Title = "Title", Description = "Desc", Tags = tags };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Tags);
    }
}
