using FluentValidation;

namespace Learnix.Application.LessonProgress.Commands.MarkLessonComplete;

public sealed class MarkLessonCompleteValidator : AbstractValidator<MarkLessonCompleteCommand>
{
    public MarkLessonCompleteValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
        RuleFor(x => x.LessonId).NotEmpty();
    }
}
