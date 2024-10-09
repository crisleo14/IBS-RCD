using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class RemoveComputedAmountInSalesInvoiceTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "net_discount",
                table: "sales_invoices");

            migrationBuilder.DropColumn(
                name: "vat_amount",
                table: "sales_invoices");

            migrationBuilder.DropColumn(
                name: "vat_exempt",
                table: "sales_invoices");

            migrationBuilder.DropColumn(
                name: "vatable_sales",
                table: "sales_invoices");

            migrationBuilder.DropColumn(
                name: "with_holding_tax_amount",
                table: "sales_invoices");

            migrationBuilder.DropColumn(
                name: "with_holding_vat_amount",
                table: "sales_invoices");

            migrationBuilder.DropColumn(
                name: "zero_rated",
                table: "sales_invoices");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "net_discount",
                table: "sales_invoices",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "vat_amount",
                table: "sales_invoices",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "vat_exempt",
                table: "sales_invoices",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "vatable_sales",
                table: "sales_invoices",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "with_holding_tax_amount",
                table: "sales_invoices",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "with_holding_vat_amount",
                table: "sales_invoices",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "zero_rated",
                table: "sales_invoices",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
