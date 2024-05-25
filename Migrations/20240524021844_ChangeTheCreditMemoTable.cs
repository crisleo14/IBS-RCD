using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class ChangeTheCreditMemoTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CreditMemos_SalesInvoices_SIId",
                table: "CreditMemos");

            migrationBuilder.DropIndex(
                name: "IX_CreditMemos_SIId",
                table: "CreditMemos");

            migrationBuilder.DropColumn(
                name: "SINo",
                table: "CreditMemos");

            migrationBuilder.DropColumn(
                name: "SOANo",
                table: "CreditMemos");

            migrationBuilder.RenameColumn(
                name: "SIId",
                table: "CreditMemos",
                newName: "ServiceInvoiceId");

            migrationBuilder.DropColumn(
                name: "Period",
                table: "CreditMemos");

            migrationBuilder.DropColumn(
                name: "Amount",
                table: "CreditMemos");

            migrationBuilder.AddColumn<int>(
                name: "Period",
                table: "CreditMemos",
                type: "date",
                nullable: true,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "Amount",
                table: "CreditMemos",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SalesInvoiceId",
                table: "CreditMemos",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreditMemos_SalesInvoiceId",
                table: "CreditMemos",
                column: "SalesInvoiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_CreditMemos_SalesInvoices_SalesInvoiceId",
                table: "CreditMemos",
                column: "SalesInvoiceId",
                principalTable: "SalesInvoices",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CreditMemos_SalesInvoices_SalesInvoiceId",
                table: "CreditMemos");

            migrationBuilder.DropIndex(
                name: "IX_CreditMemos_SalesInvoiceId",
                table: "CreditMemos");

            migrationBuilder.DropColumn(
                name: "SalesInvoiceId",
                table: "CreditMemos");

            migrationBuilder.RenameColumn(
                name: "ServiceInvoiceId",
                table: "CreditMemos",
                newName: "SIId");

            migrationBuilder.AlterColumn<DateTime[]>(
                name: "Period",
                table: "CreditMemos",
                type: "date[]",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "date");

            migrationBuilder.AlterColumn<decimal[]>(
                name: "Amount",
                table: "CreditMemos",
                type: "numeric[]",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<string>(
                name: "SINo",
                table: "CreditMemos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SOANo",
                table: "CreditMemos",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreditMemos_SIId",
                table: "CreditMemos",
                column: "SIId");

            migrationBuilder.AddForeignKey(
                name: "FK_CreditMemos_SalesInvoices_SIId",
                table: "CreditMemos",
                column: "SIId",
                principalTable: "SalesInvoices",
                principalColumn: "Id");
        }
    }
}
