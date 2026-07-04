namespace Learnix.Domain.Constants;

public static class Roles
{
    public const string Student = nameof(Student);
    public const string Instructor = nameof(Instructor);
    public const string Admin = nameof(Admin);

    public static IReadOnlyList<string> All { get; } = [Student, Instructor, Admin];
}
