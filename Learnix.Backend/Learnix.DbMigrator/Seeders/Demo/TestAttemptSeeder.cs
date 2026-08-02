using Learnix.DbMigrator.Constants;
using Learnix.DbMigrator.Seeders.Demo.CourseSeeders;
using Learnix.Domain.Entities;
using Learnix.Domain.Enums;
using Learnix.Domain.ValueObjects;
using Learnix.Infrastructure.Persistence.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Learnix.DbMigrator.Seeders;

/// <summary>
/// Opt-in dev seeder: backfills submitted <see cref="TestAttempt"/> rows for students whose seeded
/// <see cref="LessonProgress"/> already marks one of the seed instructor's test lessons complete, so the
/// instructor analytics "Tests" tab (average score / pass rate per test) has real data to show.
/// <para>
/// <see cref="StudentSeeder"/> marks the first N lessons of a course complete — including test lessons —
/// without ever creating the attempt that completion would normally come from. This seeder closes that
/// gap rather than picking its own students, so lesson completion and test attempts stay consistent.
/// </para>
/// Must run after <see cref="CourseSeeder"/> and <see cref="StudentSeeder"/>.
/// Idempotent — skips if any TestAttempt already exists for the seed instructor's courses.
/// </summary>
public sealed class TestAttemptSeeder(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    ILogger<TestAttemptSeeder> logger) : IDataSeeder
{
    // S2245: this randomness only picks demo answer correctness — nothing here is a secret, a token,
    // or a decision an attacker could exploit, so a PRNG is the right tool.
#pragma warning disable S2245
    private static readonly Random Rng = new();
#pragma warning restore S2245

    private static readonly string[] WrongTextAnswers = ["n/a", "not sure", "unknown", "incorrect"];

    /// <summary>The demo courses with test lessons, picked by referencing their real definitions
    /// rather than duplicating title strings that would silently drift out of sync.</summary>
    private static readonly string[] TargetCourseTitles =
    [
        CSharpFundamentalsSeeder.GetDefinition().Title,
        DesignPatternsSeeder.GetDefinition().Title,
        React19Seeder.GetDefinition().Title,
        PythonDataAnalysisSeeder.GetDefinition().Title
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var email = configuration[$"{ConfigurationSectionNameConstants.SeedData}:InstructorEmail"];
        if (string.IsNullOrWhiteSpace(email))
            return;

        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var instructorId = await db.Users
            .Where(u => u.Email == email)
            .Select(u => (Guid?)u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (instructorId is null)
            return;

        var courses = await db.Courses
            .Where(c => c.InstructorId == instructorId && TargetCourseTitles.Contains(c.Title))
            .Include(c => c.Sections)
            .ThenInclude(s => s.Lessons)
            .ToListAsync(cancellationToken);

        if (courses.Count == 0)
            return;

        var courseIds = courses.Select(c => c.Id).ToList();

        var alreadySeeded = await db.TestAttempts
            .AnyAsync(a => courseIds.Contains(a.CourseId), cancellationToken);

        if (alreadySeeded)
        {
            logger.LogInformation("Test attempt seeder: attempts already exist — skipping.");
            return;
        }

        var lessonsWithCourse = courses
            .SelectMany(c => c.Sections.SelectMany(s => s.Lessons)
                .OfType<TestLesson>()
                .Where(t => t.CurrentVersionId.HasValue)
                .Select(t => (Course: c, TestLesson: t)))
            .ToList();

        if (lessonsWithCourse.Count == 0)
            return;

        var versionIds = lessonsWithCourse.Select(x => x.TestLesson.CurrentVersionId!.Value).ToList();
        var versionsById = await db.TestVersions
            .Where(v => versionIds.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id, cancellationToken);

        // The completed-lesson rows StudentSeeder already left behind — the set of (course, lesson,
        // student) triples that should have a submitted attempt but don't.
        var completedProgress = await db.LessonProgresses
            .Where(p => courseIds.Contains(p.CourseId) && p.IsCompleted && p.CompletedAt.HasValue)
            .Select(p => new { p.CourseId, p.LessonId, p.StudentId, CompletedAt = p.CompletedAt!.Value })
            .ToListAsync(cancellationToken);

        var progressByLesson = completedProgress
            .GroupBy(p => p.LessonId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var dateBackfill = new List<(TestAttempt Attempt, DateTime StartedAt, DateTime SubmittedAt)>();
        var attemptCount = 0;

        foreach (var (course, testLesson) in lessonsWithCourse)
        {
            if (!versionsById.TryGetValue(testLesson.CurrentVersionId!.Value, out var version))
                continue;

            if (!progressByLesson.TryGetValue(testLesson.Id, out var candidates))
                continue;

            foreach (var candidate in candidates)
            {
                var answers = version.Questions
                    .Select(q => BuildAnswer(q, Rng.NextDouble() < NextStudentSkill()))
                    .ToList();

                var attempt = TestAttempt.Create(
                    course.Id, testLesson.Id, version.Id, candidate.StudentId, attemptNumber: 1);
                attempt.Submit(answers, version.Score(answers), version.MaxScore, testLesson.PassingThreshold);
                attempt.ClearDomainEvents();

                db.TestAttempts.Add(attempt);

                var submittedAt = candidate.CompletedAt;
                var startedAt = submittedAt.AddMinutes(-Rng.Next(3, 20));
                dateBackfill.Add((attempt, startedAt, submittedAt));
                attemptCount++;
            }
        }

        if (attemptCount == 0)
            return;

        await db.SaveChangesAsync(cancellationToken);

        foreach (var (attempt, startedAt, submittedAt) in dateBackfill)
        {
            db.Entry(attempt).Property(nameof(TestAttempt.StartedAt)).CurrentValue = startedAt;
            db.Entry(attempt).Property(nameof(TestAttempt.SubmittedAt)).CurrentValue = submittedAt;
        }

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Test attempt seeder: seeded {Count} test attempts across {CourseCount} courses.",
            attemptCount, courses.Count);
    }

    /// <summary>A per-attempt probability of answering any given question correctly, so some students
    /// pass comfortably, some fail, and the average/pass-rate per test has a real spread.</summary>
    private static double NextStudentSkill() => 0.3 + (Rng.NextDouble() * 0.65);

    private static StudentAnswer BuildAnswer(Question question, bool correct) => question.Type switch
    {
        QuestionType.SingleChoice => BuildSingleChoiceAnswer(question, correct),
        QuestionType.MultipleChoice => BuildMultipleChoiceAnswer(question, correct),
        QuestionType.TextInput => BuildTextAnswer(question, correct),
        _ => throw new InvalidOperationException($"Unknown question type: {question.Type}")
    };

    private static StudentAnswer BuildSingleChoiceAnswer(Question question, bool correct)
    {
        if (correct)
        {
            var correctOption = question.Options.Single(o => o.IsCorrect);
            return new StudentAnswer(question.Order, [correctOption.Order], null);
        }

        var wrongOptions = question.Options.Where(o => !o.IsCorrect).ToList();
        var picked = wrongOptions[Rng.Next(wrongOptions.Count)];
        return new StudentAnswer(question.Order, [picked.Order], null);
    }

    private static StudentAnswer BuildMultipleChoiceAnswer(Question question, bool correct)
    {
        var correctOrders = question.Options.Where(o => o.IsCorrect).Select(o => o.Order).ToHashSet();

        if (correct)
            return new StudentAnswer(question.Order, [.. correctOrders], null);

        // Flip exactly one option's membership: the resulting set is guaranteed to differ from the
        // correct one, which is what makes this a wrong (not merely a different) answer.
        var toFlip = question.Options[Rng.Next(question.Options.Count)];
        var selected = new HashSet<int>(correctOrders);
        if (!selected.Add(toFlip.Order))
            selected.Remove(toFlip.Order);

        return new StudentAnswer(question.Order, [.. selected], null);
    }

    private static StudentAnswer BuildTextAnswer(Question question, bool correct)
    {
        var textValue = correct
            ? question.TextAnswer!.CorrectAnswer
            : WrongTextAnswers[Rng.Next(WrongTextAnswers.Length)];

        return new StudentAnswer(question.Order, [], textValue);
    }
}
