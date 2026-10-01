using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BannerService.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionAutoRenewAndPendingPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(name: "AutoRenew", table: "Subscriptions", type: "bit", nullable: false, defaultValue: true);
            migrationBuilder.AddColumn<Guid>(name: "PendingPlanId", table: "Subscriptions", type: "uniqueidentifier", nullable: true);
            migrationBuilder.AddColumn<decimal>(name: "PendingPrice", table: "Subscriptions", type: "decimal(18,2)", nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "PendingPlanEffectiveAt", table: "Subscriptions", type: "datetime2", nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "AutoRenew", table: "Subscriptions");
            migrationBuilder.DropColumn(name: "PendingPlanId", table: "Subscriptions");
            migrationBuilder.DropColumn(name: "PendingPrice", table: "Subscriptions");
            migrationBuilder.DropColumn(name: "PendingPlanEffectiveAt", table: "Subscriptions");
        }
    }
}
