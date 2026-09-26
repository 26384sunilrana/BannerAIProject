namespace BannerService.Infrastructure.Data.Migrations;

using Microsoft.EntityFrameworkCore.Migrations;

public partial class AddMediaFiles : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Create MediaFiles table
        migrationBuilder.CreateTable(
            name: "MediaFiles",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ShopId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                FileType = table.Column<int>(type: "int", nullable: false),
                ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                StoragePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                Status = table.Column<int>(type: "int", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                VideoMetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MediaFiles", x => x.Id);
            });

        // Create UploadChunks table
        migrationBuilder.CreateTable(
            name: "UploadChunks",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                MediaFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ChunkNumber = table.Column<int>(type: "int", nullable: false),
                SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                ChecksumMD5 = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                Status = table.Column<int>(type: "int", nullable: false),
                UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                StoragePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UploadChunks", x => x.Id);
                table.ForeignKey(
                    name: "FK_UploadChunks_MediaFiles_MediaFileId",
                    column: x => x.MediaFileId,
                    principalTable: "MediaFiles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        // Create indexes
        migrationBuilder.CreateIndex(
            name: "IX_MediaFiles_Query",
            table: "MediaFiles",
            columns: new[] { "ShopId", "Status", "CreatedAt" },
            descending: new[] { false, false, true });

        migrationBuilder.CreateIndex(
            name: "UQ_MediaFiles_StoragePath",
            table: "MediaFiles",
            column: "StoragePath",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "UQ_UploadChunks_Unique",
            table: "UploadChunks",
            columns: new[] { "MediaFileId", "ChunkNumber" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_UploadChunks_Query",
            table: "UploadChunks",
            columns: new[] { "MediaFileId", "ChunkNumber" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "UploadChunks");

        migrationBuilder.DropTable(
            name: "MediaFiles");
    }
}
