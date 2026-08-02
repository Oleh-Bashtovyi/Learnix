using FluentValidation;
using Learnix.Application.TestAttempts.Constants;

namespace Learnix.Application.TestAttempts.Commands.SubmitTestAttempt;

public sealed class SubmitTestAttemptValidator : AbstractValidator<SubmitTestAttemptCommand>
{
    public SubmitTestAttemptValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
        RuleFor(x => x.LessonId).NotEmpty();
        RuleFor(x => x.AttemptId).NotEmpty();
        RuleFor(x => x.Answers).NotNull();
        RuleForEach(x => x.Answers).ChildRules(a =>
            a.RuleFor(x => x.QuestionOrder).GreaterThanOrEqualTo(0));

        // One answer per question. Two answers for the same one is not a question the scorer can settle —
        // whichever it took would be an arbitrary choice made on the student's behalf — so it is a
        // malformed request and gets told so.
        RuleFor(x => x.Answers)
            .Must(answers => answers.Select(a => a.QuestionOrder).Distinct().Count() == answers.Count)
            .When(x => x.Answers is not null)
            .WithMessage(TestAttemptMessages.DuplicateQuestionOrder);
    }
}
