using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BannerService.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalyticsTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DashboardReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShopId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Metrics = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "[]"),
                    ComparisonData = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "{}"),
                    TrendData = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "{}"),
                    ExportFormat = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ExportUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TotalRecords = table.Column<int>(type: "int", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExportedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DashboardReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DashboardReports_Shops_ShopId",
                        column: x => x.ShopId,
                        principalTable: "Shops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AnalyticsEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShopId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    EventName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ResourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResourceType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Properties = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "{}"),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SessionId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalyticsEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnalyticsEvents_Shops_ShopId",
                        column: x => x.ShopId,
                        principalTable: "Shops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DashboardReports_ShopId",
                table: "DashboardReports",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_DashboardReports_Type",
                table: "DashboardReports",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_DashboardReports_Status",
                table: "DashboardReports",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DashboardReports_CreatedAt",
                table: "DashboardReports",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DashboardReports_DateRange",
                table: "DashboardReports",
                columns: new[] { "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_DashboardReports_Composite",
                table: "DashboardReports",
                columns: new[] { "ShopId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsEvents_ShopId",
                table: "AnalyticsEvents",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsEvents_EventType",
                table: "AnalyticsEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsEvents_UserId",
                table: "AnalyticsEvents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsEvents_ResourceId",
                table: "AnalyticsEvents",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsEvents_OccurredAt",
                table: "AnalyticsEvents",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsEvents_CreatedAt",
                table: "AnalyticsEvents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsEvents_Composite",
                table: "AnalyticsEvents",
                columns: new[] { "ShopId", "EventType", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DashboardReports");

            migrationBuilder.DropTable(
                name: "AnalyticsEvents");
        }
    }
}
