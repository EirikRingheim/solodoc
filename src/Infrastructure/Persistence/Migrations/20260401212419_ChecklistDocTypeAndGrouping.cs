using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solodoc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ChecklistDocTypeAndGrouping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "caption",
                table: "procedure_blocks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "image_file_key",
                table: "procedure_blocks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "document_type",
                table: "checklist_templates",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "group_id",
                table: "checklist_instances",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "group_index",
                table: "checklist_instances",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "group_prefix",
                table: "checklist_instances",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "procedure_read_confirmations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    procedure_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    read_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_procedure_read_confirmations", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "procedure_read_confirmations");

            migrationBuilder.DropColumn(
                name: "caption",
                table: "procedure_blocks");

            migrationBuilder.DropColumn(
                name: "image_file_key",
                table: "procedure_blocks");

            migrationBuilder.DropColumn(
                name: "document_type",
                table: "checklist_templates");

            migrationBuilder.DropColumn(
                name: "group_id",
                table: "checklist_instances");

            migrationBuilder.DropColumn(
                name: "group_index",
                table: "checklist_instances");

            migrationBuilder.DropColumn(
                name: "group_prefix",
                table: "checklist_instances");
        }
    }
}
