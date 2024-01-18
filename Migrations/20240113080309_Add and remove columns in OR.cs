using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class AddandremovecolumnsinOR : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FormOfPayment",
                table: "OfficialReceipts");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "OfficialReceipts",
                newName: "WVAT");

            migrationBuilder.AlterColumn<string>(
                name: "CheckNo",
                table: "OfficialReceipts",
                type: "varchar(20)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<decimal>(
                name: "CashAmount",
                table: "OfficialReceipts",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CheckAmount",
                table: "OfficialReceipts",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "EWT",
                table: "OfficialReceipts",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CashAmount",
                table: "OfficialReceipts");

            migrationBuilder.DropColumn(
                name: "CheckAmount",
                table: "OfficialReceipts");

            migrationBuilder.DropColumn(
                name: "EWT",
                table: "OfficialReceipts");

            migrationBuilder.RenameColumn(
                name: "WVAT",
                table: "OfficialReceipts",
                newName: "Amount");

            migrationBuilder.AlterColumn<int>(
                name: "CheckNo",
                table: "OfficialReceipts",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FormOfPayment",
                table: "OfficialReceipts",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
