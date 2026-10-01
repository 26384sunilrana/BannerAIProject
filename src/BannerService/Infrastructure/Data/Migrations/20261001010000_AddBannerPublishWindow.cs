using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BannerService.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBannerPublishWindow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(name: "PublishStartAt", table: "Banners", type: "datetime2", nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "PublishEndAt", table: "Banners", type: "datetime2", nullable: true);
            migrationBuilder.CreateIndex(name: "IX_Banners_ShopId_PublishWindow", table: "Banners", columns: new[] { "ShopId", "PublishStartAt", "PublishEndAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_Banners_ShopId_PublishWindow", table: "Banners");
            migrationBuilder.DropColumn(name: "PublishStartAt", table: "Banners");
            migrationBuilder.DropColumn(name: "PublishEndAt", table: "Banners");
        }
    }
}
