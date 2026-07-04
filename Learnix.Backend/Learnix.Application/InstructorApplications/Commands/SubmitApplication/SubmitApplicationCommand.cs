using FluentResults;
using MediatR;

namespace Learnix.Application.InstructorApplications.Commands.SubmitApplication;

public record SubmitApplicationCommand(string MotivationText, string? PortfolioUrl) : IRequest<Result<Guid>>;
