using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class ImplementICollectionInModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "cv_header_id",
                table: "check_voucher_details",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_check_voucher_details_cv_header_id",
                table: "check_voucher_details",
                column: "cv_header_id");

            migrationBuilder.AddForeignKey(
                name: "fk_check_voucher_details_check_voucher_headers_cv_header_id",
                table: "check_voucher_details",
                column: "cv_header_id",
                principalTable: "check_voucher_headers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_check_voucher_details_check_voucher_headers_cv_header_id",
                table: "check_voucher_details");

            migrationBuilder.DropIndex(
                name: "ix_check_voucher_details_cv_header_id",
                table: "check_voucher_details");

            migrationBuilder.DropColumn(
                name: "cv_header_id",
                table: "check_voucher_details");
        }
    }
}
