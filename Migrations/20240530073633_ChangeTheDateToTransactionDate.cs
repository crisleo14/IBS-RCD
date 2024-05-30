using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class ChangeTheDateToTransactionDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Date",
                table: "DebitMemos",
                newName: "TransactionDate");

            migrationBuilder.RenameColumn(
                name: "Date",
                table: "CreditMemos",
                newName: "TransactionDate");

            migrationBuilder.RenameColumn(
                name: "Date",
                table: "CollectionReceipts",
                newName: "TransactionDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TransactionDate",
                table: "DebitMemos",
                newName: "Date");

            migrationBuilder.RenameColumn(
                name: "TransactionDate",
                table: "CreditMemos",
                newName: "Date");

            migrationBuilder.RenameColumn(
                name: "TransactionDate",
                table: "CollectionReceipts",
                newName: "Date");
        }
    }
}
