namespace Learnix.Application.Courses.Constants;

public static class CourseMessages
{
    public static string OnlyAdminsForceDelete => "Only admins can force-delete courses.";
    public static string CourseIdNotFound(Guid courseId) => $"Course {courseId} not found.";
    public static string CourseAlreadyDeleted => "Course is already deleted.";
    public static string OnlyAdminsForcePublish => "Only admins can force-publish courses.";
    public static string CourseAlreadyPublished => "Course is already published.";
    public static string OnlyAdminsRecoverCourses => "Only admins can recover courses.";
    public static string CourseNotDeleted => "Course is not deleted.";
    public static string OnlyAdminsForceUnpublish => "Only admins can force-unpublish courses.";
    public static string CourseNotPublished => "Course is not published.";
    public static string OnlyInstructorsCreateCourses => "Only instructors can create courses.";
    public static string CategoryNotFound(Guid categoryId) => $"Category '{categoryId}' was not found.";
    public static string CannotPublishNoCoverImage => "Course cannot be published without a cover image.";
    public static string CannotPublishNoSection => "Course cannot be published without at least one section.";
    public static string CannotPublishNoLesson => "Course cannot be published without at least one lesson.";
    public static string OnlyAdminsViewAllCourses => "Only admins can view all courses.";
    public static string NotAllowedToViewCourse => "You are not allowed to view this course.";
    public static string OnlyInstructorsAccessTheirCourses => "Only instructors can access their courses.";
}
