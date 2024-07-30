using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class AddForeignKeyCustomerInCRModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_collection_receipts_customer_id",
                table: "collection_receipts",
                column: "customer_id");

            migrationBuilder.AddForeignKey(
                name: "fk_collection_receipts_customers_customer_id",
                table: "collection_receipts",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_collection_receipts_customers_customer_id",
                table: "collection_receipts");

            migrationBuilder.DropIndex(
                name: "ix_collection_receipts_customer_id",
                table: "collection_receipts");
        }
    }
}
