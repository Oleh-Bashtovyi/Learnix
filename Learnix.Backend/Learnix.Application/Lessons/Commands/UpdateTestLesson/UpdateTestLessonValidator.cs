using FluentValidation;
using Learnix.Application.Lessons.Validation;

namespace Learnix.Application.Lessons.Commands.UpdateTestLesson;

public sealed class UpdateTestLessonValidator : AbstractValidator<UpdateTestLessonCommand>
{
    public UpdateTestLessonValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
        RuleFor(x => x.LessonId).NotEmpty();

        RuleFor(x => x.Title).ApplyLessonTitleRules();

        RuleFor(x => x.Description).ApplyTestDescriptionRules();

        RuleFor(x => x.AttemptLimit).ApplyTestAttemptLimitRules();

        RuleFor(x => x.CooldownMinutes).ApplyTestCooldownRules();

        RuleFor(x => x.PassingThreshold).ApplyTestPassingThresholdRules();

        RuleFor(x => x.ReviewMode).ApplyTestReviewModeRules();

        RuleFor(x => x.Questions).ApplyTestQuestionsRules();
    }
}
