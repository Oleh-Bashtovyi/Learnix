namespace Learnix.Infrastructure.Email;

internal sealed class SmtpSettings
{
    public required string Host { get; init; }
    public required int Port { get; init; }
    public required string Username { get; init; }
    public required string Password { get; init; }
    public required string SenderEmail { get; init; }
    public required string SenderName { get; init; }
    public bool EnableSsl { get; init; } = true;
}
