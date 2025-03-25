using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class AddNeededEntityForEnhancementOfCheckVoucherModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "filpride_cv_trade_payments",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    document_id = table.Column<int>(type: "integer", nullable: false),
                    document_type = table.Column<string>(type: "text", nullable: false),
                    check_voucher_id = table.Column<int>(type: "integer", nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_filpride_cv_trade_payments", x => x.id);
                    table.ForeignKey(
                        name: "fk_filpride_cv_trade_payments_check_voucher_headers_check_vouc",
                        column: x => x.check_voucher_id,
                        principalTable: "check_voucher_headers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "multiple_check_voucher_payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    check_voucher_header_payment_id = table.Column<int>(type: "integer", nullable: false),
                    check_voucher_header_invoice_id = table.Column<int>(type: "integer", nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_multiple_check_voucher_payments", x => x.id);
                    table.ForeignKey(
                        name: "fk_multiple_check_voucher_payments_check_voucher_headers_check",
                        column: x => x.check_voucher_header_invoice_id,
                        principalTable: "check_voucher_headers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_multiple_check_voucher_payments_check_voucher_headers_check1",
                        column: x => x.check_voucher_header_payment_id,
                        principalTable: "check_voucher_headers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_filpride_cv_trade_payments_check_voucher_id",
                table: "filpride_cv_trade_payments",
                column: "check_voucher_id");

            migrationBuilder.CreateIndex(
                name: "ix_multiple_check_voucher_payments_check_voucher_header_invoic",
                table: "multiple_check_voucher_payments",
                column: "check_voucher_header_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_multiple_check_voucher_payments_check_voucher_header_paymen",
                table: "multiple_check_voucher_payments",
                column: "check_voucher_header_payment_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "filpride_cv_trade_payments");

            migrationBuilder.DropTable(
                name: "multiple_check_voucher_payments");
        }
    }
}
