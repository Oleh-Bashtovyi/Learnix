using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using NpgsqlTypes;

#nullable disable

namespace Learnix.Infrastructure.Persistence.EntityFramework.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AspNetRoles",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetRoles", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUsers",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Language = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false, defaultValue: "en"),
                AvatarBlobPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                Bio = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                GoogleId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                PurgeAfter = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                PasswordHash = table.Column<string>(type: "text", nullable: true),
                SecurityStamp = table.Column<string>(type: "text", nullable: true),
                ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                PhoneNumber = table.Column<string>(type: "text", nullable: true),
                PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUsers", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Categories",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Slug = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                ImageBlobPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                CoursesCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Categories", x => x.Id);
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

        migrationBuilder.CreateTable(
            name: "AspNetRoleClaims",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                ClaimType = table.Column<string>(type: "text", nullable: true),
                ClaimValue = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                table.ForeignKey(
                    name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                    column: x => x.RoleId,
                    principalTable: "AspNetRoles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUserClaims",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                ClaimType = table.Column<string>(type: "text", nullable: true),
                ClaimValue = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                table.ForeignKey(
                    name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUserLogins",
            columns: table => new
            {
                LoginProvider = table.Column<string>(type: "text", nullable: false),
                ProviderKey = table.Column<string>(type: "text", nullable: false),
                ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                UserId = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                table.ForeignKey(
                    name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUserRoles",
            columns: table => new
            {
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                RoleId = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                table.ForeignKey(
                    name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                    column: x => x.RoleId,
                    principalTable: "AspNetRoles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "AspNetUserTokens",
            columns: table => new
            {
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                LoginProvider = table.Column<string>(type: "text", nullable: false),
                Name = table.Column<string>(type: "text", nullable: false),
                Value = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                table.ForeignKey(
                    name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

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
            name: "Notifications",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                Type = table.Column<int>(type: "integer", nullable: false),
                Parameters = table.Column<string>(type: "jsonb", nullable: true),
                IsRead = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Notifications", x => x.Id);
                table.ForeignKey(
                    name: "FK_Notifications_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "RefreshTokens",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                IsRevoked = table.Column<bool>(type: "boolean", nullable: false),
                RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                table.ForeignKey(
                    name: "FK_RefreshTokens_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "UserAchievementProgresses",
            columns: table => new
            {
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                LessonsCompleted = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                CoursesCompleted = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                DistinctCategoriesCompleted = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                ProfileCompleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserAchievementProgresses", x => x.UserId);
                table.ForeignKey(
                    name: "FK_UserAchievementProgresses_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "UserAchievements",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                UnlockedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Seen = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserAchievements", x => x.Id);
                table.ForeignKey(
                    name: "FK_UserAchievements_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Courses",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                InstructorId = table.Column<Guid>(type: "uuid", nullable: false),
                CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                CoverBlobPath = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                Price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                EnrollmentsCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                AverageRating = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false, defaultValue: 0m),
                ReviewsCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                Tags = table.Column<List<string>>(type: "text[]", nullable: false),
                SearchVector = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: true, computedColumnSql: "setweight(to_tsvector('english'::regconfig, coalesce(\"Title\", '')), 'A') ||\nsetweight(to_tsvector('english'::regconfig, coalesce(\"Description\", '')), 'B') ||\nsetweight(array_to_tsvector(coalesce(\"Tags\", ARRAY[]::text[])), 'C')", stored: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Courses", x => x.Id);
                table.ForeignKey(
                    name: "FK_Courses_Categories_CategoryId",
                    column: x => x.CategoryId,
                    principalTable: "Categories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "UserCompletedCategories",
            columns: table => new
            {
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserCompletedCategories", x => new { x.UserId, x.CategoryId });
                table.ForeignKey(
                    name: "FK_UserCompletedCategories_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_UserCompletedCategories_Categories_CategoryId",
                    column: x => x.CategoryId,
                    principalTable: "Categories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "CourseConversations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                InstructorId = table.Column<Guid>(type: "uuid", nullable: false),
                StudentUnreadCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                InstructorUnreadCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                LastMessagePreview = table.Column<string>(type: "character varying(103)", maxLength: 103, nullable: true),
                LastMessageAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CourseConversations", x => x.Id);
                table.ForeignKey(
                    name: "FK_CourseConversations_AspNetUsers_InstructorId",
                    column: x => x.InstructorId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_CourseConversations_AspNetUsers_StudentId",
                    column: x => x.StudentId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_CourseConversations_Courses_CourseId",
                    column: x => x.CourseId,
                    principalTable: "Courses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "CourseReviews",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                Rating = table.Column<int>(type: "integer", nullable: false),
                Comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                CompletedLessonsAtReview = table.Column<int>(type: "integer", nullable: false),
                TotalLessonsAtReview = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CourseReviews", x => x.Id);
                table.ForeignKey(
                    name: "FK_CourseReviews_AspNetUsers_StudentId",
                    column: x => x.StudentId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_CourseReviews_Courses_CourseId",
                    column: x => x.CourseId,
                    principalTable: "Courses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Enrollments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                PaymentStatus = table.Column<int>(type: "integer", nullable: false),
                PricePaid = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                EnrolledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Enrollments", x => x.Id);
                table.ForeignKey(
                    name: "FK_Enrollments_Courses_CourseId",
                    column: x => x.CourseId,
                    principalTable: "Courses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Sections",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Sections", x => x.Id);
                table.ForeignKey(
                    name: "FK_Sections_Courses_CourseId",
                    column: x => x.CourseId,
                    principalTable: "Courses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "WishlistItems",
            columns: table => new
            {
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WishlistItems", x => new { x.UserId, x.CourseId });
                table.ForeignKey(
                    name: "FK_WishlistItems_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_WishlistItems_Courses_CourseId",
                    column: x => x.CourseId,
                    principalTable: "Courses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "CourseMessages",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                SenderId = table.Column<Guid>(type: "uuid", nullable: false),
                Content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CourseMessages", x => x.Id);
                table.ForeignKey(
                    name: "FK_CourseMessages_AspNetUsers_SenderId",
                    column: x => x.SenderId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_CourseMessages_CourseConversations_ConversationId",
                    column: x => x.ConversationId,
                    principalTable: "CourseConversations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Certificates",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                EnrollmentId = table.Column<Guid>(type: "uuid", nullable: false),
                Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                FilePath = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                IssuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Certificates", x => x.Id);
                table.ForeignKey(
                    name: "FK_Certificates_Courses_CourseId",
                    column: x => x.CourseId,
                    principalTable: "Courses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Certificates_Enrollments_EnrollmentId",
                    column: x => x.EnrollmentId,
                    principalTable: "Enrollments",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Payments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                EnrollmentId = table.Column<Guid>(type: "uuid", nullable: false),
                Amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                PaymentProvider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Payments", x => x.Id);
                table.ForeignKey(
                    name: "FK_Payments_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Payments_Courses_CourseId",
                    column: x => x.CourseId,
                    principalTable: "Courses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Payments_Enrollments_EnrollmentId",
                    column: x => x.EnrollmentId,
                    principalTable: "Enrollments",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Lessons",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                SectionId = table.Column<Guid>(type: "uuid", nullable: false),
                Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                IsHidden = table.Column<bool>(type: "boolean", nullable: false),
                LessonType = table.Column<int>(type: "integer", nullable: false),
                Content = table.Column<string>(type: "character varying(50000)", maxLength: 50000, nullable: true),
                Description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                AttemptLimit = table.Column<int>(type: "integer", nullable: true),
                CooldownMinutes = table.Column<int>(type: "integer", nullable: true),
                PassingThreshold = table.Column<int>(type: "integer", nullable: true),
                CurrentVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                QuestionsCount = table.Column<int>(type: "integer", nullable: true),
                ReviewMode = table.Column<int>(type: "integer", nullable: true),
                VideoBlobPath = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                VideoLesson_Description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                DurationSeconds = table.Column<int>(type: "integer", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Lessons", x => x.Id);
                table.ForeignKey(
                    name: "FK_Lessons_Sections_SectionId",
                    column: x => x.SectionId,
                    principalTable: "Sections",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "LessonProgresses",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                LessonId = table.Column<Guid>(type: "uuid", nullable: false),
                StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                LastAccessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LessonProgresses", x => x.Id);
                table.ForeignKey(
                    name: "FK_LessonProgresses_Courses_CourseId",
                    column: x => x.CourseId,
                    principalTable: "Courses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_LessonProgresses_Lessons_LessonId",
                    column: x => x.LessonId,
                    principalTable: "Lessons",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "TestVersions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TestLessonId = table.Column<Guid>(type: "uuid", nullable: false),
                VersionNumber = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Questions = table.Column<string>(type: "jsonb", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TestVersions", x => x.Id);
                table.ForeignKey(
                    name: "FK_TestVersions_Lessons_TestLessonId",
                    column: x => x.TestLessonId,
                    principalTable: "Lessons",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "TestAttempts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                TestLessonId = table.Column<Guid>(type: "uuid", nullable: false),
                TestVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                AttemptNumber = table.Column<int>(type: "integer", nullable: false),
                StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                Score = table.Column<int>(type: "integer", nullable: true),
                MaxScore = table.Column<int>(type: "integer", nullable: true),
                Passed = table.Column<bool>(type: "boolean", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Answers = table.Column<string>(type: "jsonb", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TestAttempts", x => x.Id);
                table.ForeignKey(
                    name: "FK_TestAttempts_Courses_CourseId",
                    column: x => x.CourseId,
                    principalTable: "Courses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_TestAttempts_Lessons_TestLessonId",
                    column: x => x.TestLessonId,
                    principalTable: "Lessons",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_TestAttempts_TestVersions_TestVersionId",
                    column: x => x.TestVersionId,
                    principalTable: "TestVersions",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateIndex(
            name: "IX_AspNetRoleClaims_RoleId",
            table: "AspNetRoleClaims",
            column: "RoleId");

        migrationBuilder.CreateIndex(
            name: "RoleNameIndex",
            table: "AspNetRoles",
            column: "NormalizedName",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_AspNetUserClaims_UserId",
            table: "AspNetUserClaims",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_AspNetUserLogins_UserId",
            table: "AspNetUserLogins",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_AspNetUserRoles_RoleId",
            table: "AspNetUserRoles",
            column: "RoleId");

        migrationBuilder.CreateIndex(
            name: "EmailIndex",
            table: "AspNetUsers",
            column: "NormalizedEmail");

        migrationBuilder.CreateIndex(
            name: "IX_AspNetUsers_GoogleId",
            table: "AspNetUsers",
            column: "GoogleId",
            unique: true,
            filter: "\"GoogleId\" IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_AspNetUsers_PurgeAfter",
            table: "AspNetUsers",
            column: "PurgeAfter",
            filter: "\"PurgeAfter\" IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "UserNameIndex",
            table: "AspNetUsers",
            column: "NormalizedUserName",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Categories_Name",
            table: "Categories",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Categories_Slug",
            table: "Categories",
            column: "Slug",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Certificates_Code",
            table: "Certificates",
            column: "Code",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Certificates_CourseId",
            table: "Certificates",
            column: "CourseId");

        migrationBuilder.CreateIndex(
            name: "IX_Certificates_EnrollmentId",
            table: "Certificates",
            column: "EnrollmentId");

        migrationBuilder.CreateIndex(
            name: "IX_Certificates_StudentId_CourseId",
            table: "Certificates",
            columns: new[] { "StudentId", "CourseId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_CourseConversations_CourseId_StudentId",
            table: "CourseConversations",
            columns: new[] { "CourseId", "StudentId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_CourseConversations_InstructorId",
            table: "CourseConversations",
            column: "InstructorId");

        migrationBuilder.CreateIndex(
            name: "IX_CourseConversations_StudentId",
            table: "CourseConversations",
            column: "StudentId");

        migrationBuilder.CreateIndex(
            name: "IX_CourseMessages_ConversationId",
            table: "CourseMessages",
            column: "ConversationId");

        migrationBuilder.CreateIndex(
            name: "IX_CourseMessages_CreatedAt",
            table: "CourseMessages",
            column: "CreatedAt");

        migrationBuilder.CreateIndex(
            name: "IX_CourseMessages_SenderId",
            table: "CourseMessages",
            column: "SenderId");

        migrationBuilder.CreateIndex(
            name: "IX_CourseReviews_CourseId",
            table: "CourseReviews",
            column: "CourseId");

        migrationBuilder.CreateIndex(
            name: "IX_CourseReviews_StudentId_CourseId",
            table: "CourseReviews",
            columns: new[] { "StudentId", "CourseId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Courses_CategoryId",
            table: "Courses",
            column: "CategoryId");

        migrationBuilder.CreateIndex(
            name: "IX_Courses_InstructorId",
            table: "Courses",
            column: "InstructorId");

        migrationBuilder.CreateIndex(
            name: "IX_Courses_SearchVector",
            table: "Courses",
            column: "SearchVector")
            .Annotation("Npgsql:IndexMethod", "gin");

        migrationBuilder.CreateIndex(
            name: "IX_Courses_Status",
            table: "Courses",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_Enrollments_CourseId",
            table: "Enrollments",
            column: "CourseId");

        migrationBuilder.CreateIndex(
            name: "IX_Enrollments_StudentId",
            table: "Enrollments",
            column: "StudentId");

        migrationBuilder.CreateIndex(
            name: "IX_Enrollments_StudentId_CourseId",
            table: "Enrollments",
            columns: new[] { "StudentId", "CourseId" },
            unique: true);

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
            name: "IX_LessonProgresses_CourseId",
            table: "LessonProgresses",
            column: "CourseId");

        migrationBuilder.CreateIndex(
            name: "IX_LessonProgresses_LessonId",
            table: "LessonProgresses",
            column: "LessonId");

        migrationBuilder.CreateIndex(
            name: "IX_LessonProgresses_StudentId",
            table: "LessonProgresses",
            column: "StudentId");

        migrationBuilder.CreateIndex(
            name: "IX_LessonProgresses_StudentId_LessonId",
            table: "LessonProgresses",
            columns: new[] { "StudentId", "LessonId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Lessons_SectionId",
            table: "Lessons",
            column: "SectionId");

        migrationBuilder.CreateIndex(
            name: "IX_Notifications_UserId_CreatedAt",
            table: "Notifications",
            columns: new[] { "UserId", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_OutboxMessages_Processing",
            table: "OutboxMessages",
            columns: new[] { "ProcessedAt", "NextRetryAt", "OccurredAt" });

        migrationBuilder.CreateIndex(
            name: "IX_Payments_CourseId",
            table: "Payments",
            column: "CourseId");

        migrationBuilder.CreateIndex(
            name: "IX_Payments_EnrollmentId",
            table: "Payments",
            column: "EnrollmentId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Payments_UserId",
            table: "Payments",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_RefreshTokens_TokenHash",
            table: "RefreshTokens",
            column: "TokenHash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_RefreshTokens_UserId",
            table: "RefreshTokens",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_Sections_CourseId",
            table: "Sections",
            column: "CourseId");

        migrationBuilder.CreateIndex(
            name: "IX_TestAttempts_CourseId",
            table: "TestAttempts",
            column: "CourseId");

        migrationBuilder.CreateIndex(
            name: "IX_TestAttempts_OneInProgress",
            table: "TestAttempts",
            columns: new[] { "StudentId", "TestLessonId" },
            unique: true,
            filter: "\"SubmittedAt\" IS NULL");

        migrationBuilder.CreateIndex(
            name: "IX_TestAttempts_TestLessonId",
            table: "TestAttempts",
            column: "TestLessonId");

        migrationBuilder.CreateIndex(
            name: "IX_TestAttempts_TestVersionId",
            table: "TestAttempts",
            column: "TestVersionId");

        migrationBuilder.CreateIndex(
            name: "IX_TestVersions_LessonVersion",
            table: "TestVersions",
            columns: new[] { "TestLessonId", "VersionNumber" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_UserAchievements_UserId_Code",
            table: "UserAchievements",
            columns: new[] { "UserId", "Code" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_UserCompletedCategories_CategoryId",
            table: "UserCompletedCategories",
            column: "CategoryId");

        migrationBuilder.CreateIndex(
            name: "IX_UserCompletedCategories_UserId",
            table: "UserCompletedCategories",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_WishlistItems_CourseId",
            table: "WishlistItems",
            column: "CourseId");

        migrationBuilder.CreateIndex(
            name: "IX_WishlistItems_UserId",
            table: "WishlistItems",
            column: "UserId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "AspNetRoleClaims");

        migrationBuilder.DropTable(
            name: "AspNetUserClaims");

        migrationBuilder.DropTable(
            name: "AspNetUserLogins");

        migrationBuilder.DropTable(
            name: "AspNetUserRoles");

        migrationBuilder.DropTable(
            name: "AspNetUserTokens");

        migrationBuilder.DropTable(
            name: "Certificates");

        migrationBuilder.DropTable(
            name: "CourseMessages");

        migrationBuilder.DropTable(
            name: "CourseReviews");

        migrationBuilder.DropTable(
            name: "InstructorApplications");

        migrationBuilder.DropTable(
            name: "LessonProgresses");

        migrationBuilder.DropTable(
            name: "Notifications");

        migrationBuilder.DropTable(
            name: "OutboxMessages");

        migrationBuilder.DropTable(
            name: "Payments");

        migrationBuilder.DropTable(
            name: "RefreshTokens");

        migrationBuilder.DropTable(
            name: "TestAttempts");

        migrationBuilder.DropTable(
            name: "UserAchievementProgresses");

        migrationBuilder.DropTable(
            name: "UserAchievements");

        migrationBuilder.DropTable(
            name: "UserCompletedCategories");

        migrationBuilder.DropTable(
            name: "WishlistItems");

        migrationBuilder.DropTable(
            name: "AspNetRoles");

        migrationBuilder.DropTable(
            name: "CourseConversations");

        migrationBuilder.DropTable(
            name: "Enrollments");

        migrationBuilder.DropTable(
            name: "TestVersions");

        migrationBuilder.DropTable(
            name: "AspNetUsers");

        migrationBuilder.DropTable(
            name: "Lessons");

        migrationBuilder.DropTable(
            name: "Sections");

        migrationBuilder.DropTable(
            name: "Courses");

        migrationBuilder.DropTable(
            name: "Categories");
    }
}
