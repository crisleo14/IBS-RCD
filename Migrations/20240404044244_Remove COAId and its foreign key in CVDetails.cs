using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCOAIdanditsforeignkeyinCVDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CheckVoucherDetails_ChartOfAccounts_COAId",
                table: "CheckVoucherDetails");

            migrationBuilder.DropIndex(
                name: "IX_CheckVoucherDetails_COAId",
                table: "CheckVoucherDetails");

            migrationBuilder.DropColumn(
                name: "COAId",
                table: "CheckVoucherDetails");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "COAId",
                table: "CheckVoucherDetails",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_CheckVoucherDetails_COAId",
                table: "CheckVoucherDetails",
                column: "COAId");

            migrationBuilder.AddForeignKey(
                name: "FK_CheckVoucherDetails_ChartOfAccounts_COAId",
                table: "CheckVoucherDetails",
                column: "COAId",
                principalTable: "ChartOfAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}