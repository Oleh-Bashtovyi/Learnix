using FluentResults;
using MediatR;

namespace Learnix.Application.Categories.Queries.GetAdminCategories;

public sealed record GetAdminCategoriesQuery() : IRequest<Result<IReadOnlyList<AdminCategoryListItemDto>>>;
