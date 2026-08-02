using Ardalis.Specification.EntityFrameworkCore;
using Learnix.Application.Lessons.Abstractions;
using Learnix.Domain.Entities;

namespace Learnix.Infrastructure.Persistence.EntityFramework.Repositories;

internal sealed class TestVersionRepository(ApplicationDbContext context)
    : RepositoryBase<TestVersion>(context), ITestVersionRepository
{
    public void Add(TestVersion version) => context.Set<TestVersion>().Add(version);
}
