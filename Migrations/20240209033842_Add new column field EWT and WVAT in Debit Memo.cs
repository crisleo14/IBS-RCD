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
            migrationBuilder.AddColumn<decimal>(
                name: "WithHoldingTaxAmount",
                table: "DebitMemos",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WithHoldingVatAmount",
                table: "DebitMemos",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);
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
