using FluentValidation;

namespace Learnix.Application.Enrollments.Commands.EnrollInCourse;

public sealed class EnrollInCourseValidator : AbstractValidator<EnrollInCourseCommand>
{
    public EnrollInCourseValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
    }
}
