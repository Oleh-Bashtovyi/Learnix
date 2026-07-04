using FluentValidation;

namespace Learnix.Application.Courses.Commands.AdminDeleteCourse;

internal sealed class AdminDeleteCourseValidator : AbstractValidator<AdminDeleteCourseCommand>
{
    public AdminDeleteCourseValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
    }
}
