using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class AddnewfieldSINoandchangedatatypeofRRNoinCV : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string[]>(
                name: "RRNo",
                table: "CheckVoucherHeaders",
                type: "varchar[]",
                nullable: true,
                oldClrType: typeof(string[]),
                oldType: "varchar[]");

            migrationBuilder.AddColumn<string[]>(
                name: "SINo",
                table: "CheckVoucherHeaders",
                type: "varchar[]",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SINo",
                table: "CheckVoucherHeaders");

            migrationBuilder.AlterColumn<string[]>(
                name: "RRNo",
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