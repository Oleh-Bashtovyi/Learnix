using FluentValidation;
using Learnix.Application.Lessons.Commands.DeleteLesson;

namespace Learnix.Application.Lessons.Commands.ToggleLessonVisibility;

public sealed class ToggleLessonVisibilityValidator : AbstractValidator<DeleteLessonCommand>
{
    public ToggleLessonVisibilityValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
        RuleFor(x => x.LessonId).NotEmpty();
    }
}
