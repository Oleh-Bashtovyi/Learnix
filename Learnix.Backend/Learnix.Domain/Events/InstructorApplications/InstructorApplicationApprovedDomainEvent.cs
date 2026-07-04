using Learnix.Domain.Common;

namespace Learnix.Domain.Events.InstructorApplications;

public sealed record InstructorApplicationApprovedDomainEvent(
    Guid ApplicationId,
    Guid UserId
) : DomainEvent;
