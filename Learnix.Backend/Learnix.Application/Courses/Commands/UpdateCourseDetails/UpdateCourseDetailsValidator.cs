using FluentValidation;
using Learnix.Application.Courses.Validation;

namespace Learnix.Application.Courses.Commands.UpdateCourseDetails;

public sealed class UpdateCourseDetailsValidator : AbstractValidator<UpdateCourseDetailsCommand>
{
    public UpdateCourseDetailsValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
        RuleFor(x => x.CategoryId).NotEmpty();

        RuleFor(x => x.Title).ApplyCourseTitleRules();

        RuleFor(x => x.Description).ApplyCourseDescriptionRules();

        RuleFor(x => x.Price).ApplyCoursePriceRules();

        When(x => !string.IsNullOrWhiteSpace(x.CoverImageUrl), () =>
        {
            RuleFor(x => x.CoverImageUrl!).ApplyCoverImageRules();
        });

        RuleForEach(x => x.Tags).ApplyCourseTagItemRules();

        RuleFor(x => x.Tags).ApplyCourseTagsRules();
    }
}
