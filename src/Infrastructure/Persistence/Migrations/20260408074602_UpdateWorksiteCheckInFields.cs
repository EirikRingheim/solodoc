using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solodoc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateWorksiteCheckInFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "project_id",
                table: "worksite_check_ins",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "job_id",
                table: "worksite_check_ins",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "latitude_out",
                table: "worksite_check_ins",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "location_id",
                table: "worksite_check_ins",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "longitude_out",
                table: "worksite_check_ins",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source",
                table: "worksite_check_ins",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "qr_code_slug",
                table: "locations",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "job_id",
                table: "worksite_check_ins");

            migrationBuilder.DropColumn(
                name: "latitude_out",
                table: "worksite_check_ins");

            migrationBuilder.DropColumn(
                name: "location_id",
                table: "worksite_check_ins");

            migrationBuilder.DropColumn(
                name: "longitude_out",
                table: "worksite_check_ins");

            migrationBuilder.DropColumn(
                name: "source",
                table: "worksite_check_ins");

            migrationBuilder.DropColumn(
                name: "qr_code_slug",
                table: "locations");

            migrationBuilder.AlterColumn<Guid>(
                name: "project_id",
                table: "worksite_check_ins",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
