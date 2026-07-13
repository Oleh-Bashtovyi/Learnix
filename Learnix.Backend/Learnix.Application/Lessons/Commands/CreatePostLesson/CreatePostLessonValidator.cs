using FluentValidation;
using Learnix.Application.Lessons.Validation;

namespace Learnix.Application.Lessons.Commands.CreatePostLesson;

public sealed class CreatePostLessonValidator : AbstractValidator<CreatePostLessonCommand>
{
    public CreatePostLessonValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
        RuleFor(x => x.SectionId).NotEmpty();

        RuleFor(x => x.Title).ApplyLessonTitleRules();

        RuleFor(x => x.Content).ApplyPostContentRules();
    }
}
