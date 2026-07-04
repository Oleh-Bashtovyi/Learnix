using Learnix.Domain.Common;

namespace Learnix.Domain.Events.Enrollments;

public sealed record EnrollmentCompletedDomainEvent(
    Guid StudentId,
    Guid CourseId
) : DomainEvent;
