using FluentValidation;

namespace Learnix.Application.Categories.Commands.SetCategoryImage;

public sealed class SetCategoryImageCommandValidator : AbstractValidator<SetCategoryImageCommand>
{
    public SetCategoryImageCommandValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.BlobPath).NotEmpty();
    }
}
