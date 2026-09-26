namespace BannerService.Infrastructure.Data.Migrations;

using Microsoft.EntityFrameworkCore.Migrations;

public partial class AddCarousels : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Create Carousels table
        migrationBuilder.CreateTable(
            name: "Carousels",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                BannerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                IntervalMs = table.Column<int>(type: "int", nullable: false),
                TransitionDuration = table.Column<int>(type: "int", nullable: false),
                TransitionType = table.Column<int>(type: "int", nullable: false),
                IsAutoplay = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                Loop = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Carousels", x => x.Id);
                table.ForeignKey(
                    name: "FK_Carousels_Banners_BannerId",
                    column: x => x.BannerId,
                    principalTable: "Banners",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        // Create CarouselComponents junction table
        migrationBuilder.CreateTable(
            name: "CarouselComponents",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CarouselId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ComponentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Order = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CarouselComponents", x => x.Id);
                table.ForeignKey(
                    name: "FK_CarouselComponents_Carousels_CarouselId",
                    column: x => x.CarouselId,
                    principalTable: "Carousels",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_CarouselComponents_Components_ComponentId",
                    column: x => x.ComponentId,
                    principalTable: "Components",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        // Create indexes
        migrationBuilder.CreateIndex(
            name: "IX_Carousels_BannerId",
            table: "Carousels",
            column: "BannerId");

        migrationBuilder.CreateIndex(
            name: "IX_CarouselComponents_Query",
            table: "CarouselComponents",
            columns: new[] { "CarouselId", "Order" });

        migrationBuilder.CreateIndex(
            name: "UQ_CarouselComponents_Unique",
            table: "CarouselComponents",
            columns: new[] { "CarouselId", "ComponentId" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "CarouselComponents");

        migrationBuilder.DropTable(
            name: "Carousels");
    }
}
