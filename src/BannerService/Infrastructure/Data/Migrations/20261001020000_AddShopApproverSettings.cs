using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BannerService.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddShopApproverSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(name: "OwnerIsApprover", table: "Shops", type: "bit", nullable: false, defaultValue: true);
            migrationBuilder.AddColumn<string>(name: "ApproverUserIds", table: "Shops", type: "nvarchar(max)", nullable: false, defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "OwnerIsApprover", table: "Shops");
            migrationBuilder.DropColumn(name: "ApproverUserIds", table: "Shops");
        }
    }
}
