using FluentValidation;
using Learnix.Domain.Constants;

namespace Learnix.Application.Lessons.Commands.CreateTestLesson;

public sealed class CreateTestLessonValidator : AbstractValidator<CreateTestLessonCommand>
{
    public CreateTestLessonValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
        RuleFor(x => x.SectionId).NotEmpty();
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(LessonConstants.TitleMaxLength);
        RuleFor(x => x.Description)
            .MaximumLength(LessonConstants.DescriptionMaxLength);
        RuleFor(x => x.AttemptLimit)
            .GreaterThan(0)
            .When(x => x.AttemptLimit.HasValue);
        RuleFor(x => x.CooldownMinutes)
            .GreaterThanOrEqualTo(0)
            .When(x => x.CooldownMinutes.HasValue);
        RuleFor(x => x.PassingThreshold)
            .InclusiveBetween(LessonConstants.MinPassingThreshold, LessonConstants.MaxPassingThreshold);
        RuleFor(x => x.Questions)
            .NotEmpty();
    }
}
