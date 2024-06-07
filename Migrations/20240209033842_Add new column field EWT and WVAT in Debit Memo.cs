using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class AddnewcolumnfieldEWTandWVATinDebitMemo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WithHoldingTaxAmount",
                table: "DebitMemos");

            migrationBuilder.DropColumn(
                name: "WithHoldingVatAmount",
                table: "DebitMemos");
        }
    }
}