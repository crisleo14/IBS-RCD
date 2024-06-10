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