using Learnix.Domain.Common;
using Learnix.Domain.Common.Exceptions;
using Learnix.Domain.Constants;
using Learnix.Domain.Enums;
using Learnix.Domain.ValueObjects;

namespace Learnix.Domain.Entities;

/// <summary>
/// One immutable-once-attempted edition of a test's questions.
/// <para>
/// A <see cref="StudentAnswer"/> names its question and its chosen options by <em>position</em>, so a
/// set of answers only means anything against the exact list it was given. A <see cref="TestLesson"/>
/// therefore does not hold its questions: it points at the version that is current, students pin the
/// version they were served, and an instructor's edit branches a new one rather than rewriting the
/// list underneath everyone who has already sat the test (ADR-BACK-LMS-006).
/// </para>
/// <para>
/// Branching only happens when a version has attempts to protect. Until then <see cref="Overwrite"/>
/// reuses the row, so a test that nobody has taken keeps exactly one version no matter how many times
/// it is edited.
/// </para>
/// </summary>
public sealed class TestVersion : BaseEntity
{
    private List<Question> _questions = [];

    private TestVersion() { }

    private TestVersion(Guid testLessonId, int versionNumber, IReadOnlyList<QuestionBlueprint> blueprints)
    {
        TestLessonId = testLessonId;
        VersionNumber = versionNumber;
        _questions = Build(blueprints);
    }

    public Guid TestLessonId { get; private set; }
    public int VersionNumber { get; private set; }
    public IReadOnlyList<Question> Questions => _questions;

    /// <summary>One question, one point.</summary>
    public int MaxScore => _questions.Count;

    /// <summary>The first version of a test that has none yet.</summary>
    public static TestVersion Create(Guid testLessonId, IReadOnlyList<QuestionBlueprint> blueprints)
        => new(testLessonId, InitialVersionNumber, blueprints);

    /// <summary>
    /// The next version of the same test. Call this — rather than <see cref="Overwrite"/> — once an
    /// attempt references this version, so those attempts keep the questions they were taken against.
    /// </summary>
    public TestVersion Branch(IReadOnlyList<QuestionBlueprint> blueprints)
        => new(TestLessonId, VersionNumber + 1, blueprints);

    /// <summary>
    /// Replaces the questions in place, keeping the version number. Only safe while no attempt points
    /// here — the caller owns that check.
    /// </summary>
    public void Overwrite(IReadOnlyList<QuestionBlueprint> blueprints) => _questions = Build(blueprints);

    /// <summary>
    /// Whether these blueprints would produce exactly this version's questions — the test for whether
    /// a save touched the questions at all. An instructor who reorders two questions and saves gets a
    /// new version; one who fixed a typo in the title gets nothing.
    /// </summary>
    public bool Matches(IReadOnlyList<QuestionBlueprint> blueprints)
    {
        if (blueprints.Count != _questions.Count)
            return false;

        // Both sides are position-ordered by construction: Build assigns Order = index, and the JSON
        // column round-trips the array in that order.
        for (int i = 0; i < blueprints.Count; i++)
        {
            if (!Matches(_questions[i], blueprints[i]))
                return false;
        }

        return true;
    }

    /// <summary>
    /// The number of questions these answers got right. Answers for a question that is not in this
    /// version are ignored, and a question with no answer is simply wrong.
    /// </summary>
    public int Score(IEnumerable<StudentAnswer> answers)
    {
        var answersByQuestion = new Dictionary<int, StudentAnswer>();

        // TryAdd, not ToDictionary: a client that sends the same question twice is rejected by the
        // validator, and a scorer is the wrong place to discover it by throwing.
        foreach (var answer in answers)
            answersByQuestion.TryAdd(answer.QuestionOrder, answer);

        return _questions.Count(q =>
            answersByQuestion.TryGetValue(q.Order, out var answer) && q.IsAnsweredCorrectly(answer));
    }

    private const int InitialVersionNumber = 1;

    private static bool Matches(Question question, QuestionBlueprint blueprint)
    {
        if (question.Text != blueprint.Text || question.Type != blueprint.Type)
            return false;

        var options = blueprint.Options ?? [];

        if (question.Options.Count != options.Count)
            return false;

        for (int i = 0; i < options.Count; i++)
        {
            if (question.Options[i].Text != options[i].Text ||
                question.Options[i].IsCorrect != options[i].IsCorrect)
                return false;
        }

        return (question.TextAnswer, blueprint.TextAnswer) switch
        {
            (null, null) => true,
            (not null, not null) =>
                question.TextAnswer.CorrectAnswer == blueprint.TextAnswer.CorrectAnswer &&
                question.TextAnswer.IgnoreCase == blueprint.TextAnswer.IgnoreCase &&
                question.TextAnswer.AllowFuzzy == blueprint.TextAnswer.AllowFuzzy,
            _ => false
        };
    }

    private static List<Question> Build(IReadOnlyList<QuestionBlueprint> blueprints)
    {
        if (blueprints.Count == 0)
            throw new DomainException("Test must have at least one question.");

        return [.. blueprints.Select(BuildQuestion)];
    }

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
