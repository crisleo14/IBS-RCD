using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class adddatatypetotermsinsoa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Terms",
                table: "StatementOfAccounts",
                type: "varchar(5)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Terms",
                table: "StatementOfAccounts",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(5)");
        }
    }
}