using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class ImplementICollectionInJVModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "jv_header_id",
                table: "journal_voucher_details",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_journal_voucher_details_jv_header_id",
                table: "journal_voucher_details",
                column: "jv_header_id");

            migrationBuilder.AddForeignKey(
                name: "fk_journal_voucher_details_journal_voucher_headers_jv_header_id",
                table: "journal_voucher_details",
                column: "jv_header_id",
                principalTable: "journal_voucher_headers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_journal_voucher_details_journal_voucher_headers_jv_header_id",
                table: "journal_voucher_details");

            migrationBuilder.DropIndex(
                name: "ix_journal_voucher_details_jv_header_id",
                table: "journal_voucher_details");

            migrationBuilder.DropColumn(
                name: "jv_header_id",
                table: "journal_voucher_details");
        }
    }
}
