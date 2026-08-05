using Ardalis.Specification;
using Learnix.Domain.Entities;

namespace Learnix.Application.Courses.Abstractions;

public interface ICourseRepository : IRepositoryBase<Course>
{
    /// <summary>
    /// The tags carried by the most published courses, most used first, ties broken alphabetically.
    /// A repository method rather than a specification: the result is a projection over groups of
    /// tags, not a set of <see cref="Course"/> rows.
    /// </summary>
    /// <param name="categoryId">Restricts the count to one category; null counts every category.</param>
    /// <param name="limit">Maximum number of tags returned.</param>
    /// <param name="minCourses">How many courses a tag must appear in before it is returned at all.</param>
    Task<IReadOnlyList<string>> GetPopularTagsAsync(
        Guid? categoryId,
        int limit,
        int minCourses,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The course's category name and instructor full name, joined in a single query. A repository
    /// method rather than two specification calls: the result is a projection joining Category and
    /// User, not a set of <see cref="Course"/> rows, and doing it here avoids two extra round trips.
    /// </summary>
    /// <returns>Empty strings if the course, category or instructor cannot be found.</returns>
    Task<(string CategoryName, string InstructorFullName)> GetCategoryAndInstructorNamesAsync(
        Guid courseId,
        CancellationToken cancellationToken = default);
}
