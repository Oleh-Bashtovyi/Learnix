using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Specifications;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.Enrollments.Specifications;
using Learnix.Application.LessonProgress.Abstractions;
using Learnix.Application.LessonProgress.Specifications;
using MediatR;

namespace Learnix.Application.LessonProgress.Queries.GetCourseProgress;

public sealed class GetCourseProgressQueryHandler(
    ICurrentUserService currentUser,
    ICourseRepository courseRepository,
    IEnrollmentRepository enrollmentRepository,
    ILessonProgressRepository lessonProgressRepository)
    : IRequestHandler<GetCourseProgressQuery, Result<CourseProgressResponse>>
{
    public async Task<Result<CourseProgressResponse>> Handle(
        GetCourseProgressQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        var studentId = currentUser.UserId.Value;

        var isEnrolled = await enrollmentRepository.AnyAsync(
            new ActiveEnrollmentByStudentAndCourseSpecification(studentId, request.CourseId),
            cancellationToken);

        if (!isEnrolled)
            return Result.Fail(new ForbiddenError(CommonMessages.NotEnrolledInCourse));

        var course = await courseRepository.FirstOrDefaultAsync(
            new CourseByIdSpecification(request.CourseId, includeSections: true, includeLessons: true),
            cancellationToken);

        if (course is null)
            return Result.Fail(new NotFoundError(CommonMessages.CourseNotFound(request.CourseId)));

        var progressRecords = await lessonProgressRepository.ListAsync(
            new CourseProgressByStudentSpecification(studentId, request.CourseId),
            cancellationToken);

        var progressByLessonId = progressRecords.ToDictionary(p => p.LessonId);

        var sections = course.Sections
            .OrderBy(s => s.DisplayOrder)
            .Select(s =>
            {
                var lessons = s.Lessons
                    .Where(l => !l.IsHidden)
                    .OrderBy(l => l.DisplayOrder)
                    .Select(l =>
                    {
                        progressByLessonId.TryGetValue(l.Id, out var p);
                        return new LessonProgressItemDto(
                            l.Id,
                            l.Title,
                            l.LessonType.ToString(),
                            l.DisplayOrder,
                            p?.IsCompleted ?? false,
                            p?.CompletedAt,
                            p?.LastAccessedAt);
                    })
                    .ToList();

                return new SectionProgressDto(s.Id, s.Title, s.DisplayOrder, lessons);
            })
            .ToList();

        var allLessons = sections.SelectMany(s => s.Lessons).ToList();

        return Result.Ok(new CourseProgressResponse(
            course.Id,
            allLessons.Count,
            allLessons.Count(l => l.IsCompleted),
            sections));
    }
}
