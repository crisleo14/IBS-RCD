using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class Revisedthecreationofcreateandshowtableofbankaccountmasterfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Amount",
                table: "BankAccounts");

            migrationBuilder.DropColumn(
                name: "AmountInWords",
                table: "BankAccounts");

            migrationBuilder.DropColumn(
                name: "CheckNo",
                table: "BankAccounts");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "BankAccounts");

            migrationBuilder.AddColumn<string>(
                name: "AccountName",
                table: "BankAccounts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AccountNo",
                table: "BankAccounts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankCode",
                table: "BankAccounts",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountName",
                table: "BankAccounts");

            migrationBuilder.DropColumn(
                name: "AccountNo",
                table: "BankAccounts");

            migrationBuilder.DropColumn(
                name: "BankCode",
                table: "BankAccounts");

            migrationBuilder.AddColumn<decimal>(
                name: "Amount",
                table: "BankAccounts",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "AmountInWords",
                table: "BankAccounts",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CheckNo",
                table: "BankAccounts",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateOnly>(
                name: "Date",
                table: "BankAccounts",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));
        }
    }
}