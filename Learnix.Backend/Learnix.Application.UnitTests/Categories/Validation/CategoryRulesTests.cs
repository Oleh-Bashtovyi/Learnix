using FluentValidation;
using FluentValidation.TestHelper;
using Learnix.Application.Categories.Validation;
using Learnix.Domain.Constants;

namespace Learnix.Application.UnitTests.Categories.Validation;

public class CategoryRulesTests
{
    private class DummyModel
    {
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
    }

    private class DummyValidator : AbstractValidator<DummyModel>
    {
        public DummyValidator()
        {
            RuleFor(x => x.Name).ApplyCategoryNameRules();
            RuleFor(x => x.Slug).ApplyCategorySlugRules();
        }
    }

    private readonly DummyValidator _validator = new();

    [Fact]
    public void Validate_WhenFieldsAreValid_ShouldPass()
    {
        var model = new DummyModel { Name = "Valid Name", Slug = "valid-slug" };
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenNameIsTooLong_ShouldFail()
    {
        var model = new DummyModel { Name = new string('a', CategoryConstants.NameMaxLength + 1), Slug = "valid" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenSlugIsTooLong_ShouldFail()
    {
        var model = new DummyModel { Name = "Valid", Slug = new string('a', CategoryConstants.SlugMaxLength + 1) };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Slug);
    }
}
