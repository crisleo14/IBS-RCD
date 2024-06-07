using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class AddnewcolumnfieldnamedBankIdandaddforeignkeyinCVtable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BankId",
                table: "CheckVoucherHeaders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_CheckVoucherHeaders_BankId",
                table: "CheckVoucherHeaders",
                column: "BankId");

            migrationBuilder.AddForeignKey(
                name: "FK_CheckVoucherHeaders_BankAccounts_BankId",
                table: "CheckVoucherHeaders",
                column: "BankId",
                principalTable: "BankAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CheckVoucherHeaders_BankAccounts_BankId",
                table: "CheckVoucherHeaders");

            migrationBuilder.DropIndex(
                name: "IX_CheckVoucherHeaders_BankId",
                table: "CheckVoucherHeaders");

            migrationBuilder.DropColumn(
                name: "BankId",
                table: "CheckVoucherHeaders");
        }
    }
}