using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class AddFieldInSupplierModelDefaultExpenseNumberAndWithholdingTaxPercent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "default_expense_number",
                table: "suppliers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "withholding_tax_percent",
                table: "suppliers",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "default_expense_number",
                table: "suppliers");

            migrationBuilder.DropColumn(
                name: "withholding_tax_percent",
                table: "suppliers");
        }
    }
}
