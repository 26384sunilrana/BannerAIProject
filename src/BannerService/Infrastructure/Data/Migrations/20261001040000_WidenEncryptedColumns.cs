using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BannerService.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Encrypted values are longer than the plain text, so the columns that now hold them are widened.
    /// Existing plain values stay readable and are encrypted the next time the row is saved.
    /// </summary>
    public partial class WidenEncryptedColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(name: "Address", table: "Shops", type: "nvarchar(1024)", maxLength: 1024, nullable: true);
            migrationBuilder.AlterColumn<string>(name: "PostalCode", table: "Shops", type: "nvarchar(256)", maxLength: 256, nullable: true);
            migrationBuilder.AlterColumn<string>(name: "PhoneNumber", table: "Shops", type: "nvarchar(256)", maxLength: 256, nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(name: "Address", table: "Shops", type: "nvarchar(256)", maxLength: 256, nullable: true);
            migrationBuilder.AlterColumn<string>(name: "PostalCode", table: "Shops", type: "nvarchar(20)", maxLength: 20, nullable: true);
            migrationBuilder.AlterColumn<string>(name: "PhoneNumber", table: "Shops", type: "nvarchar(20)", maxLength: 20, nullable: true);
        }
    }
}
