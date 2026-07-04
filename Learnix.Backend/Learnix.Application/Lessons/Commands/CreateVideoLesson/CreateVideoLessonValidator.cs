using FluentValidation;
using Learnix.Domain.Constants;

namespace Learnix.Application.Lessons.Commands.CreateVideoLesson;

public sealed class CreateVideoLessonValidator : AbstractValidator<CreateVideoLessonCommand>
{
    public CreateVideoLessonValidator()
    {
        RuleFor(x => x.CourseId)
            .NotEmpty();
        RuleFor(x => x.SectionId)
            .NotEmpty();
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(LessonConstants.TitleMaxLength);
        RuleFor(x => x.VideoBlobPath)
            .NotEmpty()
            .MaximumLength(LessonConstants.VideoUrlMaxLength);
        RuleFor(x => x.Description)
            .MaximumLength(LessonConstants.VideoDescriptionMaxLength);
        RuleFor(x => x.DurationSeconds)
            .GreaterThan(0)
            .When(x => x.DurationSeconds.HasValue);
    }
}
