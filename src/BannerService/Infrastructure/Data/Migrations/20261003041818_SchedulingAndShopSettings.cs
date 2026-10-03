using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BannerService.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SchedulingAndShopSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultBoardBackground",
                table: "Shops",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DefaultBoardLogoMediaId",
                table: "Shops",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultBoardMessage",
                table: "Shops",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultBoardTextColor",
                table: "Shops",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TimeZoneId",
                table: "Shops",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TimeZoneId",
                table: "Countries",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TimeZoneId",
                table: "Cities",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ActiveDays",
                table: "Banners",
                type: "int",
                nullable: false,
                defaultValue: 127);

            migrationBuilder.AddColumn<int>(
                name: "DailyEndMinutes",
                table: "Banners",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DailyStartMinutes",
                table: "Banners",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultBoardBackground",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "DefaultBoardLogoMediaId",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "DefaultBoardMessage",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "DefaultBoardTextColor",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "TimeZoneId",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "TimeZoneId",
                table: "Countries");

            migrationBuilder.DropColumn(
                name: "TimeZoneId",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "ActiveDays",
                table: "Banners");

            migrationBuilder.DropColumn(
                name: "DailyEndMinutes",
                table: "Banners");

            migrationBuilder.DropColumn(
                name: "DailyStartMinutes",
                table: "Banners");
        }
    }
}
