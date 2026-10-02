using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BannerService.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class LocationHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UniqueId",
                table: "States",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CityId",
                table: "Shops",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GroupId",
                table: "Shops",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UniqueId",
                table: "Shops",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UniqueId",
                table: "Countries",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Cities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StateId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UniqueId = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cities_States_StateId",
                        column: x => x.StateId,
                        principalTable: "States",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LocationGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CityId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UniqueId = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocationGroups_Cities_CityId",
                        column: x => x.CityId,
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_States_UniqueId",
                table: "States",
                column: "UniqueId",
                unique: true,
                filter: "[UniqueId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Shops_CityId",
                table: "Shops",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_Shops_GroupId",
                table: "Shops",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Shops_UniqueId",
                table: "Shops",
                column: "UniqueId",
                unique: true,
                filter: "[UniqueId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Countries_UniqueId",
                table: "Countries",
                column: "UniqueId",
                unique: true,
                filter: "[UniqueId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Cities_StateId_Name",
                table: "Cities",
                columns: new[] { "StateId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cities_UniqueId",
                table: "Cities",
                column: "UniqueId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocationGroups_CityId_Name",
                table: "LocationGroups",
                columns: new[] { "CityId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocationGroups_UniqueId",
                table: "LocationGroups",
                column: "UniqueId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Shops_Cities_CityId",
                table: "Shops",
                column: "CityId",
                principalTable: "Cities",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Shops_LocationGroups_GroupId",
                table: "Shops",
                column: "GroupId",
                principalTable: "LocationGroups",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Shops_Cities_CityId",
                table: "Shops");

            migrationBuilder.DropForeignKey(
                name: "FK_Shops_LocationGroups_GroupId",
                table: "Shops");

            migrationBuilder.DropTable(
                name: "LocationGroups");

            migrationBuilder.DropTable(
                name: "Cities");

            migrationBuilder.DropIndex(
                name: "IX_States_UniqueId",
                table: "States");

            migrationBuilder.DropIndex(
                name: "IX_Shops_CityId",
                table: "Shops");

            migrationBuilder.DropIndex(
                name: "IX_Shops_GroupId",
                table: "Shops");

            migrationBuilder.DropIndex(
                name: "IX_Shops_UniqueId",
                table: "Shops");

            migrationBuilder.DropIndex(
                name: "IX_Countries_UniqueId",
                table: "Countries");

            migrationBuilder.DropColumn(
                name: "UniqueId",
                table: "States");

            migrationBuilder.DropColumn(
                name: "CityId",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "GroupId",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "UniqueId",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "UniqueId",
                table: "Countries");
        }
    }
}
