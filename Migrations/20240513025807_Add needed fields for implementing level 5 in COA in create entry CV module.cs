using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class Addneededfieldsforimplementinglevel5inCOAincreateentryCVmodule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccountNameCOA",
                table: "BankAccounts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccountNoCOA",
                table: "BankAccounts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Level",
                table: "BankAccounts",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountNameCOA",
                table: "BankAccounts");

            migrationBuilder.DropColumn(
                name: "AccountNoCOA",
                table: "BankAccounts");

            migrationBuilder.DropColumn(
                name: "Level",
                table: "BankAccounts");
        }
    }
}
