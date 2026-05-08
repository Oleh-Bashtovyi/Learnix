using Learnix.Domain.Common;

namespace Learnix.Domain.Events.Course;

public sealed record CourseAdminDeletedDomainEvent(Guid CourseId, Guid InstructorId, Guid CategoryId, bool WasPublished) : DomainEvent;
