using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class MajorChangesInInventoryTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CreditMemos_ServiceInvoices_SOAId",
                table: "CreditMemos");

            migrationBuilder.DropTable(
                name: "Ledgers");

            migrationBuilder.DropIndex(
                name: "IX_CreditMemos_SOAId",
                table: "CreditMemos");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Inventories");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Inventories");

            migrationBuilder.DropColumn(
                name: "PO",
                table: "Inventories");

            migrationBuilder.DropColumn(
                name: "QuantityBalance",
                table: "Inventories");

            migrationBuilder.DropColumn(
                name: "QuantityServe",
                table: "Inventories");

            migrationBuilder.DropColumn(
                name: "SOAId",
                table: "CreditMemos");

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "Inventories",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<decimal>(
                name: "AverageCost",
                table: "Inventories",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Cost",
                table: "Inventories",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Date",
                table: "Inventories",
                type: "varchar(10)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "InventoryBalance",
                table: "Inventories",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Particular",
                table: "Inventories",
                type: "varchar(200)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Reference",
                table: "Inventories",
                type: "varchar(12)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Total",
                table: "Inventories",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalBalance",
                table: "Inventories",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_CreditMemos_ServiceInvoiceId",
                table: "CreditMemos",
                column: "ServiceInvoiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_CreditMemos_ServiceInvoices_ServiceInvoiceId",
                table: "CreditMemos",
                column: "ServiceInvoiceId",
                principalTable: "ServiceInvoices",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CreditMemos_ServiceInvoices_ServiceInvoiceId",
                table: "CreditMemos");

            migrationBuilder.DropIndex(
                name: "IX_CreditMemos_ServiceInvoiceId",
                table: "CreditMemos");

            migrationBuilder.DropColumn(
                name: "AverageCost",
                table: "Inventories");

            migrationBuilder.DropColumn(
                name: "Cost",
                table: "Inventories");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "Inventories");

            migrationBuilder.DropColumn(
                name: "InventoryBalance",
                table: "Inventories");

            migrationBuilder.DropColumn(
                name: "Particular",
                table: "Inventories");

            migrationBuilder.DropColumn(
                name: "Reference",
                table: "Inventories");

            migrationBuilder.DropColumn(
                name: "Total",
                table: "Inventories");

            migrationBuilder.DropColumn(
                name: "TotalBalance",
                table: "Inventories");

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "Inventories",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Inventories",
                type: "varchar(50)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "Inventories",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "PO",
                table: "Inventories",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "QuantityBalance",
                table: "Inventories",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "QuantityServe",
                table: "Inventories",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "SOAId",
                table: "CreditMemos",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Ledgers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccountNo = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    CanceledBy = table.Column<string>(type: "varchar(50)", nullable: true),
                    CanceledDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Category = table.Column<string>(type: "text", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(50)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsCanceled = table.Column<bool>(type: "boolean", nullable: false),
                    IsPosted = table.Column<bool>(type: "boolean", nullable: false),
                    IsPrinted = table.Column<bool>(type: "boolean", nullable: false),
                    IsVoided = table.Column<bool>(type: "boolean", nullable: false),
                    PostedBy = table.Column<string>(type: "varchar(50)", nullable: true),
                    PostedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TransactionDate = table.Column<string>(type: "text", nullable: false),
                    TransactionNo = table.Column<string>(type: "text", nullable: false),
                    VoidedBy = table.Column<string>(type: "varchar(50)", nullable: true),
                    VoidedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ledgers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CreditMemos_SOAId",
                table: "CreditMemos",
                column: "SOAId");

            migrationBuilder.AddForeignKey(
                name: "FK_CreditMemos_ServiceInvoices_SOAId",
                table: "CreditMemos",
                column: "SOAId",
                principalTable: "ServiceInvoices",
                principalColumn: "Id");
        }
    }
}
