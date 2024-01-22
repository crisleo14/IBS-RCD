using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class addnewfieldsinsupplierandadddatatypeofeachproperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BusinessStyle",
                table: "Suppliers");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Suppliers",
                type: "varchar(20)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "TinNo",
                table: "Suppliers",
                type: "varchar(20)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Suppliers",
                type: "varchar(50)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "Suppliers",
                type: "varchar(200)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "ReasonOfExemption",
                table: "Suppliers",
                type: "varchar(100)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Terms",
                table: "Suppliers",
                type: "varchar(5)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Validity",
                table: "Suppliers",
                type: "varchar(20)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ValidityDate",
                table: "Suppliers",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReasonOfExemption",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "Terms",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "Validity",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "ValidityDate",
                table: "Suppliers");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Suppliers",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(20)");

            migrationBuilder.AlterColumn<string>(
                name: "TinNo",
                table: "Suppliers",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(20)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Suppliers",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)");

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "Suppliers",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(200)");

            migrationBuilder.AddColumn<string>(
                name: "BusinessStyle",
                table: "Suppliers",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
