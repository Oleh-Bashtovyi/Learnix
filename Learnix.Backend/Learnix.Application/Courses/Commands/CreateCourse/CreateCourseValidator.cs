using FluentValidation;
using Learnix.Domain.Constants;

namespace Learnix.Application.Courses.Commands.CreateCourse;

public sealed class CreateCourseValidator : AbstractValidator<CreateCourseCommand>
{
    public CreateCourseValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();

        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(CourseConstants.TitleMaxLength);

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(CourseConstants.DescriptionMaxLength);

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(CourseConstants.MaxPrice);

        When(x => x.Tags is not null, () =>
        {
            RuleForEach(x => x.Tags!)
                .NotEmpty()
                .MaximumLength(CourseConstants.TagMaxLength);

            RuleFor(x => x.Tags!)
                .Must(tags => tags.Count() <= CourseConstants.MaxTagsPerCourse)
                .WithMessage($"Cannot have more than {CourseConstants.MaxTagsPerCourse} tags.");

            RuleFor(x => x.Tags!)
                .Must(HaveUniqueTags)
                .WithMessage("Tags must be unique (case-insensitive).");
        });
    }

    private static bool HaveUniqueTags(IEnumerable<string> tags)
    {
        var list = tags.ToList();
        return list.Select(t => t.Trim().ToLowerInvariant()).Distinct().Count() == list.Count;
    }
}
