using Ardalis.Specification.EntityFrameworkCore;
using Learnix.Application.InstructorApplications.Abstractions;
using Learnix.Domain.Entities;

namespace Learnix.Infrastructure.Persistence.EntityFramework.Repositories;

internal sealed class InstructorApplicationRepository(ApplicationDbContext context)
    : RepositoryBase<InstructorApplication>(context), IInstructorApplicationRepository
{
}
