using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Commands;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Lessons.Abstractions;
using Learnix.Domain.Entities;

namespace Learnix.Application.Lessons.Commands.CreatePostLesson;

internal sealed class CreatePostLessonCommandHandler(
    ICourseRepository courseRepository,
    ILessonRepository lessonRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser)
    : CourseSectionCommandHandler<CreatePostLessonCommand, Result<Guid>>(courseRepository, currentUser)
{
    protected override async Task<Result<Guid>> HandleAsync(
        CreatePostLessonCommand request, Course course, CancellationToken ct)
    {
        var displayOrder = await lessonRepository.GetMaxDisplayOrderAsync(request.SectionId, ct) + 1;

        var lesson = PostLesson.Create(request.SectionId, request.Title, displayOrder, request.Content);

        await lessonRepository.AddAsync(lesson, ct);

        await unitOfWork.SaveChangesAsync(ct);

        return Result.Ok(lesson.Id);
    }
}
