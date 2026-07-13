using FluentResults;
using Learnix.Application.AiChat.Constants;
using Learnix.Application.AiChat.Specifications;
using Learnix.Application.Common.Abstractions.Identity;
using Learnix.Application.Common.Errors;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Specifications;
using Learnix.Application.Users.Abstractions;
using Learnix.Application.Users.Specifications;
using Learnix.Domain.Constants;
using Learnix.Domain.Entities;
using MediatR;

namespace Learnix.Application.AiChat.Queries.GetCoursesByInstructor;

internal sealed class GetCoursesByInstructorQueryHandler(
    IUserRepository userRepository,
    IUserRoleService roleService,
    ICourseRepository courseRepository,
    ICategoryRepository categoryRepository)
    : IRequestHandler<GetCoursesByInstructorQuery, Result<InstructorCoursesDto>>
{
    public async Task<Result<InstructorCoursesDto>> Handle(
        GetCoursesByInstructorQuery request,
        CancellationToken cancellationToken)
    {
        var candidates = await FindCandidatesAsync(request, cancellationToken);
        var instructors = await KeepInstructorsAsync(candidates, cancellationToken);

        if (instructors.Count == 0)
            return Result.Fail(new NotFoundError(AiChatMessages.InstructorNotFound));

        if (instructors.Count > 1)
        {
            var matches = instructors
                .Select(u => new InstructorSummaryDto(u.Id, FullNameOf(u), Bio: null))
                .ToList();

            return Result.Ok(new InstructorCoursesDto(Instructor: null, Courses: null, Ambiguous: matches));
        }

        var instructor = instructors[0];

        var courses = await courseRepository.ListAsync(
            new PublishedCoursesByInstructorSpecification(instructor.Id, AiChatToolLimits.InstructorCourses),
            cancellationToken);

        var categoryNames = await ResolveCategoryNamesAsync(courses, cancellationToken);

        var courseDtos = courses.Select(c => new InstructorCourseDto(
            c.Id,
            c.Title,
            Preview(c.Description),
            categoryNames.GetValueOrDefault(c.CategoryId, "Unknown"),
            c.Price,
            c.Price == 0m,
            c.EnrollmentsCount,
            c.AverageRating,
            c.ReviewsCount)).ToList();

        return Result.Ok(new InstructorCoursesDto(
            new InstructorSummaryDto(instructor.Id, FullNameOf(instructor), instructor.Bio),
            courseDtos,
            Ambiguous: null));
    }

    private async Task<IReadOnlyList<User>> FindCandidatesAsync(
        GetCoursesByInstructorQuery request,
        CancellationToken cancellationToken)
    {
        if (request.InstructorId is { } id)
        {
            var user = await userRepository.FirstOrDefaultAsync(new UserByIdSpecification(id), cancellationToken);
            return user is null ? [] : [user];
        }

        return await userRepository.ListAsync(
            new InstructorCandidatesByNameSpecification(request.InstructorName!, AiChatToolLimits.InstructorCandidates),
            cancellationToken);
    }

    private async Task<IReadOnlyList<User>> KeepInstructorsAsync(IReadOnlyList<User> candidates, CancellationToken cancellationToken)
    {
        if (candidates.Count == 0)
            return [];

        var roleMap = await roleService.GetRolesBulkAsync(candidates.Select(u => u.Id), cancellationToken);

        return candidates
            .Where(u => roleMap.TryGetValue(u.Id, out var roles) && roles.Contains(Roles.Instructor))
            .ToList();
    }

    private async Task<IReadOnlyDictionary<Guid, string>> ResolveCategoryNamesAsync(
        IReadOnlyList<Course> courses,
        CancellationToken cancellationToken)
    {
        var ids = courses.Select(c => c.CategoryId).Distinct().ToList();

        if (ids.Count == 0)
            return new Dictionary<Guid, string>();

        var categories = await categoryRepository.ListAsync(new CategoriesByIdsSpecification(ids), cancellationToken);
        return categories.ToDictionary(c => c.Id, c => c.Name);
    }

    private static string FullNameOf(User user) => $"{user.FirstName} {user.LastName}";

    private static string Preview(string description) =>
        description.Length > AiChatToolLimits.CourseDescriptionPreviewLength
            ? description[..AiChatToolLimits.CourseDescriptionPreviewLength] + "..."
            : description;
}
