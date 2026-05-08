using Learnix.Domain.Common;

namespace Learnix.Domain.Events.Course;

public sealed record CourseAdminUnpublishedDomainEvent(Guid CourseId, Guid InstructorId, Guid CategoryId) : DomainEvent;
