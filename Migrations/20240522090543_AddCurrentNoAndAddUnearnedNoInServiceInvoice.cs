using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrentNoAndAddUnearnedNoInServiceInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Unearned",
                table: "Services",
                newName: "UnearnedTitle");

            migrationBuilder.RenameColumn(
                name: "CurrentAndPrevious",
                table: "Services",
                newName: "CurrentAndPreviousTitle");

            migrationBuilder.AddColumn<string>(
                name: "CurrentAndPreviousNo",
                table: "Services",
                type: "varchar(20)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UnearnedNo",
                table: "Services",
                type: "varchar(20)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentAndPreviousNo",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "UnearnedNo",
                table: "Services");

            migrationBuilder.RenameColumn(
                name: "UnearnedTitle",
                table: "Services",
                newName: "Unearned");

            migrationBuilder.RenameColumn(
                name: "CurrentAndPreviousTitle",
                table: "Services",
                newName: "CurrentAndPrevious");
        }
    }
}