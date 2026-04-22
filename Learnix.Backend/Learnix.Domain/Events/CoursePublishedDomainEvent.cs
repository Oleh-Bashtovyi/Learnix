using Learnix.Domain.Common;

namespace Learnix.Domain.Events;

public sealed record CoursePublishedDomainEvent(Guid CourseId) : DomainEvent;
