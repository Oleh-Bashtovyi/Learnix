namespace Learnix.Application.AiChat.Queries.GetCategories;

public sealed record CategoryAiDto(
    string Name,
    string Slug,
    int CoursesCount);
