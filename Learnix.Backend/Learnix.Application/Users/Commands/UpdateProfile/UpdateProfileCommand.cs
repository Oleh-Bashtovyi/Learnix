using FluentResults;
using MediatR;

namespace Learnix.Application.Users.Commands.UpdateProfile;

public sealed record UpdateProfileCommand(
    string FirstName,
    string LastName,
    string? Bio,
    string? AvatarBlobPath) : IRequest<Result>;
