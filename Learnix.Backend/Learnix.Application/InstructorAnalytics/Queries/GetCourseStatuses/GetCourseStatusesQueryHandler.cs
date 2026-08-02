using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.InstructorAnalytics.Services;
using Learnix.Application.InstructorAnalytics.Specifications;

namespace Learnix.Application.InstructorAnalytics.Queries.GetCourseStatuses;

public sealed class GetCourseStatusesQueryHandler(
    ICurrentUserService currentUser,
    ICourseRepository courseRepository)
    : InstructorAnalyticsQueryHandler<GetCourseStatusesQuery, CourseStatusesDto>(currentUser)
{
    protected override async Task<Result<CourseStatusesDto>> HandleAsync(
        GetCourseStatusesQuery request, Guid instructorId, CancellationToken cancellationToken)
    {
        var courses = await courseRepository.ListAsync(
            new InstructorCoursesForAnalyticsSpecification(instructorId),
            cancellationToken);

        return Result.Ok(InstructorAnalyticsCalculations.Statuses(courses));
    }
}
