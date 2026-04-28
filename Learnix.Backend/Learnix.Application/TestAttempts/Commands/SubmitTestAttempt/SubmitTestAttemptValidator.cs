using FluentValidation;

namespace Learnix.Application.TestAttempts.Commands.SubmitTestAttempt;

public sealed class SubmitTestAttemptValidator : AbstractValidator<SubmitTestAttemptCommand>
{
    public SubmitTestAttemptValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
        RuleFor(x => x.LessonId).NotEmpty();
        RuleFor(x => x.Answers).NotNull();
        RuleForEach(x => x.Answers).ChildRules(a => a.RuleFor(x => x.QuestionOrder).GreaterThanOrEqualTo(0));
    }
}
