using Learnix.Domain.Common;

namespace Learnix.Domain.Events;

public sealed record CourseArchivedDomainEvent(Guid CourseId) : DomainEvent;
