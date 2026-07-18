using Ardalis.Specification.EntityFrameworkCore;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Domain.Entities;
using Learnix.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Learnix.Infrastructure.Persistence.EntityFramework.Repositories;

internal sealed class EnrollmentRepository(ApplicationDbContext context)
    : RepositoryBase<Enrollment>(context), IEnrollmentRepository
{
    public async Task<int> CountDistinctStudentsForInstructorAsync(
        Guid instructorId, CancellationToken cancellationToken = default)
    {
        // The join onto Course applies its soft-delete query filter, so enrollments in deleted courses
        // drop out — matching how the analytics course list is loaded.
        return await context.Enrollments
            .Where(e => e.Course!.InstructorId == instructorId)
            .Select(e => e.StudentId)
            .Distinct()
            .CountAsync(cancellationToken);
    }

    public async Task<(int Total, int Completed)> GetEnrollmentFunnelCountsAsync(
        Guid instructorId, CancellationToken cancellationToken = default)
    {
        var counts = await context.Enrollments
            .Where(e => e.Course!.InstructorId == instructorId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Completed = g.Count(e => e.Status == EnrollmentStatus.Completed),
            })
            .FirstOrDefaultAsync(cancellationToken);

        return counts is null ? (0, 0) : (counts.Total, counts.Completed);
    }
}
