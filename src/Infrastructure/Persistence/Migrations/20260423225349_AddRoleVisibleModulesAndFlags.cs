using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solodoc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleVisibleModulesAndFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "feature_flag_overrides",
                table: "custom_roles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "visible_modules",
                table: "custom_roles",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "feature_flag_overrides",
                table: "custom_roles");

            migrationBuilder.DropColumn(
                name: "visible_modules",
                table: "custom_roles");
        }
    }
}
