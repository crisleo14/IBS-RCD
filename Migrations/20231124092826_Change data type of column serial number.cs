using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class Changedatatypeofcolumnserialnumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SerialNo",
                table: "SalesInvoices");

            migrationBuilder.AddColumn<string>(
                name: "SINo",
                table: "SalesInvoices",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SINo",
                table: "SalesInvoices");

            migrationBuilder.AddColumn<int>(
                name: "SerialNo",
                table: "SalesInvoices",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
