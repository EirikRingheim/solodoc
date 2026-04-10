using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solodoc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftsAndOvertimeRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "overtime_stacking_mode",
                table: "tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "timebank_enabled",
                table: "tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "priority",
                table: "allowance_rules",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "shift_definition_id",
                table: "allowance_rules",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "stacks_with_overtime",
                table: "allowance_rules",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "overtime_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    rate_percent = table.Column<decimal>(type: "numeric", nullable: false),
                    applicable_days = table.Column<string>(type: "text", nullable: true),
                    applies_to_red_days = table.Column<bool>(type: "boolean", nullable: false),
                    applies_to_saturday = table.Column<bool>(type: "boolean", nullable: false),
                    applies_to_sunday = table.Column<bool>(type: "boolean", nullable: false),
                    applies_to_weekdays = table.Column<bool>(type: "boolean", nullable: false),
                    time_range_start = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    time_range_end = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    shift_definition_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_overtime_rules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "rotation_patterns",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    cycle_length_days = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rotation_patterns", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "shift_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    color = table.Column<string>(type: "text", nullable: false),
                    is_work_day = table.Column<bool>(type: "boolean", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    break_minutes = table.Column<int>(type: "integer", nullable: false),
                    normal_hours = table.Column<decimal>(type: "numeric", nullable: false),
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
                    table.PrimaryKey("pk_shift_definitions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "employee_rotation_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rotation_pattern_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cycle_start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_employee_rotation_assignments", x => x.id);
                    table.ForeignKey(
                        name: "fk_employee_rotation_assignments_rotation_patterns_rotation_pa",
                        column: x => x.rotation_pattern_id,
                        principalTable: "rotation_patterns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rotation_pattern_days",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    rotation_pattern_id = table.Column<Guid>(type: "uuid", nullable: false),
                    day_in_cycle = table.Column<int>(type: "integer", nullable: false),
                    shift_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rotation_pattern_days", x => x.id);
                    table.ForeignKey(
                        name: "fk_rotation_pattern_days_rotation_patterns_rotation_pattern_id",
                        column: x => x.rotation_pattern_id,
                        principalTable: "rotation_patterns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_rotation_pattern_days_shift_definitions_shift_definition_id",
                        column: x => x.shift_definition_id,
                        principalTable: "shift_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_employee_rotation_assignments_rotation_pattern_id",
                table: "employee_rotation_assignments",
                column: "rotation_pattern_id");

            migrationBuilder.CreateIndex(
                name: "ix_rotation_pattern_days_rotation_pattern_id",
                table: "rotation_pattern_days",
                column: "rotation_pattern_id");

            migrationBuilder.CreateIndex(
                name: "ix_rotation_pattern_days_shift_definition_id",
                table: "rotation_pattern_days",
                column: "shift_definition_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "employee_rotation_assignments");

            migrationBuilder.DropTable(
                name: "overtime_rules");

            migrationBuilder.DropTable(
                name: "rotation_pattern_days");

            migrationBuilder.DropTable(
                name: "rotation_patterns");

            migrationBuilder.DropTable(
                name: "shift_definitions");

            migrationBuilder.DropColumn(
                name: "overtime_stacking_mode",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "timebank_enabled",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "priority",
                table: "allowance_rules");

            migrationBuilder.DropColumn(
                name: "shift_definition_id",
                table: "allowance_rules");

            migrationBuilder.DropColumn(
                name: "stacks_with_overtime",
                table: "allowance_rules");
        }
    }
}
