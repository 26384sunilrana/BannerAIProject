using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BannerService.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddShopOwnerDashboardTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShopOwnerDashboards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShopId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentSummary = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "{}"),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopOwnerDashboards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopOwnerDashboards_Shops_ShopId",
                        column: x => x.ShopId,
                        principalTable: "Shops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShopDashboardMetricSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShopId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SnapshotDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MetricType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    MetricValue = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopDashboardMetricSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopDashboardMetricSnapshots_Shops_ShopId",
                        column: x => x.ShopId,
                        principalTable: "Shops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShopDashboardAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShopId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AlertType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopDashboardAlerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopDashboardAlerts_Shops_ShopId",
                        column: x => x.ShopId,
                        principalTable: "Shops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShopOwnerDashboards_ShopId",
                table: "ShopOwnerDashboards",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopOwnerDashboards_LastUpdatedAt",
                table: "ShopOwnerDashboards",
                column: "LastUpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ShopDashboardMetricSnapshots_ShopId",
                table: "ShopDashboardMetricSnapshots",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopDashboardMetricSnapshots_MetricType",
                table: "ShopDashboardMetricSnapshots",
                column: "MetricType");

            migrationBuilder.CreateIndex(
                name: "IX_ShopDashboardMetricSnapshots_SnapshotDate",
                table: "ShopDashboardMetricSnapshots",
                column: "SnapshotDate");

            migrationBuilder.CreateIndex(
                name: "IX_ShopDashboardMetricSnapshots_Composite",
                table: "ShopDashboardMetricSnapshots",
                columns: new[] { "ShopId", "MetricType", "SnapshotDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ShopDashboardAlerts_ShopId",
                table: "ShopDashboardAlerts",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopDashboardAlerts_IsResolved",
                table: "ShopDashboardAlerts",
                column: "IsResolved");

            migrationBuilder.CreateIndex(
                name: "IX_ShopDashboardAlerts_Severity",
                table: "ShopDashboardAlerts",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_ShopDashboardAlerts_CreatedAt",
                table: "ShopDashboardAlerts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ShopDashboardAlerts_AlertType",
                table: "ShopDashboardAlerts",
                column: "AlertType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShopOwnerDashboards");

            migrationBuilder.DropTable(
                name: "ShopDashboardMetricSnapshots");

            migrationBuilder.DropTable(
                name: "ShopDashboardAlerts");
        }
    }
}
