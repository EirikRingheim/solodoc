using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solodoc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPowerOfficeEmployeeId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "power_office_employee_id",
                table: "persons",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "power_office_employee_id",
                table: "persons");
        }
    }
}
