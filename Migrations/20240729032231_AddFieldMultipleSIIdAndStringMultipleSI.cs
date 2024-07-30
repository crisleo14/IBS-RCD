using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class AddFieldMultipleSIIdAndStringMultipleSI : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string[]>(
                name: "multiple_si",
                table: "collection_receipts",
                type: "text[]",
                nullable: true);

            migrationBuilder.AddColumn<int[]>(
                name: "multiple_si_id",
                table: "collection_receipts",
                type: "integer[]",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "multiple_si",
                table: "collection_receipts");

            migrationBuilder.DropColumn(
                name: "multiple_si_id",
                table: "collection_receipts");
        }
    }
}
