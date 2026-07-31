namespace Learnix.Application.AiChat.Constants;

/// <summary>
/// The names the model calls tools by. They travel far from the tool that owns them — into the system
/// prompt, into tool results the model reads, and into the client's activity indicator — so a rename that
/// misses one of those places leaves the model calling something that is not there.
/// </summary>
public static class ChatToolNames
{
    public const string SearchCourses = "search_courses";
    public const string GetCategories = "get_categories";
    public const string GetPlatformStats = "get_platform_stats";
    public const string GetInstructorCourses = "get_instructor_courses";
    public const string GetMyLearningProfile = "get_my_learning_profile";
    public const string GetPlatformInfo = "get_platform_info";
    public const string GetCurrentLesson = "get_current_lesson";
    public const string GetMyTestReview = "get_my_test_review";
}
