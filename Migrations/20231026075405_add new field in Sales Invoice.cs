using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class addnewfieldinSalesInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.AddColumn<bool>(
                name: "WithHoldingTax",
                table: "SalesInvoices",
                type: "boolean",
                nullable: true);
          
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WithHoldingTax",
                table: "SalesInvoices");

          
        }
    }
}
