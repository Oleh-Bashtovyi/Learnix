using static Learnix.DbMigrator.Seeders.Demo.CourseSeeders.SeedHelpers;

namespace Learnix.DbMigrator.Seeders.Demo.CourseSeeders;

internal static class HtmlCssBasicsSeeder
{
    public static SeedCourseDefinition GetDefinition() => new(
        "web-development",
        "HTML & CSS Basics",
        "A quick introduction to building web pages with HTML and styling them with CSS.",
        9.99m,
        ["html", "css", "web", "frontend"],
        [
            new("Getting Started",
            [
                new SeedVideo("What is HTML?", "Introduction to HyperText Markup Language."),
                new SeedPost("First HTML Page", "Write your first tags and build a simple structure."),
            ]),
            new("Styling",
            [
                new SeedVideo("Introduction to CSS", "Adding colors, fonts, and layout to your page."),
                new SeedTest("HTML/CSS Quiz",
                    [
                        SC("Which tag is used for the largest heading?", "<h1>", "<h6>", "<header>", "<heading>"),
                        SC("What does CSS stand for?", "Cascading Style Sheets", "Computer Style Sheets", "Creative Style Sheets", "Colorful Style Sheets")
                    ],
                    PassingThreshold: 50,
                    AttemptLimit: 3,
                    CooldownMinutes: 1)
            ])
        ],
        "generic_thumbnail.png");
}


