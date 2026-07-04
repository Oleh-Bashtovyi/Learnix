using Learnix.Domain.Common;

namespace Learnix.Domain.Events.Course;

public sealed record CourseUnpublishedDomainEvent(Guid CourseId, Guid CategoryId) : DomainEvent;
