using FluentResults;
using Learnix.Application.Achievements.Abstractions;
using Learnix.Application.Achievements.Constants;
using Learnix.Application.Achievements.Specifications;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using MediatR;

namespace Learnix.Application.Achievements.Commands.MarkAchievementSeen;

internal sealed class MarkAchievementSeenCommandHandler(
    ICurrentUserService currentUser,
    IUserAchievementRepository achievementRepo,
    IUnitOfWork unitOfWork)
    : IRequestHandler<MarkAchievementSeenCommand, Result>
{
    public async Task<Result> Handle(MarkAchievementSeenCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        var achievement = await achievementRepo.FirstOrDefaultAsync(
            new UserAchievementByIdSpecification(currentUser.UserId.Value, request.AchievementId),
            cancellationToken);

        if (achievement is null)
            return Result.Fail(new NotFoundError(AchievementMessages.AchievementNotFound));

        achievement.MarkSeen();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
