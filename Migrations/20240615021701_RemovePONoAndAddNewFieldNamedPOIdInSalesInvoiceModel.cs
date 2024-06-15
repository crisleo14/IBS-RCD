using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class RemovePONoAndAddNewFieldNamedPOIdInSalesInvoiceModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "po_no",
                table: "sales_invoices");

            migrationBuilder.AddColumn<int>(
                name: "po_id",
                table: "sales_invoices",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_sales_invoices_po_id",
                table: "sales_invoices",
                column: "po_id");

            migrationBuilder.AddForeignKey(
                name: "fk_sales_invoices_purchase_orders_po_id",
                table: "sales_invoices",
                column: "po_id",
                principalTable: "purchase_orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_sales_invoices_purchase_orders_po_id",
                table: "sales_invoices");

            migrationBuilder.DropIndex(
                name: "ix_sales_invoices_po_id",
                table: "sales_invoices");

            migrationBuilder.DropColumn(
                name: "po_id",
                table: "sales_invoices");

            migrationBuilder.AddColumn<string>(
                name: "po_no",
                table: "sales_invoices",
                type: "varchar(20)",
                nullable: false,
                defaultValue: "");
        }
    }
}
