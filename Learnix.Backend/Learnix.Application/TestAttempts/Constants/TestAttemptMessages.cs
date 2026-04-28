namespace Learnix.Application.TestAttempts.Constants;

internal static class TestAttemptMessages
{
    internal static string TestLessonNotFound => "Test not found in this course.";
    internal static string AttemptLimitReached => "You have reached the maximum number of attempts for this test.";
    internal static string CooldownActive(int remainingMinutes) =>
        $"Cooldown is active. Please wait {remainingMinutes} more minute(s) before retaking.";
}
