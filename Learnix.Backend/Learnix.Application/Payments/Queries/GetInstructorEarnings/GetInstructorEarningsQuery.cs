using FluentResults;
using MediatR;

namespace Learnix.Application.Payments.Queries.GetInstructorEarnings;

public sealed record GetInstructorEarningsQuery : IRequest<Result<InstructorEarningsResponse>>;
