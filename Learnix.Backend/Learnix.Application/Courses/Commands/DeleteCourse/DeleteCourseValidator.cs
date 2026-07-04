using FluentValidation;

namespace Learnix.Application.Courses.Commands.DeleteCourse;

public sealed class DeleteCourseValidator : AbstractValidator<DeleteCourseCommand>
{
    public DeleteCourseValidator() => RuleFor(x => x.CourseId).NotEmpty();
}
