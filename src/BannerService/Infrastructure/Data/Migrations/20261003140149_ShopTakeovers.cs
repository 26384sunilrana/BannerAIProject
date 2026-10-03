using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BannerService.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ShopTakeovers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ApprovalReviewRequired",
                table: "Shops",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ShopTakeovers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExistingShopId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NewShopId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NewOwnerUserId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    OldOwnerUserId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Associates = table.Column<int>(type: "int", nullable: true),
                    DecidedByUserId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    DecidedByName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DecidedByAdmin = table.Column<bool>(type: "bit", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DecidedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopTakeovers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShopTakeovers_ExistingShopId_Status",
                table: "ShopTakeovers",
                columns: new[] { "ExistingShopId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ShopTakeovers_NewShopId",
                table: "ShopTakeovers",
                column: "NewShopId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShopTakeovers");

            migrationBuilder.DropColumn(
                name: "ApprovalReviewRequired",
                table: "Shops");
        }
    }
}
