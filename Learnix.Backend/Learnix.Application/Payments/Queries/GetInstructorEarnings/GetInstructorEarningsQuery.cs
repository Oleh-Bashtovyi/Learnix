using FluentResults;
using Learnix.Application.Payments.Models;
using MediatR;

namespace Learnix.Application.Payments.Queries.GetInstructorEarnings;

public sealed record GetInstructorEarningsQuery(Guid InstructorId) : IRequest<Result<InstructorEarningsResponse>>;
