using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class ChangeTheDebitMemoTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DebitMemos_ServiceInvoices_SOAId",
                table: "DebitMemos");

            migrationBuilder.RenameColumn(
                name: "SOAId",
                table: "DebitMemos",
                newName: "ServiceInvoiceId");

            migrationBuilder.RenameIndex(
                name: "IX_DebitMemos_SOAId",
                table: "DebitMemos",
                newName: "IX_DebitMemos_ServiceInvoiceId");

            migrationBuilder.AddColumn<decimal>(
                name: "Amount",
                table: "DebitMemos",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "Period",
                table: "DebitMemos",
                type: "date",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddForeignKey(
                name: "FK_DebitMemos_ServiceInvoices_ServiceInvoiceId",
                table: "DebitMemos",
                column: "ServiceInvoiceId",
                principalTable: "ServiceInvoices",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DebitMemos_ServiceInvoices_ServiceInvoiceId",
                table: "DebitMemos");

            migrationBuilder.DropColumn(
                name: "Amount",
                table: "DebitMemos");

            migrationBuilder.DropColumn(
                name: "Period",
                table: "DebitMemos");

            migrationBuilder.RenameColumn(
                name: "ServiceInvoiceId",
                table: "DebitMemos",
                newName: "SOAId");

            migrationBuilder.RenameIndex(
                name: "IX_DebitMemos_ServiceInvoiceId",
                table: "DebitMemos",
                newName: "IX_DebitMemos_SOAId");

            migrationBuilder.AddForeignKey(
                name: "FK_DebitMemos_ServiceInvoices_SOAId",
                table: "DebitMemos",
                column: "SOAId",
                principalTable: "ServiceInvoices",
                principalColumn: "Id");
        }
    }
}
