using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solodoc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExpandDeviationsPhase2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "body_part",
                table: "deviations",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "category_id",
                table: "deviations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "corrective_action",
                table: "deviations",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "corrective_action_completed_at",
                table: "deviations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "corrective_action_deadline",
                table: "deviations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "first_aid_given",
                table: "deviations",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "hospital_visit",
                table: "deviations",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "injury_description",
                table: "deviations",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_confidential",
                table: "deviations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "latitude",
                table: "deviations",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "location_accuracy",
                table: "deviations",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "longitude",
                table: "deviations",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "type",
                table: "deviations",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "deviation_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
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
                    table.PrimaryKey("pk_deviation_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "deviation_comments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    deviation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    text = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    posted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_deviation_comments", x => x.id);
                    table.ForeignKey(
                        name: "fk_deviation_comments_deviations_deviation_id",
                        column: x => x.deviation_id,
                        principalTable: "deviations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "deviation_photos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    deviation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    thumbnail_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_annotated = table.Column<bool>(type: "boolean", nullable: false),
                    is_before_photo = table.Column<bool>(type: "boolean", nullable: false),
                    annotated_file_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_deviation_photos", x => x.id);
                    table.ForeignKey(
                        name: "fk_deviation_photos_deviations_deviation_id",
                        column: x => x.deviation_id,
                        principalTable: "deviations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "deviation_visibilities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    deviation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_deviation_visibilities", x => x.id);
                    table.ForeignKey(
                        name: "fk_deviation_visibilities_deviations_deviation_id",
                        column: x => x.deviation_id,
                        principalTable: "deviations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "related_deviations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    deviation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    related_deviation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_related_deviations", x => x.id);
                    table.ForeignKey(
                        name: "fk_related_deviations_deviations_deviation_id",
                        column: x => x.deviation_id,
                        principalTable: "deviations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_related_deviations_deviations_related_deviation_id",
                        column: x => x.related_deviation_id,
                        principalTable: "deviations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_deviations_category_id",
                table: "deviations",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_deviation_categories_tenant_id",
                table: "deviation_categories",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_deviation_categories_tenant_id_name",
                table: "deviation_categories",
                columns: new[] { "tenant_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_deviation_comments_deviation_id",
                table: "deviation_comments",
                column: "deviation_id");

            migrationBuilder.CreateIndex(
                name: "ix_deviation_photos_deviation_id",
                table: "deviation_photos",
                column: "deviation_id");

            migrationBuilder.CreateIndex(
                name: "ix_deviation_visibilities_deviation_id_person_id",
                table: "deviation_visibilities",
                columns: new[] { "deviation_id", "person_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_deviation_visibilities_person_id",
                table: "deviation_visibilities",
                column: "person_id");

            migrationBuilder.CreateIndex(
                name: "ix_related_deviations_deviation_id_related_deviation_id",
                table: "related_deviations",
                columns: new[] { "deviation_id", "related_deviation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_related_deviations_related_deviation_id",
                table: "related_deviations",
                column: "related_deviation_id");

            migrationBuilder.AddForeignKey(
                name: "fk_deviations_deviation_categories_category_id",
                table: "deviations",
                column: "category_id",
                principalTable: "deviation_categories",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_deviations_deviation_categories_category_id",
                table: "deviations");

            migrationBuilder.DropTable(
                name: "deviation_categories");

            migrationBuilder.DropTable(
                name: "deviation_comments");

            migrationBuilder.DropTable(
                name: "deviation_photos");

            migrationBuilder.DropTable(
                name: "deviation_visibilities");

            migrationBuilder.DropTable(
                name: "related_deviations");

            migrationBuilder.DropIndex(
                name: "ix_deviations_category_id",
                table: "deviations");

            migrationBuilder.DropColumn(
                name: "body_part",
                table: "deviations");

            migrationBuilder.DropColumn(
                name: "category_id",
                table: "deviations");

            migrationBuilder.DropColumn(
                name: "corrective_action",
                table: "deviations");

            migrationBuilder.DropColumn(
                name: "corrective_action_completed_at",
                table: "deviations");

            migrationBuilder.DropColumn(
                name: "corrective_action_deadline",
                table: "deviations");

            migrationBuilder.DropColumn(
                name: "first_aid_given",
                table: "deviations");

            migrationBuilder.DropColumn(
                name: "hospital_visit",
                table: "deviations");

            migrationBuilder.DropColumn(
                name: "injury_description",
                table: "deviations");

            migrationBuilder.DropColumn(
                name: "is_confidential",
                table: "deviations");

            migrationBuilder.DropColumn(
                name: "latitude",
                table: "deviations");

            migrationBuilder.DropColumn(
                name: "location_accuracy",
                table: "deviations");

            migrationBuilder.DropColumn(
                name: "longitude",
                table: "deviations");

            migrationBuilder.DropColumn(
                name: "type",
                table: "deviations");
        }
    }
}
