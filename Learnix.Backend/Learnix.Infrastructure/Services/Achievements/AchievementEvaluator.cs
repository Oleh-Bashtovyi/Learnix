using Learnix.Application.Achievements.Abstractions;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Domain.Constants;
using Learnix.Domain.Entities;
using Learnix.Infrastructure.Persistence.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Learnix.Infrastructure.Services.Achievements;

/// <summary>
/// Each method recomputes from authoritative aggregates rather than incrementing,
/// so re-processing the same outbox message is a safe no-op (counter idempotency).
/// Achievement insertion is guarded by <see cref="IUserAchievementRepository.HasAchievementAsync"/>.
/// </summary>
internal sealed class AchievementEvaluator(
    ApplicationDbContext db,
    IUserAchievementRepository achievementRepo,
    IUserAchievementProgressRepository progressRepo,
    IUserCompletedCategoryRepository categoryRepo,
    IUnitOfWork unitOfWork)
    : IAchievementEvaluator
{
    private static readonly (int Threshold, string Code)[] LessonThresholds =
    [
        (1, AchievementCodes.FirstLesson),
        (50, AchievementCodes.Lessons50),
        (200, AchievementCodes.Lessons200),
        (500, AchievementCodes.Lessons500),
    ];

    private static readonly (int Threshold, string Code)[] CourseThresholds =
    [
        (1, AchievementCodes.FirstCourse),
        (3, AchievementCodes.Courses3),
        (5, AchievementCodes.Courses5),
    ];

    public async Task OnLessonCompletedAsync(Guid userId, CancellationToken ct)
    {
        var lessonsCompleted = await db.LessonProgresses
            .CountAsync(lp => lp.StudentId == userId && lp.IsCompleted, ct);

        var progress = await progressRepo.GetOrCreateAsync(userId, ct);
        progress.SetLessonsCompleted(lessonsCompleted);

        await UnlockThresholdsAsync(userId, lessonsCompleted, LessonThresholds, ct);

        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task OnEnrollmentCompletedAsync(Guid userId, Guid courseId, CancellationToken ct)
    {
        var coursesCompleted = await db.Enrollments
            .CountAsync(e => e.StudentId == userId && e.Status == Domain.Enums.EnrollmentStatus.Completed, ct);

        var categoryId = await db.Courses
            .Where(c => c.Id == courseId)
            .Select(c => (Guid?)c.CategoryId)
            .FirstOrDefaultAsync(ct);

        if (categoryId.HasValue)
            await categoryRepo.AddIfMissingAsync(userId, categoryId.Value, ct);

        // Save the category insert before counting so the count reflects it.
        await unitOfWork.SaveChangesAsync(ct);

        var distinctCategories = await categoryRepo.CountDistinctCategoriesAsync(userId, ct);

        var progress = await progressRepo.GetOrCreateAsync(userId, ct);
        progress.SetCoursesCompleted(coursesCompleted);
        progress.SetDistinctCategoriesCompleted(distinctCategories);

        await UnlockThresholdsAsync(userId, coursesCompleted, CourseThresholds, ct);

        if (distinctCategories >= AchievementCodes.PolymathMinCategories
            && !await achievementRepo.HasAchievementAsync(userId, AchievementCodes.Polymath, ct))
        {
            await achievementRepo.AddAsync(UserAchievement.Unlock(userId, AchievementCodes.Polymath), ct);
        }

        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task OnTestSubmittedAsync(
        Guid userId, int questionsCount, int durationSeconds, bool passed, CancellationToken ct)
    {
        if (!passed) return;
        if (questionsCount < AchievementCodes.SpeedDemonMinQuestions) return;
        if (durationSeconds >= AchievementCodes.SpeedDemonMaxDurationSeconds) return;

        if (await achievementRepo.HasAchievementAsync(userId, AchievementCodes.SpeedDemon, ct))
            return;

        await achievementRepo.AddAsync(UserAchievement.Unlock(userId, AchievementCodes.SpeedDemon), ct);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task OnProfileChangedAsync(Guid userId, CancellationToken ct)
    {
        var user = await db.Users
            .Where(u => u.Id == userId)
            .Select(u => new { u.FirstName, u.LastName, u.Bio, u.AvatarBlobPath })
            .FirstOrDefaultAsync(ct);

        if (user is null) return;

        var profileComplete =
            !string.IsNullOrWhiteSpace(user.FirstName)
            && !string.IsNullOrWhiteSpace(user.LastName)
            && !string.IsNullOrWhiteSpace(user.Bio)
            && !string.IsNullOrWhiteSpace(user.AvatarBlobPath);

        var progress = await progressRepo.GetOrCreateAsync(userId, ct);
        progress.SetProfileCompleted(profileComplete);

        if (profileComplete
            && !await achievementRepo.HasAchievementAsync(userId, AchievementCodes.ProfileComplete, ct))
        {
            await achievementRepo.AddAsync(UserAchievement.Unlock(userId, AchievementCodes.ProfileComplete), ct);
        }

        await unitOfWork.SaveChangesAsync(ct);
    }

    private async Task UnlockThresholdsAsync(
        Guid userId,
        int currentValue,
        (int Threshold, string Code)[] thresholds,
        CancellationToken ct)
    {
        foreach (var (threshold, code) in thresholds)
        {
            if (currentValue < threshold) continue;
            if (await achievementRepo.HasAchievementAsync(userId, code, ct)) continue;

            await achievementRepo.AddAsync(UserAchievement.Unlock(userId, code), ct);
        }
    }
}
