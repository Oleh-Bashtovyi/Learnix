using Learnix.Domain.Common;
using Learnix.Domain.Common.Exceptions;
using Learnix.Domain.Events.TestAttempts;
using Learnix.Domain.ValueObjects;

namespace Learnix.Domain.Entities;

public class TestAttempt : BaseEntity
{
    private List<StudentAnswer> _answers = [];

    private TestAttempt() { }

    private TestAttempt(Guid courseId, Guid testLessonId, Guid studentId, int attemptNumber)
    {
        CourseId = courseId;
        TestLessonId = testLessonId;
        StudentId = studentId;
        AttemptNumber = attemptNumber;
        StartedAt = DateTime.UtcNow;
    }

    public Guid CourseId { get; private set; }
    public Guid TestLessonId { get; private set; }
    public Guid StudentId { get; private set; }
    public int AttemptNumber { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? SubmittedAt { get; private set; }
    public int? Score { get; private set; }
    public int? MaxScore { get; private set; }
    public bool? Passed { get; private set; }
    public IReadOnlyList<StudentAnswer> Answers => _answers.AsReadOnly();
    public bool IsSubmitted => SubmittedAt.HasValue;

    public static TestAttempt Create(Guid courseId, Guid testLessonId, Guid studentId, int attemptNumber)
        => new(courseId, testLessonId, studentId, attemptNumber);

    public void Submit(
        IReadOnlyList<StudentAnswer> answers,
        int score,
        int maxScore,
        int passingThreshold)
    {
        if (IsSubmitted)
            throw new DomainException("Test attempt has already been submitted.");

        if (score < 0 || maxScore < 0 || score > maxScore)
            throw new DomainException("Invalid test score.");

        _answers.Clear();
        _answers.AddRange(answers);

        Score = score;
        MaxScore = maxScore;
        SubmittedAt = DateTime.UtcNow;

        var percent = maxScore == 0 ? 0 : (int)Math.Round(score * 100m / maxScore, MidpointRounding.AwayFromZero);
        Passed = percent >= passingThreshold;

        var durationSeconds = (int)Math.Max(0, (SubmittedAt!.Value - StartedAt).TotalSeconds);
        RaiseDomainEvent(new TestSubmittedDomainEvent(
            StudentId, TestLessonId, Id, maxScore, durationSeconds, Passed.Value));
    }
}
