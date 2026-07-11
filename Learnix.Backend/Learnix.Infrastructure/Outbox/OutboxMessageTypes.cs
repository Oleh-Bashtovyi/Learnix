namespace Learnix.Infrastructure.Outbox;

public static class OutboxMessageTypes
{
    public const string DeleteBlob = "DeleteBlob";
    public const string InstructorApprovedEmail = "InstructorApprovedEmail";
    public const string InstructorRejectedEmail = "InstructorRejectedEmail";
    public const string UserBannedEmail = "UserBannedEmail";
    public const string UserUnbannedEmail = "UserUnbannedEmail";
    public const string UserRoleChangedEmail = "UserRoleChangedEmail";
    public const string AccountDeletedEmail = "AccountDeletedEmail";
    public const string AccountRecoveredEmail = "AccountRecoveredEmail";
    public const string CourseAdminUnpublishedEmail = "CourseAdminUnpublishedEmail";
    public const string CourseAdminDeletedEmail = "CourseAdminDeletedEmail";
    public const string PasswordResetEmail = "PasswordResetEmail";
    public const string EmailConfirmation = "EmailConfirmation";

    public const string EvaluateLessonCompleted = "EvaluateLessonCompleted";
    public const string EvaluateEnrollmentCompleted = "EvaluateEnrollmentCompleted";
    public const string EvaluateTestSubmitted = "EvaluateTestSubmitted";
    public const string EvaluateProfileChanged = "EvaluateProfileChanged";

    public const string NotifyAchievementUnlocked = "NotifyAchievementUnlocked";
    public const string NotifyInstructorApproved = "NotifyInstructorApproved";
    public const string NotifyInstructorRejected = "NotifyInstructorRejected";
    public const string NotifyCertificateIssued = "NotifyCertificateIssued";
}

public record DeleteBlobPayload(string BlobPath);
