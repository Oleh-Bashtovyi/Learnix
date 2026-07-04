using static Learnix.DbMigrator.Seeders.Demo.CourseSeeders.SeedHelpers;

namespace Learnix.DbMigrator.Seeders.Demo.CourseSeeders;

internal static class UiUxDesignSeeder
{
    public static SeedCourseDefinition GetDefinition() => new(
        "design",
        "UI/UX Design Principles",
        "Learn the fundamentals of user interface and user experience design. Covers visual design, information architecture, and prototyping in Figma.",
        14.99m,
        ["design", "ux", "ui", "figma"],
        [
            new("Design Fundamentals",
            [
                new SeedVideo("Introduction to UI/UX Design",
                    "A tour of the UX design process: discovery → definition → ideation → prototyping → testing → launch. We look at real before/after redesigns to see the impact of user-centred design."),
                new SeedPost("The Difference Between UI and UX",
                    "UX (User Experience) is the overall feeling of using a product — is it useful, usable, and delightful? UI (User Interface) is the visual layer — colours, typography, spacing, and interactions. Good UX without good UI is a rough road on a well-planned route. Both disciplines are essential."),
            ]),
            new("Visual Design",
            [
                new SeedVideo("Colour Theory and Typography",
                    "We build a complete colour palette from a single brand colour using HSL, check contrast ratios with a Figma plugin, and pair a geometric sans-serif with a humanist body typeface."),
                new SeedPost("Colour Theory for UI",
                    "Use a primary, secondary, and semantic palette (success/warning/error). Ensure 4.5:1 contrast ratio for body text (WCAG AA) and 3:1 for large text. HSL makes it easy to create tints and shades — adjust L while keeping H consistent. Never rely on colour alone to convey meaning; always add an icon or label."),
                new SeedPost("Typography and Type Hierarchy",
                    "Limit typefaces to two: one for headings, one for body. Use a typographic scale (1.25× ratio). Establish hierarchy through size, weight, and spacing — not colour alone. Aim for 60–80 characters per line. Line height: 1.4–1.6× for body text."),
                new SeedPost("Layout and Spacing Systems",
                    "The 8px grid system aligns elements to multiples of 8 (8, 16, 24, 32, 48px), creating visual rhythm and making hand-off to developers predictable. Use Figma's Auto Layout to make components responsive. Whitespace is not empty space — it gives elements room to breathe and guides attention."),
            ]),
            new("UX Process",
            [
                new SeedVideo("User Research Methods",
                    "A practical guide to choosing the right research method: when to run interviews vs surveys vs usability tests, how many participants you need, and how to synthesise findings into actionable insights."),
                new SeedPost("Conducting User Interviews",
                    "Prepare 5–8 open-ended questions. Start with warm-up questions about the participant's role and context. Avoid leading questions. Use the 5 Whys technique to dig beneath surface answers. Synthesise findings with affinity mapping — cluster quotes and observations into themes."),
                new SeedPost("Creating Wireframes in Figma",
                    "Start with low-fidelity wireframes (grey boxes, placeholder text) to explore structure without getting distracted by visual details. Use Figma Frames, not groups, for responsive layouts. Add Auto Layout to simulate how the design adapts to content changes."),
                new SeedVideo("Prototyping and Usability Testing",
                    "We turn static wireframes into a clickable prototype in Figma using Prototype mode, then run a 5-participant usability test, record findings, and prioritise fixes."),
            ]),
            new("Design Systems",
            [
                new SeedVideo("Building a Design System",
                    "We structure a Figma file into a design system with a type scale, colour tokens, and a component library — then show how to publish it and consume it in a product file."),
                new SeedPost("Components and Variants in Figma",
                    "A Component in Figma is a reusable element. Variants group related states (default, hover, active, disabled) into a single component set, accessible via the properties panel. Use named props (State, Size, Icon) not numbered suffixes. Publish your library so the whole team uses the same source of truth."),
                new SeedPost("Accessibility and WCAG Guidelines",
                    "WCAG 2.1 defines three levels: A (minimum), AA (standard requirement), AAA (enhanced). At AA: 4.5:1 contrast for body text, 3:1 for large text and UI components, all interactive elements reachable by keyboard, images have descriptive alt text. Check contrast during design — not after development."),
            ]),
            new("Design Assessments",
            [
                new SeedTest("Quick Check: Design Basics",
                    [
                        SC("What contrast ratio does WCAG AA require for body text?",
                            "4.5:1", "3:1", "7:1", "2.1:1"),
                        SC("What tool is most widely used for UI design and prototyping today?",
                            "Figma", "Adobe XD", "Sketch", "InVision"),
                        SC("What research method reveals WHY users behave a certain way?",
                            "User interviews", "A/B testing", "Analytics dashboards", "Quantitative surveys"),
                    ],
                    PassingThreshold: 60,
                    AttemptLimit: null),

                new SeedTest("Concept Quiz: Visual Design",
                    [
                        MC("Which are principles used to establish visual hierarchy?",
                            ["Size", "Weight", "Colour", "Spacing"],
                            ["Random placement", "Equal sizing throughout"]),
                        MC("Which are qualitative user research methods?",
                            ["User interviews", "Contextual inquiry", "Diary studies"],
                            ["A/B tests", "Analytics", "Conversion rate tracking"]),
                        MC("Which meet WCAG AA requirements?",
                            ["4.5:1 contrast for body text", "3:1 contrast for large text",
                             "Keyboard-accessible interactive elements"],
                            ["Colour alone used to convey state", "Text embedded in images"]),
                    ],
                    PassingThreshold: 70,
                    AttemptLimit: 3,
                    CooldownMinutes: 2),

                new SeedTest("Comprehensive Exam",
                    [
                        SC("Nielsen's research suggests how many participants for a usability test to surface 85% of issues?",
                            "5", "10", "3", "20"),
                        MC("Which are widely used spacing systems in UI design?",
                            ["4px grid", "8px grid", "Fibonacci-based scale"],
                            ["No fixed spacing system", "100px grid"]),
                        TI("What Figma feature groups multiple states of a component (e.g. default, hover, disabled) into one?",
                            "Variants", ignoreCase: true),
                    ],
                    Description: "Covers usability testing numbers, spacing systems, and Figma Variants.",
                    PassingThreshold: 80,
                    AttemptLimit: 2,
                    CooldownMinutes: 3),

                new SeedTest("Terminology Test",
                    [
                        TI("What CSS colour model uses Hue, Saturation, and Lightness?", "HSL"),
                        TI("What term describes designing products usable by people with disabilities?",
                            "accessibility"),
                        TI("What typeface style has decorative strokes at the ends of letterforms?", "Serif"),
                    ],
                    PassingThreshold: 50,
                    AttemptLimit: 1),

                new SeedTest("Mastery Challenge",
                    [
                        SC("Which principle removes unnecessary elements to reduce cognitive load?",
                            "Minimalism", "Skeuomorphism", "Flat design", "Brutalism"),
                        SC("What contrast ratio is the WCAG AA threshold for large text and UI components?",
                            "3:1", "4.5:1", "7:1", "2:1"),
                        MC("Which are UX design deliverables?",
                            ["Wireframes", "User flow diagrams", "Personas", "Usability test reports"],
                            ["JavaScript source code", "Database schemas", "Server logs"]),
                        TI("What Figma feature makes components adapt to their content size automatically?",
                            "Auto Layout", ignoreCase: true),
                    ],
                    PassingThreshold: 100,
                    AttemptLimit: 5,
                    CooldownMinutes: 1),
            ]),
        ],
        "UI-UX_thumbnail.png");
}


