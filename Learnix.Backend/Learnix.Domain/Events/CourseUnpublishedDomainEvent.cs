using Learnix.Domain.Common;

namespace Learnix.Domain.Events;

public sealed record CourseUnpublishedDomainEvent(Guid CourseId) : DomainEvent;
