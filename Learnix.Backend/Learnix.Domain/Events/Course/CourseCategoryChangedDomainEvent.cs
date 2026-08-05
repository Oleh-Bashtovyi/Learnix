using Learnix.Domain.Common;

namespace Learnix.Domain.Events.Course;

public sealed record CourseCategoryChangedDomainEvent(Guid CourseId, Guid OldCategoryId, Guid NewCategoryId)
    : DomainEvent;
