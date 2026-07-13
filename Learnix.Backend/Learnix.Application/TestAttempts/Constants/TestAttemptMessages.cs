namespace Learnix.Application.TestAttempts.Constants;

internal static class TestAttemptMessages
{
    internal static string TestLessonNotFound => "Test not found in this course.";
    internal static string AttemptLimitReached => "You have reached the maximum number of attempts for this test.";
    internal static string CooldownActive(int remainingMinutes) =>
        $"Cooldown is active. Please wait {remainingMinutes} more minute(s) before retaking.";
    internal static string AttemptNotFound => "Test attempt not found.";
    internal static string AttemptAlreadySubmitted => "This test attempt has already been submitted.";
    internal static string AttemptNotSubmitted => "This test attempt has not been submitted yet.";
    internal static string ReviewNotAllowed => "The instructor does not allow reviewing this test.";
}
