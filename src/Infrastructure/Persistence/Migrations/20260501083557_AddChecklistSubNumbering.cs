using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solodoc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChecklistSubNumbering : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "parent_item_id",
                table: "checklist_template_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "sub_number",
                table: "checklist_template_items",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "parent_item_id",
                table: "checklist_template_items");

            migrationBuilder.DropColumn(
                name: "sub_number",
                table: "checklist_template_items");
        }
    }
}
