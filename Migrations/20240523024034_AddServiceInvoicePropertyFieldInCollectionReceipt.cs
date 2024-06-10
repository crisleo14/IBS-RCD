using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceInvoicePropertyFieldInCollectionReceipt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SVNo",
                table: "CollectionReceipts",
                type: "varchar(12)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ServiceInvoiceId",
                table: "CollectionReceipts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_CollectionReceipts_ServiceInvoiceId",
                table: "CollectionReceipts",
                column: "ServiceInvoiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_CollectionReceipts_ServiceInvoices_ServiceInvoiceId",
                table: "CollectionReceipts",
                column: "ServiceInvoiceId",
                principalTable: "ServiceInvoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CollectionReceipts_ServiceInvoices_ServiceInvoiceId",
                table: "CollectionReceipts");

            migrationBuilder.DropIndex(
                name: "IX_CollectionReceipts_ServiceInvoiceId",
                table: "CollectionReceipts");

            migrationBuilder.DropColumn(
                name: "SVNo",
                table: "CollectionReceipts");

            migrationBuilder.DropColumn(
                name: "ServiceInvoiceId",
                table: "CollectionReceipts");
        }
    }
}