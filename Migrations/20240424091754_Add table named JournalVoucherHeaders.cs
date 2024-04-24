using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class AddtablenamedJournalVoucherHeaders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "journalVoucherHeaders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JVNo = table.Column<string>(type: "text", nullable: true),
                    SeriesNumber = table.Column<long>(type: "bigint", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    References = table.Column<string>(type: "text", nullable: true),
                    CVId = table.Column<int>(type: "integer", nullable: false),
                    Particulars = table.Column<string>(type: "text", nullable: false),
                    CRNo = table.Column<string>(type: "text", nullable: true),
                    JVReason = table.Column<string>(type: "text", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(50)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsPrinted = table.Column<bool>(type: "boolean", nullable: false),
                    IsCanceled = table.Column<bool>(type: "boolean", nullable: false),
                    IsVoided = table.Column<bool>(type: "boolean", nullable: false),
                    IsPosted = table.Column<bool>(type: "boolean", nullable: false),
                    CanceledBy = table.Column<string>(type: "varchar(50)", nullable: true),
                    CanceledDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VoidedBy = table.Column<string>(type: "varchar(50)", nullable: true),
                    VoidedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PostedBy = table.Column<string>(type: "varchar(50)", nullable: true),
                    PostedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_journalVoucherHeaders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_journalVoucherHeaders_CheckVoucherHeaders_CVId",
                        column: x => x.CVId,
                        principalTable: "CheckVoucherHeaders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_journalVoucherHeaders_CVId",
                table: "journalVoucherHeaders",
                column: "CVId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "journalVoucherHeaders");
        }
    }
}
