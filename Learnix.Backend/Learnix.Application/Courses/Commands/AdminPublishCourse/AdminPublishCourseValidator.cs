using FluentValidation;

namespace Learnix.Application.Courses.Commands.AdminPublishCourse;

public sealed class AdminPublishCourseValidator : AbstractValidator<AdminPublishCourseCommand>
{
    public AdminPublishCourseValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
    }
}
