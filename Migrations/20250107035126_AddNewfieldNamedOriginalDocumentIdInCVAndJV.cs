using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class AddNewfieldNamedOriginalDocumentIdInCVAndJV : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "original_document_id",
                table: "journal_voucher_details",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "original_document_id",
                table: "check_voucher_details",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "original_document_id",
                table: "journal_voucher_details");

            migrationBuilder.DropColumn(
                name: "original_document_id",
                table: "check_voucher_details");
        }
    }
}
