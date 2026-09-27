using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BannerService.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminDashboardTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create AdminDashboards table
            migrationBuilder.CreateTable(
                name: "AdminDashboards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentSummary = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "{}"),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminDashboards", x => x.Id);
                });

            // Create DashboardMetricSnapshots table
            migrationBuilder.CreateTable(
                name: "DashboardMetricSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SnapshotDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    MetricType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    MetricValue = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "{}")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DashboardMetricSnapshots", x => x.Id);
                });

            // Create AdminDashboardAlerts table
            migrationBuilder.CreateTable(
                name: "AdminDashboardAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AlertType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminDashboardAlerts", x => x.Id);
                });

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_AdminDashboards_LastUpdatedAt",
                table: "AdminDashboards",
                column: "LastUpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DashboardMetricSnapshots_MetricType",
                table: "DashboardMetricSnapshots",
                column: "MetricType");

            migrationBuilder.CreateIndex(
                name: "IX_DashboardMetricSnapshots_SnapshotDate",
                table: "DashboardMetricSnapshots",
                column: "SnapshotDate");

            migrationBuilder.CreateIndex(
                name: "IX_DashboardMetricSnapshots_MetricType_SnapshotDate",
                table: "DashboardMetricSnapshots",
                columns: new[] { "MetricType", "SnapshotDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AdminDashboardAlerts_IsResolved",
                table: "AdminDashboardAlerts",
                column: "IsResolved");

            migrationBuilder.CreateIndex(
                name: "IX_AdminDashboardAlerts_Severity",
                table: "AdminDashboardAlerts",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_AdminDashboardAlerts_CreatedAt",
                table: "AdminDashboardAlerts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AdminDashboardAlerts_AlertType",
                table: "AdminDashboardAlerts",
                column: "AlertType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AdminDashboards");
            migrationBuilder.DropTable(name: "DashboardMetricSnapshots");
            migrationBuilder.DropTable(name: "AdminDashboardAlerts");
        }
    }
}
