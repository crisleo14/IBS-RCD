using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class UpdateNameOfFilprideCVTradePayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_filpride_cv_trade_payments_check_voucher_headers_check_vouc",
                table: "filpride_cv_trade_payments");

            migrationBuilder.DropPrimaryKey(
                name: "pk_filpride_cv_trade_payments",
                table: "filpride_cv_trade_payments");

            migrationBuilder.RenameTable(
                name: "filpride_cv_trade_payments",
                newName: "cv_trade_payments");

            migrationBuilder.RenameIndex(
                name: "ix_filpride_cv_trade_payments_check_voucher_id",
                table: "cv_trade_payments",
                newName: "ix_cv_trade_payments_check_voucher_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_cv_trade_payments",
                table: "cv_trade_payments",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_cv_trade_payments_check_voucher_headers_check_voucher_id",
                table: "cv_trade_payments",
                column: "check_voucher_id",
                principalTable: "check_voucher_headers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_cv_trade_payments_check_voucher_headers_check_voucher_id",
                table: "cv_trade_payments");

            migrationBuilder.DropPrimaryKey(
                name: "pk_cv_trade_payments",
                table: "cv_trade_payments");

            migrationBuilder.RenameTable(
                name: "cv_trade_payments",
                newName: "filpride_cv_trade_payments");

            migrationBuilder.RenameIndex(
                name: "ix_cv_trade_payments_check_voucher_id",
                table: "filpride_cv_trade_payments",
                newName: "ix_filpride_cv_trade_payments_check_voucher_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_filpride_cv_trade_payments",
                table: "filpride_cv_trade_payments",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_filpride_cv_trade_payments_check_voucher_headers_check_vouc",
                table: "filpride_cv_trade_payments",
                column: "check_voucher_id",
                principalTable: "check_voucher_headers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
