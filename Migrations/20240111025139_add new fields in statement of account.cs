using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class addnewfieldsinstatementofaccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
          
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AmountPaid",
                table: "StatementOfAccounts");

            migrationBuilder.DropColumn(
                name: "Balance",
                table: "StatementOfAccounts");

            migrationBuilder.DropColumn(
                name: "IsPaid",
                table: "StatementOfAccounts");

            migrationBuilder.DropColumn(
                name: "NetAmount",
                table: "StatementOfAccounts");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "StatementOfAccounts");

            migrationBuilder.DropColumn(
                name: "VatAmount",
                table: "StatementOfAccounts");

            migrationBuilder.DropColumn(
                name: "WithholdingTaxAmount",
                table: "StatementOfAccounts");

            migrationBuilder.DropColumn(
                name: "WithholdingVatAmount",
                table: "StatementOfAccounts");
        }
    }
}
