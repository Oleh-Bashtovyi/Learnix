using FluentValidation;

namespace Learnix.Application.Categories.Commands.DeleteCategory;

public sealed class DeleteCategoryValidator : AbstractValidator<DeleteCategoryCommand>
{
    public DeleteCategoryValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
    }
}
