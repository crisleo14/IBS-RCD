using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class AddNeededFieldsForImplementingImportAndExport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_inventories_products_product_id",
                table: "inventories");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_invoices_purchase_orders_po_id",
                table: "sales_invoices");

            migrationBuilder.AddColumn<int>(
                name: "original_customer_id",
                table: "service_invoices",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "original_series_number",
                table: "service_invoices",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "original_services_id",
                table: "service_invoices",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "po_id",
                table: "sales_invoices",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "original_customer_id",
                table: "sales_invoices",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "original_po_id",
                table: "sales_invoices",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "original_product_id",
                table: "sales_invoices",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "original_series_number",
                table: "sales_invoices",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "original_po_id",
                table: "receiving_reports",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "original_series_number",
                table: "receiving_reports",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "original_product_id",
                table: "purchase_orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "original_series_number",
                table: "purchase_orders",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "original_supplier_id",
                table: "purchase_orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "original_cv_id",
                table: "journal_voucher_headers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "original_series_number",
                table: "journal_voucher_headers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "product_id",
                table: "inventories",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "original_po_id",
                table: "inventories",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "original_product_id",
                table: "inventories",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "services_id",
                table: "debit_memos",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "original_sales_invoice_id",
                table: "debit_memos",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "original_series_number",
                table: "debit_memos",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "original_service_invoice_id",
                table: "debit_memos",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "services_id",
                table: "credit_memos",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "original_sales_invoice_id",
                table: "credit_memos",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "original_series_number",
                table: "credit_memos",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "original_service_invoice_id",
                table: "credit_memos",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "original_customer_id",
                table: "collection_receipts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "original_sales_invoice_id",
                table: "collection_receipts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "original_series_number",
                table: "collection_receipts",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "original_service_invoice_id",
                table: "collection_receipts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "original_bank_id",
                table: "check_voucher_headers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "original_series_number",
                table: "check_voucher_headers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "original_supplier_id",
                table: "check_voucher_headers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_inventories_products_product_id",
                table: "inventories",
                column: "product_id",
                principalTable: "products",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_sales_invoices_purchase_orders_po_id",
                table: "sales_invoices",
                column: "po_id",
                principalTable: "purchase_orders",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_inventories_products_product_id",
                table: "inventories");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_invoices_purchase_orders_po_id",
                table: "sales_invoices");

            migrationBuilder.DropColumn(
                name: "original_customer_id",
                table: "service_invoices");

            migrationBuilder.DropColumn(
                name: "original_series_number",
                table: "service_invoices");

            migrationBuilder.DropColumn(
                name: "original_services_id",
                table: "service_invoices");

            migrationBuilder.DropColumn(
                name: "original_customer_id",
                table: "sales_invoices");

            migrationBuilder.DropColumn(
                name: "original_po_id",
                table: "sales_invoices");

            migrationBuilder.DropColumn(
                name: "original_product_id",
                table: "sales_invoices");

            migrationBuilder.DropColumn(
                name: "original_series_number",
                table: "sales_invoices");

            migrationBuilder.DropColumn(
                name: "original_po_id",
                table: "receiving_reports");

            migrationBuilder.DropColumn(
                name: "original_series_number",
                table: "receiving_reports");

            migrationBuilder.DropColumn(
                name: "original_product_id",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "original_series_number",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "original_supplier_id",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "original_cv_id",
                table: "journal_voucher_headers");

            migrationBuilder.DropColumn(
                name: "original_series_number",
                table: "journal_voucher_headers");

            migrationBuilder.DropColumn(
                name: "original_po_id",
                table: "inventories");

            migrationBuilder.DropColumn(
                name: "original_product_id",
                table: "inventories");

            migrationBuilder.DropColumn(
                name: "original_sales_invoice_id",
                table: "debit_memos");

            migrationBuilder.DropColumn(
                name: "original_series_number",
                table: "debit_memos");

            migrationBuilder.DropColumn(
                name: "original_service_invoice_id",
                table: "debit_memos");

            migrationBuilder.DropColumn(
                name: "original_sales_invoice_id",
                table: "credit_memos");

            migrationBuilder.DropColumn(
                name: "original_series_number",
                table: "credit_memos");

            migrationBuilder.DropColumn(
                name: "original_service_invoice_id",
                table: "credit_memos");

            migrationBuilder.DropColumn(
                name: "original_customer_id",
                table: "collection_receipts");

            migrationBuilder.DropColumn(
                name: "original_sales_invoice_id",
                table: "collection_receipts");

            migrationBuilder.DropColumn(
                name: "original_series_number",
                table: "collection_receipts");

            migrationBuilder.DropColumn(
                name: "original_service_invoice_id",
                table: "collection_receipts");

            migrationBuilder.DropColumn(
                name: "original_bank_id",
                table: "check_voucher_headers");

            migrationBuilder.DropColumn(
                name: "original_series_number",
                table: "check_voucher_headers");

            migrationBuilder.DropColumn(
                name: "original_supplier_id",
                table: "check_voucher_headers");

            migrationBuilder.AlterColumn<int>(
                name: "po_id",
                table: "sales_invoices",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "product_id",
                table: "inventories",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "services_id",
                table: "debit_memos",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "services_id",
                table: "credit_memos",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_inventories_products_product_id",
                table: "inventories",
                column: "product_id",
                principalTable: "products",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_invoices_purchase_orders_po_id",
                table: "sales_invoices",
                column: "po_id",
                principalTable: "purchase_orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
