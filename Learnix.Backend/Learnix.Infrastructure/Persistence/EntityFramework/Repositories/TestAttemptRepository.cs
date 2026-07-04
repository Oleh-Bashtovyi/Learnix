using Ardalis.Specification.EntityFrameworkCore;
using Learnix.Application.TestAttempts.Abstractions;
using Learnix.Domain.Entities;

namespace Learnix.Infrastructure.Persistence.EntityFramework.Repositories;

internal sealed class TestAttemptRepository(ApplicationDbContext context)
    : RepositoryBase<TestAttempt>(context), ITestAttemptRepository
{
}
