using FluentResults;
using Learnix.Application.Certificates.Abstractions;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.InstructorAnalytics.Services;
using Learnix.Application.InstructorAnalytics.Specifications;
using Learnix.Application.Payments.Abstractions;
using Learnix.Application.Payments.Specifications;

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
        var payments = await paymentRepository.ListAsync(new InstructorPaymentsSpecification(instructorId), cancellationToken);
        var certificates = await certificateRepository.CountAsync(new InstructorCertificatesSpecification(instructorId), cancellationToken);
        var totalStudents = await enrollmentRepository.CountDistinctStudentsForInstructorAsync(instructorId, cancellationToken);

        var totalRevenue = payments.Sum(p => p.Amount);

        return Result.Ok(new InstructorAnalyticsSummaryDto(
            totalStudents,
            totalRevenue,
            InstructorAnalyticsCalculations.WeightedAverageRating(courses),
            certificates));
    }
}
