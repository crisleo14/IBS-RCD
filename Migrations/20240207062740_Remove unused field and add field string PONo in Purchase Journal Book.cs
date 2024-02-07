using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class RemoveunusedfieldandaddfieldstringPONoinPurchaseJournalBook : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Number",
                table: "PurchaseJournalBooks");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "PurchaseJournalBooks");

            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "PurchaseJournalBooks");

            migrationBuilder.DropColumn(
                name: "VatExempt",
                table: "PurchaseJournalBooks");

            migrationBuilder.DropColumn(
                name: "Vatable",
                table: "PurchaseJournalBooks");

            migrationBuilder.DropColumn(
                name: "ZeroRated",
                table: "PurchaseJournalBooks");

            migrationBuilder.AddColumn<string>(
                name: "PONo",
                table: "PurchaseJournalBooks",
                type: "varchar(12)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PONo",
                table: "PurchaseJournalBooks");

            migrationBuilder.AddColumn<long>(
                name: "Number",
                table: "PurchaseJournalBooks",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<decimal>(
                name: "Quantity",
                table: "PurchaseJournalBooks",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "PurchaseJournalBooks",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "VatExempt",
                table: "PurchaseJournalBooks",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Vatable",
                table: "PurchaseJournalBooks",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ZeroRated",
                table: "PurchaseJournalBooks",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
