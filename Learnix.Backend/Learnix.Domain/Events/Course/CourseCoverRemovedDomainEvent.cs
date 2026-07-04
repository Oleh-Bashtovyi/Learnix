using Learnix.Domain.Common;

namespace Learnix.Domain.Events.Course;

public sealed record CourseCoverRemovedDomainEvent(
    Guid CourseId,
    string CoverBlobPath) : DomainEvent;
