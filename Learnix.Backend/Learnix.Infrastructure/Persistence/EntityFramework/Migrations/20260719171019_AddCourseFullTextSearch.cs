using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace Learnix.Infrastructure.Persistence.EntityFramework.Migrations;

/// <inheritdoc />
public partial class AddCourseFullTextSearch : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<NpgsqlTsVector>(
            name: "SearchVector",
            table: "Courses",
            type: "tsvector",
            nullable: true,
            computedColumnSql: "setweight(to_tsvector('english'::regconfig, coalesce(\"Title\", '')), 'A') ||\nsetweight(to_tsvector('english'::regconfig, coalesce(\"Description\", '')), 'B') ||\nsetweight(array_to_tsvector(coalesce(\"Tags\", ARRAY[]::text[])), 'C')",
            stored: true);

        migrationBuilder.CreateIndex(
            name: "IX_Courses_SearchVector",
            table: "Courses",
            column: "SearchVector")
            .Annotation("Npgsql:IndexMethod", "gin");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Courses_SearchVector",
            table: "Courses");

        migrationBuilder.DropColumn(
            name: "SearchVector",
            table: "Courses");
    }
}
