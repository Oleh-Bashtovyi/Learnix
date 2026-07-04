using Ardalis.Specification.EntityFrameworkCore;
using Learnix.Application.Messaging.Abstractions;
using Learnix.Domain.Entities;

namespace Learnix.Infrastructure.Persistence.EntityFramework.Repositories;

internal sealed class MessageRepository(ApplicationDbContext context)
    : RepositoryBase<CourseMessage>(context), IMessageRepository
{
}
