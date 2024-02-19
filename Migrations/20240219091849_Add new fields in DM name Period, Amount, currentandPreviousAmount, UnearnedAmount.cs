using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class AddnewfieldsinDMnamePeriodAmountcurrentandPreviousAmountUnearnedAmount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal[]>(
                name: "Amount",
                table: "DebitMemos",
                type: "numeric[]",
                nullable: false,
                defaultValue: new decimal[0]);

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentAndPreviousAmount",
                table: "DebitMemos",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime[]>(
                name: "Period",
                table: "DebitMemos",
                type: "date[]",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UnearnedAmount",
                table: "DebitMemos",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Amount",
                table: "DebitMemos");

            migrationBuilder.DropColumn(
                name: "CurrentAndPreviousAmount",
                table: "DebitMemos");

            migrationBuilder.DropColumn(
                name: "Period",
                table: "DebitMemos");

            migrationBuilder.DropColumn(
                name: "UnearnedAmount",
                table: "DebitMemos");
        }
    }
}
