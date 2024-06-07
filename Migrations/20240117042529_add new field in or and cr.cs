using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class addnewfieldinorandcr : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "F2306FilePath",
                table: "OfficialReceipts");

            migrationBuilder.DropColumn(
                name: "F2307FilePath",
                table: "OfficialReceipts");

            migrationBuilder.DropColumn(
                name: "IsCertificateUpload",
                table: "OfficialReceipts");

            migrationBuilder.DropColumn(
                name: "F2306FilePath",
                table: "CollectionReceipts");

            migrationBuilder.DropColumn(
                name: "F2307FilePath",
                table: "CollectionReceipts");

            migrationBuilder.DropColumn(
                name: "IsCertificateUpload",
                table: "CollectionReceipts");
        }
    }
}