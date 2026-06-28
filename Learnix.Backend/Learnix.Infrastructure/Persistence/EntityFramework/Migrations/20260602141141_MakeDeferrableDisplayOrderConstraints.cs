using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Learnix.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MakeDeferrableDisplayOrderConstraints : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // EF Core issues one UPDATE per row, which temporarily violates the unique
            // constraint between statements. Making the constraints DEFERRABLE INITIALLY DEFERRED
            // moves the uniqueness check to transaction commit time.
            migrationBuilder.Sql("""
                DROP INDEX "IX_Sections_CourseId_DisplayOrder";
                ALTER TABLE "Sections"
                    ADD CONSTRAINT "IX_Sections_CourseId_DisplayOrder"
                    UNIQUE ("CourseId", "DisplayOrder")
                    DEFERRABLE INITIALLY DEFERRED;

                DROP INDEX "IX_Lessons_SectionId_DisplayOrder";
                ALTER TABLE "Lessons"
                    ADD CONSTRAINT "IX_Lessons_SectionId_DisplayOrder"
                    UNIQUE ("SectionId", "DisplayOrder")
                    DEFERRABLE INITIALLY DEFERRED;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Sections" DROP CONSTRAINT "IX_Sections_CourseId_DisplayOrder";
                CREATE UNIQUE INDEX "IX_Sections_CourseId_DisplayOrder"
                    ON "Sections" ("CourseId", "DisplayOrder");

                ALTER TABLE "Lessons" DROP CONSTRAINT "IX_Lessons_SectionId_DisplayOrder";
                CREATE UNIQUE INDEX "IX_Lessons_SectionId_DisplayOrder"
                    ON "Lessons" ("SectionId", "DisplayOrder");
                """);
        }
    }
}
