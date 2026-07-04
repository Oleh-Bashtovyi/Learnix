using FluentValidation;
using Learnix.Domain.Constants;

namespace Learnix.Application.Lessons.Commands.UpdatePostLesson;

public sealed class UpdatePostLessonValidator : AbstractValidator<UpdatePostLessonCommand>
{
    public UpdatePostLessonValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
        RuleFor(x => x.LessonId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(LessonConstants.TitleMaxLength);
        RuleFor(x => x.Content).NotEmpty().MaximumLength(LessonConstants.PostContentMaxLength);
    }
}
