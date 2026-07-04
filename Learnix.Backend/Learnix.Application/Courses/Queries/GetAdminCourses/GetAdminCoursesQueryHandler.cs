using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Common.Pagination;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Constants;
using Learnix.Application.Courses.Queries.GetInstructorCourses;
using Learnix.Application.Courses.Specifications;
using Learnix.Domain.Constants;
using MediatR;

namespace Learnix.Application.Courses.Queries.GetAdminCourses;

public sealed class GetAdminCoursesQueryHandler(
    ICurrentUserService currentUser,
    ICourseRepository courseRepository,
    IBlobStorageService blobStorage)
    : IRequestHandler<GetAdminCoursesQuery, Result<PaginatedResult<ManageCourseCardDto>>>
{
    public async Task<Result<PaginatedResult<ManageCourseCardDto>>> Handle(
        GetAdminCoursesQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        if (!currentUser.IsInRole(Roles.Admin))
            return Result.Fail(new ForbiddenError(CourseMessages.OnlyAdminsViewAllCourses));

        var pagination = PaginationRequest.FromOffset(request.Skip, request.Take);

        long totalCount;
        IReadOnlyList<Domain.Entities.Course> courses;

        if (request.IncludeDeleted)
        {
            totalCount = await courseRepository.CountAsync(
                new AdminCourseListCountSpecification(request.Search, request.CategoryId),
                cancellationToken);

            if (totalCount == 0)
                return Result.Ok(PaginatedResult<ManageCourseCardDto>.Empty(pagination.PageIndex, pagination.PageSize));

            courses = await courseRepository.ListAsync(
                new AdminCourseListSpecification(request.Search, request.CategoryId, pagination.Skip, pagination.Take),
                cancellationToken);
        }
        else
        {
            totalCount = await courseRepository.CountAsync(
                new CourseListCountSpecification(null, request.Search, request.CategoryId),
                cancellationToken);

            if (totalCount == 0)
                return Result.Ok(PaginatedResult<ManageCourseCardDto>.Empty(pagination.PageIndex, pagination.PageSize));

            courses = await courseRepository.ListAsync(
                new CourseListSpecification(null, request.Search, request.CategoryId, pagination.Skip, pagination.Take),
                cancellationToken);
        }

        var result = PaginatedResult<ManageCourseCardDto>.Create(
            courses.Select(c => new ManageCourseCardDto(
                c.Id,
                c.InstructorId,
                c.CategoryId,
                c.Title,
                c.Description,
                !string.IsNullOrWhiteSpace(c.CoverBlobPath) ? blobStorage.GetPublicUrl(c.CoverBlobPath) : null,
                c.Price,
                c.Price == 0m,
                c.Status.ToString(),
                c.EnrollmentsCount,
                c.Tags.ToList(),
                c.CreatedAt,
                c.UpdatedAt,
                c.IsDeleted)),
            pagination.PageIndex,
            pagination.PageSize,
            totalCount);

        return Result.Ok(result);
    }
}
