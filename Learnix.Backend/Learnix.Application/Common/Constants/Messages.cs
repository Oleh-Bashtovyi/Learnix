using MediatR;

namespace Learnix.Application.Common.Constants;

internal static class CommonMessages
{
    internal static string CourseNotFound(Guid courseId) => $"Course {courseId} not found.";
    internal static string SectionNotFound(Guid sectionId) => $"Section {sectionId} not found.";
    internal static string LessonNotFound(Guid lessonId) => $"Lesson {lessonId} not found.";
    internal static string InstructorApplicationNotFound(Guid applicationId) => $"Application {applicationId} not found.";
    internal static string CourseCategoryNotFound(Guid categoryId) => $"Course category '{categoryId}' not found.";
    internal static string NotOwnerOfCourse => "You are not the owner of this course.";
    internal static string OnlyAdminCanManageCategories => "Only admins can manage categories.";
    internal static string NotAuthenticated => "Not authenticated.";
    internal static string NotEnrolledInCourse => "You are not enrolled in this course.";
    internal static string LessonNotInCourse => "Lesson does not belong to the specified course, or is not visible.";
    internal static string AlreadyEnrolled => "You are already enrolled in this course.";
    internal static string CourseNotPublished => "Only published courses can be enrolled in.";
}
