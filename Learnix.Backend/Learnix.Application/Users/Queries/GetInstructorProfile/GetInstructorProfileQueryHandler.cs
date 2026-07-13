using FluentResults;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Abstractions.Storage;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Specifications;
using Learnix.Application.Users.Abstractions;
using Learnix.Application.Users.Constants;
using Learnix.Application.Users.Specifications;
using Learnix.Domain.Constants;
using MediatR;

namespace Learnix.Application.Users.Queries.GetInstructorProfile;

/// <summary>
/// Separate from <see cref="GetUserProfile.GetUserProfileQuery"/> rather than an extension of it: the
/// aggregates below mean nothing for a student, and folding them in would make every profile fetch pay
/// for a course query it does not need.
/// </summary>
internal sealed class GetInstructorProfileQueryHandler(
    IUserRepository userRepository,
    ICourseRepository courseRepository,
    IUserRoleService roleService,
    IBlobStorageService blobStorage)
    : IRequestHandler<GetInstructorProfileQuery, Result<InstructorProfileResponse>>
{
    public async Task<Result<InstructorProfileResponse>> Handle(
        GetInstructorProfileQuery request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.FirstOrDefaultAsync(
            new UserByIdSpecification(request.InstructorId),
            cancellationToken);

        if (user is null)
            return Result.Fail(new NotFoundError(UserMessages.GenericUserNotFound));

        // An instructor profile of somebody who is not an instructor is not a thing. Without this,
        // any user id renders as a teacher with no courses, no students and no rating — a page about
        // a student, built from their name, that they never asked for and cannot see coming.
        // NotFound rather than Forbidden: the resource does not exist, and saying "forbidden" would
        // confirm that the id belongs to a real account.
        var roles = await roleService.GetRolesAsync(user.Id, cancellationToken);

        if (!roles.Contains(Roles.Instructor))
            return Result.Fail(new NotFoundError(UserMessages.GenericUserNotFound));

        var courses = await courseRepository.ListAsync(
            new PublishedCoursesByInstructorSpecification(request.InstructorId),
            cancellationToken);

        var reviewsCount = courses.Sum(c => c.ReviewsCount);

        // Weighted by review count, not a mean of the means: a single 5.0 review on a new course would
        // otherwise pull the instructor's average as hard as forty reviews averaging 4.5.
        var averageRating = reviewsCount == 0
            ? 0m
            : Math.Round(
                courses.Sum(c => c.AverageRating * c.ReviewsCount) / reviewsCount,
                2,
                MidpointRounding.AwayFromZero);

        return Result.Ok(new InstructorProfileResponse(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Bio,
            !string.IsNullOrWhiteSpace(user.AvatarBlobPath)
                ? blobStorage.GetPublicUrl(user.AvatarBlobPath)
                : null,
            user.CreatedAt,
            courses.Count,
            courses.Sum(c => c.EnrollmentsCount),
            averageRating,
            reviewsCount));
    }
}
