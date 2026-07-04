using FluentResults;
using Learnix.Application.Common.Pagination;
using MediatR;

namespace Learnix.Application.Wishlist.Queries.GetMyWishlist;

public sealed record GetMyWishlistQuery(int Skip, int Take)
    : IRequest<Result<PaginatedResult<WishlistCourseDto>>>;
