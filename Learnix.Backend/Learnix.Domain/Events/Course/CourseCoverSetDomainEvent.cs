using Learnix.Domain.Common;

namespace Learnix.Domain.Events.Course;

public sealed record CourseCoverSetDomainEvent(
    Guid CourseId,
    string CoverBlobPath) : DomainEvent;
