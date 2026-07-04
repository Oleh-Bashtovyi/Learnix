using Learnix.Domain.Enums;

namespace Learnix.Domain.ValueObjects;

public sealed record QuestionBlueprint(
    string Text,
    QuestionType Type,
    IReadOnlyList<QuestionOptionBlueprint>? Options,
    TextAnswerBlueprint? TextAnswer);

public sealed record QuestionOptionBlueprint(string Text, bool IsCorrect);
public sealed record TextAnswerBlueprint(string CorrectAnswer, bool IgnoreCase, bool AllowFuzzy);
