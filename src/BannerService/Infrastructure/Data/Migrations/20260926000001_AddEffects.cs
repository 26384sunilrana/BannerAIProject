namespace BannerService.Infrastructure.Data.Migrations;

using Microsoft.EntityFrameworkCore.Migrations;

public partial class AddEffects : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Add CarouselId to Components
        migrationBuilder.AddColumn<Guid>(
            name: "CarouselId",
            table: "Components",
            type: "uniqueidentifier",
            nullable: true);

        // Create Effects table
        migrationBuilder.CreateTable(
            name: "Effects",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ComponentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                EffectType = table.Column<int>(type: "int", nullable: false),
                Parameters = table.Column<string>(type: "nvarchar(max)", nullable: false),
                IsEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Effects", x => x.Id);
                table.ForeignKey(
                    name: "FK_Effects_Components_ComponentId",
                    column: x => x.ComponentId,
                    principalTable: "Components",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Effects_ComponentId",
            table: "Effects",
            column: "ComponentId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Effects");

        migrationBuilder.DropColumn(
            name: "CarouselId",
            table: "Components");
    }
}
