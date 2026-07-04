using Ardalis.Specification;
using LessonProgressEntity = Learnix.Domain.Entities.LessonProgress;

namespace Learnix.Application.LessonProgress.Abstractions;

public interface ILessonProgressRepository : IRepositoryBase<LessonProgressEntity>
{
}
