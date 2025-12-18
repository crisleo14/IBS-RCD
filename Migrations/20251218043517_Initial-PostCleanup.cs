using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostCleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    discriminator = table.Column<string>(type: "character varying(21)", maxLength: 21, nullable: false),
                    first_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    security_stamp = table.Column<string>(type: "text", nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true),
                    phone_number = table.Column<string>(type: "text", nullable: true),
                    phone_number_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    two_factor_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    lockout_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lockout_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    access_failed_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audit_trails",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    machine_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    activity = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    document_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_trails", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "bank_accounts",
                columns: table => new
                {
                    bank_account_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    bank = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    account_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    original_bank_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bank_accounts", x => x.bank_account_id);
                });

            migrationBuilder.CreateTable(
                name: "cash_receipt_books",
                columns: table => new
                {
                    cash_receipt_book_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    ref_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    customer_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    bank = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    check_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    coa = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    particulars = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    debit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    credit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cash_receipt_books", x => x.cash_receipt_book_id);
                });

            migrationBuilder.CreateTable(
                name: "chart_of_accounts",
                columns: table => new
                {
                    account_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    is_main = table.Column<bool>(type: "boolean", nullable: false),
                    account_number = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    account_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    account_type = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: true),
                    normal_balance = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    level = table.Column<int>(type: "integer", nullable: false),
                    parent_account_id = table.Column<int>(type: "integer", nullable: true),
                    parent = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    edited_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    edited_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    original_chart_of_account_id = table.Column<int>(type: "integer", nullable: true),
                    has_children = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_chart_of_accounts", x => x.account_id);
                    table.ForeignKey(
                        name: "fk_chart_of_accounts_chart_of_accounts_parent_account_id",
                        column: x => x.parent_account_id,
                        principalTable: "chart_of_accounts",
                        principalColumn: "account_id");
                });

            migrationBuilder.CreateTable(
                name: "customers",
                columns: table => new
                {
                    customer_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    number = table.Column<int>(type: "integer", nullable: false),
                    customer_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    customer_address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    customer_tin = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    business_style = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    customer_terms = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    customer_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    with_holding_vat = table.Column<bool>(type: "boolean", nullable: false),
                    with_holding_tax = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    original_customer_id = table.Column<int>(type: "integer", nullable: false),
                    original_customer_number = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    zip_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customers", x => x.customer_id);
                });

            migrationBuilder.CreateTable(
                name: "disbursement_books",
                columns: table => new
                {
                    disbursement_book_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    cv_no = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    payee = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    particulars = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    bank = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    check_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    check_date = table.Column<string>(type: "text", nullable: false),
                    chart_of_account = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    debit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    credit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_disbursement_books", x => x.disbursement_book_id);
                });

            migrationBuilder.CreateTable(
                name: "general_ledger_books",
                columns: table => new
                {
                    general_ledger_book_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    reference = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    account_no = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    account_title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    debit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    credit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    is_posted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_general_ledger_books", x => x.general_ledger_book_id);
                });

            migrationBuilder.CreateTable(
                name: "import_export_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_record_id = table.Column<int>(type: "integer", nullable: false),
                    module = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    table_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    column_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    original_value = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    adjusted_value = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    time_stamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    uploaded_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    action = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    executed = table.Column<bool>(type: "boolean", nullable: false),
                    document_no = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    database_name = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_import_export_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "journal_books",
                columns: table => new
                {
                    journal_book_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    reference = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    account_title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    debit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    credit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_journal_books", x => x.journal_book_id);
                });

            migrationBuilder.CreateTable(
                name: "offsettings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    account_no = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    account_title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    reference = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    is_removed = table.Column<bool>(type: "boolean", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_offsettings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    product_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    product_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    product_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    product_unit = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    original_product_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_products", x => x.product_id);
                });

            migrationBuilder.CreateTable(
                name: "purchase_journal_books",
                columns: table => new
                {
                    purchase_book_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    supplier_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    supplier_tin = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    supplier_address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    document_no = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    discount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    vat_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    wht_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    net_purchases = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    po_no = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchase_journal_books", x => x.purchase_book_id);
                });

            migrationBuilder.CreateTable(
                name: "sales_books",
                columns: table => new
                {
                    sales_book_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    transaction_date = table.Column<DateOnly>(type: "date", nullable: false),
                    serial_no = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    sold_to = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tin_no = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    vat_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    vatable_sales = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    vat_exempt_sales = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    zero_rated = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    discount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    net_sales = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    document_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sales_books", x => x.sales_book_id);
                });

            migrationBuilder.CreateTable(
                name: "services",
                columns: table => new
                {
                    service_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    service_no = table.Column<int>(type: "integer", nullable: false),
                    current_and_previous_no = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    current_and_previous_title = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    unearned_title = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    unearned_no = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    percent = table.Column<int>(type: "integer", nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    original_service_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_services", x => x.service_id);
                });

            migrationBuilder.CreateTable(
                name: "suppliers",
                columns: table => new
                {
                    supplier_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    number = table.Column<int>(type: "integer", nullable: false),
                    supplier_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    supplier_address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    supplier_tin = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    supplier_terms = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    vat_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    tax_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    proof_of_registration_file_path = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    reason_of_exemption = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    validity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    validity_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    proof_of_exemption_file_path = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    trade_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    branch = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    default_expense_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    withholding_tax_percent = table.Column<int>(type: "integer", nullable: true),
                    withholding_taxtitle = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    original_supplier_id = table.Column<int>(type: "integer", nullable: true),
                    zip_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_suppliers", x => x.supplier_id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    role_id = table.Column<string>(type: "text", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_role_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_asp_net_role_claims_asp_net_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "AspNetRoles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_user_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_asp_net_user_claims_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    login_provider = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    provider_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    provider_display_name = table.Column<string>(type: "text", nullable: true),
                    user_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_user_logins", x => new { x.login_provider, x.provider_key });
                    table.ForeignKey(
                        name: "fk_asp_net_user_logins_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "text", nullable: false),
                    role_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_asp_net_user_roles_asp_net_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "AspNetRoles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_asp_net_user_roles_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "text", nullable: false),
                    login_provider = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_user_tokens", x => new { x.user_id, x.login_provider, x.name });
                    table.ForeignKey(
                        name: "fk_asp_net_user_tokens_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inventories",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: true),
                    particular = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    reference = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    cost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    inventory_balance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    average_cost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    total_balance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unit = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    is_validated = table.Column<bool>(type: "boolean", nullable: false),
                    validated_by = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    validated_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    original_product_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventories", x => x.id);
                    table.ForeignKey(
                        name: "fk_inventories_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "product_id");
                });

            migrationBuilder.CreateTable(
                name: "sales_invoices",
                columns: table => new
                {
                    sales_invoice_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sales_invoice_no = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    other_ref_no = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    transaction_date = table.Column<DateOnly>(type: "date", nullable: false),
                    discount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    balance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_paid = table.Column<bool>(type: "boolean", nullable: false),
                    is_tax_and_vat_paid = table.Column<bool>(type: "boolean", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    original_customer_id = table.Column<int>(type: "integer", nullable: true),
                    original_product_id = table.Column<int>(type: "integer", nullable: true),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    is_printed = table.Column<bool>(type: "boolean", nullable: false),
                    is_canceled = table.Column<bool>(type: "boolean", nullable: false),
                    is_voided = table.Column<bool>(type: "boolean", nullable: false),
                    is_posted = table.Column<bool>(type: "boolean", nullable: false),
                    canceled_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    canceled_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    voided_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    voided_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    posted_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    posted_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    cancellation_remarks = table.Column<string>(type: "text", nullable: true),
                    original_series_number = table.Column<string>(type: "text", nullable: true),
                    original_document_id = table.Column<int>(type: "integer", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: false),
                    edited_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sales_invoices", x => x.sales_invoice_id);
                    table.ForeignKey(
                        name: "fk_sales_invoices_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "customer_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sales_invoices_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "product_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "service_invoices",
                columns: table => new
                {
                    service_invoice_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    service_invoice_no = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    services_id = table.Column<int>(type: "integer", nullable: false),
                    service_no = table.Column<int>(type: "integer", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    period = table.Column<DateOnly>(type: "date", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    discount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    current_and_previous_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unearned_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    balance = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    instructions = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_paid = table.Column<bool>(type: "boolean", nullable: false),
                    original_customer_id = table.Column<int>(type: "integer", nullable: true),
                    original_services_id = table.Column<int>(type: "integer", nullable: true),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    is_printed = table.Column<bool>(type: "boolean", nullable: false),
                    is_canceled = table.Column<bool>(type: "boolean", nullable: false),
                    is_voided = table.Column<bool>(type: "boolean", nullable: false),
                    is_posted = table.Column<bool>(type: "boolean", nullable: false),
                    canceled_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    canceled_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    voided_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    voided_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    posted_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    posted_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    cancellation_remarks = table.Column<string>(type: "text", nullable: true),
                    original_series_number = table.Column<string>(type: "text", nullable: true),
                    original_document_id = table.Column<int>(type: "integer", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: false),
                    edited_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_invoices", x => x.service_invoice_id);
                    table.ForeignKey(
                        name: "fk_service_invoices_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "customer_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_service_invoices_services_services_id",
                        column: x => x.services_id,
                        principalTable: "services",
                        principalColumn: "service_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "check_voucher_headers",
                columns: table => new
                {
                    check_voucher_header_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    check_voucher_header_no = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    rr_no = table.Column<string[]>(type: "varchar[]", nullable: true),
                    si_no = table.Column<string[]>(type: "varchar[]", nullable: true),
                    po_no = table.Column<string[]>(type: "varchar[]", nullable: true),
                    supplier_id = table.Column<int>(type: "integer", nullable: true),
                    total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    amount = table.Column<decimal[]>(type: "numeric(18,4)[]", nullable: true),
                    particulars = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    bank_id = table.Column<int>(type: "integer", nullable: true),
                    check_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    payee = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    check_date = table.Column<DateOnly>(type: "date", nullable: true),
                    start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    number_of_months = table.Column<int>(type: "integer", nullable: false),
                    number_of_months_created = table.Column<int>(type: "integer", nullable: false),
                    last_created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    amount_per_month = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_complete = table.Column<bool>(type: "boolean", nullable: false),
                    accrued_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    reference = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    cv_type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    check_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_paid = table.Column<bool>(type: "boolean", nullable: false),
                    invoice_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    original_supplier_id = table.Column<int>(type: "integer", nullable: true),
                    original_bank_id = table.Column<int>(type: "integer", nullable: true),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    is_printed = table.Column<bool>(type: "boolean", nullable: false),
                    is_canceled = table.Column<bool>(type: "boolean", nullable: false),
                    is_voided = table.Column<bool>(type: "boolean", nullable: false),
                    is_posted = table.Column<bool>(type: "boolean", nullable: false),
                    canceled_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    canceled_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    voided_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    voided_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    posted_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    posted_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    cancellation_remarks = table.Column<string>(type: "text", nullable: true),
                    original_series_number = table.Column<string>(type: "text", nullable: true),
                    original_document_id = table.Column<int>(type: "integer", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: false),
                    edited_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_check_voucher_headers", x => x.check_voucher_header_id);
                    table.ForeignKey(
                        name: "fk_check_voucher_headers_bank_accounts_bank_id",
                        column: x => x.bank_id,
                        principalTable: "bank_accounts",
                        principalColumn: "bank_account_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_check_voucher_headers_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "suppliers",
                        principalColumn: "supplier_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_orders",
                columns: table => new
                {
                    purchase_order_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    purchase_order_no = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    supplier_id = table.Column<int>(type: "integer", nullable: false),
                    supplier_no = table.Column<int>(type: "integer", nullable: false),
                    product_id = table.Column<int>(type: "integer", nullable: false),
                    product_no = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    terms = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    final_price = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    quantity_received = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_received = table.Column<bool>(type: "boolean", nullable: false),
                    received_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    is_closed = table.Column<bool>(type: "boolean", nullable: false),
                    original_supplier_id = table.Column<int>(type: "integer", nullable: true),
                    original_product_id = table.Column<int>(type: "integer", nullable: true),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    is_printed = table.Column<bool>(type: "boolean", nullable: false),
                    is_canceled = table.Column<bool>(type: "boolean", nullable: false),
                    is_voided = table.Column<bool>(type: "boolean", nullable: false),
                    is_posted = table.Column<bool>(type: "boolean", nullable: false),
                    canceled_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    canceled_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    voided_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    voided_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    posted_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    posted_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    cancellation_remarks = table.Column<string>(type: "text", nullable: true),
                    original_series_number = table.Column<string>(type: "text", nullable: true),
                    original_document_id = table.Column<int>(type: "integer", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: false),
                    edited_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchase_orders", x => x.purchase_order_id);
                    table.ForeignKey(
                        name: "fk_purchase_orders_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "product_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_purchase_orders_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "suppliers",
                        principalColumn: "supplier_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "collection_receipts",
                columns: table => new
                {
                    collection_receipt_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    collection_receipt_no = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    sales_invoice_id = table.Column<int>(type: "integer", nullable: true),
                    si_no = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    multiple_si_id = table.Column<int[]>(type: "integer[]", nullable: true),
                    multiple_si = table.Column<string[]>(type: "text[]", nullable: true),
                    service_invoice_id = table.Column<int>(type: "integer", nullable: true),
                    sv_no = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    customer_id = table.Column<int>(type: "integer", nullable: false),
                    transaction_date = table.Column<DateOnly>(type: "date", nullable: false),
                    reference_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    cash_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    check_date = table.Column<DateOnly>(type: "date", nullable: true),
                    check_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    check_bank = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    check_branch = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    check_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    manager_check_date = table.Column<DateOnly>(type: "date", nullable: true),
                    manager_check_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    manager_check_bank = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    manager_check_branch = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    manager_check_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ewt = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    wvat = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_certificate_upload = table.Column<bool>(type: "boolean", nullable: false),
                    f2306file_path = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    f2307file_path = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    si_multiple_amount = table.Column<decimal[]>(type: "numeric(18,4)[]", nullable: true),
                    multiple_transaction_date = table.Column<DateOnly[]>(type: "date[]", nullable: true),
                    original_sales_invoice_id = table.Column<int>(type: "integer", nullable: true),
                    original_service_invoice_id = table.Column<int>(type: "integer", nullable: true),
                    original_customer_id = table.Column<int>(type: "integer", nullable: true),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    is_printed = table.Column<bool>(type: "boolean", nullable: false),
                    is_canceled = table.Column<bool>(type: "boolean", nullable: false),
                    is_voided = table.Column<bool>(type: "boolean", nullable: false),
                    is_posted = table.Column<bool>(type: "boolean", nullable: false),
                    canceled_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    canceled_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    voided_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    voided_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    posted_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    posted_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    cancellation_remarks = table.Column<string>(type: "text", nullable: true),
                    original_series_number = table.Column<string>(type: "text", nullable: true),
                    original_document_id = table.Column<int>(type: "integer", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: false),
                    edited_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_collection_receipts", x => x.collection_receipt_id);
                    table.ForeignKey(
                        name: "fk_collection_receipts_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "customer_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_collection_receipts_sales_invoices_sales_invoice_id",
                        column: x => x.sales_invoice_id,
                        principalTable: "sales_invoices",
                        principalColumn: "sales_invoice_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_collection_receipts_service_invoices_service_invoice_id",
                        column: x => x.service_invoice_id,
                        principalTable: "service_invoices",
                        principalColumn: "service_invoice_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "credit_memos",
                columns: table => new
                {
                    credit_memo_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    credit_memo_no = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    transaction_date = table.Column<DateOnly>(type: "date", nullable: false),
                    sales_invoice_id = table.Column<int>(type: "integer", nullable: true),
                    service_invoice_id = table.Column<int>(type: "integer", nullable: true),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    adjusted_price = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    credit_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    period = table.Column<DateOnly>(type: "date", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    current_and_previous_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unearned_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    services_id = table.Column<int>(type: "integer", nullable: true),
                    original_sales_invoice_id = table.Column<int>(type: "integer", nullable: true),
                    original_service_invoice_id = table.Column<int>(type: "integer", nullable: true),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    is_printed = table.Column<bool>(type: "boolean", nullable: false),
                    is_canceled = table.Column<bool>(type: "boolean", nullable: false),
                    is_voided = table.Column<bool>(type: "boolean", nullable: false),
                    is_posted = table.Column<bool>(type: "boolean", nullable: false),
                    canceled_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    canceled_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    voided_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    voided_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    posted_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    posted_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    cancellation_remarks = table.Column<string>(type: "text", nullable: true),
                    original_series_number = table.Column<string>(type: "text", nullable: true),
                    original_document_id = table.Column<int>(type: "integer", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: false),
                    edited_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_credit_memos", x => x.credit_memo_id);
                    table.ForeignKey(
                        name: "fk_credit_memos_sales_invoices_sales_invoice_id",
                        column: x => x.sales_invoice_id,
                        principalTable: "sales_invoices",
                        principalColumn: "sales_invoice_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_credit_memos_service_invoices_service_invoice_id",
                        column: x => x.service_invoice_id,
                        principalTable: "service_invoices",
                        principalColumn: "service_invoice_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "debit_memos",
                columns: table => new
                {
                    debit_memo_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sales_invoice_id = table.Column<int>(type: "integer", nullable: true),
                    service_invoice_id = table.Column<int>(type: "integer", nullable: true),
                    debit_memo_no = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    transaction_date = table.Column<DateOnly>(type: "date", nullable: false),
                    debit_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    adjusted_price = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    period = table.Column<DateOnly>(type: "date", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    current_and_previous_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unearned_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    services_id = table.Column<int>(type: "integer", nullable: true),
                    original_sales_invoice_id = table.Column<int>(type: "integer", nullable: true),
                    original_service_invoice_id = table.Column<int>(type: "integer", nullable: true),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    is_printed = table.Column<bool>(type: "boolean", nullable: false),
                    is_canceled = table.Column<bool>(type: "boolean", nullable: false),
                    is_voided = table.Column<bool>(type: "boolean", nullable: false),
                    is_posted = table.Column<bool>(type: "boolean", nullable: false),
                    canceled_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    canceled_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    voided_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    voided_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    posted_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    posted_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    cancellation_remarks = table.Column<string>(type: "text", nullable: true),
                    original_series_number = table.Column<string>(type: "text", nullable: true),
                    original_document_id = table.Column<int>(type: "integer", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: false),
                    edited_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_debit_memos", x => x.debit_memo_id);
                    table.ForeignKey(
                        name: "fk_debit_memos_sales_invoices_sales_invoice_id",
                        column: x => x.sales_invoice_id,
                        principalTable: "sales_invoices",
                        principalColumn: "sales_invoice_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_debit_memos_service_invoices_service_invoice_id",
                        column: x => x.service_invoice_id,
                        principalTable: "service_invoices",
                        principalColumn: "service_invoice_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "check_voucher_details",
                columns: table => new
                {
                    check_voucher_detail_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    account_no = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    account_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    transaction_no = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    debit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    credit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    check_voucher_header_id = table.Column<int>(type: "integer", nullable: true),
                    original_document_id = table.Column<int>(type: "integer", nullable: true),
                    supplier_id = table.Column<int>(type: "integer", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_vatable = table.Column<bool>(type: "boolean", nullable: false),
                    ewt_percent = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_user_selected = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_check_voucher_details", x => x.check_voucher_detail_id);
                    table.ForeignKey(
                        name: "fk_check_voucher_details_check_voucher_headers_check_voucher_h",
                        column: x => x.check_voucher_header_id,
                        principalTable: "check_voucher_headers",
                        principalColumn: "check_voucher_header_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_check_voucher_details_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "suppliers",
                        principalColumn: "supplier_id");
                });

            migrationBuilder.CreateTable(
                name: "cv_trade_payments",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    document_id = table.Column<int>(type: "integer", nullable: false),
                    document_type = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    check_voucher_id = table.Column<int>(type: "integer", nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cv_trade_payments", x => x.id);
                    table.ForeignKey(
                        name: "fk_cv_trade_payments_check_voucher_headers_check_voucher_id",
                        column: x => x.check_voucher_id,
                        principalTable: "check_voucher_headers",
                        principalColumn: "check_voucher_header_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "journal_voucher_headers",
                columns: table => new
                {
                    journal_voucher_header_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    journal_voucher_header_no = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    references = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    cv_id = table.Column<int>(type: "integer", nullable: true),
                    particulars = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    cr_no = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    jv_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    original_cv_id = table.Column<int>(type: "integer", nullable: true),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    is_printed = table.Column<bool>(type: "boolean", nullable: false),
                    is_canceled = table.Column<bool>(type: "boolean", nullable: false),
                    is_voided = table.Column<bool>(type: "boolean", nullable: false),
                    is_posted = table.Column<bool>(type: "boolean", nullable: false),
                    canceled_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    canceled_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    voided_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    voided_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    posted_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    posted_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    cancellation_remarks = table.Column<string>(type: "text", nullable: true),
                    original_series_number = table.Column<string>(type: "text", nullable: true),
                    original_document_id = table.Column<int>(type: "integer", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: false),
                    edited_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_journal_voucher_headers", x => x.journal_voucher_header_id);
                    table.ForeignKey(
                        name: "fk_journal_voucher_headers_check_voucher_headers_cv_id",
                        column: x => x.cv_id,
                        principalTable: "check_voucher_headers",
                        principalColumn: "check_voucher_header_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "multiple_check_voucher_payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    check_voucher_header_payment_id = table.Column<int>(type: "integer", nullable: false),
                    check_voucher_header_invoice_id = table.Column<int>(type: "integer", nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_multiple_check_voucher_payments", x => x.id);
                    table.ForeignKey(
                        name: "fk_multiple_check_voucher_payments_check_voucher_headers_check",
                        column: x => x.check_voucher_header_invoice_id,
                        principalTable: "check_voucher_headers",
                        principalColumn: "check_voucher_header_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_multiple_check_voucher_payments_check_voucher_headers_check1",
                        column: x => x.check_voucher_header_payment_id,
                        principalTable: "check_voucher_headers",
                        principalColumn: "check_voucher_header_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "receiving_reports",
                columns: table => new
                {
                    receiving_report_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    receiving_report_no = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    po_id = table.Column<int>(type: "integer", nullable: false),
                    po_no = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    supplier_invoice_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    supplier_invoice_date = table.Column<string>(type: "text", nullable: true),
                    truck_or_vessels = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    quantity_delivered = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    quantity_received = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    gain_or_loss = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    other_ref = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    is_paid = table.Column<bool>(type: "boolean", nullable: false),
                    paid_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    canceled_quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    received_date = table.Column<DateOnly>(type: "date", nullable: true),
                    original_po_id = table.Column<int>(type: "integer", nullable: true),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    is_printed = table.Column<bool>(type: "boolean", nullable: false),
                    is_canceled = table.Column<bool>(type: "boolean", nullable: false),
                    is_voided = table.Column<bool>(type: "boolean", nullable: false),
                    is_posted = table.Column<bool>(type: "boolean", nullable: false),
                    canceled_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    canceled_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    voided_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    voided_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    posted_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    posted_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    cancellation_remarks = table.Column<string>(type: "text", nullable: true),
                    original_series_number = table.Column<string>(type: "text", nullable: true),
                    original_document_id = table.Column<int>(type: "integer", nullable: false),
                    edited_by = table.Column<string>(type: "varchar(50)", nullable: false),
                    edited_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_receiving_reports", x => x.receiving_report_id);
                    table.ForeignKey(
                        name: "fk_receiving_reports_purchase_orders_po_id",
                        column: x => x.po_id,
                        principalTable: "purchase_orders",
                        principalColumn: "purchase_order_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "journal_voucher_details",
                columns: table => new
                {
                    journal_voucher_detail_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    account_no = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    account_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    transaction_no = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    debit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    credit = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    journal_voucher_header_id = table.Column<int>(type: "integer", nullable: false),
                    original_document_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_journal_voucher_details", x => x.journal_voucher_detail_id);
                    table.ForeignKey(
                        name: "fk_journal_voucher_details_journal_voucher_headers_journal_vou",
                        column: x => x.journal_voucher_header_id,
                        principalTable: "journal_voucher_headers",
                        principalColumn: "journal_voucher_header_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_role_claims_role_id",
                table: "AspNetRoleClaims",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "normalized_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_user_claims_user_id",
                table: "AspNetUserClaims",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_user_logins_user_id",
                table: "AspNetUserLogins",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_user_roles_role_id",
                table: "AspNetUserRoles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "normalized_email");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "normalized_user_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_chart_of_accounts_parent_account_id",
                table: "chart_of_accounts",
                column: "parent_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_check_voucher_details_check_voucher_header_id",
                table: "check_voucher_details",
                column: "check_voucher_header_id");

            migrationBuilder.CreateIndex(
                name: "ix_check_voucher_details_supplier_id",
                table: "check_voucher_details",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_check_voucher_headers_bank_id",
                table: "check_voucher_headers",
                column: "bank_id");

            migrationBuilder.CreateIndex(
                name: "ix_check_voucher_headers_supplier_id",
                table: "check_voucher_headers",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_collection_receipts_customer_id",
                table: "collection_receipts",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_collection_receipts_sales_invoice_id",
                table: "collection_receipts",
                column: "sales_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_collection_receipts_service_invoice_id",
                table: "collection_receipts",
                column: "service_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_credit_memos_sales_invoice_id",
                table: "credit_memos",
                column: "sales_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_credit_memos_service_invoice_id",
                table: "credit_memos",
                column: "service_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_cv_trade_payments_check_voucher_id",
                table: "cv_trade_payments",
                column: "check_voucher_id");

            migrationBuilder.CreateIndex(
                name: "ix_debit_memos_sales_invoice_id",
                table: "debit_memos",
                column: "sales_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_debit_memos_service_invoice_id",
                table: "debit_memos",
                column: "service_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventories_product_id",
                table: "inventories",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_journal_voucher_details_journal_voucher_header_id",
                table: "journal_voucher_details",
                column: "journal_voucher_header_id");

            migrationBuilder.CreateIndex(
                name: "ix_journal_voucher_headers_cv_id",
                table: "journal_voucher_headers",
                column: "cv_id");

            migrationBuilder.CreateIndex(
                name: "ix_multiple_check_voucher_payments_check_voucher_header_invoic",
                table: "multiple_check_voucher_payments",
                column: "check_voucher_header_invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_multiple_check_voucher_payments_check_voucher_header_paymen",
                table: "multiple_check_voucher_payments",
                column: "check_voucher_header_payment_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_product_id",
                table: "purchase_orders",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_supplier_id",
                table: "purchase_orders",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_receiving_reports_po_id",
                table: "receiving_reports",
                column: "po_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_invoices_customer_id",
                table: "sales_invoices",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_sales_invoices_product_id",
                table: "sales_invoices",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_invoices_customer_id",
                table: "service_invoices",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_invoices_services_id",
                table: "service_invoices",
                column: "services_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "audit_trails");

            migrationBuilder.DropTable(
                name: "cash_receipt_books");

            migrationBuilder.DropTable(
                name: "chart_of_accounts");

            migrationBuilder.DropTable(
                name: "check_voucher_details");

            migrationBuilder.DropTable(
                name: "collection_receipts");

            migrationBuilder.DropTable(
                name: "credit_memos");

            migrationBuilder.DropTable(
                name: "cv_trade_payments");

            migrationBuilder.DropTable(
                name: "debit_memos");

            migrationBuilder.DropTable(
                name: "disbursement_books");

            migrationBuilder.DropTable(
                name: "general_ledger_books");

            migrationBuilder.DropTable(
                name: "import_export_logs");

            migrationBuilder.DropTable(
                name: "inventories");

            migrationBuilder.DropTable(
                name: "journal_books");

            migrationBuilder.DropTable(
                name: "journal_voucher_details");

            migrationBuilder.DropTable(
                name: "multiple_check_voucher_payments");

            migrationBuilder.DropTable(
                name: "offsettings");

            migrationBuilder.DropTable(
                name: "purchase_journal_books");

            migrationBuilder.DropTable(
                name: "receiving_reports");

            migrationBuilder.DropTable(
                name: "sales_books");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "sales_invoices");

            migrationBuilder.DropTable(
                name: "service_invoices");

            migrationBuilder.DropTable(
                name: "journal_voucher_headers");

            migrationBuilder.DropTable(
                name: "purchase_orders");

            migrationBuilder.DropTable(
                name: "customers");

            migrationBuilder.DropTable(
                name: "services");

            migrationBuilder.DropTable(
                name: "check_voucher_headers");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "bank_accounts");

            migrationBuilder.DropTable(
                name: "suppliers");
        }
    }
}
