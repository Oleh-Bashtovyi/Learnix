using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Common.Extensions;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Constants;
using Learnix.Application.Courses.Specifications;
using Learnix.Domain.Entities;
using MediatR;

namespace Learnix.Application.Courses.Queries.GetCourseForEditById;

public sealed class GetCourseForEditByIdQueryHandler(
    ICurrentUserService currentUser,
    ICourseRepository courseRepository,
    IBlobStorageService blobStorage)
    : IRequestHandler<GetCourseForEditByIdQuery, Result<CourseForEditDto>>
{
    public async Task<Result<CourseForEditDto>> Handle(GetCourseForEditByIdQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        var course = await courseRepository.FirstOrDefaultAsync(
            new CourseByIdSpecification(request.CourseId, includeSections: true, includeLessons: true),
            cancellationToken);

        if (course is null)
            return Result.Fail(new NotFoundError(CommonMessages.CourseNotFound(request.CourseId)));

        if (!course.IsOwnerOrAdmin(currentUser))
            return Result.Fail(new ForbiddenError(CourseMessages.NotAllowedToViewCourse));

        var dto = new CourseForEditDto(
            course.Id,
            course.InstructorId,
            course.CategoryId,
            course.Title,
            course.Description,
            !string.IsNullOrWhiteSpace(course.CoverBlobPath)
                ? blobStorage.GetPublicUrl(course.CoverBlobPath)
                : null,
            course.Price,
            course.Price == 0m,
            course.Status.ToString(),
            course.EnrollmentsCount,
            course.Tags.ToList(),
            course.Sections
                .OrderBy(s => s.DisplayOrder)
                .Select(s => new CourseForEditSectionDto(
                    s.Id,
                    s.Title,
                    s.DisplayOrder,
                    s.Lessons
                        .OrderBy(l => l.DisplayOrder)
                        .Select(MapLesson)
                        .ToList()))
                .ToList(),
            course.CreatedAt,
            course.UpdatedAt);

        return Result.Ok(dto);
    }

    private CourseForEditLessonDto MapLesson(Lesson lesson) => lesson switch
    {
        VideoLesson video => new CourseForEditLessonDto(
            video.Id,
            video.Title,
            video.DisplayOrder,
            video.LessonType.ToString(),
            video.IsHidden,
            !string.IsNullOrWhiteSpace(video.VideoBlobPath) ? blobStorage.GenerateReadUrl(video.VideoBlobPath, BlobUrlTtlConstants.VideoLessonReadUrl) : null,
            video.Description,
            video.DurationSeconds,
            null,
            null,
            null,
            null,
            null,
            []),

        PostLesson post => new CourseForEditLessonDto(
            post.Id,
            post.Title,
            post.DisplayOrder,
            post.LessonType.ToString(),
            post.IsHidden,
            null,
            null,
            null,
            post.Content,
            null,
            null,
            null,
            null,
            []),

        TestLesson test => new CourseForEditLessonDto(
            test.Id,
            test.Title,
            test.DisplayOrder,
            test.LessonType.ToString(),
            test.IsHidden,
            null,
            test.Description,
            null,
            null,
            test.AttemptLimit,
            test.CooldownMinutes,
            test.PassingThreshold,
            test.ReviewMode,
            test.Questions
                .OrderBy(q => q.Order)
                .Select(q => new CourseForEditQuestionDto(
                    q.Id,
                    q.Text,
                    q.Type.ToString(),
                    q.Order,
                    q.Options
                        .OrderBy(o => o.Order)
                        .Select(o => new CourseForEditQuestionOptionDto(
                            o.Id,
                            o.Text,
                            o.IsCorrect,
                            o.Order))
                        .ToList(),
                    q.TextAnswer?.CorrectAnswer,
                    q.TextAnswer?.IgnoreCase ?? false,
                    q.TextAnswer?.AllowFuzzy ?? false))
                .ToList()),

        _ => throw new InvalidOperationException($"Unsupported lesson type: {lesson.GetType().Name}")
    };
}
