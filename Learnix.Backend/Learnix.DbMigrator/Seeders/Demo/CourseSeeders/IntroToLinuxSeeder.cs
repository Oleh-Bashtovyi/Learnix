using static Learnix.DbMigrator.Seeders.Demo.CourseSeeders.SeedHelpers;

namespace Learnix.DbMigrator.Seeders.Demo.CourseSeeders;

internal static class IntroToLinuxSeeder
{
    public static SeedCourseDefinition GetDefinition() => new(
        "programming",
        "Introduction to Linux",
        "Master the command line and basic Linux administration.",
        0m,
        ["linux", "os", "command-line", "bash"],
        [
            new("The Command Line",
            [
                new SeedVideo("Navigating the File System", "cd, ls, pwd, and more."),
                new SeedPost("File Permissions", "Understanding read, write, and execute permissions (chmod)."),
            ]),
            new("Package Management",
            [
                new SeedVideo("Installing Software", "apt, yum, and pacman basics."),
                new SeedTest("Linux Quiz",
                    [
                        SC("Which command shows your current directory path?", "pwd", "cd", "ls", "dir"),
                        SC("Which command is used to change file permissions?", "chmod", "chown", "chgrp", "mod")
                    ],
                    PassingThreshold: 50,
                    AttemptLimit: 3,
                    CooldownMinutes: 1)
            ])
        ],
        "generic_thumbnail.png");
}


