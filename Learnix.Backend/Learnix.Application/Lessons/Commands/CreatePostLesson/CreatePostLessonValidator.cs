using FluentValidation;
using Learnix.Domain.Constants;

namespace Learnix.Application.Lessons.Commands.CreatePostLesson;

public sealed class CreatePostLessonValidator : AbstractValidator<CreatePostLessonCommand>
{
    public CreatePostLessonValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
        RuleFor(x => x.SectionId).NotEmpty();
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(LessonConstants.TitleMaxLength);
        RuleFor(x => x.Content)
            .NotEmpty()
            .MaximumLength(LessonConstants.PostContentMaxLength);
    }
}
