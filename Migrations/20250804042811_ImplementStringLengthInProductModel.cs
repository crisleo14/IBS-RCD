using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class ImplementStringLengthInProductModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "code",
                table: "products");

            migrationBuilder.DropColumn(
                name: "name",
                table: "products");

            migrationBuilder.DropColumn(
                name: "unit",
                table: "products");

            migrationBuilder.AddColumn<string>(
                name: "product_code",
                table: "products",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "product_name",
                table: "products",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "product_unit",
                table: "products",
                type: "character varying(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "product_code",
                table: "products");

            migrationBuilder.DropColumn(
                name: "product_name",
                table: "products");

            migrationBuilder.DropColumn(
                name: "product_unit",
                table: "products");

            migrationBuilder.AddColumn<string>(
                name: "code",
                table: "products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "products",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "unit",
                table: "products",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
