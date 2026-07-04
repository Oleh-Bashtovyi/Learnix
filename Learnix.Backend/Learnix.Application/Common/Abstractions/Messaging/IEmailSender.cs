namespace Learnix.Application.Common.Abstractions.Messaging;

public interface IEmailSender
{
    Task SendEmailConfirmationAsync(string toEmail, string firstName, string confirmationCode, string language = "en", CancellationToken ct = default);
    Task SendPasswordResetAsync(string toEmail, string firstName, string resetLink, string language = "en", CancellationToken ct = default);
    Task SendInstructorApplicationApprovedAsync(string toEmail, string firstName, string language = "en", CancellationToken ct = default);
    Task SendInstructorApplicationRejectedAsync(string toEmail, string firstName, string? rejectionReason, string language = "en", CancellationToken ct = default);
    Task SendUserBannedAsync(string toEmail, string firstName, string language = "en", CancellationToken ct = default);
    Task SendUserUnbannedAsync(string toEmail, string firstName, string language = "en", CancellationToken ct = default);
    Task SendUserRoleChangedAsync(string toEmail, string firstName, string role, bool assigned, string language = "en", CancellationToken ct = default);
    Task SendCourseAdminUnpublishedAsync(string toEmail, string instructorFirstName, string courseTitle, string language = "en", CancellationToken ct = default);
    Task SendCourseAdminDeletedAsync(string toEmail, string instructorFirstName, string courseTitle, string language = "en", CancellationToken ct = default);
}
