using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solodoc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProcedureCategoryAndFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "category",
                table: "procedure_templates",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "category",
                table: "procedure_templates");
        }
    }
}
