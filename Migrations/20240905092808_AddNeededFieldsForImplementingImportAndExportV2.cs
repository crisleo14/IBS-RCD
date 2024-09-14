using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class AddNeededFieldsForImplementingImportAndExportV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "original_document_id",
                table: "service_invoices",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "original_document_id",
                table: "sales_invoices",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "original_document_id",
                table: "receiving_reports",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "original_document_id",
                table: "purchase_orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "original_document_id",
                table: "journal_voucher_headers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "original_document_id",
                table: "debit_memos",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "original_document_id",
                table: "credit_memos",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "original_document_id",
                table: "collection_receipts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "original_document_id",
                table: "check_voucher_headers",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "original_document_id",
                table: "service_invoices");

            migrationBuilder.DropColumn(
                name: "original_document_id",
                table: "sales_invoices");

            migrationBuilder.DropColumn(
                name: "original_document_id",
                table: "receiving_reports");

            migrationBuilder.DropColumn(
                name: "original_document_id",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "original_document_id",
                table: "journal_voucher_headers");

            migrationBuilder.DropColumn(
                name: "original_document_id",
                table: "debit_memos");

            migrationBuilder.DropColumn(
                name: "original_document_id",
                table: "credit_memos");

            migrationBuilder.DropColumn(
                name: "original_document_id",
                table: "collection_receipts");

            migrationBuilder.DropColumn(
                name: "original_document_id",
                table: "check_voucher_headers");
        }
    }
}
