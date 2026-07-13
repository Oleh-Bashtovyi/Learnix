using FluentValidation;
using Learnix.Domain.Constants;

namespace Learnix.Application.Sections.Validation;

public static class SectionRules
{
    public static IRuleBuilderOptions<T, string> ApplySectionTitleRules<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .MaximumLength(SectionConstants.TitleMaxLength);
    }
}
