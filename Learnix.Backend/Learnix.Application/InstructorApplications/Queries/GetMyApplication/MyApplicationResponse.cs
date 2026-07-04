namespace Learnix.Application.InstructorApplications.Queries.GetMyApplication;

public record MyApplicationResponse(
    Guid Id,
    string Status,
    string MotivationText,
    string? PortfolioUrl,
    string? RejectionReason,
    DateTime SubmittedAt,
    DateTime? ReviewedAt);
