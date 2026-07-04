using FluentValidation;

namespace Learnix.Application.Courses.Commands.UnpublishCourse;

public sealed class UnpublishCourseValidator : AbstractValidator<UnpublishCourseCommand>
{
    public UnpublishCourseValidator() => RuleFor(x => x.CourseId).NotEmpty();
}
