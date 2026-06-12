namespace Learnix.Application.Admin.Queries.GetAdminStats;

public sealed record AdminStatsResponse(
    int TotalUsers,
    int TotalCourses,
    int PublishedCourses,
    int DraftCourses,
    int PendingApplications);
