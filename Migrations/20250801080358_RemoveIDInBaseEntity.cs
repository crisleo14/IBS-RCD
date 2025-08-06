using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIDInBaseEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "id",
                table: "suppliers",
                newName: "supplier_id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "services",
                newName: "service_id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "service_invoices",
                newName: "service_invoice_id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "sales_invoices",
                newName: "sales_invoice_id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "sales_books",
                newName: "sales_book_id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "receiving_reports",
                newName: "receiving_report_id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "purchase_orders",
                newName: "purchase_order_id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "purchase_journal_books",
                newName: "purchase_book_id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "products",
                newName: "product_id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "journal_voucher_headers",
                newName: "journal_voucher_header_id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "journal_voucher_details",
                newName: "journal_voucher_detail_id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "journal_books",
                newName: "journal_book_id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "general_ledger_books",
                newName: "general_ledger_book_id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "disbursement_books",
                newName: "disbursement_book_id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "debit_memos",
                newName: "debit_memo_id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "customers",
                newName: "customer_id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "credit_memos",
                newName: "credit_memo_id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "collection_receipts",
                newName: "collection_receipt_id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "check_voucher_headers",
                newName: "check_voucher_header_id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "cash_receipt_books",
                newName: "cash_receipt_book_id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "bank_accounts",
                newName: "bank_account_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "supplier_id",
                table: "suppliers",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "service_id",
                table: "services",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "service_invoice_id",
                table: "service_invoices",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "sales_invoice_id",
                table: "sales_invoices",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "sales_book_id",
                table: "sales_books",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "receiving_report_id",
                table: "receiving_reports",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "purchase_order_id",
                table: "purchase_orders",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "purchase_book_id",
                table: "purchase_journal_books",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "product_id",
                table: "products",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "journal_voucher_header_id",
                table: "journal_voucher_headers",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "journal_voucher_detail_id",
                table: "journal_voucher_details",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "journal_book_id",
                table: "journal_books",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "general_ledger_book_id",
                table: "general_ledger_books",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "disbursement_book_id",
                table: "disbursement_books",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "debit_memo_id",
                table: "debit_memos",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "customer_id",
                table: "customers",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "credit_memo_id",
                table: "credit_memos",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "collection_receipt_id",
                table: "collection_receipts",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "check_voucher_header_id",
                table: "check_voucher_headers",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "cash_receipt_book_id",
                table: "cash_receipt_books",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "bank_account_id",
                table: "bank_accounts",
                newName: "id");
        }
    }
}
