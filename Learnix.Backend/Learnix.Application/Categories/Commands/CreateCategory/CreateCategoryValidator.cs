using FluentValidation;
using Learnix.Domain.Constants;

namespace Learnix.Application.Categories.Commands.CreateCategory;

public sealed class CreateCategoryValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(CategoryConstants.NameMaxLength);

        RuleFor(x => x.Slug)
            .NotEmpty()
            .MaximumLength(CategoryConstants.SlugMaxLength);
    }
}
