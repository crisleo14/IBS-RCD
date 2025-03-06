using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSeriesNumberAndAccountNoInBankMasterFile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "account_no_coa",
                table: "bank_accounts");

            migrationBuilder.DropColumn(
                name: "series_number",
                table: "bank_accounts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "account_no_coa",
                table: "bank_accounts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "series_number",
                table: "bank_accounts",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }
    }
}
