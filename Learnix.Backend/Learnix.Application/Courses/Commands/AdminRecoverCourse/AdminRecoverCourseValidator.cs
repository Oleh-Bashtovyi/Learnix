using FluentValidation;

namespace Learnix.Application.Courses.Commands.AdminRecoverCourse;

internal sealed class AdminRecoverCourseValidator : AbstractValidator<AdminRecoverCourseCommand>
{
    public AdminRecoverCourseValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
    }
}
