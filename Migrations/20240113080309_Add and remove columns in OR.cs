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
