using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Enrollments.Abstractions;

public interface IEnrollmentRepository : IRepositoryBase<Enrollment>
{
    /// <summary>
    /// The number of distinct students enrolled across all of the instructor's (non-deleted) courses.
    /// Unlike summing each course's <c>EnrollmentsCount</c>, a student enrolled in several of the
    /// instructor's courses is counted once.
    /// </summary>
    Task<int> CountDistinctStudentsForInstructorAsync(Guid instructorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Total and completed enrollment counts across the instructor's (non-deleted) courses — the first
    /// and third stages of the engagement funnel.
    /// </summary>
    Task<(int Total, int Completed)> GetEnrollmentFunnelCountsAsync(
        Guid instructorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enrollment count per calendar day (UTC) across the instructor's courses, within
    /// <paramref name="startUtc"/>–<paramref name="endUtc"/> inclusive.
    /// </summary>
    Task<IReadOnlyDictionary<DateTime, int>> GetDailyEnrollmentCountsAsync(
        Guid instructorId, DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Students whose <em>first</em> enrollment with this instructor falls within
    /// <paramref name="startUtc"/>–<paramref name="endUtc"/> inclusive — a headcount of people who
    /// were not this instructor's students before the window, which is what makes it comparable to
    /// <see cref="CountDistinctStudentsForInstructorAsync"/>. Counting enrollments instead would
    /// count a returning student again for every further course they take.
    /// </summary>
    Task<int> CountNewStudentsAsync(
        Guid instructorId, DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default);
}
