using static Learnix.DbMigrator.Seeders.Demo.CourseSeeders.SeedHelpers;

namespace Learnix.DbMigrator.Seeders.Demo.CourseSeeders;

internal static class AgileMethodologiesSeeder
{
    public static SeedCourseDefinition GetDefinition() => new(
        "programming",
        "Agile Methodologies",
        "Understand Scrum, Kanban, and modern software delivery.",
        0m,
        ["agile", "scrum", "kanban", "process"],
        [
            new("Agile Principles",
            [
                new SeedVideo("The Agile Manifesto", "Values and principles of Agile development."),
                new SeedPost("Scrum vs Kanban", "Comparing the two most popular Agile frameworks."),
            ]),
            new("Scrum Roles and Artifacts",
            [
                new SeedVideo("The Scrum Master", "Responsibilities of the Scrum Master and Product Owner."),
                new SeedTest("Agile Quiz",
                    [
                        SC("How long does a typical Sprint last?", "1-4 weeks", "1-6 months", "1 year", "1-2 days"),
                        SC("Who is responsible for maximizing the value of the product?", "Product Owner", "Scrum Master", "Development Team", "Project Manager")
                    ],
                    PassingThreshold: 50,
                    AttemptLimit: 3,
                    CooldownMinutes: 1)
            ])
        ],
        "generic_thumbnail.png");
}


