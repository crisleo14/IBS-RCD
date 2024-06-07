using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class InthedataentryofRRseparatetheSupplierInvoicenumberandDatealsointhedatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SupplierInvoiceDate",
                table: "ReceivingReports",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SupplierInvoiceNumber",
                table: "ReceivingReports",
                type: "varchar(100)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SupplierInvoiceDate",
                table: "ReceivingReports");

            migrationBuilder.DropColumn(
                name: "SupplierInvoiceNumber",
                table: "ReceivingReports");
        }
    }
}