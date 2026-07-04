namespace Learnix.Application.AiChat.Queries.SearchCourses;

public sealed record CourseSearchResultDto(
    Guid CourseId,
    string Title,
    string ShortDescription,
    string CategoryName,
    decimal Price,
    bool IsFree,
    int EnrollmentsCount);
