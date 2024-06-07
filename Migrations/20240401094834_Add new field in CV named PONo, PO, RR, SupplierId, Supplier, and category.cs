using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class AddnewfieldinCVnamedPONoPORRSupplierIdSupplierandcategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CheckVoucherHeaders_ReceivingReports_RRId",
                table: "CheckVoucherHeaders");

            migrationBuilder.DropIndex(
                name: "IX_CheckVoucherHeaders_RRId",
                table: "CheckVoucherHeaders");

            migrationBuilder.DropColumn(
                name: "RRNo",
                table: "CheckVoucherHeaders");

            migrationBuilder.RenameColumn(
                name: "RRId",
                table: "CheckVoucherHeaders",
                newName: "SupplierId");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "CheckVoucherHeaders",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string[]>(
                name: "PONo",
                table: "CheckVoucherHeaders",
                type: "varchar[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.AddColumn<string>(
                name: "Supplier",
                table: "CheckVoucherHeaders",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "CheckVoucherHeaders");

            migrationBuilder.DropColumn(
                name: "PONo",
                table: "CheckVoucherHeaders");

            migrationBuilder.DropColumn(
                name: "Supplier",
                table: "CheckVoucherHeaders");

            migrationBuilder.RenameColumn(
                name: "SupplierId",
                table: "CheckVoucherHeaders",
                newName: "RRId");

            migrationBuilder.AddColumn<string>(
                name: "RRNo",
                table: "CheckVoucherHeaders",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CheckVoucherHeaders_RRId",
                table: "CheckVoucherHeaders",
                column: "RRId");

            migrationBuilder.AddForeignKey(
                name: "FK_CheckVoucherHeaders_ReceivingReports_RRId",
                table: "CheckVoucherHeaders",
                column: "RRId",
                principalTable: "ReceivingReports",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}