using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class RenameFieldNameInCheckVoucherDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_check_voucher_details_check_voucher_headers_cv_header_id",
                table: "check_voucher_details");

            migrationBuilder.RenameColumn(
                name: "cv_header_id",
                table: "check_voucher_details",
                newName: "check_voucher_header_id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "check_voucher_details",
                newName: "check_voucher_detail_id");

            migrationBuilder.RenameIndex(
                name: "ix_check_voucher_details_cv_header_id",
                table: "check_voucher_details",
                newName: "ix_check_voucher_details_check_voucher_header_id");

            migrationBuilder.AlterColumn<string>(
                name: "transaction_no",
                table: "check_voucher_details",
                type: "character varying(13)",
                maxLength: 13,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<decimal>(
                name: "ewt_percent",
                table: "check_voucher_details",
                type: "numeric(18,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "account_no",
                table: "check_voucher_details",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "account_name",
                table: "check_voucher_details",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddForeignKey(
                name: "fk_check_voucher_details_check_voucher_headers_check_voucher_h",
                table: "check_voucher_details",
                column: "check_voucher_header_id",
                principalTable: "check_voucher_headers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_check_voucher_details_check_voucher_headers_check_voucher_h",
                table: "check_voucher_details");

            migrationBuilder.RenameColumn(
                name: "check_voucher_header_id",
                table: "check_voucher_details",
                newName: "cv_header_id");

            migrationBuilder.RenameColumn(
                name: "check_voucher_detail_id",
                table: "check_voucher_details",
                newName: "id");

            migrationBuilder.RenameIndex(
                name: "ix_check_voucher_details_check_voucher_header_id",
                table: "check_voucher_details",
                newName: "ix_check_voucher_details_cv_header_id");

            migrationBuilder.AlterColumn<string>(
                name: "transaction_no",
                table: "check_voucher_details",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(13)",
                oldMaxLength: 13);

            migrationBuilder.AlterColumn<decimal>(
                name: "ewt_percent",
                table: "check_voucher_details",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)");

            migrationBuilder.AlterColumn<string>(
                name: "account_no",
                table: "check_voucher_details",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "account_name",
                table: "check_voucher_details",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AddForeignKey(
                name: "fk_check_voucher_details_check_voucher_headers_cv_header_id",
                table: "check_voucher_details",
                column: "cv_header_id",
                principalTable: "check_voucher_headers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
