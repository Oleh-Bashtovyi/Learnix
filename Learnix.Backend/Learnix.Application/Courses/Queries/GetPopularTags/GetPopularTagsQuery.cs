using FluentResults;
using Learnix.Application.Common.Caching;
using Learnix.Application.Common.Constants;
using MediatR;

namespace Learnix.Application.Courses.Queries.GetPopularTags;

/// <param name="CategoryId">
/// Narrows the count to one category. Null asks for the platform-wide list — what the editor needs
/// before a category has been picked.
/// </param>
public sealed record GetPopularTagsQuery(Guid? CategoryId)
    : IRequest<Result<IReadOnlyList<string>>>, ICacheable<IReadOnlyList<string>>
{
    public string CacheKey => CacheKeys.Courses.PopularTags(CategoryId);
    public TimeSpan Expiration => CacheKeys.Courses.PopularTagsTtl;
}
