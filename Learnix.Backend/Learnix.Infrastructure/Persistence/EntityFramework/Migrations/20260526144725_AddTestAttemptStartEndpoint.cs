using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Learnix.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTestAttemptStartEndpoint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Replace the general (StudentId, TestLessonId) index with a partial unique index.
            // The partial index enforces at most one in-progress attempt per student per test,
            // acting as a DB-level race-condition guard for concurrent start calls.
            migrationBuilder.DropIndex(
                name: "IX_TestAttempts_StudentId_TestLessonId",
                table: "TestAttempts");

            migrationBuilder.CreateIndex(
                name: "IX_TestAttempts_OneInProgress",
                table: "TestAttempts",
                columns: new[] { "StudentId", "TestLessonId" },
                unique: true,
                filter: "\"SubmittedAt\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TestAttempts_OneInProgress",
                table: "TestAttempts");

            migrationBuilder.CreateIndex(
                name: "IX_TestAttempts_StudentId_TestLessonId",
                table: "TestAttempts",
                columns: new[] { "StudentId", "TestLessonId" });
        }
    }
}
