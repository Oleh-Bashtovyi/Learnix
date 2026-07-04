using FluentValidation;

namespace Learnix.Application.Courses.Commands.PublishCourse;

public sealed class PublishCourseValidator : AbstractValidator<PublishCourseCommand>
{
    public PublishCourseValidator() => RuleFor(x => x.CourseId).NotEmpty();
}
