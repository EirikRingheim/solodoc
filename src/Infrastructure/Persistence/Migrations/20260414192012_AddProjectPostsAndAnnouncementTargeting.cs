using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solodoc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectPostsAndAnnouncementTargeting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "photo_file_key",
                table: "announcements",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "target_person_ids",
                table: "announcements",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "project_posts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    photo_file_key = table.Column<string>(type: "text", nullable: true),
                    is_pinned = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_project_posts", x => x.id);
                    table.ForeignKey(
                        name: "fk_project_posts_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_post_comments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    post_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_project_post_comments", x => x.id);
                    table.ForeignKey(
                        name: "fk_project_post_comments_project_posts_post_id",
                        column: x => x.post_id,
                        principalTable: "project_posts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_project_post_comments_post_id",
                table: "project_post_comments",
                column: "post_id");

            migrationBuilder.CreateIndex(
                name: "ix_project_posts_project_id",
                table: "project_posts",
                column: "project_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "project_post_comments");

            migrationBuilder.DropTable(
                name: "project_posts");

            migrationBuilder.DropColumn(
                name: "photo_file_key",
                table: "announcements");

            migrationBuilder.DropColumn(
                name: "target_person_ids",
                table: "announcements");
        }
    }
}
