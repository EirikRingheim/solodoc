using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solodoc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateShiftAndHolidayFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "project_id",
                table: "shift_definitions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_enabled",
                table: "public_holidays",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "rate_percent",
                table: "public_holidays",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "public_holidays",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "project_id",
                table: "shift_definitions");

            migrationBuilder.DropColumn(
                name: "is_enabled",
                table: "public_holidays");

            migrationBuilder.DropColumn(
                name: "rate_percent",
                table: "public_holidays");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "public_holidays");
        }
    }
}
