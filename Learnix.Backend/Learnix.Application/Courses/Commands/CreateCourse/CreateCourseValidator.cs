using FluentValidation;
using Learnix.Application.Courses.Validation;

namespace Learnix.Application.Courses.Commands.CreateCourse;

public sealed class CreateCourseValidator : AbstractValidator<CreateCourseCommand>
{
    public CreateCourseValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();

        RuleFor(x => x.Title).ApplyCourseTitleRules();

        RuleFor(x => x.Description).ApplyCourseDescriptionRules();

        RuleFor(x => x.Price).ApplyCoursePriceRules();

        When(x => x.Tags is not null, () =>
        {
            RuleForEach(x => x.Tags!).ApplyCourseTagItemRules();

            RuleFor(x => x.Tags!).ApplyCourseTagsRules();
        });
    }
}
