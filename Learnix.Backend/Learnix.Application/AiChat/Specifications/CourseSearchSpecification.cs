using Ardalis.Specification;
using Learnix.Domain.Entities;
using Learnix.Domain.Enums;

namespace Learnix.Application.AiChat.Specifications;

/// <summary>
/// Keyword search over published courses, for the AI tool.
/// <para>
/// Every keyword must appear somewhere — the title, the description, or the tags. The query used to
/// be matched as one literal string, which meant the search only found a course whose text contained
/// the user's phrase verbatim: "python data analysis" returned nothing, because no title or
/// description runs those three words together, even with a Python data-analysis course sitting right
/// there. Matching keyword by keyword is what makes an ordinary multi-word query work at all.
/// </para>
/// <para>
/// A keyword that matches nothing still sinks the whole search, so
/// <see cref="Queries.SearchCourses.SearchCoursesQueryHandler"/> retries with the keywords taken
/// separately when this finds nothing. TD-007 records why substring matching is the wrong instrument
/// here to begin with.
/// </para>
/// </summary>
public sealed class CourseSearchSpecification : Specification<Course>
{
    public CourseSearchSpecification(
        IReadOnlyCollection<string> keywords,
        Guid? categoryId,
        int maxResults)
    {
        Query.Where(c => c.Status == CourseStatus.Published);

        // Ardalis ANDs successive Where() calls together, so this reads as "every keyword matches".
        foreach (var keyword in keywords)
        {
            var needle = keyword;
            Query.Where(c => c.Title.ToLower().Contains(needle)
                          || c.Description.ToLower().Contains(needle)
                          || c.Tags.Contains(needle));
        }

        if (categoryId.HasValue)
            Query.Where(c => c.CategoryId == categoryId.Value);

        Query
            .OrderByDescending(c => c.EnrollmentsCount)
            .ThenByDescending(c => c.UpdatedAt)
            .Take(maxResults)
            .AsNoTracking();
    }
}
