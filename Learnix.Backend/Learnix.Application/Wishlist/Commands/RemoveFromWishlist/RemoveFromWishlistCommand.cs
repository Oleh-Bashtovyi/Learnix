using FluentResults;
using MediatR;

namespace Learnix.Application.Wishlist.Commands.RemoveFromWishlist;

public sealed record RemoveFromWishlistCommand(Guid CourseId) : IRequest<Result>;
