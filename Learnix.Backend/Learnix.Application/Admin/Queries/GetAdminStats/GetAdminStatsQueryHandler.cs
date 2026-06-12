using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Specifications;
using Learnix.Application.InstructorApplications.Abstractions;
using Learnix.Application.InstructorApplications.Specifications;
using Learnix.Application.Users.Abstractions;
using Learnix.Application.Users.Specifications;
using Learnix.Domain.Constants;
using Learnix.Domain.Enums;
using MediatR;

namespace Learnix.Application.Admin.Queries.GetAdminStats;

internal sealed class GetAdminStatsQueryHandler(
    ICurrentUserService currentUser,
    IUserRepository userRepository,
    ICourseRepository courseRepository,
    IInstructorApplicationRepository applicationRepository)
    : IRequestHandler<GetAdminStatsQuery, Result<AdminStatsResponse>>
{
    public async Task<Result<AdminStatsResponse>> Handle(
        GetAdminStatsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError("Not authenticated."));

        if (!currentUser.IsInRole(Roles.Admin))
            return Result.Fail(new ForbiddenError("Only admins can view platform stats."));

        var totalUsers = await userRepository.CountAsync(
            new AdminUserListCountSpecification(search: null), cancellationToken);

        var totalCourses = await courseRepository.CountAsync(
            new AdminCourseListCountSpecification(search: null, categoryId: null), cancellationToken);

        var publishedCourses = await courseRepository.CountAsync(
            new AdminCoursesByStatusCountSpecification(CourseStatus.Published), cancellationToken);

        var pendingApplications = await applicationRepository.CountAsync(
            new PendingApplicationsCountSpecification(), cancellationToken);

        return Result.Ok(new AdminStatsResponse(
            TotalUsers: totalUsers,
            TotalCourses: totalCourses,
            PublishedCourses: publishedCourses,
            DraftCourses: totalCourses - publishedCourses,
            PendingApplications: pendingApplications));
    }
}
