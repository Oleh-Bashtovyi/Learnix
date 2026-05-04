namespace Learnix.Application.Categories.Queries.GetAllCategories;

public sealed record CategoryListItemDto(Guid Id, string Name, string Slug, string? ImageUrl);
