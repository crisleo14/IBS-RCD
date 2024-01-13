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
            migrationBuilder.AddColumn<decimal>(
                name: "AmountPaid",
                table: "StatementOfAccounts",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Balance",
                table: "StatementOfAccounts",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsPaid",
                table: "StatementOfAccounts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "NetAmount",
                table: "StatementOfAccounts",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "StatementOfAccounts",
                type: "varchar(20)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "VatAmount",
                table: "StatementOfAccounts",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WithholdingTaxAmount",
                table: "StatementOfAccounts",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WithholdingVatAmount",
                table: "StatementOfAccounts",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);
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
