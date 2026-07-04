using FluentValidation;
using Learnix.Domain.Constants;

namespace Learnix.Application.Lessons.Commands.UpdateVideoLesson;

public sealed class UpdateVideoLessonValidator : AbstractValidator<UpdateVideoLessonCommand>
{
    public UpdateVideoLessonValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
        RuleFor(x => x.LessonId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(LessonConstants.TitleMaxLength);
        RuleFor(x => x.VideoBlobPath).NotEmpty().MaximumLength(LessonConstants.VideoUrlMaxLength);
        RuleFor(x => x.Description).MaximumLength(LessonConstants.VideoDescriptionMaxLength);
        RuleFor(x => x.DurationSeconds).GreaterThan(0).When(x => x.DurationSeconds.HasValue);
    }
}
