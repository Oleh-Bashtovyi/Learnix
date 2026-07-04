using FluentValidation;

namespace Learnix.Application.Courses.Commands.AdminUnpublishCourse;

internal sealed class AdminUnpublishCourseValidator : AbstractValidator<AdminUnpublishCourseCommand>
{
    public AdminUnpublishCourseValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
    }
}
