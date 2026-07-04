using FluentResults;
using MediatR;

namespace Learnix.Application.Users.Queries.GetMyProfile;

public sealed record GetMyProfileQuery : IRequest<Result<MyProfileResponse>>;
