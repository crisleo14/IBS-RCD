using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class AddNewFieldInChartOfAccountModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "level",
                table: "chart_of_accounts",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "has_children",
                table: "chart_of_accounts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "parent_account_id",
                table: "chart_of_accounts",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_chart_of_accounts_parent_account_id",
                table: "chart_of_accounts",
                column: "parent_account_id");

            migrationBuilder.AddForeignKey(
                name: "fk_chart_of_accounts_chart_of_accounts_parent_account_id",
                table: "chart_of_accounts",
                column: "parent_account_id",
                principalTable: "chart_of_accounts",
                principalColumn: "account_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_chart_of_accounts_chart_of_accounts_parent_account_id",
                table: "chart_of_accounts");

            migrationBuilder.DropIndex(
                name: "ix_chart_of_accounts_parent_account_id",
                table: "chart_of_accounts");

            migrationBuilder.DropColumn(
                name: "has_children",
                table: "chart_of_accounts");

            migrationBuilder.DropColumn(
                name: "parent_account_id",
                table: "chart_of_accounts");

            migrationBuilder.AlterColumn<int>(
                name: "level",
                table: "chart_of_accounts",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");
        }
    }
}
