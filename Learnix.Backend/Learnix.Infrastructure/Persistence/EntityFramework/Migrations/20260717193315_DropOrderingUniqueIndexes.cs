using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Learnix.Infrastructure.Persistence.EntityFramework.Migrations;

/// <inheritdoc />
public partial class DropOrderingUniqueIndexes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Sections_CourseId_DisplayOrder",
            table: "Sections");

        migrationBuilder.DropIndex(
            name: "IX_Lessons_SectionId_DisplayOrder",
            table: "Lessons");

        migrationBuilder.CreateIndex(
            name: "IX_Sections_CourseId",
            table: "Sections",
            column: "CourseId");

        migrationBuilder.CreateIndex(
            name: "IX_Lessons_SectionId",
            table: "Lessons",
            column: "SectionId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Sections_CourseId",
            table: "Sections");

        migrationBuilder.DropIndex(
            name: "IX_Lessons_SectionId",
            table: "Lessons");

        migrationBuilder.CreateIndex(
            name: "IX_Sections_CourseId_DisplayOrder",
            table: "Sections",
            columns: new[] { "CourseId", "DisplayOrder" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Lessons_SectionId_DisplayOrder",
            table: "Lessons",
            columns: new[] { "SectionId", "DisplayOrder" },
            unique: true);
    }
}
