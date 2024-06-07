using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class RevisedatatypeofsupplierforeignandmakenullableinCV : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Supplier",
                table: "CheckVoucherHeaders");

            migrationBuilder.AlterColumn<int>(
                name: "SupplierId",
                table: "CheckVoucherHeaders",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateIndex(
                name: "IX_CheckVoucherHeaders_SupplierId",
                table: "CheckVoucherHeaders",
                column: "SupplierId");

            migrationBuilder.AddForeignKey(
                name: "FK_CheckVoucherHeaders_Suppliers_SupplierId",
                table: "CheckVoucherHeaders",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CheckVoucherHeaders_Suppliers_SupplierId",
                table: "CheckVoucherHeaders");

            migrationBuilder.DropIndex(
                name: "IX_CheckVoucherHeaders_SupplierId",
                table: "CheckVoucherHeaders");

            migrationBuilder.AlterColumn<int>(
                name: "SupplierId",
                table: "CheckVoucherHeaders",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Supplier",
                table: "CheckVoucherHeaders",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}