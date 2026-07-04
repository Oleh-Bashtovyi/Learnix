namespace Learnix.Infrastructure.Outbox.Payloads;

public sealed record SendCourseAdminActionEmailPayload(string ToEmail, string InstructorFirstName, string CourseTitle, string Language);
