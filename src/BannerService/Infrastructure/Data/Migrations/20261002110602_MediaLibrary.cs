using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BannerService.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MediaLibrary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "MediaFiles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DurationSeconds",
                table: "MediaFiles",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Height",
                table: "MediaFiles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Width",
                table: "MediaFiles",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_Cleanup",
                table: "MediaFiles",
                columns: new[] { "Status", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MediaFiles_Cleanup",
                table: "MediaFiles");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "MediaFiles");

            migrationBuilder.DropColumn(
                name: "DurationSeconds",
                table: "MediaFiles");

            migrationBuilder.DropColumn(
                name: "Height",
                table: "MediaFiles");

            migrationBuilder.DropColumn(
                name: "Width",
                table: "MediaFiles");
        }
    }
}
