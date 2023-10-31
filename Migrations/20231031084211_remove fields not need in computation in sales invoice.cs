using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class removefieldsnotneedincomputationinsalesinvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalSales",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "VatExemptSales",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "ZeroRatedSales",
                table: "SalesInvoices");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TotalSales",
                table: "SalesInvoices",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "VatExemptSales",
                table: "SalesInvoices",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ZeroRatedSales",
                table: "SalesInvoices",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
