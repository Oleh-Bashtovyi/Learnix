using FluentResults;
using Learnix.Application.Certificates.Abstractions;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.InstructorAnalytics.Queries.GetInstructorAnalyticsSummary;
using Learnix.Application.InstructorAnalytics.Services;
using Learnix.Application.InstructorAnalytics.Specifications;
using Learnix.Application.Payments.Abstractions;
using Learnix.Application.Reviews.Abstractions;

namespace Learnix.Application.InstructorAnalytics.Queries.GetInstructorOverview;

public sealed class GetInstructorOverviewQueryHandler(
    ICurrentUserService currentUser,
    ICourseRepository courseRepository,
    IEnrollmentRepository enrollmentRepository,
    IPaymentRepository paymentRepository,
    ICertificateRepository certificateRepository,
    ICourseReviewRepository reviewRepository)
    : InstructorAnalyticsQueryHandler<GetInstructorOverviewQuery, InstructorOverviewDto>(currentUser)
{
    protected override async Task<Result<InstructorOverviewDto>> HandleAsync(
        GetInstructorOverviewQuery request, Guid instructorId, CancellationToken cancellationToken)
    {
        // The course list is loaded once here and feeds the summary, statuses and popularity charts —
        // instead of each of those endpoints re-running the same query.
        var courses = await courseRepository.ListAsync(
            new InstructorCoursesForAnalyticsSpecification(instructorId), cancellationToken);
        var totalRevenue = await paymentRepository.GetTotalEarningsAsync(instructorId, cancellationToken);
        var certificates = await certificateRepository.CountAsync(
            new InstructorCertificatesSpecification(instructorId), cancellationToken);
        var totalStudents = await enrollmentRepository.CountDistinctStudentsForInstructorAsync(
            instructorId, cancellationToken);

        var courseIds = courses.Select(c => c.Id).ToList();
        var ratingCounts = await reviewRepository.GetRatingDistributionAsync(courseIds, cancellationToken);

        var summary = new InstructorAnalyticsSummaryDto(
            totalStudents,
            totalRevenue,
            InstructorAnalyticsCalculations.WeightedAverageRating(courses),
            certificates);

        return Result.Ok(new InstructorOverviewDto(
            summary,
            InstructorAnalyticsCalculations.Statuses(courses),
            InstructorAnalyticsCalculations.Popularity(courses),
            InstructorAnalyticsCalculations.Distribution(ratingCounts)));
    }
}
