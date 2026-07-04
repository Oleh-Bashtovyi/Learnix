using FluentValidation;

namespace Learnix.Application.Courses.Commands.UnarchiveCourse;

public sealed class UnarchiveCourseValidator : AbstractValidator<UnarchiveCourseCommand>
{
    public UnarchiveCourseValidator() => RuleFor(x => x.CourseId).NotEmpty();
}
