using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BannerService.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdRatesAndPricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PricePerHour",
                table: "ShopAds",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ShopSharePercent",
                table: "ShopAds",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StoppedAt",
                table: "ShopAds",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AdRates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    CountryCode = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    StateId = table.Column<int>(type: "int", nullable: true),
                    CityId = table.Column<int>(type: "int", nullable: true),
                    Kind = table.Column<int>(type: "int", nullable: true),
                    PricePerHour = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ShopSharePercent = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdRates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdRates_Level_CountryCode_StateId_CityId_Kind",
                table: "AdRates",
                columns: new[] { "Level", "CountryCode", "StateId", "CityId", "Kind" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdRates");

            migrationBuilder.DropColumn(
                name: "PricePerHour",
                table: "ShopAds");

            migrationBuilder.DropColumn(
                name: "ShopSharePercent",
                table: "ShopAds");

            migrationBuilder.DropColumn(
                name: "StoppedAt",
                table: "ShopAds");
        }
    }
}
