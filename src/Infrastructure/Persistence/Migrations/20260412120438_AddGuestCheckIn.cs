using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solodoc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGuestCheckIn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "person_id",
                table: "worksite_check_ins",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "guest_company",
                table: "worksite_check_ins",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "guest_name",
                table: "worksite_check_ins",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "guest_phone",
                table: "worksite_check_ins",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_guest",
                table: "worksite_check_ins",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "guest_company",
                table: "worksite_check_ins");

            migrationBuilder.DropColumn(
                name: "guest_name",
                table: "worksite_check_ins");

            migrationBuilder.DropColumn(
                name: "guest_phone",
                table: "worksite_check_ins");

            migrationBuilder.DropColumn(
                name: "is_guest",
                table: "worksite_check_ins");

            migrationBuilder.AlterColumn<Guid>(
                name: "person_id",
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
