using Learnix.Domain.Common;

namespace Learnix.Domain.Events;

public sealed record CourseDeletedDomainEvent(Guid CourseId) : DomainEvent;
