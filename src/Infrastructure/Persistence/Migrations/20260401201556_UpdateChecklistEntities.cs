using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solodoc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateChecklistEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "completed_by_id",
                table: "checklist_instances",
                newName: "submitted_by_id");

            migrationBuilder.RenameColumn(
                name: "completed_at",
                table: "checklist_instances",
                newName: "submitted_at");

            migrationBuilder.AddColumn<Guid>(
                name: "base_template_id",
                table: "checklist_templates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "category",
                table: "checklist_templates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "document_number",
                table: "checklist_templates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_base_template",
                table: "checklist_templates",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_locked",
                table: "checklist_templates",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "require_signature",
                table: "checklist_templates",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "signature_count",
                table: "checklist_templates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "signature_roles",
                table: "checklist_templates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "allow_comment",
                table: "checklist_template_items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "allow_photo",
                table: "checklist_template_items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "require_comment_on_irrelevant",
                table: "checklist_template_items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "source",
                table: "checklist_template_items",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "unit_label",
                table: "checklist_template_items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "location_identifier",
                table: "checklist_instances",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "original_snapshot_json",
                table: "checklist_instances",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "reopened_at",
                table: "checklist_instances",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "reopened_by_id",
                table: "checklist_instances",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reopened_reason",
                table: "checklist_instances",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "comment",
                table: "checklist_instance_items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "completed_at",
                table: "checklist_instance_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "irrelevant_comment",
                table: "checklist_instance_items",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_irrelevant",
                table: "checklist_instance_items",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "base_template_id",
                table: "checklist_templates");

            migrationBuilder.DropColumn(
                name: "category",
                table: "checklist_templates");

            migrationBuilder.DropColumn(
                name: "document_number",
                table: "checklist_templates");

            migrationBuilder.DropColumn(
                name: "is_base_template",
                table: "checklist_templates");

            migrationBuilder.DropColumn(
                name: "is_locked",
                table: "checklist_templates");

            migrationBuilder.DropColumn(
                name: "require_signature",
                table: "checklist_templates");

            migrationBuilder.DropColumn(
                name: "signature_count",
                table: "checklist_templates");

            migrationBuilder.DropColumn(
                name: "signature_roles",
                table: "checklist_templates");

            migrationBuilder.DropColumn(
                name: "allow_comment",
                table: "checklist_template_items");

            migrationBuilder.DropColumn(
                name: "allow_photo",
                table: "checklist_template_items");

            migrationBuilder.DropColumn(
                name: "require_comment_on_irrelevant",
                table: "checklist_template_items");

            migrationBuilder.DropColumn(
                name: "source",
                table: "checklist_template_items");

            migrationBuilder.DropColumn(
                name: "unit_label",
                table: "checklist_template_items");

            migrationBuilder.DropColumn(
                name: "location_identifier",
                table: "checklist_instances");

            migrationBuilder.DropColumn(
                name: "original_snapshot_json",
                table: "checklist_instances");

            migrationBuilder.DropColumn(
                name: "reopened_at",
                table: "checklist_instances");

            migrationBuilder.DropColumn(
                name: "reopened_by_id",
                table: "checklist_instances");

            migrationBuilder.DropColumn(
                name: "reopened_reason",
                table: "checklist_instances");

            migrationBuilder.DropColumn(
                name: "comment",
                table: "checklist_instance_items");

            migrationBuilder.DropColumn(
                name: "completed_at",
                table: "checklist_instance_items");

            migrationBuilder.DropColumn(
                name: "irrelevant_comment",
                table: "checklist_instance_items");

            migrationBuilder.DropColumn(
                name: "is_irrelevant",
                table: "checklist_instance_items");

            migrationBuilder.RenameColumn(
                name: "submitted_by_id",
                table: "checklist_instances",
                newName: "completed_by_id");

            migrationBuilder.RenameColumn(
                name: "submitted_at",
                table: "checklist_instances",
                newName: "completed_at");
        }
    }
}
