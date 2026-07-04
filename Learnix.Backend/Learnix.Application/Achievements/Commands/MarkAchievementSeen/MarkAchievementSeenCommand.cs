using FluentResults;
using MediatR;

namespace Learnix.Application.Achievements.Commands.MarkAchievementSeen;

public sealed record MarkAchievementSeenCommand(Guid AchievementId) : IRequest<Result>;
