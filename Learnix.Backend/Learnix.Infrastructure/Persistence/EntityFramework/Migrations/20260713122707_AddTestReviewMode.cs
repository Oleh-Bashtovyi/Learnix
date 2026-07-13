using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Learnix.Infrastructure.Persistence.EntityFramework.Migrations;

/// <inheritdoc />
public partial class AddTestReviewMode : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "ReviewMode",
            table: "Lessons",
            type: "integer",
            nullable: true);

        // Lessons is a TPH table, so a column only TestLesson has must be nullable in the database —
        // but TestLesson.ReviewMode is not nullable, and reading a NULL back into it would throw on
        // the first query. Every test that already exists was authored when the platform disclosed
        // everything at submission time, so FullReview (3) is not just a safe default: it is what
        // those tests have been doing all along. LessonType 2 is Test.
        migrationBuilder.Sql(
            @"UPDATE ""Lessons"" SET ""ReviewMode"" = 3 WHERE ""LessonType"" = 2 AND ""ReviewMode"" IS NULL;");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ReviewMode",
            table: "Lessons");
    }
}
