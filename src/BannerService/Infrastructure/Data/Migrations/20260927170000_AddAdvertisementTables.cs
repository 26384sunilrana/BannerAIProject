using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BannerService.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAdvertisementTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Advertisements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShopId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ClickUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BudgetLimit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DailyBudgetLimit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Target = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "{}"),
                    Metrics = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "{}"),
                    Priority = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PausedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Advertisements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Advertisements_Shops_ShopId",
                        column: x => x.ShopId,
                        principalTable: "Shops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AdMetricsHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdvertisementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Impressions = table.Column<long>(type: "bigint", nullable: false),
                    Clicks = table.Column<long>(type: "bigint", nullable: false),
                    Conversions = table.Column<long>(type: "bigint", nullable: false),
                    TotalSpent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EstimatedRevenue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Views = table.Column<long>(type: "bigint", nullable: false),
                    Engagements = table.Column<long>(type: "bigint", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdMetricsHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdMetricsHistory_Advertisements_AdvertisementId",
                        column: x => x.AdvertisementId,
                        principalTable: "Advertisements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Advertisements_ShopId",
                table: "Advertisements",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_Advertisements_Status",
                table: "Advertisements",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Advertisements_Type",
                table: "Advertisements",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Advertisements_CreatedByUserId",
                table: "Advertisements",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Advertisements_IsActive",
                table: "Advertisements",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Advertisements_CreatedAt",
                table: "Advertisements",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Advertisements_DateRange",
                table: "Advertisements",
                columns: new[] { "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Advertisements_Composite",
                table: "Advertisements",
                columns: new[] { "ShopId", "Status", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_AdMetricsHistory_AdvertisementId",
                table: "AdMetricsHistory",
                column: "AdvertisementId");

            migrationBuilder.CreateIndex(
                name: "IX_AdMetricsHistory_RecordedAt",
                table: "AdMetricsHistory",
                column: "RecordedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdMetricsHistory");

            migrationBuilder.DropTable(
                name: "Advertisements");
        }
    }
}
