using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class RevisemodelCVHeaderv2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CheckVoucherHeaders_CheckVoucherHeaders_CVId",
                table: "CheckVoucherHeaders");

            migrationBuilder.DropIndex(
                name: "IX_CheckVoucherHeaders_CVId",
                table: "CheckVoucherHeaders");

            migrationBuilder.DropColumn(
                name: "AmountInWords",
                table: "CheckVoucherHeaders");

            migrationBuilder.DropColumn(
                name: "CVId",
                table: "CheckVoucherHeaders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AmountInWords",
                table: "CheckVoucherHeaders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CVId",
                table: "CheckVoucherHeaders",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CheckVoucherHeaders_CVId",
                table: "CheckVoucherHeaders",
                column: "CVId");

            migrationBuilder.AddForeignKey(
                name: "FK_CheckVoucherHeaders_CheckVoucherHeaders_CVId",
                table: "CheckVoucherHeaders",
                column: "CVId",
                principalTable: "CheckVoucherHeaders",
                principalColumn: "Id");
        }
    }
}