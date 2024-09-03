using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class AddFieldReceivedDateInRRModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "received_date",
                table: "receiving_reports",
                type: "date",
                nullable: true);
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

            migrationBuilder.DropColumn(
                name: "received_date",
                table: "receiving_reports");

            migrationBuilder.DropColumn(
                name: "multiple_transaction_date",
                table: "collection_receipts");

            migrationBuilder.DropColumn(
                name: "si_multiple_amount",
                table: "collection_receipts");
        }
    }
}
