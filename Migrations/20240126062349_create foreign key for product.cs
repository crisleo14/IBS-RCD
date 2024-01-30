using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class createforeignkeyforproduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProductName",
                table: "PurchaseOrders");

            migrationBuilder.AddColumn<int>(
                name: "ProductId",
                table: "PurchaseOrders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProductNo",
                table: "PurchaseOrders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_ProductId",
                table: "PurchaseOrders",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_Products_ProductId",
                table: "PurchaseOrders",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_Products_ProductId",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_ProductId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "ProductNo",
                table: "PurchaseOrders");

            migrationBuilder.AddColumn<string>(
                name: "ProductName",
                table: "PurchaseOrders",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
