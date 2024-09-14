using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class AddNeededFieldsForImplementingImportAndExportV10 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "bank_code",
                table: "bank_accounts");

            migrationBuilder.AddColumn<int>(
                name: "original_bank_id",
                table: "bank_accounts",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "original_bank_id",
                table: "bank_accounts");

            migrationBuilder.AddColumn<string>(
                name: "bank_code",
                table: "bank_accounts",
                type: "text",
                nullable: true);
        }
    }
}
