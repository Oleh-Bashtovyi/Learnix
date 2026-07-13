using FluentValidation;
using Learnix.Application.Lessons.Validation;

namespace Learnix.Application.Lessons.Commands.UpdatePostLesson;

public sealed class UpdatePostLessonValidator : AbstractValidator<UpdatePostLessonCommand>
{
    public UpdatePostLessonValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
        RuleFor(x => x.LessonId).NotEmpty();

        RuleFor(x => x.Title).ApplyLessonTitleRules();

        RuleFor(x => x.Content).ApplyPostContentRules();
    }
}
