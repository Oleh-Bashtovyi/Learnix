using FluentValidation;

namespace Learnix.Application.Sections.Commands.DeleteSection;

public sealed class DeleteSectionValidator : AbstractValidator<DeleteSectionCommand>
{
    public DeleteSectionValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
        RuleFor(x => x.SectionId).NotEmpty();
    }
}
