namespace Learnix.Domain.Constants;

/// <summary>
/// Stable identifiers for achievements. Frontend maps these to icons/copy.
/// Changing a code is a breaking change for already-unlocked rows.
/// </summary>
public static class AchievementCodes
{
    public const string FirstLesson = "FIRST_LESSON";
    public const string Lessons50 = "LESSONS_50";
    public const string Lessons200 = "LESSONS_200";
    public const string Lessons500 = "LESSONS_500";

    public const string FirstCourse = "FIRST_COURSE";
    public const string Courses3 = "COURSES_3";
    public const string Courses5 = "COURSES_5";

    public const string SpeedDemon = "SPEED_DEMON";
    public const string Polymath = "POLYMATH";
    public const string ProfileComplete = "PROFILE_COMPLETE";

    public const int SpeedDemonMinQuestions = 20;
    public const int SpeedDemonMaxDurationSeconds = 5 * 60;
    public const int PolymathMinCategories = 3;
}
