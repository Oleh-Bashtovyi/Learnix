using FluentValidation;
using Learnix.Application.Categories.Validation;

namespace Learnix.Application.Categories.Commands.UpdateCategory;

public sealed class UpdateCategoryValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();

        RuleFor(x => x.Name)
            .ApplyCategoryNameRules();

        RuleFor(x => x.Slug)
            .ApplyCategorySlugRules();
    }
}
