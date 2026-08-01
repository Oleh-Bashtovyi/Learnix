using FluentResults;
using Learnix.Application.Certificates.Abstractions;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.InstructorAnalytics.Services;
using Learnix.Application.InstructorAnalytics.Specifications;
using Learnix.Application.Payments.Abstractions;

namespace Learnix.Application.InstructorAnalytics.Queries.GetInstructorAnalyticsSummary;

public sealed class GetInstructorAnalyticsSummaryQueryHandler(
    ICurrentUserService currentUser,
    ICourseRepository courseRepository,
    IEnrollmentRepository enrollmentRepository,
    IPaymentRepository paymentRepository,
    ICertificateRepository certificateRepository)
    : InstructorAnalyticsQueryHandler<GetInstructorAnalyticsSummaryQuery, InstructorAnalyticsSummaryDto>(currentUser)
{
    protected override async Task<Result<InstructorAnalyticsSummaryDto>> HandleAsync(
        GetInstructorAnalyticsSummaryQuery request, Guid instructorId, CancellationToken cancellationToken)
    {
        var courses = await courseRepository.ListAsync(new InstructorCoursesForAnalyticsSpecification(instructorId), cancellationToken);
        var totalRevenue = await paymentRepository.GetTotalEarningsAsync(instructorId, cancellationToken);
        var certificates = await certificateRepository.CountAsync(new InstructorCertificatesSpecification(instructorId), cancellationToken);
        var totalStudents = await enrollmentRepository.CountDistinctStudentsForInstructorAsync(instructorId, cancellationToken);

        var trends = await InstructorSummaryTrends.LoadAsync(
            instructorId, DateTime.UtcNow, enrollmentRepository, paymentRepository, certificateRepository, cancellationToken);

        return Result.Ok(new InstructorAnalyticsSummaryDto(
            totalStudents,
            totalRevenue,
            InstructorAnalyticsCalculations.WeightedAverageRating(courses),
            certificates,
            trends.NewStudents,
            trends.Revenue,
            trends.Certificates));
    }
}
