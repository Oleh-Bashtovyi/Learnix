using FluentValidation;

namespace Learnix.Application.Courses.Commands.ArchiveCourse;

public sealed class ArchiveCourseValidator : AbstractValidator<ArchiveCourseCommand>
{
    public ArchiveCourseValidator() => RuleFor(x => x.CourseId).NotEmpty();
}
