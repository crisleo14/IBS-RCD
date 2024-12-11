using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class RemovePoAndRrFieldsInSiAndRemovePoIdInReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_inventories_purchase_orders_po_id",
                table: "inventories");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_invoices_purchase_orders_po_id",
                table: "sales_invoices");

            migrationBuilder.DropIndex(
                name: "ix_sales_invoices_po_id",
                table: "sales_invoices");

            migrationBuilder.DropIndex(
                name: "ix_inventories_po_id",
                table: "inventories");

            migrationBuilder.DropColumn(
                name: "original_po_id",
                table: "sales_invoices");

            migrationBuilder.DropColumn(
                name: "original_receiving_report_id",
                table: "sales_invoices");

            migrationBuilder.DropColumn(
                name: "po_id",
                table: "sales_invoices");

            migrationBuilder.DropColumn(
                name: "receiving_report_id",
                table: "sales_invoices");

            migrationBuilder.DropColumn(
                name: "original_po_id",
                table: "inventories");

            migrationBuilder.DropColumn(
                name: "po_id",
                table: "inventories");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "original_po_id",
                table: "sales_invoices",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "original_receiving_report_id",
                table: "sales_invoices",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "po_id",
                table: "sales_invoices",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "receiving_report_id",
                table: "sales_invoices",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "original_po_id",
                table: "inventories",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "po_id",
                table: "inventories",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_sales_invoices_po_id",
                table: "sales_invoices",
                column: "po_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventories_po_id",
                table: "inventories",
                column: "po_id");

            migrationBuilder.AddForeignKey(
                name: "fk_inventories_purchase_orders_po_id",
                table: "inventories",
                column: "po_id",
                principalTable: "purchase_orders",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_sales_invoices_purchase_orders_po_id",
                table: "sales_invoices",
                column: "po_id",
                principalTable: "purchase_orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
