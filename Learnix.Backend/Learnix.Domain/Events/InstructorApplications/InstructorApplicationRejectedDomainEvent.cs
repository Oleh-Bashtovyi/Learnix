using Learnix.Domain.Common;

namespace Learnix.Domain.Events.InstructorApplications;

public sealed record InstructorApplicationRejectedDomainEvent(
    Guid ApplicationId,
    Guid UserId,
    string? RejectionReason
) : DomainEvent;
