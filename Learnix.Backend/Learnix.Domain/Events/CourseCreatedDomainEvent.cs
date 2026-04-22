using Learnix.Domain.Common;

namespace Learnix.Domain.Events;

public sealed record CourseCreatedDomainEvent(
    Guid CourseId,
    Guid InstructorId,
    Guid CategoryId) : DomainEvent;
