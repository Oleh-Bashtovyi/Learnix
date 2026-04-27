namespace Learnix.Application.InstructorApplications.Queries.GetPendingApplications;

public record PendingApplicationResponse(
    Guid Id,
    Guid UserId,
    string FirstName,
    string LastName,
    string Email,
    string MotivationText,
    string? PortfolioUrl,
    DateTime SubmittedAt);
