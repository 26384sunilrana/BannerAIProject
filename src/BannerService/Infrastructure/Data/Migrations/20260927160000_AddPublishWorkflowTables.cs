using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BannerService.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPublishWorkflowTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PublishWorkflows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BannerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShopId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmittedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Events = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "[]"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublishWorkflows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PublishWorkflows_Banners_BannerId",
                        column: x => x.BannerId,
                        principalTable: "Banners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PublishWorkflows_Shops_ShopId",
                        column: x => x.ShopId,
                        principalTable: "Shops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PublishWorkflowId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReviewerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReviewerName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ReviewerEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    DecisionComment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DecisionMadeAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalRequests_PublishWorkflows_PublishWorkflowId",
                        column: x => x.PublishWorkflowId,
                        principalTable: "PublishWorkflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PublishWorkflows_BannerId",
                table: "PublishWorkflows",
                column: "BannerId");

            migrationBuilder.CreateIndex(
                name: "IX_PublishWorkflows_ShopId",
                table: "PublishWorkflows",
                column: "ShopId");

            migrationBuilder.CreateIndex(
                name: "IX_PublishWorkflows_Status",
                table: "PublishWorkflows",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PublishWorkflows_SubmittedByUserId",
                table: "PublishWorkflows",
                column: "SubmittedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PublishWorkflows_CreatedAt",
                table: "PublishWorkflows",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PublishWorkflows_Composite",
                table: "PublishWorkflows",
                columns: new[] { "ShopId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_PublishWorkflowId",
                table: "ApprovalRequests",
                column: "PublishWorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_ReviewerId",
                table: "ApprovalRequests",
                column: "ReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_IsPending",
                table: "ApprovalRequests",
                column: "DecisionMadeAt");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_RequestedAt",
                table: "ApprovalRequests",
                column: "RequestedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprovalRequests");

            migrationBuilder.DropTable(
                name: "PublishWorkflows");
        }
    }
}
