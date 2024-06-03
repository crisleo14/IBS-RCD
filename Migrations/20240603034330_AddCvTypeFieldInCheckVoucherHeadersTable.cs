using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class AddCvTypeFieldInCheckVoucherHeadersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Sequence",
                table: "CheckVoucherHeaders");

            migrationBuilder.AlterColumn<decimal[]>(
                name: "Amount",
                table: "CheckVoucherHeaders",
                type: "numeric[]",
                nullable: true,
                oldClrType: typeof(decimal[]),
                oldType: "numeric[]");

            migrationBuilder.AddColumn<string>(
                name: "CvType",
                table: "CheckVoucherHeaders",
                type: "varchar(10)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CvType",
                table: "CheckVoucherHeaders");

            migrationBuilder.AlterColumn<decimal[]>(
                name: "Amount",
                table: "CheckVoucherHeaders",
                type: "numeric[]",
                nullable: false,
                defaultValue: new decimal[0],
                oldClrType: typeof(decimal[]),
                oldType: "numeric[]",
                oldNullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Sequence",
                table: "CheckVoucherHeaders",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }
    }
}
