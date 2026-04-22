using Ardalis.Specification.EntityFrameworkCore;
using Learnix.Application.Lessons.Abstractions;
using Learnix.Domain.Entities;
using Microsoft.EntityFrameworkCore;

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

    public Task<int> GetMaxDisplayOrderAsync(Guid sectionId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task ShiftLessonsOrderAsync(Guid sectionId, Guid lessonId, int newOrder, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}
