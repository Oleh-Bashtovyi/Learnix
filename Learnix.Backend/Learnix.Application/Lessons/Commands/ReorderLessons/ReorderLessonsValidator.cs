using FluentValidation;

namespace Learnix.Application.Lessons.Commands.ReorderLessons;

public sealed class ReorderLessonsValidator : AbstractValidator<ReorderLessonsCommand>
{
    public ReorderLessonsValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
        RuleFor(x => x.SectionId).NotEmpty();
        RuleFor(x => x.Items)
            .NotEmpty()
            .Must(items => items.Count <= 1000)
            .WithMessage("Too many lessons in reorder payload.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Id).NotEmpty();
            item.RuleFor(i => i.Order).GreaterThanOrEqualTo(0);
        });
    }
}
