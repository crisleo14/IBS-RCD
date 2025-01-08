using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class ChangeColumnNameModifiedByToUploadedBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "modified_by",
                table: "import_export_logs");

            migrationBuilder.AddColumn<string>(
                name: "uploaded_by",
                table: "import_export_logs",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "uploaded_by",
                table: "import_export_logs");

            migrationBuilder.AddColumn<string>(
                name: "modified_by",
                table: "import_export_logs",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
