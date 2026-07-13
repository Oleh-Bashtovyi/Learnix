using FluentResults;
using Learnix.Application.AiChat.Constants;
using Learnix.Application.AiChat.Specifications;
using Learnix.Application.Courses.Abstractions;
using Learnix.Application.Courses.Specifications;
using Learnix.Application.Users.Abstractions;
using Learnix.Domain.Entities;
using MediatR;

namespace Learnix.Application.AiChat.Queries.SearchCourses;

internal sealed class SearchCoursesQueryHandler(
    ICourseRepository courseRepository,
    ICategoryRepository categoryRepository,
    IUserRepository userRepository)
    : IRequestHandler<SearchCoursesQuery, Result<IReadOnlyList<CourseSearchResultDto>>>
{
    public async Task<Result<IReadOnlyList<CourseSearchResultDto>>> Handle(
        SearchCoursesQuery request,
        CancellationToken cancellationToken)
    {
        var maxResults = Math.Clamp(request.MaxResults, 1, 20);

        Guid? categoryId = null;
        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            var category = await categoryRepository.FirstOrDefaultAsync(
                new CategoryBySlugSpecification(request.Category),
                cancellationToken);
            categoryId = category?.Id;
        }

        var keywords = Tokenize(request.Query);
        var courses = await SearchAsync(keywords, categoryId, maxResults, cancellationToken);

        var categoryIds = courses.Select(c => c.CategoryId).Distinct().ToList();
        var categories = await categoryRepository.ListAsync(
            new CategoriesByIdsSpecification(categoryIds), cancellationToken);
        var categoryMap = categories.ToDictionary(c => c.Id, c => c.Name);

        var instructorIds = courses.Select(c => c.InstructorId).Distinct().ToList();
        var instructors = await userRepository.ListAsync(
            new UsersByIdsSpecification(instructorIds), cancellationToken);
        var instructorMap = instructors.ToDictionary(u => u.Id, u => $"{u.FirstName} {u.LastName}");

        var results = courses
            .Select(c => new CourseSearchResultDto(
                c.Id,
                c.Title,
                c.Description.Length > AiChatToolLimits.CourseDescriptionPreviewLength
                    ? c.Description[..AiChatToolLimits.CourseDescriptionPreviewLength] + "..."
                    : c.Description,
                categoryMap.TryGetValue(c.CategoryId, out var name) ? name : "Unknown",
                c.InstructorId,
                instructorMap.TryGetValue(c.InstructorId, out var instructor) ? instructor : "Unknown",
                c.Price,
                c.Price == 0,
                c.EnrollmentsCount))
            .ToList();

        return Result.Ok<IReadOnlyList<CourseSearchResultDto>>(results);
    }

    private static IReadOnlyList<string> Tokenize(string query) =>
        query
            .ToLowerInvariant()
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct()
            .ToList();

    /// <summary>
    /// All keywords first; then, if that finds nothing, each keyword on its own.
    /// <para>
    /// The strict pass is what a search should do, but one dud word is enough to empty it — a model
    /// relaying "Python courses" verbatim gets nothing, because no course text contains the word
    /// "courses", however many Python courses exist. Rather than teach the search a list of words to
    /// ignore (which would have to know every language a user might type in), fall back to the union
    /// of the single-keyword searches. The dud contributes nothing and the real keyword still finds
    /// its courses. Results stay ordered by popularity, and the model picks from them.
    /// </para>
    /// </summary>
    private async Task<List<Course>> SearchAsync(
        IReadOnlyList<string> keywords,
        Guid? categoryId,
        int maxResults,
        CancellationToken cancellationToken)
    {
        var strict = await courseRepository.ListAsync(
            new CourseSearchSpecification(keywords, categoryId, maxResults),
            cancellationToken);

        if (strict.Count > 0 || keywords.Count <= 1)
            return strict.ToList();

        var byKeyword = new Dictionary<Guid, Course>();
        foreach (var keyword in keywords)
        {
            var matches = await courseRepository.ListAsync(
                new CourseSearchSpecification([keyword], categoryId, maxResults),
                cancellationToken);

            foreach (var course in matches)
                byKeyword.TryAdd(course.Id, course);
        }

        return byKeyword.Values
            .OrderByDescending(c => c.EnrollmentsCount)
            .ThenByDescending(c => c.UpdatedAt)
            .Take(maxResults)
            .ToList();
    }
}
