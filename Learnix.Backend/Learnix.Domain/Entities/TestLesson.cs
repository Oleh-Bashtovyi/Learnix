using Learnix.Domain.Common.Exceptions;
using Learnix.Domain.Constants;
using Learnix.Domain.Enums;
using Learnix.Domain.ValueObjects;

namespace Learnix.Domain.Entities;

public class TestLesson : Lesson
{
    private TestLesson() { }

    private TestLesson(
        Guid sectionId,
        string title,
        string? description,
        int? attemptLimit,
        int? cooldownMinutes,
        int passingThreshold,
        TestReviewMode reviewMode)
        : base(sectionId, title, LessonType.Test)
    {
        Description = description;
        AttemptLimit = attemptLimit;
        CooldownMinutes = cooldownMinutes;
        PassingThreshold = passingThreshold;
        ReviewMode = reviewMode;
    }

    private List<Question> _questions = [];
    public IReadOnlyList<Question> Questions => _questions;
    public string? Description { get; private set; }
    public int? AttemptLimit { get; private set; }
    public int? CooldownMinutes { get; private set; }
    public int PassingThreshold { get; private set; }

    /// <summary>How much of a submitted attempt the student may see back. See <see cref="TestReviewMode"/>.</summary>
    public TestReviewMode ReviewMode { get; private set; } = TestReviewMode.FullReview;

    public int MaxScore => _questions.Count;

    public static TestLesson Create(
        Guid sectionId, string title,
        string? description = null,
        int? attemptLimit = null,
        int? cooldownMinutes = null,
        int passingThreshold = LessonConstants.DefaultPassingThreshold,
        TestReviewMode reviewMode = TestReviewMode.FullReview)
        => new(sectionId, title, description, attemptLimit, cooldownMinutes, passingThreshold, reviewMode);

    public void ReplaceQuestions(IReadOnlyList<QuestionBlueprint> blueprints)
    {
        if (blueprints.Count == 0)
            throw new DomainException("Test must have at least one question.");

        _questions = blueprints.Select((bp, index) => BuildQuestion(bp, index)).ToList();

        EvaluateVisibility();
    }

    public int Score(IEnumerable<StudentAnswer> answers)
    {
        var map = answers.ToDictionary(a => a.QuestionOrder);
        return _questions.Count(q =>
            map.TryGetValue(q.Order, out var answer) && q.IsAnsweredCorrectly(answer));
    }

    public void UpdateTest(
        string title,
        string? description,
        int? attemptLimit,
        int? cooldownMinutes,
        int passingThreshold,
        TestReviewMode reviewMode,
        IReadOnlyList<QuestionBlueprint> blueprints)
    {
        UpdateTitle(title);
        UpdateSettings(description, attemptLimit, cooldownMinutes, passingThreshold, reviewMode);
        ReplaceQuestions(blueprints);
    }

    public void UpdateSettings(
        string? description, int? attemptLimit,
        int? cooldownMinutes, int passingThreshold,
        TestReviewMode reviewMode)
    {
        Description = description;
        AttemptLimit = attemptLimit;
        CooldownMinutes = cooldownMinutes;
        PassingThreshold = passingThreshold;
        ReviewMode = reviewMode;
    }

    public override bool IsPublishReady() => _questions.Count > 0;

    private static Question BuildQuestion(QuestionBlueprint bp, int order) => bp.Type switch
    {
        QuestionType.SingleChoice or QuestionType.MultipleChoice =>
            BuildChoiceQuestion(bp, order),

        QuestionType.TextInput =>
            BuildTextQuestion(bp, order),

        _ => throw new DomainException($"Unknown question type: {bp.Type}")
    };

    private static Question BuildChoiceQuestion(QuestionBlueprint bp, int order)
    {
        if (bp.Options is null || bp.Options.Count == 0)
            throw new DomainException("Choice question must have options.");

        if (bp.TextAnswer is not null)
            throw new DomainException("Choice question cannot have a text answer config.");

        var options = bp.Options.Select((o, i) => new QuestionOption
        {
            Text = o.Text,
            IsCorrect = o.IsCorrect,
            Order = i
        }).ToList();

        ValidateChoiceOptions(options, bp.Type);

        return new Question
        {
            Text = bp.Text,
            Type = bp.Type,
            Order = order,
            Options = options
        };
    }

    private static Question BuildTextQuestion(QuestionBlueprint bp, int order)
    {
        if (bp.TextAnswer is null)
            throw new DomainException("TextInput question must have a text answer config.");

        if (bp.Options is not null && bp.Options.Count > 0)
            throw new DomainException("TextInput question cannot have options.");

        return new Question
        {
            Text = bp.Text,
            Type = bp.Type,
            Order = order,
            TextAnswer = new TextAnswerConfig
            {
                CorrectAnswer = bp.TextAnswer.CorrectAnswer,
                IgnoreCase = bp.TextAnswer.IgnoreCase,
                AllowFuzzy = bp.TextAnswer.AllowFuzzy
            }
        };
    }

    private static void ValidateChoiceOptions(List<QuestionOption> options, QuestionType type)
    {
        if (options.Count < QuestionConstants.MinOptionsPerChoiceQuestion)
            throw new DomainException(
                $"Choice question must have at least {QuestionConstants.MinOptionsPerChoiceQuestion} options.");

        if (options.Count > QuestionConstants.MaxOptionsPerChoiceQuestion)
            throw new DomainException(
                $"Choice question cannot have more than {QuestionConstants.MaxOptionsPerChoiceQuestion} options.");

        if (options.Any(o => string.IsNullOrWhiteSpace(o.Text)))
            throw new DomainException("Option text cannot be empty.");

        int correctCount = options.Count(o => o.IsCorrect);

        if (type == QuestionType.SingleChoice && correctCount != 1)
            throw new DomainException("SingleChoice question must have exactly one correct option.");

        if (type == QuestionType.MultipleChoice && correctCount < 1)
            throw new DomainException("MultipleChoice question must have at least one correct option.");
    }
}
