using Learnix.Domain.Common;
using Learnix.Domain.Events.Achievements;

namespace Learnix.Domain.Entities;

public class UserAchievement : BaseEntity
{
    private UserAchievement() { }

    private UserAchievement(Guid userId, string code)
    {
        UserId = userId;
        Code = code;
        UnlockedAt = DateTime.UtcNow;
        Seen = false;
    }

    public Guid UserId { get; private set; }
    public string Code { get; private set; } = null!;
    public DateTime UnlockedAt { get; private set; }
    public bool Seen { get; private set; }

    public static UserAchievement Unlock(Guid userId, string code)
    {
        var achievement = new UserAchievement(userId, code);
        achievement.RaiseDomainEvent(new AchievementUnlockedDomainEvent(
            achievement.Id, userId, code, achievement.UnlockedAt));
        return achievement;
    }

    public void MarkSeen()
    {
        if (Seen) return;
        Seen = true;
    }
}
