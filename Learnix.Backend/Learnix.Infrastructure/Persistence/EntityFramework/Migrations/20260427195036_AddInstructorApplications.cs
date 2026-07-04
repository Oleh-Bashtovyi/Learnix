using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Learnix.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInstructorApplications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Order",
                table: "Sections",
                newName: "DisplayOrder");

            migrationBuilder.RenameIndex(
                name: "IX_Sections_CourseId_Order",
                table: "Sections",
                newName: "IX_Sections_CourseId_DisplayOrder");

            migrationBuilder.RenameColumn(
                name: "VideoUrl",
                table: "Lessons",
                newName: "VideoBlobPath");

            migrationBuilder.RenameColumn(
                name: "Order",
                table: "Lessons",
                newName: "DisplayOrder");

            migrationBuilder.RenameIndex(
                name: "IX_Lessons_SectionId_Order",
                table: "Lessons",
                newName: "IX_Lessons_SectionId_DisplayOrder");

            migrationBuilder.RenameColumn(
                name: "CoverImageUrl",
                table: "Courses",
                newName: "CoverBlobPath");

            migrationBuilder.RenameColumn(
                name: "AvatarUrl",
                table: "AspNetUsers",
                newName: "AvatarBlobPath");

            migrationBuilder.AddColumn<bool>(
                name: "IsHidden",
                table: "Lessons",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Questions",
                table: "Lessons",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InstructorApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    MotivationText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    PortfolioUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RejectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReviewedByAdminId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstructorApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InstructorApplications_AspNetUsers_ReviewedByAdminId",
                        column: x => x.ReviewedByAdminId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InstructorApplications_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LastAttemptAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    NextRetryAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InstructorApplications_ReviewedByAdminId",
                table: "InstructorApplications",
                column: "ReviewedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_InstructorApplications_UserId",
                table: "InstructorApplications",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Processing",
                table: "OutboxMessages",
                columns: new[] { "ProcessedAt", "NextRetryAt", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InstructorApplications");

            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "IsHidden",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "Questions",
                table: "Lessons");

            migrationBuilder.RenameColumn(
                name: "DisplayOrder",
                table: "Sections",
                newName: "Order");

            migrationBuilder.RenameIndex(
                name: "IX_Sections_CourseId_DisplayOrder",
                table: "Sections",
                newName: "IX_Sections_CourseId_Order");

            migrationBuilder.RenameColumn(
                name: "VideoBlobPath",
                table: "Lessons",
                newName: "VideoUrl");

            migrationBuilder.RenameColumn(
                name: "DisplayOrder",
                table: "Lessons",
                newName: "Order");

            migrationBuilder.RenameIndex(
                name: "IX_Lessons_SectionId_DisplayOrder",
                table: "Lessons",
                newName: "IX_Lessons_SectionId_Order");

            migrationBuilder.RenameColumn(
                name: "CoverBlobPath",
                table: "Courses",
                newName: "CoverImageUrl");

            migrationBuilder.RenameColumn(
                name: "AvatarBlobPath",
                table: "AspNetUsers",
                newName: "AvatarUrl");
        }
    }
}
