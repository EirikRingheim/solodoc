using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solodoc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixOrgNumberUniqueFilter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_tenants_org_number",
                table: "tenants");

            migrationBuilder.CreateIndex(
                name: "ix_tenants_org_number",
                table: "tenants",
                column: "org_number",
                unique: true,
                filter: "org_number <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_tenants_org_number",
                table: "tenants");

            migrationBuilder.CreateIndex(
                name: "ix_tenants_org_number",
                table: "tenants",
                column: "org_number",
                unique: true);
        }
    }
}
