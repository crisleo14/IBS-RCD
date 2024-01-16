using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class retrievetheprevioussnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CheckDate",
                table: "OfficialReceipts",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<decimal>(
                name: "CashAmount",
                table: "OfficialReceipts",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CheckAmount",
                table: "OfficialReceipts",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "EWT",
                table: "OfficialReceipts",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "SOANo",
                table: "OfficialReceipts",
                type: "varchar(13)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Total",
                table: "OfficialReceipts",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CashAmount",
                table: "OfficialReceipts");

            migrationBuilder.DropColumn(
                name: "CheckAmount",
                table: "OfficialReceipts");

            migrationBuilder.DropColumn(
                name: "EWT",
                table: "OfficialReceipts");

            migrationBuilder.DropColumn(
                name: "SOANo",
                table: "OfficialReceipts");

            migrationBuilder.DropColumn(
                name: "Total",
                table: "OfficialReceipts");

            migrationBuilder.DropColumn(
                name: "WVAT",
                table: "OfficialReceipts");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CheckDate",
                table: "OfficialReceipts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }
    }
}
