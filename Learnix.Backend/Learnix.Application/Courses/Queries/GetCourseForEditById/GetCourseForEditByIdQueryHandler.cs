using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Common.Extensions;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Constants;
using Learnix.Application.Courses.Specifications;
using Learnix.Application.Lessons.Abstractions;
using Learnix.Application.Lessons.Specifications;
using Learnix.Domain.Entities;
using Learnix.Domain.ValueObjects;
using MediatR;

namespace Learnix.Application.Courses.Queries.GetCourseForEditById;

public sealed class GetCourseForEditByIdQueryHandler(
    ICurrentUserService currentUser,
    ICourseRepository courseRepository,
    ITestVersionRepository testVersionRepository,
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

        // The editor is one of the two places that wants a test's *live* questions, and those live on
        // the current TestVersion rather than the lesson (ADR-BACK-LMS-006). Fetched for the whole
        // course at once so a curriculum with twenty tests is still one extra query.
        var currentVersionIds = course.Sections
            .SelectMany(s => s.Lessons)
            .OfType<TestLesson>()
            .Select(t => t.CurrentVersionId)
            .OfType<Guid>()
            .ToList();

        var questionsByVersionId = currentVersionIds.Count == 0
            ? []
            : (await testVersionRepository.ListAsync(
                new TestVersionsByIdsSpecification(currentVersionIds), cancellationToken))
                .ToDictionary(v => v.Id, v => v.Questions);

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
                        .Select(l => MapLesson(l, questionsByVersionId))
                        .ToList()))
                .ToList(),
            course.CreatedAt,
            course.UpdatedAt);

        return Result.Ok(dto);
    }

    private CourseForEditLessonDto MapLesson(
        Lesson lesson,
        IReadOnlyDictionary<Guid, IReadOnlyList<Question>> questionsByVersionId) => lesson switch
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
                CurrentQuestionsOf(test, questionsByVersionId)
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

    private static IReadOnlyList<Question> CurrentQuestionsOf(
        TestLesson test,
        IReadOnlyDictionary<Guid, IReadOnlyList<Question>> questionsByVersionId)
        => test.CurrentVersionId is Guid versionId
            ? questionsByVersionId.GetValueOrDefault(versionId, [])
            : [];
}
