using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class ChangeDataTypeCheckDateStringToDateOnlyInCollectionReceiptModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "check_date",
                table: "collection_receipts");

            migrationBuilder.AddColumn<DateOnly>(
                name: "check_date",
                table: "collection_receipts",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "check_date",
                table: "collection_receipts");

            migrationBuilder.AddColumn<string>(
                name: "check_date",
                table: "collection_receipts",
                type: "text",
                nullable: true);
        }
    }
}
