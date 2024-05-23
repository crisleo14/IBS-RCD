using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class SetNullableTheForeignKeyId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CollectionReceipts_SalesInvoices_SalesInvoiceId",
                table: "CollectionReceipts");

            migrationBuilder.DropForeignKey(
                name: "FK_CollectionReceipts_ServiceInvoices_ServiceInvoiceId",
                table: "CollectionReceipts");

            migrationBuilder.AlterColumn<int>(
                name: "ServiceInvoiceId",
                table: "CollectionReceipts",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "SalesInvoiceId",
                table: "CollectionReceipts",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_CollectionReceipts_SalesInvoices_SalesInvoiceId",
                table: "CollectionReceipts",
                column: "SalesInvoiceId",
                principalTable: "SalesInvoices",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CollectionReceipts_ServiceInvoices_ServiceInvoiceId",
                table: "CollectionReceipts",
                column: "ServiceInvoiceId",
                principalTable: "ServiceInvoices",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CollectionReceipts_SalesInvoices_SalesInvoiceId",
                table: "CollectionReceipts");

            migrationBuilder.DropForeignKey(
                name: "FK_CollectionReceipts_ServiceInvoices_ServiceInvoiceId",
                table: "CollectionReceipts");

            migrationBuilder.AlterColumn<int>(
                name: "ServiceInvoiceId",
                table: "CollectionReceipts",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "SalesInvoiceId",
                table: "CollectionReceipts",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CollectionReceipts_SalesInvoices_SalesInvoiceId",
                table: "CollectionReceipts",
                column: "SalesInvoiceId",
                principalTable: "SalesInvoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CollectionReceipts_ServiceInvoices_ServiceInvoiceId",
                table: "CollectionReceipts",
                column: "ServiceInvoiceId",
                principalTable: "ServiceInvoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
