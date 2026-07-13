using FluentValidation;
using Learnix.Application.Lessons.Validation;

namespace Learnix.Application.Lessons.Commands.CreateTestLesson;

public sealed class CreateTestLessonValidator : AbstractValidator<CreateTestLessonCommand>
{
    public CreateTestLessonValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
        RuleFor(x => x.SectionId).NotEmpty();

        RuleFor(x => x.Title).ApplyLessonTitleRules();

        RuleFor(x => x.Description).ApplyTestDescriptionRules();

        RuleFor(x => x.AttemptLimit).ApplyTestAttemptLimitRules();

        RuleFor(x => x.CooldownMinutes).ApplyTestCooldownRules();

        RuleFor(x => x.PassingThreshold).ApplyTestPassingThresholdRules();

        RuleFor(x => x.ReviewMode).ApplyTestReviewModeRules();

        RuleFor(x => x.Questions).ApplyTestQuestionsRules();
    }
}
