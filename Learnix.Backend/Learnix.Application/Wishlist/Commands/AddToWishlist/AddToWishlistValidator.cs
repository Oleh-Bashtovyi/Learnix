using FluentValidation;

namespace Learnix.Application.Wishlist.Commands.AddToWishlist;

public sealed class AddToWishlistValidator : AbstractValidator<AddToWishlistCommand>
{
    public AddToWishlistValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
    }
}
