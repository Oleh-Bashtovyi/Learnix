using FluentResults;
using MediatR;

namespace Learnix.Application.InstructorApplications.Commands.RejectApplication;

public record RejectApplicationCommand(Guid ApplicationId, string? RejectionReason) : IRequest<Result>;
