using FluentValidation;
using Learnix.Domain.Constants;

namespace Learnix.Application.Categories.Validation;

public static class CategoryRules
{
    public static IRuleBuilderOptions<T, string> ApplyCategoryNameRules<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .MaximumLength(CategoryConstants.NameMaxLength);
    }

    public static IRuleBuilderOptions<T, string> ApplyCategorySlugRules<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .MaximumLength(CategoryConstants.SlugMaxLength);
    }
}
