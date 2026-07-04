using static Learnix.DbMigrator.Seeders.Demo.CourseSeeders.SeedHelpers;

namespace Learnix.DbMigrator.Seeders.Demo.CourseSeeders;

internal static class DockerForBeginnersSeeder
{
    public static SeedCourseDefinition GetDefinition() => new(
        "programming",
        "Docker for Beginners",
        "Containerize your applications with Docker for consistent environments.",
        0m,
        ["docker", "containers", "devops"],
        [
            new("Introduction to Containers",
            [
                new SeedVideo("VMs vs Containers", "Understanding the difference and why containers are popular."),
                new SeedPost("Installing Docker", "Setup instructions for Windows, Mac, and Linux."),
            ]),
            new("Working with Images",
            [
                new SeedVideo("Writing a Dockerfile", "Step-by-step guide to building your first image."),
                new SeedTest("Docker Quiz",
                    [
                        SC("Which command lists running containers?", "docker ps", "docker ls", "docker containers", "docker run"),
                        SC("What file is used to define a Docker image?", "Dockerfile", "docker-compose.yml", "Imagefile", "Containerfile")
                    ],
                    PassingThreshold: 50,
                    AttemptLimit: 3,
                    CooldownMinutes: 1)
            ])
        ],
        "generic_thumbnail.png");
}


