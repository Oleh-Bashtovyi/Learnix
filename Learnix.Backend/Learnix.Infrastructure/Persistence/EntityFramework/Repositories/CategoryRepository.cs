using Ardalis.Specification.EntityFrameworkCore;
using Learnix.Application.Courses.Abstractions;
using Learnix.Domain.Entities;

namespace Learnix.Infrastructure.Persistence.EntityFramework.Repositories;

internal sealed class CategoryRepository(ApplicationDbContext context)
    : RepositoryBase<Category>(context), ICategoryRepository
{
}
