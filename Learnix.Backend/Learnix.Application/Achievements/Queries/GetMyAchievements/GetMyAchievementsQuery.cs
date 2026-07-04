using FluentResults;
using MediatR;

namespace Learnix.Application.Achievements.Queries.GetMyAchievements;

public sealed record GetMyAchievementsQuery : IRequest<Result<GetMyAchievementsResponse>>;
