using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class editamountdatatypeandperiod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal[]>(
                name: "Amount",
                table: "StatementOfAccounts",
                type: "numeric(18,2)[]",
                nullable: false,
                defaultValue: new decimal[0]);

            migrationBuilder.AddColumn<DateTime[]>(
                name: "Period",
                table: "StatementOfAccounts",
                type: "timestamp without time zone[]",
                nullable: false,
                defaultValue: new DateTime[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Amount",
                table: "StatementOfAccounts");

            migrationBuilder.DropColumn(
                name: "Period",
                table: "StatementOfAccounts");
        }
    }
}