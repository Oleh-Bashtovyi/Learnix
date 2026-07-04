using FluentValidation;

namespace Learnix.Application.Lessons.Commands.ToggleLessonVisibility;

public sealed class ToggleLessonVisibilityValidator : AbstractValidator<ToggleLessonVisibilityCommand>
{
    public ToggleLessonVisibilityValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
        RuleFor(x => x.LessonId).NotEmpty();
    }
}
