using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class RevisedMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JournalVoucherHeaders_CheckVoucherHeaders_CVId",
                table: "JournalVoucherHeaders");

            migrationBuilder.AlterColumn<int>(
                name: "CVId",
                table: "JournalVoucherHeaders",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Remarks",
                table: "DebitMemos",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_JournalVoucherHeaders_CheckVoucherHeaders_CVId",
                table: "JournalVoucherHeaders",
                column: "CVId",
                principalTable: "CheckVoucherHeaders",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JournalVoucherHeaders_CheckVoucherHeaders_CVId",
                table: "JournalVoucherHeaders");

            migrationBuilder.AlterColumn<int>(
                name: "CVId",
                table: "JournalVoucherHeaders",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Remarks",
                table: "DebitMemos",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddForeignKey(
                name: "FK_JournalVoucherHeaders_CheckVoucherHeaders_CVId",
                table: "JournalVoucherHeaders",
                column: "CVId",
                principalTable: "CheckVoucherHeaders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
