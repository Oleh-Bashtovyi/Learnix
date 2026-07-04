using FluentResults;
using MediatR;

namespace Learnix.Application.Wishlist.Commands.AddToWishlist;

public sealed record AddToWishlistCommand(Guid CourseId) : IRequest<Result>;
