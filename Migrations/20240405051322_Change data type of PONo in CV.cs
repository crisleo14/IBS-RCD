using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class ChangedatatypeofPONoinCV : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string[]>(
                name: "PONo",
                table: "CheckVoucherHeaders",
                type: "varchar[]",
                nullable: true,
                oldClrType: typeof(string[]),
                oldType: "varchar[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string[]>(
                name: "PONo",
                table: "CheckVoucherHeaders",
                type: "varchar[]",
                nullable: false,
                defaultValue: new string[0],
                oldClrType: typeof(string[]),
                oldType: "varchar[]",
                oldNullable: true);
        }
    }
}