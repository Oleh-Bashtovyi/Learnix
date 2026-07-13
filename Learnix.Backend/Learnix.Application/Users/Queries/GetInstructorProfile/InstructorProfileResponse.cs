namespace Learnix.Application.Users.Queries.GetInstructorProfile;

/// <summary>
/// The public face of an instructor: who they are, and what their published work adds up to.
/// <para>
/// Deliberately no email. This is served anonymously and the page that renders it is indexed, so an
/// address here is a permanent, scrapable one — and the platform already has a contact channel that
/// does not require publishing it.
/// </para>
/// </summary>
/// <param name="TotalStudents">Enrollments summed across the published courses. Counts a person once per course.</param>
/// <param name="AverageRating">Weighted by how many reviews each course has, so a 5.0 from one review does not outweigh a 4.5 from forty. Zero when nothing has been reviewed.</param>
/// <param name="ReviewsCount">How many reviews the average rests on — an average without it says very little.</param>
public sealed record InstructorProfileResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string? Bio,
    string? AvatarUrl,
    DateTime JoinedAt,
    int CoursesCount,
    int TotalStudents,
    decimal AverageRating,
    int ReviewsCount);
