namespace Learnix.Domain.Enums;

public enum NotificationType
{
    NewMessage = 0,
    AchievementEarned = 1,
    EnrollmentConfirmed = 2,
    CertificateReady = 3,
    InstructorApproved = 4,
    InstructorRejected = 5,

    /// <summary>An admin granted a role directly, rather than through an application. Carries `role`.</summary>
    RoleAssigned = 6,

    /// <summary>An admin took a role away. Carries `role`.</summary>
    RoleRemoved = 7
}
