namespace BannerService.Infrastructure.Data.Migrations;

using Microsoft.EntityFrameworkCore.Migrations;

public partial class AddBannerVersions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "BannerVersions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                BannerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ShopId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                VersionNumber = table.Column<int>(type: "int", nullable: false),
                SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                ChangeDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BannerVersions", x => x.Id);
                table.ForeignKey(
                    name: "FK_BannerVersions_Banners_BannerId",
                    column: x => x.BannerId,
                    principalTable: "Banners",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "UQ_BannerVersions_Number",
            table: "BannerVersions",
            columns: new[] { "BannerId", "VersionNumber" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_BannerVersions_Query",
            table: "BannerVersions",
            columns: new[] { "BannerId", "ShopId", "VersionNumber" },
            descending: new[] { false, false, true });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "BannerVersions");
    }
}
