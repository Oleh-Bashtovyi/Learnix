using FluentResults;
using Learnix.Application.Payments.Models;
using MediatR;

namespace Learnix.Application.Payments.Queries.GetMyEarnings;

public sealed record GetMyEarningsQuery : IRequest<Result<InstructorEarningsResponse>>;
