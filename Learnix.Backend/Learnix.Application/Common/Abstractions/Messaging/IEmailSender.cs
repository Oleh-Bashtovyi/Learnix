namespace Learnix.Application.Common.Abstractions.Messaging;

public interface IEmailSender
{
    Task SendEmailConfirmationAsync(string toEmail, string firstName, string confirmationLink, CancellationToken ct = default);
    Task SendPasswordResetAsync(string toEmail, string firstName, string resetLink, CancellationToken ct = default);
    Task SendInstructorApplicationApprovedAsync(string toEmail, string firstName, CancellationToken ct = default);
    Task SendInstructorApplicationRejectedAsync(string toEmail, string firstName, string? rejectionReason, CancellationToken ct = default);
}
