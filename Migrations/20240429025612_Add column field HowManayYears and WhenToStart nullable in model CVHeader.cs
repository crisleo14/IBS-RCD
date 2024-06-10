using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class AddcolumnfieldHowManayYearsandWhenToStartnullableinmodelCVHeader : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HowManyYears",
                table: "CheckVoucherHeaders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "WhenToStart",
                table: "CheckVoucherHeaders",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HowManyYears",
                table: "CheckVoucherHeaders");

            migrationBuilder.DropColumn(
                name: "WhenToStart",
                table: "CheckVoucherHeaders");
        }
    }
}