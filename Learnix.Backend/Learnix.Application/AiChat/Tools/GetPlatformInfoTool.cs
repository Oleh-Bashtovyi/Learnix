using System.Text.Json;
using System.Text.Json.Serialization;
using Learnix.Application.AiChat.Abstractions.Models;

namespace Learnix.Application.AiChat.Tools;

public sealed class GetPlatformInfoTool : IChatTool
{
    private static readonly string ParametersSchema = JsonSerializer.Serialize(new
    {
        type = "object",
        properties = new
        {
            section = new
            {
                type = "string",
                description =
                    "Topic to retrieve. One of: overview, enrollment, lessons, tests, " +
                    "achievements, certificates, becoming_instructor, payment, chat, account. " +
                    "Omit to get a list of available sections.",
                @enum = new[]
                {
                    "overview", "enrollment", "lessons", "tests",
                    "achievements", "certificates", "becoming_instructor",
                    "payment", "chat", "account"
                }
            }
        },
        required = Array.Empty<string>()
    });

    private static readonly IReadOnlyDictionary<string, string> Sections =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["overview"] =
                "Learnix is an online learning management platform. " +
                "Students browse and enroll in courses, complete lessons, take tests, earn achievements and certificates. " +
                "Instructors create course content (video lessons, post lessons, tests). " +
                "Admins moderate the platform — manage users, review instructor applications, oversee courses.",

            ["enrollment"] =
                "How to enroll in a course:\n" +
                "- Browse all published courses on the catalog page. Filter by category, price (free/paid), rating, or sort by newest/most popular.\n" +
                "- Free courses: click Enroll — immediate access, no payment needed.\n" +
                "- Paid courses: click Enroll → payment page (Stripe test mode) → on success, enrollment is created and access is granted.\n" +
                "- Enrolled courses appear in your personal dashboard under 'My Courses'.",

            ["lessons"] =
                "Learnix has three lesson types inside a course:\n" +
                "1. Video lesson — watch video content with a player.\n" +
                "2. Post lesson — read rich markdown-formatted text (bold, italic, lists).\n" +
                "3. Test/Assignment — answer questions and get scored (see the 'tests' section for details).\n" +
                "You can mark each lesson as completed after viewing. You can also like individual lessons.\n" +
                "Lessons are organized into Sections (modules) within a course. Instructors can reorder lessons via drag-and-drop.",

            ["tests"] =
                "Tests are a lesson type that students complete to check their knowledge.\n" +
                "Question types:\n" +
                "- Single choice — pick one correct answer from options.\n" +
                "- Multiple choice — pick one or more correct answers.\n" +
                "- Text input — type a written answer (supports exact match, case-insensitive match, or fuzzy match allowing 1 character error).\n" +
                "After submission: score is shown as a percentage, and correct answers are revealed.\n" +
                "Retry policy (set per test by the instructor):\n" +
                "- Unlimited — retake anytime.\n" +
                "- Limited — N attempts allowed; once exhausted, you must wait for a cooldown period (e.g. 24 hours) before retrying.\n" +
                "A passing threshold (e.g. 70%) may be set per test.",

            ["achievements"] =
                "Achievements are awarded automatically when conditions are met. Each has a name, description, icon, and XP value.\n\n" +
                "Lesson-based:\n" +
                "- First Step — complete your first lesson.\n" +
                "- Getting Started — complete 5 lessons.\n" +
                "- On a Roll — complete 15 lessons.\n" +
                "- Knowledge Seeker — complete 50 lessons.\n\n" +
                "Course-based:\n" +
                "- Course Graduate — complete 1 course.\n" +
                "- Triple Crown — complete 3 courses.\n" +
                "- Scholar — complete 5 courses.\n\n" +
                "Test-based:\n" +
                "- Test Taker — complete your first test.\n" +
                "- Quiz Enthusiast — complete 10 tests.\n" +
                "- Perfect Score — score 100% on any test.\n" +
                "- Speed Runner — complete a test with 20+ questions in under 4 minutes.\n\n" +
                "Other:\n" +
                "- Fan — like 10 lessons.\n" +
                "- Profile Complete — fill in bio and set an avatar.\n" +
                "- Early Adopter — register during the launch period (manual grant).\n\n" +
                "View your earned achievements on your profile page.",

            ["certificates"] =
                "A certificate is generated automatically when you complete all lessons in a course.\n" +
                "The certificate includes: your name, the course name, completion date, and a unique certificate ID.\n" +
                "You can download it as a PDF or share it via a unique URL.\n" +
                "Certificates are visible on your profile page.",

            ["becoming_instructor"] =
                "Any student can apply to become an instructor:\n" +
                "1. Go to your profile and submit a 'Become an Instructor' application.\n" +
                "2. Fill in a motivation text and optionally a portfolio or LinkedIn link.\n" +
                "3. An admin reviews the application — status: Pending → Approved or Rejected.\n" +
                "4. On approval your role is upgraded to Instructor and you receive a notification.\n" +
                "5. On rejection you receive a notification with an optional reason.\n" +
                "Note: you can only have one active application at a time.\n\n" +
                "As an instructor you can: create courses, add sections and lessons (video, post, test), set price, publish/unpublish, and see basic stats (enrolled students, completion rate).",

            ["payment"] =
                "Learnix uses Stripe in test mode for payments (no real money is charged).\n" +
                "Paid course flow: course page → Enroll → payment page → enter a test card number → payment processed.\n" +
                "On success an Enrollment record is created and you gain access to the course.\n" +
                "Payment statuses: Pending, Completed, Failed.\n" +
                "You can view your payment history in your student dashboard.\n" +
                "Admins can view all platform payments.\n" +
                "Free courses (price = 0) skip payment entirely.",

            ["chat"] =
                "Students who are enrolled in a course can message the instructor of that course directly.\n" +
                "It is a 1-on-1 conversation within the context of that specific course — not a global chat.\n" +
                "Instructors can reply to students from their dashboard.\n" +
                "Message history is stored and persisted.",

            ["account"] =
                "Profile settings:\n" +
                "- Edit your display name, avatar image, and bio.\n" +
                "- Set learning preferences (categories you're interested in) — used for course recommendations on the homepage.\n" +
                "- View all earned achievements and certificates.\n\n" +
                "Authentication:\n" +
                "- Register with email + password or via Google OAuth.\n" +
                "- Email confirmation is required after registration (check your inbox; you can resend the confirmation email).\n" +
                "- Forgot your password? Use the 'Forgot password' link — a reset link is sent to your email (valid for 1 hour).\n\n" +
                "Roles:\n" +
                "- Student — default role after registration.\n" +
                "- Instructor — granted after an approved application (see 'becoming_instructor').\n" +
                "- Admin — full platform access, assigned manually."
        };

    private static readonly string SectionsIndex = JsonSerializer.Serialize(new
    {
        available_sections = new[]
        {
            new { section = "overview",               description = "What Learnix is and who it's for" },
            new { section = "enrollment",             description = "How to find and enroll in courses" },
            new { section = "lessons",                description = "Lesson types and how to complete them" },
            new { section = "tests",                  description = "How tests work, question types, retries, scoring" },
            new { section = "achievements",           description = "Full list of achievements and their conditions" },
            new { section = "certificates",           description = "How certificates are earned and shared" },
            new { section = "becoming_instructor",    description = "How to apply and what instructors can do" },
            new { section = "payment",                description = "How paid course enrollment and Stripe work" },
            new { section = "chat",                   description = "Student ↔ instructor messaging" },
            new { section = "account",                description = "Profile, authentication, roles" }
        }
    });

    public string Name => "get_platform_info";

    public ToolDefinition Definition => new(
        Name: "get_platform_info",
        Description:
            "Returns information about how the Learnix platform works — enrollment, lessons, tests, " +
            "achievements, certificates, becoming an instructor, payment, chat, and account features. " +
            "Call this when the user asks how something on the platform works. " +
            "Pass a specific section for focused info, or omit it to get a list of available sections.",
        ParametersJsonSchema: ParametersSchema);

    public Task<string> ExecuteAsync(string argumentsJson, CancellationToken ct)
    {
        SectionArgs? args = null;
        try
        {
            args = JsonSerializer.Deserialize<SectionArgs>(argumentsJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch { }

        if (string.IsNullOrWhiteSpace(args?.Section))
            return Task.FromResult(SectionsIndex);

        if (Sections.TryGetValue(args.Section, out var content))
            return Task.FromResult(JsonSerializer.Serialize(new { section = args.Section, content }));

        return Task.FromResult(JsonSerializer.Serialize(new
        {
            error = $"Unknown section '{args.Section}'",
            available_sections = Sections.Keys.ToArray()
        }));
    }

    private sealed record SectionArgs(
        [property: JsonPropertyName("section")] string? Section);
}
