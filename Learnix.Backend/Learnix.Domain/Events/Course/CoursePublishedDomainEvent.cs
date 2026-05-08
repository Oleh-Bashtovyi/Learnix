using Learnix.Domain.Common;

namespace Learnix.Domain.Events.Course;

public sealed record CoursePublishedDomainEvent(Guid CourseId, Guid CategoryId) : DomainEvent;
