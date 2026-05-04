using FluentValidation;

namespace Learnix.Application.Categories.Commands.DeleteCategoryImage;

public sealed class DeleteCategoryImageCommandValidator : AbstractValidator<DeleteCategoryImageCommand>
{
    public DeleteCategoryImageCommandValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
    }
}
