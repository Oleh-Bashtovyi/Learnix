using FluentValidation;
using Learnix.Application.Sections.Validation;

namespace Learnix.Application.Sections.Commands.UpdateSectionTitle;

public sealed class UpdateSectionTitleValidator : AbstractValidator<UpdateSectionTitleCommand>
{
    public UpdateSectionTitleValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
        RuleFor(x => x.SectionId).NotEmpty();

        RuleFor(x => x.Title).ApplySectionTitleRules();
    }
}
