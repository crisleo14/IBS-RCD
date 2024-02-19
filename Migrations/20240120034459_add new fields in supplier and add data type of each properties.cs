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
