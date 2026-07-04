using Learnix.Domain.Common.Exceptions;
using Learnix.Domain.Enums;

namespace Learnix.Domain.ValueObjects;

public sealed class Question
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Text { get; init; } = null!;
    public QuestionType Type { get; init; }
    public int Order { get; init; }
    public IReadOnlyList<QuestionOption> Options { get; init; } = new List<QuestionOption>();
    public TextAnswerConfig? TextAnswer { get; init; }

    // Scoring: 1 if correct, 0 otherwise. Answers reference options by Order (stable, persisted int).
    public bool IsAnsweredCorrectly(StudentAnswer answer) => Type switch
    {
        QuestionType.SingleChoice =>
            answer.SelectedOptionOrders.Count == 1 &&
            Options.Single(o => o.IsCorrect).Order == answer.SelectedOptionOrders[0],

        QuestionType.MultipleChoice =>
            answer.SelectedOptionOrders.ToHashSet().SetEquals(
                Options.Where(o => o.IsCorrect).Select(o => o.Order)),

        QuestionType.TextInput =>
            EvaluateTextAnswer(answer.TextValue ?? ""),

        _ => throw new DomainException($"Unknown question type: {Type}")
    };

    private bool EvaluateTextAnswer(string given)
    {
        if (TextAnswer is null) return false;
        var correct = TextAnswer.CorrectAnswer;
        if (TextAnswer.IgnoreCase)
        {
            given = given.Trim().ToLowerInvariant();
            correct = correct.Trim().ToLowerInvariant();
        }
        if (given == correct) return true;
        if (TextAnswer.AllowFuzzy) return IsFuzzyMatch(given, correct);
        return false;
    }

    private static bool IsFuzzyMatch(string a, string b)
    {
        // Simple Levenshtein threshold — 1 edit for short answers, 2 for longer
#pragma warning disable S3358
        int threshold = b.Length > 2 ? 0 : (b.Length <= 5 ? 1 : 2);
#pragma warning restore S3358
        return LevenshteinDistance(a, b) <= threshold;
    }

    private static int LevenshteinDistance(string a, string b)
    {
        int[,] dp = new int[a.Length + 1, b.Length + 1];
        for (int i = 0; i <= a.Length; i++) dp[i, 0] = i;
        for (int j = 0; j <= b.Length; j++) dp[0, j] = j;
        for (int i = 1; i <= a.Length; i++)
            for (int j = 1; j <= b.Length; j++)
                dp[i, j] = a[i - 1] == b[j - 1]
                    ? dp[i - 1, j - 1]
                    : 1 + Math.Min(dp[i - 1, j - 1], Math.Min(dp[i - 1, j], dp[i, j - 1]));
        return dp[a.Length, b.Length];
    }
}

public sealed class QuestionOption
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Text { get; init; } = null!;
    public bool IsCorrect { get; init; }
    public int Order { get; init; }
}

public sealed class TextAnswerConfig
{
    public string CorrectAnswer { get; init; } = null!;
    public bool IgnoreCase { get; init; }
    public bool AllowFuzzy { get; init; }
}

public sealed record StudentAnswer(int QuestionOrder, List<int> SelectedOptionOrders, string? TextValue);
