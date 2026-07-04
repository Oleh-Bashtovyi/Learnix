using Ardalis.Specification.EntityFrameworkCore;
using Learnix.Application.Users.Abstractions;
using Learnix.Domain.Entities;

namespace Learnix.Infrastructure.Persistence.EntityFramework.Repositories;

internal sealed class UserRepository(ApplicationDbContext context)
    : RepositoryBase<User>(context), IUserRepository
{
}
