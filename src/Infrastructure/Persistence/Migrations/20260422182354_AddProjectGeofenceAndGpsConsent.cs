using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solodoc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectGeofenceAndGpsConsent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "geofence_geo_json",
                table: "projects",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "geofence_radius_meters",
                table: "projects",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "geofence_geo_json",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "geofence_radius_meters",
                table: "projects");
        }
    }
}
