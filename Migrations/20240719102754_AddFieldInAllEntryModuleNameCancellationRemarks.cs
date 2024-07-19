using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class AddFieldInAllEntryModuleNameCancellationRemarks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cancellation_remarks",
                table: "service_invoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cancellation_remarks",
                table: "sales_invoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cancellation_remarks",
                table: "receiving_reports",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cancellation_remarks",
                table: "purchase_orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cancellation_remarks",
                table: "journal_voucher_headers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cancellation_remarks",
                table: "debit_memos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cancellation_remarks",
                table: "credit_memos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cancellation_remarks",
                table: "collection_receipts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cancellation_remarks",
                table: "check_voucher_headers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cancellation_remarks",
                table: "service_invoices");

            migrationBuilder.DropColumn(
                name: "cancellation_remarks",
                table: "sales_invoices");

            migrationBuilder.DropColumn(
                name: "cancellation_remarks",
                table: "receiving_reports");

            migrationBuilder.DropColumn(
                name: "cancellation_remarks",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "cancellation_remarks",
                table: "journal_voucher_headers");

            migrationBuilder.DropColumn(
                name: "cancellation_remarks",
                table: "debit_memos");

            migrationBuilder.DropColumn(
                name: "cancellation_remarks",
                table: "credit_memos");

            migrationBuilder.DropColumn(
                name: "cancellation_remarks",
                table: "collection_receipts");

            migrationBuilder.DropColumn(
                name: "cancellation_remarks",
                table: "check_voucher_headers");
        }
    }
}
