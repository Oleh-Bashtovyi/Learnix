using FluentValidation;
using Learnix.Domain.Constants;

namespace Learnix.Application.Courses.Commands.UpdateCourseDetails;

public sealed class UpdateCourseDetailsValidator : AbstractValidator<UpdateCourseDetailsCommand>
{
    public UpdateCourseDetailsValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
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

        When(x => !string.IsNullOrWhiteSpace(x.CoverImageUrl), () =>
        {
            RuleFor(x => x.CoverImageUrl!)
                .MaximumLength(CourseConstants.CoverImageUrlMaxLength);
        });

        RuleForEach(x => x.Tags)
            .NotEmpty()
            .MaximumLength(CourseConstants.TagMaxLength);

        RuleFor(x => x.Tags)
            .Must(tags => tags.Count() <= CourseConstants.MaxTagsPerCourse)
            .WithMessage($"Cannot have more than {CourseConstants.MaxTagsPerCourse} tags.");

        RuleFor(x => x.Tags)
            .Must(HaveUniqueTags)
            .WithMessage("Tags must be unique (case-insensitive).");
    }

    private static bool HaveUniqueTags(IEnumerable<string> tags)
    {
        var list = tags.ToList();
        return list.Select(t => t.Trim().ToLowerInvariant()).Distinct().Count() == list.Count;
    }
}
