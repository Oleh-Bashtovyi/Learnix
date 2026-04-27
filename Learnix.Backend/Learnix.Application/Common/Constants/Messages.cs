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
}
