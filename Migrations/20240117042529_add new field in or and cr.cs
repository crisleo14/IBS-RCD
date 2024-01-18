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
            migrationBuilder.AddColumn<string>(
                name: "F2306FilePath",
                table: "OfficialReceipts",
                type: "varchar(50)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "F2307FilePath",
                table: "OfficialReceipts",
                type: "varchar(50)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCertificateUpload",
                table: "OfficialReceipts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "F2306FilePath",
                table: "CollectionReceipts",
                type: "varchar(50)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "F2307FilePath",
                table: "CollectionReceipts",
                type: "varchar(50)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCertificateUpload",
                table: "CollectionReceipts",
                type: "boolean",
                nullable: false,
                defaultValue: false);
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
