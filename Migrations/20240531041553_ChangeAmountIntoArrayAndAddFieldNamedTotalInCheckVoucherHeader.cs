using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class ChangeAmountIntoArrayAndAddFieldNamedTotalInCheckVoucherHeader : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateOnly>(
                name: "DueDate",
                table: "SalesBooks",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1),
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "Amount",
                table: "CheckVoucherHeaders");

            migrationBuilder.AddColumn<decimal>(
                name: "Total",
                table: "CheckVoucherHeaders",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Amount",
                table: "CheckVoucherHeaders",
                type: "numeric[]",
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Total",
                table: "CheckVoucherHeaders");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "DueDate",
                table: "SalesBooks",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "CheckVoucherHeaders",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal[]),
                oldType: "numeric[]");
        }
    }
}
