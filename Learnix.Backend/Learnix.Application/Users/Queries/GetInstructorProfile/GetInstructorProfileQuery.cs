using FluentResults;
using MediatR;

namespace Learnix.Application.Users.Queries.GetInstructorProfile;

public sealed record GetInstructorProfileQuery(Guid InstructorId)
    : IRequest<Result<InstructorProfileResponse>>;
