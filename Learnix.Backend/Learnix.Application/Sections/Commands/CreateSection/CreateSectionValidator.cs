using FluentValidation;
using Learnix.Application.Sections.Validation;

namespace Learnix.Application.Sections.Commands.CreateSection;

public sealed class CreateSectionValidator : AbstractValidator<CreateSectionCommand>
{
    public CreateSectionValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();

        RuleFor(x => x.Title).ApplySectionTitleRules();
    }
}
