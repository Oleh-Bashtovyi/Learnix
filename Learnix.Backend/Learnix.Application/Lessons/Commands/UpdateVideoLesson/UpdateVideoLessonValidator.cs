using FluentValidation;
using Learnix.Application.Lessons.Validation;

namespace Learnix.Application.Lessons.Commands.UpdateVideoLesson;

public sealed class UpdateVideoLessonValidator : AbstractValidator<UpdateVideoLessonCommand>
{
    public UpdateVideoLessonValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
        RuleFor(x => x.LessonId).NotEmpty();

        RuleFor(x => x.Title).ApplyLessonTitleRules();

        RuleFor(x => x.VideoBlobPath).ApplyVideoBlobPathRules();

        RuleFor(x => x.Description).ApplyVideoDescriptionRules();

        RuleFor(x => x.DurationSeconds).GreaterThan(0).When(x => x.DurationSeconds.HasValue);
    }
}
