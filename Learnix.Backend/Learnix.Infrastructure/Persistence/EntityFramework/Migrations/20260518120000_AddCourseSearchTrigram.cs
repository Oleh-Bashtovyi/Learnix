using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Learnix.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseSearchTrigram : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");

            migrationBuilder.Sql(@"
                CREATE INDEX ix_courses_title_trgm
                ON ""Courses"" USING GIN (LOWER(""Title"") gin_trgm_ops);");

            migrationBuilder.Sql(@"
                CREATE INDEX ix_courses_description_trgm
                ON ""Courses"" USING GIN (LOWER(""Description"") gin_trgm_ops);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ix_courses_title_trgm;");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ix_courses_description_trgm;");
        }
    }
}
