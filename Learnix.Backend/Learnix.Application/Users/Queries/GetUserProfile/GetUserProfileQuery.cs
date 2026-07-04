using FluentResults;
using MediatR;

namespace Learnix.Application.Users.Queries.GetUserProfile;

public sealed record GetUserProfileQuery(Guid UserId) : IRequest<Result<UserProfileResponse>>;
