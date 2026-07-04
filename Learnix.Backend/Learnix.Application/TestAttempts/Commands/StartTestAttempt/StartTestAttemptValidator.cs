using FluentValidation;

namespace Learnix.Application.TestAttempts.Commands.StartTestAttempt;

public sealed class StartTestAttemptValidator : AbstractValidator<StartTestAttemptCommand>
{
    public StartTestAttemptValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
        RuleFor(x => x.LessonId).NotEmpty();
    }
}
