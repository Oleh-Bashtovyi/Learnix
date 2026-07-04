using Learnix.Domain.Common;

namespace Learnix.Domain.Events.Course;

public sealed record CourseUnarchivedDomainEvent(Guid CourseId, Guid CategoryId) : DomainEvent;
