using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solodoc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExportJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "export_jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    target_entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    output_mode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    photo_option = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    selection_json = table.Column<string>(type: "text", nullable: true),
                    result_file_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    result_file_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    result_file_size_bytes = table.Column<long>(type: "bigint", nullable: true),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    requested_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    progress_percent = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_export_jobs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_export_jobs_expires_at",
                table: "export_jobs",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_export_jobs_requested_by_id",
                table: "export_jobs",
                column: "requested_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_export_jobs_status",
                table: "export_jobs",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_export_jobs_tenant_id",
                table: "export_jobs",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "export_jobs");
        }
    }
}
