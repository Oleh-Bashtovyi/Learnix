using Ardalis.Specification.EntityFrameworkCore;
using Learnix.Application.Courses.Abstractions;
using Learnix.Domain.Entities;

namespace Learnix.Infrastructure.Persistence.Repositories;

internal sealed class CourseRepository(ApplicationDbContext context)
    : RepositoryBase<Course>(context), ICourseRepository
{
}
