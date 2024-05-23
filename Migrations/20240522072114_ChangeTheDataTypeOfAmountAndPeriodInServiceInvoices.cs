using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class ChangeTheDataTypeOfAmountAndPeriodInServiceInvoices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CreditMemos_StatementOfAccounts_SOAId",
                table: "CreditMemos");

            migrationBuilder.DropForeignKey(
                name: "FK_DebitMemos_StatementOfAccounts_SOAId",
                table: "DebitMemos");

            migrationBuilder.DropForeignKey(
                name: "FK_OfficialReceipts_StatementOfAccounts_SOAId",
                table: "OfficialReceipts");

            migrationBuilder.DropTable(
                name: "StatementOfAccounts");

            migrationBuilder.CreateTable(
                name: "ServiceInvoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SVNo = table.Column<string>(type: "varchar(12)", nullable: true),
                    SeriesNumber = table.Column<long>(type: "bigint", nullable: false),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    ServicesId = table.Column<int>(type: "integer", nullable: false),
                    ServiceNo = table.Column<int>(type: "integer", nullable: false),
                    DueDate = table.Column<DateTime>(type: "date", nullable: false),
                    Period = table.Column<DateTime>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    VatAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    NetAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Discount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    WithholdingTaxAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    WithholdingVatAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CurrentAndPreviousAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    UnearnedAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", nullable: false),
                    AmountPaid = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Balance = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Instructions = table.Column<string>(type: "varchar(200)", nullable: true),
                    IsPaid = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_ServiceInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceInvoices_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceInvoices_Services_ServicesId",
                        column: x => x.ServicesId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceInvoices_CustomerId",
                table: "ServiceInvoices",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceInvoices_ServicesId",
                table: "ServiceInvoices",
                column: "ServicesId");

            migrationBuilder.AddForeignKey(
                name: "FK_CreditMemos_ServiceInvoices_SOAId",
                table: "CreditMemos",
                column: "SOAId",
                principalTable: "ServiceInvoices",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DebitMemos_ServiceInvoices_SOAId",
                table: "DebitMemos",
                column: "SOAId",
                principalTable: "ServiceInvoices",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OfficialReceipts_ServiceInvoices_SOAId",
                table: "OfficialReceipts",
                column: "SOAId",
                principalTable: "ServiceInvoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CreditMemos_ServiceInvoices_SOAId",
                table: "CreditMemos");

            migrationBuilder.DropForeignKey(
                name: "FK_DebitMemos_ServiceInvoices_SOAId",
                table: "DebitMemos");

            migrationBuilder.DropForeignKey(
                name: "FK_OfficialReceipts_ServiceInvoices_SOAId",
                table: "OfficialReceipts");

            migrationBuilder.DropTable(
                name: "ServiceInvoices");

            migrationBuilder.CreateTable(
                name: "StatementOfAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerId = table.Column<int>(type: "integer", nullable: false),
                    ServicesId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal[]>(type: "numeric[]", nullable: false),
                    AmountPaid = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Balance = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CanceledBy = table.Column<string>(type: "varchar(50)", nullable: true),
                    CanceledDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "varchar(50)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CurrentAndPreviousAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Discount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DueDate = table.Column<DateTime>(type: "date", nullable: false),
                    Instructions = table.Column<string>(type: "varchar(200)", nullable: true),
                    IsCanceled = table.Column<bool>(type: "boolean", nullable: false),
                    IsPaid = table.Column<bool>(type: "boolean", nullable: false),
                    IsPosted = table.Column<bool>(type: "boolean", nullable: false),
                    IsPrinted = table.Column<bool>(type: "boolean", nullable: false),
                    IsVoided = table.Column<bool>(type: "boolean", nullable: false),
                    NetAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Period = table.Column<DateTime[]>(type: "timestamp with time zone[]", nullable: false),
                    PostedBy = table.Column<string>(type: "varchar(50)", nullable: true),
                    PostedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SOANo = table.Column<string>(type: "varchar(12)", nullable: true),
                    SeriesNumber = table.Column<long>(type: "bigint", nullable: false),
                    ServiceNo = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    UnearnedAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    VatAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    VoidedBy = table.Column<string>(type: "varchar(50)", nullable: true),
                    VoidedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WithholdingTaxAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    WithholdingVatAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatementOfAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StatementOfAccounts_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StatementOfAccounts_Services_ServicesId",
                        column: x => x.ServicesId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StatementOfAccounts_CustomerId",
                table: "StatementOfAccounts",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_StatementOfAccounts_ServicesId",
                table: "StatementOfAccounts",
                column: "ServicesId");

            migrationBuilder.AddForeignKey(
                name: "FK_CreditMemos_StatementOfAccounts_SOAId",
                table: "CreditMemos",
                column: "SOAId",
                principalTable: "StatementOfAccounts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DebitMemos_StatementOfAccounts_SOAId",
                table: "DebitMemos",
                column: "SOAId",
                principalTable: "StatementOfAccounts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OfficialReceipts_StatementOfAccounts_SOAId",
                table: "OfficialReceipts",
                column: "SOAId",
                principalTable: "StatementOfAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
