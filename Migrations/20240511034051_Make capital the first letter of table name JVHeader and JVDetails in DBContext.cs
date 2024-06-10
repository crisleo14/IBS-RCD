using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class MakecapitalthefirstletteroftablenameJVHeaderandJVDetailsinDBContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_journalVoucherHeaders_CheckVoucherHeaders_CVId",
                table: "journalVoucherHeaders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_journalVoucherHeaders",
                table: "journalVoucherHeaders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_journalVoucherDetails",
                table: "journalVoucherDetails");

            migrationBuilder.RenameTable(
                name: "journalVoucherHeaders",
                newName: "JournalVoucherHeaders");

            migrationBuilder.RenameTable(
                name: "journalVoucherDetails",
                newName: "JournalVoucherDetails");

            migrationBuilder.RenameIndex(
                name: "IX_journalVoucherHeaders_CVId",
                table: "JournalVoucherHeaders",
                newName: "IX_JournalVoucherHeaders_CVId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_JournalVoucherHeaders",
                table: "JournalVoucherHeaders",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_JournalVoucherDetails",
                table: "JournalVoucherDetails",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_JournalVoucherHeaders_CheckVoucherHeaders_CVId",
                table: "JournalVoucherHeaders",
                column: "CVId",
                principalTable: "CheckVoucherHeaders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JournalVoucherHeaders_CheckVoucherHeaders_CVId",
                table: "JournalVoucherHeaders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_JournalVoucherHeaders",
                table: "JournalVoucherHeaders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_JournalVoucherDetails",
                table: "JournalVoucherDetails");

            migrationBuilder.RenameTable(
                name: "JournalVoucherHeaders",
                newName: "journalVoucherHeaders");

            migrationBuilder.RenameTable(
                name: "JournalVoucherDetails",
                newName: "journalVoucherDetails");

            migrationBuilder.RenameIndex(
                name: "IX_JournalVoucherHeaders_CVId",
                table: "journalVoucherHeaders",
                newName: "IX_journalVoucherHeaders_CVId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_journalVoucherHeaders",
                table: "journalVoucherHeaders",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_journalVoucherDetails",
                table: "journalVoucherDetails",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_journalVoucherHeaders_CheckVoucherHeaders_CVId",
                table: "journalVoucherHeaders",
                column: "CVId",
                principalTable: "CheckVoucherHeaders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}