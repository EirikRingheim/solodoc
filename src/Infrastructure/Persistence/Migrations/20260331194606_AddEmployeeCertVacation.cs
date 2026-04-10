using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solodoc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeCertVacation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "employee_certifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    issued_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    expiry_date = table.Column<DateOnly>(type: "date", nullable: true),
                    file_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    thumbnail_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ocr_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ocr_extracted_expiry = table.Column<DateOnly>(type: "date", nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_employee_certifications", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "internal_trainings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    topic = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    trainer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trainee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    duration_hours = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    signature_file_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_internal_trainings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sick_leave_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    days = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sick_leave_entries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vacation_balances",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    annual_allowance_days = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    carried_over_days = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    used_days = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vacation_balances", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vacation_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    days = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    approved_by_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    approved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vacation_entries", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_employee_certifications_person_id_expiry_date",
                table: "employee_certifications",
                columns: new[] { "person_id", "expiry_date" });

            migrationBuilder.CreateIndex(
                name: "ix_employee_certifications_tenant_id",
                table: "employee_certifications",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_internal_trainings_tenant_id_trainee_id",
                table: "internal_trainings",
                columns: new[] { "tenant_id", "trainee_id" });

            migrationBuilder.CreateIndex(
                name: "ix_internal_trainings_trainer_id",
                table: "internal_trainings",
                column: "trainer_id");

            migrationBuilder.CreateIndex(
                name: "ix_sick_leave_entries_person_id_start_date",
                table: "sick_leave_entries",
                columns: new[] { "person_id", "start_date" });

            migrationBuilder.CreateIndex(
                name: "ix_sick_leave_entries_tenant_id",
                table: "sick_leave_entries",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_vacation_balances_person_id_year_tenant_id",
                table: "vacation_balances",
                columns: new[] { "person_id", "year", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vacation_entries_person_id_start_date",
                table: "vacation_entries",
                columns: new[] { "person_id", "start_date" });

            migrationBuilder.CreateIndex(
                name: "ix_vacation_entries_tenant_id",
                table: "vacation_entries",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "employee_certifications");

            migrationBuilder.DropTable(
                name: "internal_trainings");

            migrationBuilder.DropTable(
                name: "sick_leave_entries");

            migrationBuilder.DropTable(
                name: "vacation_balances");

            migrationBuilder.DropTable(
                name: "vacation_entries");
        }
    }
}
