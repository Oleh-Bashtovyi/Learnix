using Ardalis.Specification.EntityFrameworkCore;
using Learnix.Application.LessonProgress.Abstractions;
using LessonProgressEntity = Learnix.Domain.Entities.LessonProgress;

namespace Learnix.Infrastructure.Persistence.Repositories;

internal sealed class LessonProgressRepository(ApplicationDbContext context)
    : RepositoryBase<LessonProgressEntity>(context), ILessonProgressRepository
{
}
