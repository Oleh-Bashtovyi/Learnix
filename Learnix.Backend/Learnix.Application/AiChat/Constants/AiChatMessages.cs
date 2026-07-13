namespace Learnix.Application.AiChat.Constants;

public static class AiChatMessages
{
    public const string InstructorNotFound = "No instructor matches the given name or id.";

    public static string UnknownSection(string section) =>
        $"Unknown section '{section}'. Allowed sections: {string.Join(", ", LearningProfileSections.All)}.";

    public const string InstructorLookupRequired =
        "Either instructorName or instructorId must be provided.";

    public const string TestNotSubmitted =
        "The student has not submitted this test yet, so there is nothing to review. "
        + "Do not reveal or guess any answers.";

    public const string TestAttemptInProgress =
        "The student has an attempt open right now. The review stays unavailable until they submit it. "
        + "Do not reveal or guess any answers; offer to explain the underlying topic instead.";

    public const string TestReviewNotAllowed =
        "The instructor of this test does not disclose its answers. Tell the student the review is "
        + "turned off for this test, and do not reveal or guess any answers; offer to explain the "
        + "underlying topic instead.";
}
