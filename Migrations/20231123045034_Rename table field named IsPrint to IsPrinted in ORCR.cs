using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class RenametablefieldnamedIsPrinttoIsPrintedinORCR : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsPrint",
                table: "OfficialReceipts",
                newName: "IsPrinted");

            migrationBuilder.RenameColumn(
                name: "IsPrint",
                table: "CollectionReceipts",
                newName: "IsPrinted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsPrinted",
                table: "OfficialReceipts",
                newName: "IsPrint");

            migrationBuilder.RenameColumn(
                name: "IsPrinted",
                table: "CollectionReceipts",
                newName: "IsPrint");
        }
    }
}
