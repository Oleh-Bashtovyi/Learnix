using FluentResults;
using Learnix.Application.Certificates.Abstractions;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.InstructorAnalytics.Constants;
using Learnix.Application.InstructorAnalytics.Specifications;
using Learnix.Application.LessonProgress.Abstractions;

namespace Learnix.Application.InstructorAnalytics.Queries.GetInstructorEngagement;

public sealed class GetInstructorEngagementQueryHandler(
    ICurrentUserService currentUser,
    ICourseRepository courseRepository,
    IEnrollmentRepository enrollmentRepository,
    ILessonProgressRepository lessonProgressRepository,
    ICertificateRepository certificateRepository)
    : InstructorAnalyticsQueryHandler<GetInstructorEngagementQuery, InstructorEngagementDto>(currentUser)
{
    protected override async Task<Result<InstructorEngagementDto>> HandleAsync(
        GetInstructorEngagementQuery request, Guid instructorId, CancellationToken cancellationToken)
    {
        var courses = await courseRepository.ListAsync(
            new InstructorCoursesForAnalyticsSpecification(instructorId), cancellationToken);
        var courseIds = courses.Select(c => c.Id).ToList();

        var (enrolled, completed) = await enrollmentRepository.GetEnrollmentFunnelCountsAsync(
            instructorId, cancellationToken);

        var started = await lessonProgressRepository.CountStartedEnrollmentsAsync(
            courseIds, cancellationToken);

        var certified = await certificateRepository.CountAsync(
            new InstructorCertificatesSpecification(instructorId), cancellationToken);

        var since = DateTime.UtcNow.AddDays(-InstructorAnalyticsConstants.ActiveStudentWindowDays);
        var activeStudents = await lessonProgressRepository.CountActiveStudentsSinceAsync(
            courseIds, since, cancellationToken);

        return Result.Ok(new InstructorEngagementDto(
            enrolled,
            started,
            completed,
            certified,
            activeStudents));
    }
}
