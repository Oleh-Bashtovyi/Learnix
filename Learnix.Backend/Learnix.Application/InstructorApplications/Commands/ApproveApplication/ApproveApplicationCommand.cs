using FluentResults;
using MediatR;

namespace Learnix.Application.InstructorApplications.Commands.ApproveApplication;

public record ApproveApplicationCommand(Guid ApplicationId) : IRequest<Result>;
