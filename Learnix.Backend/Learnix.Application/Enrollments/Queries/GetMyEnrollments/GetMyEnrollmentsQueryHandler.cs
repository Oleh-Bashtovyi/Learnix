using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Common.Pagination;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.Enrollments.Specifications;
using MediatR;

namespace Learnix.Application.Enrollments.Queries.GetMyEnrollments;

public sealed class GetMyEnrollmentsQueryHandler(
    ICurrentUserService currentUser,
    IEnrollmentRepository enrollmentRepository,
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

        var items = enrollments.Select(e => new EnrolledCourseDto(
            e.Id,
            e.CourseId,
            e.Course!.Title,
            e.Course.CoverBlobPath,
            e.Course.InstructorId,
            e.Course.CategoryId,
            e.PricePaid,
            e.Status.ToString(),
            e.PaymentStatus.ToString(),
            e.EnrolledAt,
            e.CompletedAt,
            e.Course.CoverBlobPath is not null
                ? blobStorage.GenerateReadUrl(e.Course.CoverBlobPath, TimeSpan.FromHours(24))
                : null));

        return Result.Ok(PaginatedResult<EnrolledCourseDto>.Create(
            items,
            pagination.PageIndex,
            pagination.PageSize,
            totalCount));
    }
}
