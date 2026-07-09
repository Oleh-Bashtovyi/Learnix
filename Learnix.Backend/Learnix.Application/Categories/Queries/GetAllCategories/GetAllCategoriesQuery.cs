using FluentResults;
using Learnix.Application.Common.Caching;
using Learnix.Application.Common.Constants;
using MediatR;

namespace Learnix.Application.Categories.Queries.GetAllCategories;

public sealed record GetAllCategoriesQuery()
    : IRequest<Result<IReadOnlyList<CategoryListItemDto>>>, ICacheable<IReadOnlyList<CategoryListItemDto>>
{
    public string CacheKey => CacheKeys.Categories.All;
    public TimeSpan Expiration => CacheKeys.Categories.AllTtl;
}
