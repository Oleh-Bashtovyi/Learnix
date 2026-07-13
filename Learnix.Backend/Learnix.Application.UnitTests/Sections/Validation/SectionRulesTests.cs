using FluentValidation;
using FluentValidation.TestHelper;
using Learnix.Application.Sections.Validation;
using Learnix.Domain.Constants;

namespace Learnix.Application.UnitTests.Sections.Validation;

public class SectionRulesTests
{
    private class DummyModel
    {
        public string Title { get; set; } = string.Empty;
    }

    private class DummyValidator : AbstractValidator<DummyModel>
    {
        public DummyValidator()
        {
            RuleFor(x => x.Title).ApplySectionTitleRules();
        }
    }

    private readonly DummyValidator _validator = new();

    [Fact]
    public void Validate_WhenTitleIsValid_ShouldPass()
    {
        var model = new DummyModel { Title = "Valid Title" };
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenTitleIsTooLong_ShouldFail()
    {
        var model = new DummyModel { Title = new string('a', SectionConstants.TitleMaxLength + 1) };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }
}
