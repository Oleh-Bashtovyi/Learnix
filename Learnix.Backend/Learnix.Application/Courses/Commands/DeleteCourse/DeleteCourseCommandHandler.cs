using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Commands;
using Learnix.Application.Common.Constants;
using Learnix.Application.Courses.Abstractions;
using Learnix.Domain.Entities;
using Microsoft.Extensions.Caching.Distributed;

namespace Learnix.Application.Courses.Commands.DeleteCourse;

public sealed class DeleteCourseCommandHandler
    : CourseCommandHandler<DeleteCourseCommand, Result>
{
    private readonly ICourseRepository _courseRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDistributedCache _cache;

    public DeleteCourseCommandHandler(
        ICurrentUserService currentUser,
        ICourseRepository courseRepository,
        IUnitOfWork unitOfWork,
        IDistributedCache cache)
        : base(courseRepository, currentUser)
    {
        _courseRepository = courseRepository;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    protected override async Task<Result> HandleAsync(
        DeleteCourseCommand request, Course course, CancellationToken ct)
    {
        course.MarkForDeletion();

        await _courseRepository.DeleteAsync(course, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        await Task.WhenAll(
            _cache.RemoveAsync(CacheKeys.Course(request.CourseId), ct),
            _cache.RemoveAsync(CacheKeys.CoursesFeatured, ct));

        return Result.Ok();
    }
}
