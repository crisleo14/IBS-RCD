using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class AddNewFieldsInCheckVoucherModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "invoice_amount",
                table: "check_voucher_headers",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "amount",
                table: "check_voucher_details",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "amount_paid",
                table: "check_voucher_details",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ewt_percent",
                table: "check_voucher_details",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "is_user_selected",
                table: "check_voucher_details",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_vatable",
                table: "check_voucher_details",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "supplier_id",
                table: "check_voucher_details",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_check_voucher_details_supplier_id",
                table: "check_voucher_details",
                column: "supplier_id");

            migrationBuilder.AddForeignKey(
                name: "fk_check_voucher_details_suppliers_supplier_id",
                table: "check_voucher_details",
                column: "supplier_id",
                principalTable: "suppliers",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_check_voucher_details_suppliers_supplier_id",
                table: "check_voucher_details");

            migrationBuilder.DropIndex(
                name: "ix_check_voucher_details_supplier_id",
                table: "check_voucher_details");

            migrationBuilder.DropColumn(
                name: "invoice_amount",
                table: "check_voucher_headers");

            migrationBuilder.DropColumn(
                name: "amount",
                table: "check_voucher_details");

            migrationBuilder.DropColumn(
                name: "amount_paid",
                table: "check_voucher_details");

            migrationBuilder.DropColumn(
                name: "ewt_percent",
                table: "check_voucher_details");

            migrationBuilder.DropColumn(
                name: "is_user_selected",
                table: "check_voucher_details");

            migrationBuilder.DropColumn(
                name: "is_vatable",
                table: "check_voucher_details");

            migrationBuilder.DropColumn(
                name: "supplier_id",
                table: "check_voucher_details");
        }
    }
}
