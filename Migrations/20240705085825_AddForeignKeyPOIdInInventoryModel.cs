using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class AddForeignKeyPOIdInInventoryModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_inventories_purchase_orders_po_id",
                table: "inventories");

            migrationBuilder.DropIndex(
                name: "ix_inventories_po_id",
                table: "inventories");
        }
    }
}
