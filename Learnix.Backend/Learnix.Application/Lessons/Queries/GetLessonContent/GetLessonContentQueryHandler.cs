using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Enrollments.Abstractions;
using Learnix.Application.Enrollments.Specifications;
using Learnix.Application.Lessons.Abstractions;
using Learnix.Domain.Entities;
using MediatR;

namespace Learnix.Application.Lessons.Queries.GetLessonContent;

public sealed class GetLessonContentQueryHandler(
    ICurrentUserService currentUser,
    IEnrollmentRepository enrollmentRepository,
    ILessonRepository lessonRepository,
    IBlobStorageService blobStorage)
    : IRequestHandler<GetLessonContentQuery, Result<LessonContentDto>>
{
    public async Task<Result<LessonContentDto>> Handle(
        GetLessonContentQuery request,
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

        var lesson = await lessonRepository.GetVisibleLessonInCourseAsync(
            request.CourseId, request.LessonId, cancellationToken);

        if (lesson is null)
            return Result.Fail(new NotFoundError(CommonMessages.LessonNotFound(request.LessonId)));

        var dto = lesson switch
        {
            VideoLesson v => new LessonContentDto(
                v.Id, v.Title, v.LessonType,
                VideoUrl: !string.IsNullOrWhiteSpace(v.VideoBlobPath) ? blobStorage.GenerateReadUrl(v.VideoBlobPath, BlobUrlTtlConstants.VideoLessonReadUrl) : null,
                Description: v.Description,
                DurationSeconds: v.DurationSeconds,
                Content: null),

            PostLesson p => new LessonContentDto(
                p.Id, p.Title, p.LessonType,
                VideoUrl: null,
                Description: null,
                DurationSeconds: null,
                Content: p.Content),

            TestLesson t => new LessonContentDto(
                t.Id, t.Title, t.LessonType,
                VideoUrl: null,
                Description: t.Description,
                DurationSeconds: null,
                Content: null),

            _ => throw new InvalidOperationException($"Unknown lesson type for lesson {lesson.Id}.")
        };

        return Result.Ok(dto);
    }
}
