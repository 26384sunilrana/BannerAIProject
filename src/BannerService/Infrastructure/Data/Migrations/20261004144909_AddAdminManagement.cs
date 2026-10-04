using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BannerService.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminDeletionRequests",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AdminIdToDelete = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequestedByAdminId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ApprovedByAdminId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AdminToDeleteId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminDeletionRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdminDeletionRequests_Users_AdminToDeleteId",
                        column: x => x.AdminToDeleteId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AdminDeletionRequests_Users_ApprovedByAdminId",
                        column: x => x.ApprovedByAdminId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AdminDeletionRequests_Users_RequestedByAdminId",
                        column: x => x.RequestedByAdminId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminDeletionRequests_AdminToDeleteId",
                table: "AdminDeletionRequests",
                column: "AdminToDeleteId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminDeletionRequests_ApprovedByAdminId",
                table: "AdminDeletionRequests",
                column: "ApprovedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminDeletionRequests_RequestedByAdminId",
                table: "AdminDeletionRequests",
                column: "RequestedByAdminId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminDeletionRequests");
        }
    }
}
