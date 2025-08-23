using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class ImplementStringLengthInJournalVoucherDetailModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_journal_voucher_details_journal_voucher_headers_jv_header_id",
                table: "journal_voucher_details");

            migrationBuilder.DropIndex(
                name: "ix_journal_voucher_details_jv_header_id",
                table: "journal_voucher_details");

            migrationBuilder.AlterColumn<string>(
                name: "transaction_no",
                table: "journal_voucher_details",
                type: "character varying(13)",
                maxLength: 13,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "account_no",
                table: "journal_voucher_details",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "account_name",
                table: "journal_voucher_details",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "journal_voucher_header_id",
                table: "journal_voucher_details",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_journal_voucher_details_journal_voucher_header_id",
                table: "journal_voucher_details",
                column: "journal_voucher_header_id");

            migrationBuilder.AddForeignKey(
                name: "fk_journal_voucher_details_journal_voucher_headers_journal_vou",
                table: "journal_voucher_details",
                column: "journal_voucher_header_id",
                principalTable: "journal_voucher_headers",
                principalColumn: "journal_voucher_header_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_journal_voucher_details_journal_voucher_headers_journal_vou",
                table: "journal_voucher_details");

            migrationBuilder.DropIndex(
                name: "ix_journal_voucher_details_journal_voucher_header_id",
                table: "journal_voucher_details");

            migrationBuilder.DropColumn(
                name: "journal_voucher_header_id",
                table: "journal_voucher_details");

            migrationBuilder.AlterColumn<string>(
                name: "transaction_no",
                table: "journal_voucher_details",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(13)",
                oldMaxLength: 13);

            migrationBuilder.AlterColumn<string>(
                name: "account_no",
                table: "journal_voucher_details",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "account_name",
                table: "journal_voucher_details",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.CreateIndex(
                name: "ix_journal_voucher_details_jv_header_id",
                table: "journal_voucher_details",
                column: "jv_header_id");

            migrationBuilder.AddForeignKey(
                name: "fk_journal_voucher_details_journal_voucher_headers_jv_header_id",
                table: "journal_voucher_details",
                column: "jv_header_id",
                principalTable: "journal_voucher_headers",
                principalColumn: "journal_voucher_header_id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
