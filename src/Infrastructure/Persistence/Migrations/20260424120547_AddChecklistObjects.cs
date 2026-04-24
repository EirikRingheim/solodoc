using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solodoc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChecklistObjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "checklist_object_id",
                table: "checklist_instances",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "checklist_objects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    number = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_checklist_objects", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "checklist_object_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    checklist_object_id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_checklist_object_templates", x => x.id);
                    table.ForeignKey(
                        name: "fk_checklist_object_templates_checklist_objects_checklist_obje",
                        column: x => x.checklist_object_id,
                        principalTable: "checklist_objects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_checklist_object_templates_checklist_templates_template_id",
                        column: x => x.template_id,
                        principalTable: "checklist_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_checklist_instances_checklist_object_id",
                table: "checklist_instances",
                column: "checklist_object_id");

            migrationBuilder.CreateIndex(
                name: "ix_checklist_object_templates_checklist_object_id",
                table: "checklist_object_templates",
                column: "checklist_object_id");

            migrationBuilder.CreateIndex(
                name: "ix_checklist_object_templates_template_id",
                table: "checklist_object_templates",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "ix_checklist_objects_project_id",
                table: "checklist_objects",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_checklist_objects_project_id_name_number",
                table: "checklist_objects",
                columns: new[] { "project_id", "name", "number" });

            migrationBuilder.AddForeignKey(
                name: "fk_checklist_instances_checklist_objects_checklist_object_id",
                table: "checklist_instances",
                column: "checklist_object_id",
                principalTable: "checklist_objects",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_checklist_instances_checklist_objects_checklist_object_id",
                table: "checklist_instances");

            migrationBuilder.DropTable(
                name: "checklist_object_templates");

            migrationBuilder.DropTable(
                name: "checklist_objects");

            migrationBuilder.DropIndex(
                name: "ix_checklist_instances_checklist_object_id",
                table: "checklist_instances");

            migrationBuilder.DropColumn(
                name: "checklist_object_id",
                table: "checklist_instances");
        }
    }
}
