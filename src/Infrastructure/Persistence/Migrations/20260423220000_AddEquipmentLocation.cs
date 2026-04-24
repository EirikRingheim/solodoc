using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solodoc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipmentLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "current_project_id",
                table: "equipment",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "latitude",
                table: "equipment",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "location_description",
                table: "equipment",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "longitude",
                table: "equipment",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "current_project_id",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "latitude",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "location_description",
                table: "equipment");

            migrationBuilder.DropColumn(
                name: "longitude",
                table: "equipment");
        }
    }
}
