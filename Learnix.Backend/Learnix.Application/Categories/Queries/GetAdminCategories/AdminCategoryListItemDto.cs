namespace Learnix.Application.Categories.Queries.GetAdminCategories;

public sealed record AdminCategoryListItemDto(Guid Id, string Name, string Slug, bool IsSystem, string? ImageUrl);
