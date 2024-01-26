using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class removetermsfieldandaddduedatefield : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Terms",
                table: "StatementOfAccounts");

            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "StatementOfAccounts",
                type: "date",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "StatementOfAccounts");

            migrationBuilder.AddColumn<string>(
                name: "Terms",
                table: "StatementOfAccounts",
                type: "varchar(5)",
                nullable: false,
                defaultValue: "");
        }
    }
}
