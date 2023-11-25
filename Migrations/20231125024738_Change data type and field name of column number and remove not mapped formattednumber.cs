using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class Changedatatypeandfieldnameofcolumnnumberandremovenotmappedformattednumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Number",
                table: "StatementOfAccounts");

            migrationBuilder.AddColumn<string>(
                name: "SOANo",
                table: "StatementOfAccounts",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SOANo",
                table: "StatementOfAccounts");

            migrationBuilder.AddColumn<int>(
                name: "Number",
                table: "StatementOfAccounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
