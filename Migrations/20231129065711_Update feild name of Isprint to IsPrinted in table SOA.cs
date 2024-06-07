using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class UpdatefeildnameofIsprinttoIsPrintedintableSOA : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsPrint",
                table: "StatementOfAccounts",
                newName: "IsPrinted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsPrinted",
                table: "StatementOfAccounts",
                newName: "IsPrint");
        }
    }
}