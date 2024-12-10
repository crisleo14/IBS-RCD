using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAccountNoAndBranchInBankAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "account_no",
                table: "bank_accounts");

            migrationBuilder.DropColumn(
                name: "bank",
                table: "bank_accounts");

            migrationBuilder.RenameColumn(
                name: "branch",
                table: "bank_accounts",
                newName: "bank_code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "bank_code",
                table: "bank_accounts",
                newName: "branch");

            migrationBuilder.AddColumn<string>(
                name: "account_no",
                table: "bank_accounts",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "bank",
                table: "bank_accounts",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
