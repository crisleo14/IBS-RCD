using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTheSeriesNumberFieldInAllEntryModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "series_number",
                table: "service_invoices");

            migrationBuilder.DropColumn(
                name: "series_number",
                table: "sales_invoices");

            migrationBuilder.DropColumn(
                name: "series_number",
                table: "receiving_reports");

            migrationBuilder.DropColumn(
                name: "series_number",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "series_number",
                table: "journal_voucher_headers");

            migrationBuilder.DropColumn(
                name: "series_number",
                table: "debit_memos");

            migrationBuilder.DropColumn(
                name: "series_number",
                table: "credit_memos");

            migrationBuilder.DropColumn(
                name: "series_number",
                table: "collection_receipts");

            migrationBuilder.DropColumn(
                name: "series_number",
                table: "check_voucher_headers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "series_number",
                table: "service_invoices",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "series_number",
                table: "sales_invoices",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "series_number",
                table: "receiving_reports",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "series_number",
                table: "purchase_orders",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "series_number",
                table: "journal_voucher_headers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "series_number",
                table: "debit_memos",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "series_number",
                table: "credit_memos",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "series_number",
                table: "collection_receipts",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "series_number",
                table: "check_voucher_headers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }
    }
}
