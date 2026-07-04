using static Learnix.DbMigrator.Seeders.Demo.CourseSeeders.SeedHelpers;

namespace Learnix.DbMigrator.Seeders.Demo.CourseSeeders;

internal static class GitVersionControlSeeder
{
    public static SeedCourseDefinition GetDefinition() => new(
        "programming",
        "Git Version Control",
        "Learn the essentials of Git and GitHub to manage your code history.",
        0m,
        ["git", "github", "vcs", "tools"],
        [
            new("Git Basics",
            [
                new SeedVideo("What is Version Control?", "Why we need to track changes in software development."),
                new SeedPost("Basic Commands", "init, add, commit, and status."),
            ]),
            new("Branching and Merging",
            [
                new SeedVideo("Working with Branches", "Creating and switching branches for feature development."),
                new SeedTest("Git Quiz",
                    [
                        SC("Which command is used to save changes locally?", "git commit", "git push", "git save", "git upload"),
                        SC("Which command downloads a repository from a remote server?", "git clone", "git download", "git fetch", "git pull")
                    ],
                    PassingThreshold: 50,
                    AttemptLimit: 3,
                    CooldownMinutes: 1)
            ])
        ],
        "generic_thumbnail.png");
}


