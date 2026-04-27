using Ardalis.Specification.EntityFrameworkCore;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Domain.Entities;

namespace Learnix.Infrastructure.Persistence.Repositories;

internal sealed class EnrollmentRepository(ApplicationDbContext context)
    : RepositoryBase<Enrollment>(context), IEnrollmentRepository
{
}
