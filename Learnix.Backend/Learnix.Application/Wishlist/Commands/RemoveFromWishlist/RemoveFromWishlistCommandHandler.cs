using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Wishlist.Abstractions;
using MediatR;

namespace Learnix.Application.Wishlist.Commands.RemoveFromWishlist;

public sealed class RemoveFromWishlistCommandHandler(
    ICurrentUserService currentUser,
    IWishlistRepository wishlistRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RemoveFromWishlistCommand, Result>
{
    public async Task<Result> Handle(RemoveFromWishlistCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        await wishlistRepository.RemoveIfExistsAsync(
            currentUser.UserId.Value, request.CourseId, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
