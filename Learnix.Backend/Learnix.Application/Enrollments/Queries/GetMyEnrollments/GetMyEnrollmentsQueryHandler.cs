using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Common.Pagination;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.Enrollments.Specifications;
using Learnix.Application.LessonProgress.Abstractions;
using Learnix.Application.Reviews.Abstractions;
using Learnix.Application.Users.Abstractions;
using Learnix.Application.Users.Specifications;
using MediatR;

namespace Learnix.Application.Enrollments.Queries.GetMyEnrollments;

public sealed class GetMyEnrollmentsQueryHandler(
    ICurrentUserService currentUser,
    IEnrollmentRepository enrollmentRepository,
    ILessonProgressRepository lessonProgressRepository,
    ICourseReviewRepository courseReviewRepository,
    IUserRepository userRepository,
    IBlobStorageService blobStorage)
    : IRequestHandler<GetMyEnrollmentsQuery, Result<PaginatedResult<EnrolledCourseDto>>>
{
    public async Task<Result<PaginatedResult<EnrolledCourseDto>>> Handle(
        GetMyEnrollmentsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        var studentId = currentUser.UserId.Value;
        var pagination = PaginationRequest.FromOffset(request.Skip, request.Take);

        var totalCount = await enrollmentRepository.CountAsync(
            new MyEnrollmentsCountSpecification(studentId),
            cancellationToken);

        if (totalCount == 0)
            return Result.Ok(PaginatedResult<EnrolledCourseDto>.Empty(pagination.PageIndex, pagination.PageSize));

        var enrollments = await enrollmentRepository.ListAsync(
            new MyEnrollmentsSpecification(studentId, pagination.Skip, pagination.Take),
            cancellationToken);

        var courseIds = enrollments.Select(e => e.CourseId).Distinct().ToList();
        var instructorIds = enrollments.Select(e => e.Course!.InstructorId).Distinct().ToList();

        var progressByCourse = await lessonProgressRepository.GetProgressCountsAsync(
            studentId, courseIds, cancellationToken);
        var myRatingByCourse = await courseReviewRepository.GetMyRatingsAsync(
            studentId, courseIds, cancellationToken);

        var instructors = await userRepository.ListAsync(
            new UsersByIdsSpecification(instructorIds), cancellationToken);
        var instructorNameById = instructors.ToDictionary(
            u => u.Id, u => $"{u.FirstName} {u.LastName}");

        var items = enrollments.Select(e =>
        {
            progressByCourse.TryGetValue(e.CourseId, out var progress);
            myRatingByCourse.TryGetValue(e.CourseId, out var rating);

            return new EnrolledCourseDto(
                e.Id,
                e.CourseId,
                e.Course!.Title,
                e.Course.CoverBlobPath,
                e.Course.InstructorId,
                instructorNameById.GetValueOrDefault(e.Course.InstructorId, string.Empty),
                e.Course.CategoryId,
                e.PricePaid,
                e.Status.ToString(),
                e.PaymentStatus.ToString(),
                e.EnrolledAt,
                e.CompletedAt,
                !string.IsNullOrWhiteSpace(e.Course.CoverBlobPath) ? blobStorage.GetPublicUrl(e.Course.CoverBlobPath) : null,
                progress?.CompletedLessons ?? 0,
                progress?.TotalLessons ?? 0,
                rating == 0 ? null : (int?)rating);
        });

        return Result.Ok(PaginatedResult<EnrolledCourseDto>.Create(
            items,
            pagination.PageIndex,
            pagination.PageSize,
            totalCount));
    }
}
