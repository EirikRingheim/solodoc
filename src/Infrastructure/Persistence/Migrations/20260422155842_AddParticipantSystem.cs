using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solodoc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddParticipantSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE vacation_entries ALTER COLUMN approved_by_id TYPE uuid USING approved_by_id::uuid;");

            migrationBuilder.AlterColumn<Guid>(
                name: "person_id",
                table: "sja_participants",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "external_company",
                table: "sja_participants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "external_name",
                table: "sja_participants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "external_phone",
                table: "sja_participants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_external",
                table: "sja_participants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "checklist_participants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    checklist_instance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_external = table.Column<bool>(type: "boolean", nullable: false),
                    external_name = table.Column<string>(type: "text", nullable: true),
                    external_phone = table.Column<string>(type: "text", nullable: true),
                    external_company = table.Column<string>(type: "text", nullable: true),
                    signature_file_key = table.Column<string>(type: "text", nullable: true),
                    signed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_checklist_participants", x => x.id);
                    table.ForeignKey(
                        name: "fk_checklist_participants_checklist_instances_checklist_instan",
                        column: x => x.checklist_instance_id,
                        principalTable: "checklist_instances",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_checklist_participants_checklist_instance_id",
                table: "checklist_participants",
                column: "checklist_instance_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "checklist_participants");

            migrationBuilder.DropColumn(
                name: "external_company",
                table: "sja_participants");

            migrationBuilder.DropColumn(
                name: "external_name",
                table: "sja_participants");

            migrationBuilder.DropColumn(
                name: "external_phone",
                table: "sja_participants");

            migrationBuilder.DropColumn(
                name: "is_external",
                table: "sja_participants");

            migrationBuilder.AlterColumn<string>(
                name: "approved_by_id",
                table: "vacation_entries",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "person_id",
                table: "sja_participants",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
