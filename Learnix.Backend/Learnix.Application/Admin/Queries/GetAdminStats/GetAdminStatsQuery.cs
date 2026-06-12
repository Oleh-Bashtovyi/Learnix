using FluentResults;
using MediatR;

namespace Learnix.Application.Admin.Queries.GetAdminStats;

public sealed record GetAdminStatsQuery : IRequest<Result<AdminStatsResponse>>;
