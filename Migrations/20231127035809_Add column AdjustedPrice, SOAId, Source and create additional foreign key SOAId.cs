using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class AddcolumnAdjustedPriceSOAIdSourceandcreateadditionalforeignkeySOAId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AdjustedPrice",
                table: "DebitMemos",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "SOAId",
                table: "DebitMemos",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "DebitMemos",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_DebitMemos_SOAId",
                table: "DebitMemos",
                column: "SOAId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.DropIndex(
                name: "IX_DebitMemos_SOAId",
                table: "DebitMemos");

            migrationBuilder.DropColumn(
                name: "AdjustedPrice",
                table: "DebitMemos");

            migrationBuilder.DropColumn(
                name: "SOAId",
                table: "DebitMemos");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "DebitMemos");
        }
    }
}
