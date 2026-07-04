using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Persistence;
using Learnix.Application.Common.Constants;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Constants;
using Learnix.Application.Courses.Specifications;
using Learnix.Domain.Constants;
using Learnix.Domain.Entities;
using MediatR;

namespace Learnix.Application.Courses.Commands.CreateCourse;

public sealed class CreateCourseCommandHandler(
    ICurrentUserService currentUser,
    ICategoryRepository categoryRepository,
    ICourseRepository courseRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateCourseCommand, Result<CreateCourseResponse>>
{
    public async Task<Result<CreateCourseResponse>> Handle(
        CreateCourseCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail(new AuthenticationError(CommonMessages.NotAuthenticated));

        if (!currentUser.IsInRole(Roles.Instructor) && !currentUser.IsInRole(Roles.Admin))
            return Result.Fail(new ForbiddenError(CourseMessages.OnlyInstructorsCreateCourses));

        if (!await categoryRepository.AnyAsync(new CategoryByIdSpecification(request.CategoryId), cancellationToken))
            return Result.Fail(new NotFoundError(CourseMessages.CategoryNotFound(request.CategoryId)));

        var normalizedTags = NormalizeTags(request.Tags);

        var course = Course.Create(
            instructorId: currentUser.UserId.Value,
            categoryId: request.CategoryId,
            title: request.Title.Trim(),
            description: request.Description,
            price: request.Price,
            tags: normalizedTags);

        await courseRepository.AddAsync(course, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok(new CreateCourseResponse(course.Id));
    }

    private static List<string> NormalizeTags(IEnumerable<string>? tags) =>
        tags is null
            ? []
            : tags
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .DistinctBy(t => t.ToLowerInvariant())
                .ToList();
}
