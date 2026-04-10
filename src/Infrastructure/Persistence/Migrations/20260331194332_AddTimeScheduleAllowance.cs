using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solodoc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTimeScheduleAllowance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "break_minutes",
                table: "time_entries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "category",
                table: "time_entries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "end_time",
                table: "time_entries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "gps_latitude_in",
                table: "time_entries",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "gps_latitude_out",
                table: "time_entries",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "gps_longitude_in",
                table: "time_entries",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "gps_longitude_out",
                table: "time_entries",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_manual",
                table: "time_entries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "job_id",
                table: "time_entries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "notes",
                table: "time_entries",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "overtime_hours",
                table: "time_entries",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "start_time",
                table: "time_entries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "status",
                table: "time_entries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "allowance_groups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_allowance_groups", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "allowance_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    amount_type = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    time_range_start = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    time_range_end = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    applicable_days = table.Column<string>(type: "jsonb", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    active_from = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_allowance_rules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "public_holidays",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_half_day = table.Column<bool>(type: "boolean", nullable: false),
                    half_day_cutoff = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_public_holidays", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "work_schedules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    weekly_hours = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    default_break_minutes = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_work_schedules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "allowance_group_members",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    allowance_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_allowance_group_members", x => x.id);
                    table.ForeignKey(
                        name: "fk_allowance_group_members_allowance_groups_allowance_group_id",
                        column: x => x.allowance_group_id,
                        principalTable: "allowance_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "allowance_group_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    allowance_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    allowance_rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_allowance_group_rules", x => x.id);
                    table.ForeignKey(
                        name: "fk_allowance_group_rules_allowance_groups_allowance_group_id",
                        column: x => x.allowance_group_id,
                        principalTable: "allowance_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_allowance_group_rules_allowance_rules_allowance_rule_id",
                        column: x => x.allowance_rule_id,
                        principalTable: "allowance_rules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "time_entry_allowances",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    time_entry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    allowance_rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hours = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_time_entry_allowances", x => x.id);
                    table.ForeignKey(
                        name: "fk_time_entry_allowances_allowance_rules_allowance_rule_id",
                        column: x => x.allowance_rule_id,
                        principalTable: "allowance_rules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_time_entry_allowances_time_entries_time_entry_id",
                        column: x => x.time_entry_id,
                        principalTable: "time_entries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "employee_schedule_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    work_schedule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_employee_schedule_assignments", x => x.id);
                    table.ForeignKey(
                        name: "fk_employee_schedule_assignments_work_schedules_work_schedule_",
                        column: x => x.work_schedule_id,
                        principalTable: "work_schedules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "work_schedule_days",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    work_schedule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    day_of_week = table.Column<int>(type: "integer", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    break_minutes = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_work_schedule_days", x => x.id);
                    table.ForeignKey(
                        name: "fk_work_schedule_days_work_schedules_work_schedule_id",
                        column: x => x.work_schedule_id,
                        principalTable: "work_schedules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_time_entries_tenant_id_status",
                table: "time_entries",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_allowance_group_members_allowance_group_id_person_id",
                table: "allowance_group_members",
                columns: new[] { "allowance_group_id", "person_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_allowance_group_rules_allowance_group_id_allowance_rule_id",
                table: "allowance_group_rules",
                columns: new[] { "allowance_group_id", "allowance_rule_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_allowance_group_rules_allowance_rule_id",
                table: "allowance_group_rules",
                column: "allowance_rule_id");

            migrationBuilder.CreateIndex(
                name: "ix_allowance_groups_tenant_id",
                table: "allowance_groups",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_allowance_rules_tenant_id",
                table: "allowance_rules",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_employee_schedule_assignments_person_id_effective_from",
                table: "employee_schedule_assignments",
                columns: new[] { "person_id", "effective_from" });

            migrationBuilder.CreateIndex(
                name: "ix_employee_schedule_assignments_work_schedule_id",
                table: "employee_schedule_assignments",
                column: "work_schedule_id");

            migrationBuilder.CreateIndex(
                name: "ix_public_holidays_date",
                table: "public_holidays",
                column: "date",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_time_entry_allowances_allowance_rule_id",
                table: "time_entry_allowances",
                column: "allowance_rule_id");

            migrationBuilder.CreateIndex(
                name: "ix_time_entry_allowances_time_entry_id_allowance_rule_id",
                table: "time_entry_allowances",
                columns: new[] { "time_entry_id", "allowance_rule_id" });

            migrationBuilder.CreateIndex(
                name: "ix_work_schedule_days_work_schedule_id_day_of_week",
                table: "work_schedule_days",
                columns: new[] { "work_schedule_id", "day_of_week" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_work_schedules_tenant_id",
                table: "work_schedules",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "allowance_group_members");

            migrationBuilder.DropTable(
                name: "allowance_group_rules");

            migrationBuilder.DropTable(
                name: "employee_schedule_assignments");

            migrationBuilder.DropTable(
                name: "public_holidays");

            migrationBuilder.DropTable(
                name: "time_entry_allowances");

            migrationBuilder.DropTable(
                name: "work_schedule_days");

            migrationBuilder.DropTable(
                name: "allowance_groups");

            migrationBuilder.DropTable(
                name: "allowance_rules");

            migrationBuilder.DropTable(
                name: "work_schedules");

            migrationBuilder.DropIndex(
                name: "ix_time_entries_tenant_id_status",
                table: "time_entries");

            migrationBuilder.DropColumn(
                name: "break_minutes",
                table: "time_entries");

            migrationBuilder.DropColumn(
                name: "category",
                table: "time_entries");

            migrationBuilder.DropColumn(
                name: "end_time",
                table: "time_entries");

            migrationBuilder.DropColumn(
                name: "gps_latitude_in",
                table: "time_entries");

            migrationBuilder.DropColumn(
                name: "gps_latitude_out",
                table: "time_entries");

            migrationBuilder.DropColumn(
                name: "gps_longitude_in",
                table: "time_entries");

            migrationBuilder.DropColumn(
                name: "gps_longitude_out",
                table: "time_entries");

            migrationBuilder.DropColumn(
                name: "is_manual",
                table: "time_entries");

            migrationBuilder.DropColumn(
                name: "job_id",
                table: "time_entries");

            migrationBuilder.DropColumn(
                name: "notes",
                table: "time_entries");

            migrationBuilder.DropColumn(
                name: "overtime_hours",
                table: "time_entries");

            migrationBuilder.DropColumn(
                name: "start_time",
                table: "time_entries");

            migrationBuilder.DropColumn(
                name: "status",
                table: "time_entries");
        }
    }
}
