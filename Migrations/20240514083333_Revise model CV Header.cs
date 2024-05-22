using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class RevisemodelCVHeader : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CheckVoucherHeaders_BankAccounts_BankId",
                table: "CheckVoucherHeaders");

            migrationBuilder.AlterColumn<string>(
                name: "Particulars",
                table: "CheckVoucherHeaders",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "CheckNo",
                table: "CheckVoucherHeaders",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "BankId",
                table: "CheckVoucherHeaders",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "AccruedType",
                table: "CheckVoucherHeaders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CVId",
                table: "CheckVoucherHeaders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Reference",
                table: "CheckVoucherHeaders",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CheckVoucherHeaders_CVId",
                table: "CheckVoucherHeaders",
                column: "CVId");

            migrationBuilder.AddForeignKey(
                name: "FK_CheckVoucherHeaders_BankAccounts_BankId",
                table: "CheckVoucherHeaders",
                column: "BankId",
                principalTable: "BankAccounts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CheckVoucherHeaders_CheckVoucherHeaders_CVId",
                table: "CheckVoucherHeaders",
                column: "CVId",
                principalTable: "CheckVoucherHeaders",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CheckVoucherHeaders_BankAccounts_BankId",
                table: "CheckVoucherHeaders");

            migrationBuilder.DropForeignKey(
                name: "FK_CheckVoucherHeaders_CheckVoucherHeaders_CVId",
                table: "CheckVoucherHeaders");

            migrationBuilder.DropIndex(
                name: "IX_CheckVoucherHeaders_CVId",
                table: "CheckVoucherHeaders");

            migrationBuilder.DropColumn(
                name: "AccruedType",
                table: "CheckVoucherHeaders");

            migrationBuilder.DropColumn(
                name: "CVId",
                table: "CheckVoucherHeaders");

            migrationBuilder.DropColumn(
                name: "Reference",
                table: "CheckVoucherHeaders");

            migrationBuilder.AlterColumn<string>(
                name: "Particulars",
                table: "CheckVoucherHeaders",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CheckNo",
                table: "CheckVoucherHeaders",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "BankId",
                table: "CheckVoucherHeaders",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CheckVoucherHeaders_BankAccounts_BankId",
                table: "CheckVoucherHeaders",
                column: "BankId",
                principalTable: "BankAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
