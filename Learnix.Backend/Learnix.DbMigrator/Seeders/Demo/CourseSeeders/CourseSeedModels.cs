using Learnix.Domain.Constants;
using Learnix.Domain.Enums;
using Learnix.Domain.ValueObjects;

namespace Learnix.DbMigrator.Seeders.Demo.CourseSeeders;

internal abstract record SeedLesson(string Title);

internal record SeedPost(string Title, string Content) : SeedLesson(Title);

internal record SeedVideo(string Title, string? Description = null) : SeedLesson(Title);

internal record SeedTest(
    string Title,
    QuestionBlueprint[] Questions,
    string? Description = null,
    int PassingThreshold = LessonConstants.DefaultPassingThreshold,
    int? AttemptLimit = null,
    int? CooldownMinutes = null) : SeedLesson(Title);

internal record SeedSection(string Title, SeedLesson[] Lessons);

internal record SeedCourseDefinition(
    string CategorySlug,
    string Title,
    string Description,
    decimal Price,
    string[] Tags,
    SeedSection[] Sections,
    string ImageName);

internal static class SeedHelpers
{
    public static QuestionBlueprint SC(string text, string correct, params string[] wrong)
        => new(text, QuestionType.SingleChoice,
            wrong.Prepend(correct)
                 .Select((t, i) => new QuestionOptionBlueprint(t, i == 0))
                 .ToArray(),
            null);

    public static QuestionBlueprint MC(string text, string[] correct, string[] wrong)
        => new(text, QuestionType.MultipleChoice,
            correct.Select(t => new QuestionOptionBlueprint(t, true))
                   .Concat(wrong.Select(t => new QuestionOptionBlueprint(t, false)))
                   .ToArray(),
            null);

    public static QuestionBlueprint TI(string text, string answer,
        bool ignoreCase = true, bool fuzzy = false)
        => new(text, QuestionType.TextInput, null,
            new TextAnswerBlueprint(answer, ignoreCase, fuzzy));
}


