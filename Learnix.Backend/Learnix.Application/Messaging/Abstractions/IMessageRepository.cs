using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Messaging.Abstractions;

public interface IMessageRepository : IRepositoryBase<CourseMessage>
{
}
