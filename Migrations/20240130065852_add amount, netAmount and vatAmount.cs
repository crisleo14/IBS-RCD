using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class addamountnetAmountandvatAmount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Amount",
                table: "ReceivingReports");

            migrationBuilder.DropColumn(
                name: "NetAmount",
                table: "ReceivingReports");

            migrationBuilder.DropColumn(
                name: "VatAmount",
                table: "ReceivingReports");

            migrationBuilder.AlterColumn<string>(
                name: "ProofOfRegistrationFilePath",
                table: "Suppliers",
                type: "varchar(200)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "GainOrLoss",
                table: "ReceivingReports",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");
        }
    }
}