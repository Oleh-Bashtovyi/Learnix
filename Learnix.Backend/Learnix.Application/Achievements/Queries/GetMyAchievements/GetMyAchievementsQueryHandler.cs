using FluentResults;
using Learnix.Application.Achievements.Abstractions;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using MediatR;

namespace Learnix.Application.Achievements.Queries.GetMyAchievements;

internal sealed class GetMyAchievementsQueryHandler(
    ICurrentUserService currentUser,
    IUserAchievementRepository achievementRepo,
    IUserAchievementProgressRepository progressRepo)
    : IRequestHandler<GetMyAchievementsQuery, Result<GetMyAchievementsResponse>>
{
    public async Task<Result<GetMyAchievementsResponse>> Handle(
        GetMyAchievementsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        var userId = currentUser.UserId.Value;

        var unlocked = (await achievementRepo.ListAsync(
            new Specifications.UserAchievementsByUserSpecification(userId), cancellationToken))
            .Select(ua => new UnlockedAchievementDto(ua.Id, ua.Code, ua.UnlockedAt, ua.Seen))
            .ToList();

        var progress = await progressRepo.GetAsync(userId, cancellationToken);

        var progressDto = progress is null
            ? new AchievementProgressDto(0, 0, 0, false)
            : new AchievementProgressDto(
                progress.LessonsCompleted,
                progress.CoursesCompleted,
                progress.DistinctCategoriesCompleted,
                progress.ProfileCompleted);

        return Result.Ok(new GetMyAchievementsResponse(unlocked, progressDto));
    }
}
