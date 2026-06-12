using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Common.Errors;
using Learnix.Application.Common.Pagination;
using Learnix.Application.Wishlist.Abstractions;
using MediatR;

namespace Learnix.Application.Wishlist.Queries.GetMyWishlist;

public sealed class GetMyWishlistQueryHandler(
    ICurrentUserService currentUser,
    IWishlistRepository wishlistRepository,
    IBlobStorageService blobStorage)
    : IRequestHandler<GetMyWishlistQuery, Result<PaginatedResult<WishlistCourseDto>>>
{
    public async Task<Result<PaginatedResult<WishlistCourseDto>>> Handle(
        GetMyWishlistQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError("Not authenticated."));

        var userId = currentUser.UserId.Value;
        var pagination = PaginationRequest.FromOffset(request.Skip, request.Take);

        var totalCount = await wishlistRepository.CountAsync(userId, cancellationToken);

        if (totalCount == 0)
            return Result.Ok(PaginatedResult<WishlistCourseDto>.Empty(pagination.PageIndex, pagination.PageSize));

        var items = await wishlistRepository.GetPagedAsync(
            userId, pagination.Skip, pagination.Take, cancellationToken);

        var dtos = items.Select(w => new WishlistCourseDto(
            w.CourseId,
            w.Course?.Title ?? string.Empty,
            w.Course?.CoverBlobPath is not null
                ? blobStorage.GetPublicUrl(w.Course.CoverBlobPath)
                : null,
            w.Course?.Price ?? 0m,
            w.Course?.Price == 0m,
            w.CreatedAt));

        return Result.Ok(PaginatedResult<WishlistCourseDto>.Create(
            dtos, pagination.PageIndex, pagination.PageSize, totalCount));
    }
}
