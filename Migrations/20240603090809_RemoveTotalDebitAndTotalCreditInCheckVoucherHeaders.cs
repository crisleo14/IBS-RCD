using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTotalDebitAndTotalCreditInCheckVoucherHeaders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalCredit",
                table: "CheckVoucherHeaders");

            migrationBuilder.DropColumn(
                name: "TotalDebit",
                table: "CheckVoucherHeaders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TotalCredit",
                table: "CheckVoucherHeaders",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalDebit",
                table: "CheckVoucherHeaders",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}