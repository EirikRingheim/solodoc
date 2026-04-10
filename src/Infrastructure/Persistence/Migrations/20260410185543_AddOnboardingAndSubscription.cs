using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solodoc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOnboardingAndSubscription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "company_size",
                table: "tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "enabled_modules",
                table: "tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "industry_type",
                table: "tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "max_users",
                table: "tenants",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "onboarding_completed",
                table: "tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "subscription_started_at",
                table: "tenants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "subscription_tier",
                table: "tenants",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "trial_ends_at",
                table: "tenants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "trial_started_at",
                table: "tenants",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "company_size",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "enabled_modules",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "industry_type",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "max_users",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "onboarding_completed",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "subscription_started_at",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "subscription_tier",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "trial_ends_at",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "trial_started_at",
                table: "tenants");
        }
    }
}
