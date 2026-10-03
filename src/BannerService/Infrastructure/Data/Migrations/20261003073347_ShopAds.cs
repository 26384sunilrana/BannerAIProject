using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BannerService.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ShopAds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShopAdEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShopAdId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShopId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    At = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopAdEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShopAds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShopId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    AdvertiserName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Headline = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    MediaFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Background = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    TextColor = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Placement = table.Column<int>(type: "int", nullable: false),
                    SpacePercent = table.Column<int>(type: "int", nullable: false),
                    PopupSeconds = table.Column<int>(type: "int", nullable: false),
                    PopupEveryMinutes = table.Column<int>(type: "int", nullable: false),
                    StartAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DailyStartMinutes = table.Column<int>(type: "int", nullable: true),
                    DailyEndMinutes = table.Column<int>(type: "int", nullable: true),
                    ActiveDays = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DecidedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DecidedByName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DecidedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DecisionNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopAds", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShopAdEvents_ShopAdId_At",
                table: "ShopAdEvents",
                columns: new[] { "ShopAdId", "At" });

            migrationBuilder.CreateIndex(
                name: "IX_ShopAds_ShopId_Status_StartAt",
                table: "ShopAds",
                columns: new[] { "ShopId", "Status", "StartAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ShopAds_Status_StartAt",
                table: "ShopAds",
                columns: new[] { "Status", "StartAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShopAdEvents");

            migrationBuilder.DropTable(
                name: "ShopAds");
        }
    }
}
