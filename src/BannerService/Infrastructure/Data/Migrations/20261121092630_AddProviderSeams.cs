using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BannerService.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderSeams : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Recovery tokens are now kept only as hashes (ADR-007). Links already issued held plain tokens that could no longer match,
            // and they live one hour at most, so they are cleared (this also lets the columns be given a length for their index).
            migrationBuilder.Sql(@"UPDATE Users SET PasswordResetToken = NULL, PasswordResetExpires = NULL, VerificationToken = NULL, VerificationTokenExpires = NULL
                                   WHERE PasswordResetToken IS NOT NULL OR VerificationToken IS NOT NULL");

            migrationBuilder.AlterColumn<string>(
                name: "VerificationToken",
                table: "Users",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PasswordResetToken",
                table: "Users",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "SubscriptionPlans",
                type: "nchar(3)",
                fixedLength: true,
                maxLength: 3,
                nullable: false,
                defaultValue: "INR");

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Invoices",
                type: "nchar(3)",
                fixedLength: true,
                maxLength: 3,
                nullable: false,
                defaultValue: "INR");

            migrationBuilder.CreateTable(
                name: "PaymentAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShopId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ProviderOrderId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProviderPaymentId = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentAttempts_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PaymentAttempts_Shops_ShopId",
                        column: x => x.ShopId,
                        principalTable: "Shops",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PaymentAttempts_Subscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "Subscriptions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WebhookReceipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ProviderEventId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Outcome = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookReceipts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Refunds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentAttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderRefundId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Refunds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Refunds_PaymentAttempts_PaymentAttemptId",
                        column: x => x.PaymentAttemptId,
                        principalTable: "PaymentAttempts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_PasswordResetToken",
                table: "Users",
                column: "PasswordResetToken",
                filter: "[PasswordResetToken] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Users_VerificationToken",
                table: "Users",
                column: "VerificationToken",
                filter: "[VerificationToken] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAttempts_InvoiceId",
                table: "PaymentAttempts",
                column: "InvoiceId",
                unique: true,
                filter: "[InvoiceId] IS NOT NULL AND [Status] IN (2, 4, 5)");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAttempts_Provider_ProviderOrderId",
                table: "PaymentAttempts",
                columns: new[] { "Provider", "ProviderOrderId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAttempts_ShopId",
                table: "PaymentAttempts",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAttempts_SubscriptionId",
                table: "PaymentAttempts",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_PaymentAttemptId",
                table: "Refunds",
                column: "PaymentAttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookReceipts_Provider_ProviderEventId",
                table: "WebhookReceipts",
                columns: new[] { "Provider", "ProviderEventId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Refunds");

            migrationBuilder.DropTable(
                name: "WebhookReceipts");

            migrationBuilder.DropTable(
                name: "PaymentAttempts");

            migrationBuilder.DropIndex(
                name: "IX_Users_PasswordResetToken",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_VerificationToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Invoices");

            migrationBuilder.AlterColumn<string>(
                name: "VerificationToken",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PasswordResetToken",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);
        }
    }
}
