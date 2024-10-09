using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class RemoveComputedAmountInCreditMemoTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "total_sales",
                table: "credit_memos");

            migrationBuilder.DropColumn(
                name: "vat_amount",
                table: "credit_memos");

            migrationBuilder.DropColumn(
                name: "vatable_sales",
                table: "credit_memos");

            migrationBuilder.DropColumn(
                name: "with_holding_tax_amount",
                table: "credit_memos");

            migrationBuilder.DropColumn(
                name: "with_holding_vat_amount",
                table: "credit_memos");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "total_sales",
                table: "credit_memos",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "vat_amount",
                table: "credit_memos",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "vatable_sales",
                table: "credit_memos",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "with_holding_tax_amount",
                table: "credit_memos",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "with_holding_vat_amount",
                table: "credit_memos",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
