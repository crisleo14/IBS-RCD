using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class changethewithholdingtaxproperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TaxType",
                table: "Suppliers");

            migrationBuilder.RenameColumn(
                name: "VatType",
                table: "Suppliers",
                newName: "Type");

            migrationBuilder.AddColumn<bool>(
                name: "WithholdingTax",
                table: "Suppliers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "WithholdingVat",
                table: "Suppliers",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
