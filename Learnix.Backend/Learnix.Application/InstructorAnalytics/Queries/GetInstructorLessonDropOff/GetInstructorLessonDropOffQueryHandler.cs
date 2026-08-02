using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Specifications;
using Learnix.Application.LessonProgress.Abstractions;

namespace Learnix.Application.InstructorAnalytics.Queries.GetInstructorLessonDropOff;

public sealed class GetInstructorLessonDropOffQueryHandler(
    ICurrentUserService currentUser,
    ICourseRepository courseRepository,
    ILessonProgressRepository lessonProgressRepository)
    : InstructorAnalyticsQueryHandler<GetInstructorLessonDropOffQuery, LessonDropOffDto>(currentUser)
{
    protected override async Task<Result<LessonDropOffDto>> HandleAsync(
        GetInstructorLessonDropOffQuery request, Guid instructorId, CancellationToken cancellationToken)
    {
        var course = await courseRepository.FirstOrDefaultAsync(
            new CourseByIdSpecification(request.CourseId, includeSections: true, includeLessons: true),
            cancellationToken);

        if (course is null)
            return Result.Fail(new NotFoundError(CommonMessages.CourseNotFound(request.CourseId)));

        // Resource authorization: the drop-off exposes a course's internals, so it is owner-only.
        if (course.InstructorId != instructorId)
            return Result.Fail(new ForbiddenError(CommonMessages.NotOwnerOfCourse));

        var completedByLesson = await lessonProgressRepository.GetCompletedCountByLessonAsync(
            request.CourseId, cancellationToken);

        var lessons = course.Sections
            .OrderBy(s => s.DisplayOrder)
            .SelectMany(s => s.Lessons.Where(l => !l.IsHidden).OrderBy(l => l.DisplayOrder))
            .Select(l => new LessonDropOffItemDto(
                l.Id,
                l.Title,
                completedByLesson.GetValueOrDefault(l.Id)))
            .ToList();

        return Result.Ok(new LessonDropOffDto(course.EnrollmentsCount, lessons));
    }
}
