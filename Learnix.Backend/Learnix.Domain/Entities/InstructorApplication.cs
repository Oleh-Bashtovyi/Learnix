using Learnix.Domain.Common;
using Learnix.Domain.Enums;
using Learnix.Domain.Events.InstructorApplications;

namespace Learnix.Domain.Entities;

public class InstructorApplication : BaseEntity
{
    private InstructorApplication() { }

    public Guid UserId { get; private set; }
    public string MotivationText { get; private set; } = null!;
    public string? PortfolioUrl { get; private set; }
    public ApplicationStatus Status { get; private set; }
    public string? RejectionReason { get; private set; }
    public Guid? ReviewedByAdminId { get; private set; }
    public DateTime? ReviewedAt { get; private set; }

    public User User { get; private set; } = null!;

    public static InstructorApplication Create(Guid userId, string motivationText, string? portfolioUrl)
        => new()
        {
            UserId = userId,
            MotivationText = motivationText,
            PortfolioUrl = portfolioUrl,
            Status = ApplicationStatus.Pending,
        };

    public void Resubmit(string motivationText, string? portfolioUrl)
    {
        MotivationText = motivationText;
        PortfolioUrl = portfolioUrl;
        Status = ApplicationStatus.Pending;
        RejectionReason = null;
        ReviewedByAdminId = null;
        ReviewedAt = null;
    }

    public void Approve(Guid adminId)
    {
        Status = ApplicationStatus.Approved;
        ReviewedByAdminId = adminId;
        ReviewedAt = DateTime.UtcNow;
        RaiseDomainEvent(new InstructorApplicationApprovedDomainEvent(Id, UserId));
    }

    public void Reject(Guid adminId, string? reason)
    {
        Status = ApplicationStatus.Rejected;
        ReviewedByAdminId = adminId;
        RejectionReason = reason;
        ReviewedAt = DateTime.UtcNow;
        RaiseDomainEvent(new InstructorApplicationRejectedDomainEvent(Id, UserId, reason));
    }
}
