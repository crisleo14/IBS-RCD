using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class RemovecolumnfieldnamedIsTaxAndVatPaidintableCRandAddcolumnfieldnamedIsTaxAndVatPaidintableSI : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsTaxAndVatPaid",
                table: "CollectionReceipts");

            migrationBuilder.AddColumn<bool>(
                name: "IsTaxAndVatPaid",
                table: "SalesInvoices",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsTaxAndVatPaid",
                table: "SalesInvoices");

            migrationBuilder.AddColumn<bool>(
                name: "IsTaxAndVatPaid",
                table: "CollectionReceipts",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}