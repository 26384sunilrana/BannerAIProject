using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BannerService.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdCompliance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ComplianceApprovedAt",
                table: "ShopAds",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ComplianceApprovedBy",
                table: "ShopAds",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ComplianceNote",
                table: "ShopAds",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ComplianceApprovedAt",
                table: "ShopAds");

            migrationBuilder.DropColumn(
                name: "ComplianceApprovedBy",
                table: "ShopAds");

            migrationBuilder.DropColumn(
                name: "ComplianceNote",
                table: "ShopAds");
        }
    }
}
