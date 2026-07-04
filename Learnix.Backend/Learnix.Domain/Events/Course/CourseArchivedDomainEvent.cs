using Learnix.Domain.Common;

namespace Learnix.Domain.Events.Course;

public sealed record CourseArchivedDomainEvent(Guid CourseId, Guid CategoryId, bool WasPublished) : DomainEvent;
