using Ardalis.Specification.EntityFrameworkCore;
using Learnix.Application.Lessons.Abstractions;
using Learnix.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Learnix.Infrastructure.Persistence.Repositories;

internal sealed class LessonRepository(ApplicationDbContext context)
    : RepositoryBase<Lesson>(context), ILessonRepository
{
    public Task<T?> GetLessonOfTypeByIdAsync<T>(Guid id, bool forUpdate = false, CancellationToken ct = default)
            where T : Lesson
    {
        var query = context.Set<Lesson>().OfType<T>();

        if (!forUpdate)
            query = query.AsNoTracking();

        return query.FirstOrDefaultAsync(l => l.Id == id, ct);
    }

    public Task<bool> IsLessonInCourseAsync(Guid courseId, Guid lessonId, CancellationToken ct = default)
    {
        return (
            from lesson in context.Set<Lesson>()
            join section in context.Set<Section>() on lesson.SectionId equals section.Id
            where lesson.Id == lessonId && section.CourseId == courseId && !lesson.IsHidden
            select lesson
        ).AnyAsync(ct);
    }

    public Task<TestLesson?> GetTestLessonInCourseAsync(Guid courseId, Guid lessonId, CancellationToken ct = default)
    {
        return (
            from lesson in context.Set<TestLesson>()
            join section in context.Set<Section>() on lesson.SectionId equals section.Id
            where lesson.Id == lessonId && section.CourseId == courseId && !lesson.IsHidden
            select lesson
        ).FirstOrDefaultAsync(ct);
    }

    public Task<int> GetVisibleLessonCountAsync(Guid courseId, CancellationToken ct = default)
    {
        return (
            from lesson in context.Set<Lesson>()
            join section in context.Set<Section>() on lesson.SectionId equals section.Id
            where section.CourseId == courseId && !lesson.IsHidden
            select lesson
        ).CountAsync(ct);
    }

    public Task<Lesson?> GetVisibleLessonInCourseAsync(Guid courseId, Guid lessonId, CancellationToken ct = default)
    {
        return (
            from lesson in context.Set<Lesson>()
            join section in context.Set<Section>() on lesson.SectionId equals section.Id
            where lesson.Id == lessonId && section.CourseId == courseId && !lesson.IsHidden
            select lesson
        ).AsNoTracking().FirstOrDefaultAsync(ct);
    }

    public Task<int> GetMaxDisplayOrderAsync(Guid sectionId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task ShiftLessonsOrderAsync(Guid sectionId, Guid lessonId, int newOrder, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}
