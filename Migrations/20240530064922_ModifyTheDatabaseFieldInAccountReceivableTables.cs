using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class ModifyTheDatabaseFieldInAccountReceivableTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "BusinessStyle",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "CustomerType",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "ProductName",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "ProductNo",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "ProductUnit",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "SoldTo",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "Terms",
                table: "SalesInvoices");

            migrationBuilder.DropColumn(
                name: "TinNo",
                table: "SalesInvoices");

            migrationBuilder.RenameColumn(
                name: "CustomerNo",
                table: "SalesInvoices",
                newName: "ProductId");

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "SalesInvoices",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "DebitMemos",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "AdjustedPrice",
                table: "DebitMemos",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "CreditMemos",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "AdjustedPrice",
                table: "CreditMemos",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesInvoices_ProductId",
                table: "SalesInvoices",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesInvoices_Products_ProductId",
                table: "SalesInvoices",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesInvoices_Products_ProductId",
                table: "SalesInvoices");

            migrationBuilder.DropIndex(
                name: "IX_SalesInvoices_ProductId",
                table: "SalesInvoices");

            migrationBuilder.RenameColumn(
                name: "ProductId",
                table: "SalesInvoices",
                newName: "CustomerNo");

            migrationBuilder.AlterColumn<int>(
                name: "Quantity",
                table: "SalesInvoices",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "SalesInvoices",
                type: "varchar(200)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BusinessStyle",
                table: "SalesInvoices",
                type: "varchar(50)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CustomerType",
                table: "SalesInvoices",
                type: "varchar(10)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProductName",
                table: "SalesInvoices",
                type: "varchar(50)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProductNo",
                table: "SalesInvoices",
                type: "varchar(20)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProductUnit",
                table: "SalesInvoices",
                type: "varchar(5)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SoldTo",
                table: "SalesInvoices",
                type: "varchar(100)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Terms",
                table: "SalesInvoices",
                type: "varchar(5)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TinNo",
                table: "SalesInvoices",
                type: "varchar(20)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "DebitMemos",
                type: "numeric(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "AdjustedPrice",
                table: "DebitMemos",
                type: "numeric(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AlterColumn<int>(
                name: "Quantity",
                table: "CreditMemos",
                type: "integer",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "AdjustedPrice",
                table: "CreditMemos",
                type: "numeric(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");
        }
    }
}
