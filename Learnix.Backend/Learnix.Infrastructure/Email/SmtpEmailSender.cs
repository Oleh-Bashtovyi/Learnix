using System.Globalization;
using Learnix.Application.Common.Abstractions.Messaging;
using Learnix.Infrastructure.Email.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Learnix.Infrastructure.Email;

internal sealed class SmtpEmailSender(
    IOptions<SmtpSettings> options,
    EmailRenderer renderer,
    IStringLocalizerFactory localizerFactory,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly SmtpSettings _settings = options.Value;
    private readonly IStringLocalizer _localizer = localizerFactory.Create(typeof(EmailStrings));

    public async Task SendEmailConfirmationAsync(string toEmail, string firstName, string confirmationCode, string language = "en", CancellationToken ct = default)
    {
        CultureInfo.CurrentUICulture = new CultureInfo(language);
        var subject = $"{_localizer["EmailConfirmation_Subject"]} — Learnix";
        var html = await renderer.RenderAsync("EmailConfirmation.cshtml", new EmailConfirmationModel
        {
            FirstName = firstName,
            ConfirmationCode = confirmationCode,
            Strings = _localizer
        });
        await SendAsync(toEmail, subject, html, ct);
    }

    public async Task SendPasswordResetAsync(string toEmail, string firstName, string resetLink, string language = "en", CancellationToken ct = default)
    {
        CultureInfo.CurrentUICulture = new CultureInfo(language);
        var subject = $"{_localizer["PasswordReset_Subject"]} — Learnix";
        var html = await renderer.RenderAsync("PasswordReset.cshtml", new PasswordResetModel
        {
            FirstName = firstName,
            ResetLink = resetLink,
            Strings = _localizer
        });
        await SendAsync(toEmail, subject, html, ct);
    }

    public async Task SendInstructorApplicationApprovedAsync(string toEmail, string firstName, string language = "en", CancellationToken ct = default)
    {
        CultureInfo.CurrentUICulture = new CultureInfo(language);
        var subject = $"{_localizer["InstructorApproved_Subject"]} — Learnix";
        var html = await renderer.RenderAsync("InstructorApproved.cshtml", new InstructorApprovedModel
        {
            FirstName = firstName,
            Strings = _localizer
        });
        await SendAsync(toEmail, subject, html, ct);
    }

    public async Task SendInstructorApplicationRejectedAsync(string toEmail, string firstName, string? rejectionReason, string language = "en", CancellationToken ct = default)
    {
        CultureInfo.CurrentUICulture = new CultureInfo(language);
        var subject = $"{_localizer["InstructorRejected_Subject"]} — Learnix";
        var html = await renderer.RenderAsync("InstructorRejected.cshtml", new InstructorRejectedModel
        {
            FirstName = firstName,
            RejectionReason = rejectionReason,
            Strings = _localizer
        });
        await SendAsync(toEmail, subject, html, ct);
    }

    public async Task SendUserBannedAsync(string toEmail, string firstName, string language = "en", CancellationToken ct = default)
    {
        CultureInfo.CurrentUICulture = new CultureInfo(language);
        var subject = $"{_localizer["UserBanned_Subject"]} — Learnix";
        var html = await renderer.RenderAsync("UserBanned.cshtml", new UserBannedModel
        {
            FirstName = firstName,
            Strings = _localizer
        });
        await SendAsync(toEmail, subject, html, ct);
    }

    public async Task SendUserUnbannedAsync(string toEmail, string firstName, string language = "en", CancellationToken ct = default)
    {
        CultureInfo.CurrentUICulture = new CultureInfo(language);
        var subject = $"{_localizer["UserUnbanned_Subject"]} — Learnix";
        var html = await renderer.RenderAsync("UserUnbanned.cshtml", new UserUnbannedModel
        {
            FirstName = firstName,
            Strings = _localizer
        });
        await SendAsync(toEmail, subject, html, ct);
    }

    public async Task SendUserRoleChangedAsync(string toEmail, string firstName, string role, bool assigned, string language = "en", CancellationToken ct = default)
    {
        CultureInfo.CurrentUICulture = new CultureInfo(language);
        var subject = $"{_localizer["UserRoleChanged_Subject"]} — Learnix";
        var html = await renderer.RenderAsync("UserRoleChanged.cshtml", new UserRoleChangedModel
        {
            FirstName = firstName,
            Role = role,
            Assigned = assigned,
            Strings = _localizer
        });
        await SendAsync(toEmail, subject, html, ct);
    }

    public async Task SendCourseAdminUnpublishedAsync(string toEmail, string instructorFirstName, string courseTitle, string language = "en", CancellationToken ct = default)
    {
        CultureInfo.CurrentUICulture = new CultureInfo(language);
        var subject = $"{_localizer["CourseAdminUnpublished_Subject"]} — Learnix";
        var html = await renderer.RenderAsync("CourseAdminUnpublished.cshtml", new CourseAdminActionModel
        {
            InstructorFirstName = instructorFirstName,
            CourseTitle = courseTitle,
            Strings = _localizer
        });
        await SendAsync(toEmail, subject, html, ct);
    }

    public async Task SendCourseAdminDeletedAsync(string toEmail, string instructorFirstName, string courseTitle, string language = "en", CancellationToken ct = default)
    {
        CultureInfo.CurrentUICulture = new CultureInfo(language);
        var subject = $"{_localizer["CourseAdminDeleted_Subject"]} — Learnix";
        var html = await renderer.RenderAsync("CourseAdminDeleted.cshtml", new CourseAdminActionModel
        {
            InstructorFirstName = instructorFirstName,
            CourseTitle = courseTitle,
            Strings = _localizer
        });
        await SendAsync(toEmail, subject, html, ct);
    }

    private async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient();
        try
        {
            var socketOptions = _settings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
            await client.ConnectAsync(_settings.Host, _settings.Port, socketOptions, ct);

            if (!string.IsNullOrEmpty(_settings.Username))
                await client.AuthenticateAsync(_settings.Username, _settings.Password, ct);

            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);

            logger.LogInformation("Email [{Subject}] sent to {Email}", subject, MaskEmail(toEmail));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email [{Subject}] to {Email}", subject, MaskEmail(toEmail));
            throw;
        }
    }

    private static string MaskEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@')) return "***";
        var parts = email.Split('@');
        return $"{parts[0][0]}***@{parts[1]}";
    }
}
