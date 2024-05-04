using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class AddnewfieldsrequiredinAutomationofJVfrommodelCV : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HowManyYears",
                table: "CheckVoucherHeaders");

            migrationBuilder.RenameColumn(
                name: "WhenToStart",
                table: "CheckVoucherHeaders",
                newName: "StartDate");

            migrationBuilder.AddColumn<decimal>(
                name: "AmountPerMonth",
                table: "CheckVoucherHeaders",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "CheckVoucherHeaders",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsComplete",
                table: "CheckVoucherHeaders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastCreatedDate",
                table: "CheckVoucherHeaders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NumberOfMonths",
                table: "CheckVoucherHeaders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NumberOfMonthsCreated",
                table: "CheckVoucherHeaders",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AmountPerMonth",
                table: "CheckVoucherHeaders");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "CheckVoucherHeaders");

            migrationBuilder.DropColumn(
                name: "IsComplete",
                table: "CheckVoucherHeaders");

            migrationBuilder.DropColumn(
                name: "LastCreatedDate",
                table: "CheckVoucherHeaders");

            migrationBuilder.DropColumn(
                name: "NumberOfMonths",
                table: "CheckVoucherHeaders");

            migrationBuilder.DropColumn(
                name: "NumberOfMonthsCreated",
                table: "CheckVoucherHeaders");

            migrationBuilder.RenameColumn(
                name: "StartDate",
                table: "CheckVoucherHeaders",
                newName: "WhenToStart");

            migrationBuilder.AddColumn<int>(
                name: "HowManyYears",
                table: "CheckVoucherHeaders",
                type: "integer",
                nullable: true);
        }
    }
}
