using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting_System.Migrations
{
    /// <inheritdoc />
    public partial class ChangingTheNamingConventionInDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                table: "AspNetRoleClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                table: "AspNetUserClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                table: "AspNetUserLogins");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                table: "AspNetUserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                table: "AspNetUserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                table: "AspNetUserTokens");

            migrationBuilder.DropForeignKey(
                name: "FK_CheckVoucherHeaders_BankAccounts_BankId",
                table: "CheckVoucherHeaders");

            migrationBuilder.DropForeignKey(
                name: "FK_CheckVoucherHeaders_Suppliers_SupplierId",
                table: "CheckVoucherHeaders");

            migrationBuilder.DropForeignKey(
                name: "FK_CollectionReceipts_SalesInvoices_SalesInvoiceId",
                table: "CollectionReceipts");

            migrationBuilder.DropForeignKey(
                name: "FK_CollectionReceipts_ServiceInvoices_ServiceInvoiceId",
                table: "CollectionReceipts");

            migrationBuilder.DropForeignKey(
                name: "FK_CreditMemos_SalesInvoices_SalesInvoiceId",
                table: "CreditMemos");

            migrationBuilder.DropForeignKey(
                name: "FK_CreditMemos_ServiceInvoices_ServiceInvoiceId",
                table: "CreditMemos");

            migrationBuilder.DropForeignKey(
                name: "FK_DebitMemos_SalesInvoices_SalesInvoiceId",
                table: "DebitMemos");

            migrationBuilder.DropForeignKey(
                name: "FK_DebitMemos_ServiceInvoices_ServiceInvoiceId",
                table: "DebitMemos");

            migrationBuilder.DropForeignKey(
                name: "FK_Inventories_Products_ProductId",
                table: "Inventories");

            migrationBuilder.DropForeignKey(
                name: "FK_JournalVoucherHeaders_CheckVoucherHeaders_CVId",
                table: "JournalVoucherHeaders");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_Products_ProductId",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_Suppliers_SupplierId",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_ReceivingReports_PurchaseOrders_POId",
                table: "ReceivingReports");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesInvoices_Customers_CustomerId",
                table: "SalesInvoices");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesInvoices_Products_ProductId",
                table: "SalesInvoices");

            migrationBuilder.DropForeignKey(
                name: "FK_ServiceInvoices_Customers_CustomerId",
                table: "ServiceInvoices");

            migrationBuilder.DropForeignKey(
                name: "FK_ServiceInvoices_Services_ServicesId",
                table: "ServiceInvoices");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Suppliers",
                table: "Suppliers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Services",
                table: "Services");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Products",
                table: "Products");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Offsettings",
                table: "Offsettings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Inventories",
                table: "Inventories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Customers",
                table: "Customers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserTokens",
                table: "AspNetUserTokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUsers",
                table: "AspNetUsers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserRoles",
                table: "AspNetUserRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserLogins",
                table: "AspNetUserLogins");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserClaims",
                table: "AspNetUserClaims");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetRoles",
                table: "AspNetRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetRoleClaims",
                table: "AspNetRoleClaims");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ServiceInvoices",
                table: "ServiceInvoices");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SalesInvoices",
                table: "SalesInvoices");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SalesBooks",
                table: "SalesBooks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ReceivingReports",
                table: "ReceivingReports");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PurchaseOrders",
                table: "PurchaseOrders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PurchaseJournalBooks",
                table: "PurchaseJournalBooks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_JournalVoucherHeaders",
                table: "JournalVoucherHeaders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_JournalVoucherDetails",
                table: "JournalVoucherDetails");

            migrationBuilder.DropPrimaryKey(
                name: "PK_JournalBooks",
                table: "JournalBooks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GeneralLedgerBooks",
                table: "GeneralLedgerBooks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DisbursementBooks",
                table: "DisbursementBooks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DebitMemos",
                table: "DebitMemos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CreditMemos",
                table: "CreditMemos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CollectionReceipts",
                table: "CollectionReceipts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CheckVoucherHeaders",
                table: "CheckVoucherHeaders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CheckVoucherDetails",
                table: "CheckVoucherDetails");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ChartOfAccounts",
                table: "ChartOfAccounts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CashReceiptBooks",
                table: "CashReceiptBooks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BankAccounts",
                table: "BankAccounts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AuditTrails",
                table: "AuditTrails");

            migrationBuilder.RenameTable(
                name: "Suppliers",
                newName: "suppliers");

            migrationBuilder.RenameTable(
                name: "Services",
                newName: "services");

            migrationBuilder.RenameTable(
                name: "Products",
                newName: "products");

            migrationBuilder.RenameTable(
                name: "Offsettings",
                newName: "offsettings");

            migrationBuilder.RenameTable(
                name: "Inventories",
                newName: "inventories");

            migrationBuilder.RenameTable(
                name: "Customers",
                newName: "customers");

            migrationBuilder.RenameTable(
                name: "ServiceInvoices",
                newName: "service_invoices");

            migrationBuilder.RenameTable(
                name: "SalesInvoices",
                newName: "sales_invoices");

            migrationBuilder.RenameTable(
                name: "SalesBooks",
                newName: "sales_books");

            migrationBuilder.RenameTable(
                name: "ReceivingReports",
                newName: "receiving_reports");

            migrationBuilder.RenameTable(
                name: "PurchaseOrders",
                newName: "purchase_orders");

            migrationBuilder.RenameTable(
                name: "PurchaseJournalBooks",
                newName: "purchase_journal_books");

            migrationBuilder.RenameTable(
                name: "JournalVoucherHeaders",
                newName: "journal_voucher_headers");

            migrationBuilder.RenameTable(
                name: "JournalVoucherDetails",
                newName: "journal_voucher_details");

            migrationBuilder.RenameTable(
                name: "JournalBooks",
                newName: "journal_books");

            migrationBuilder.RenameTable(
                name: "GeneralLedgerBooks",
                newName: "general_ledger_books");

            migrationBuilder.RenameTable(
                name: "DisbursementBooks",
                newName: "disbursement_books");

            migrationBuilder.RenameTable(
                name: "DebitMemos",
                newName: "debit_memos");

            migrationBuilder.RenameTable(
                name: "CreditMemos",
                newName: "credit_memos");

            migrationBuilder.RenameTable(
                name: "CollectionReceipts",
                newName: "collection_receipts");

            migrationBuilder.RenameTable(
                name: "CheckVoucherHeaders",
                newName: "check_voucher_headers");

            migrationBuilder.RenameTable(
                name: "CheckVoucherDetails",
                newName: "check_voucher_details");

            migrationBuilder.RenameTable(
                name: "ChartOfAccounts",
                newName: "chart_of_accounts");

            migrationBuilder.RenameTable(
                name: "CashReceiptBooks",
                newName: "cash_receipt_books");

            migrationBuilder.RenameTable(
                name: "BankAccounts",
                newName: "bank_accounts");

            migrationBuilder.RenameTable(
                name: "AuditTrails",
                newName: "audit_trails");

            migrationBuilder.RenameColumn(
                name: "Validity",
                table: "suppliers",
                newName: "validity");

            migrationBuilder.RenameColumn(
                name: "Terms",
                table: "suppliers",
                newName: "terms");

            migrationBuilder.RenameColumn(
                name: "Number",
                table: "suppliers",
                newName: "number");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "suppliers",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Address",
                table: "suppliers",
                newName: "address");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "suppliers",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "VatType",
                table: "suppliers",
                newName: "vat_type");

            migrationBuilder.RenameColumn(
                name: "ValidityDate",
                table: "suppliers",
                newName: "validity_date");

            migrationBuilder.RenameColumn(
                name: "TinNo",
                table: "suppliers",
                newName: "tin_no");

            migrationBuilder.RenameColumn(
                name: "TaxType",
                table: "suppliers",
                newName: "tax_type");

            migrationBuilder.RenameColumn(
                name: "ReasonOfExemption",
                table: "suppliers",
                newName: "reason_of_exemption");

            migrationBuilder.RenameColumn(
                name: "ProofOfRegistrationFilePath",
                table: "suppliers",
                newName: "proof_of_registration_file_path");

            migrationBuilder.RenameColumn(
                name: "ProofOfExemptionFilePath",
                table: "suppliers",
                newName: "proof_of_exemption_file_path");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "suppliers",
                newName: "created_date");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "suppliers",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "Percent",
                table: "services",
                newName: "percent");

            migrationBuilder.RenameColumn(
                name: "Number",
                table: "services",
                newName: "number");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "services",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "services",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UnearnedTitle",
                table: "services",
                newName: "unearned_title");

            migrationBuilder.RenameColumn(
                name: "UnearnedNo",
                table: "services",
                newName: "unearned_no");

            migrationBuilder.RenameColumn(
                name: "CurrentAndPreviousTitle",
                table: "services",
                newName: "current_and_previous_title");

            migrationBuilder.RenameColumn(
                name: "CurrentAndPreviousNo",
                table: "services",
                newName: "current_and_previous_no");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "services",
                newName: "created_date");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "services",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "Unit",
                table: "products",
                newName: "unit");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "products",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Code",
                table: "products",
                newName: "code");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "products",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "products",
                newName: "created_date");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "products",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "Source",
                table: "offsettings",
                newName: "source");

            migrationBuilder.RenameColumn(
                name: "Reference",
                table: "offsettings",
                newName: "reference");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "offsettings",
                newName: "amount");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "offsettings",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "IsRemoved",
                table: "offsettings",
                newName: "is_removed");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "offsettings",
                newName: "created_date");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "offsettings",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "AccountNo",
                table: "offsettings",
                newName: "account_no");

            migrationBuilder.RenameColumn(
                name: "Unit",
                table: "inventories",
                newName: "unit");

            migrationBuilder.RenameColumn(
                name: "Total",
                table: "inventories",
                newName: "total");

            migrationBuilder.RenameColumn(
                name: "Reference",
                table: "inventories",
                newName: "reference");

            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "inventories",
                newName: "quantity");

            migrationBuilder.RenameColumn(
                name: "Particular",
                table: "inventories",
                newName: "particular");

            migrationBuilder.RenameColumn(
                name: "Date",
                table: "inventories",
                newName: "date");

            migrationBuilder.RenameColumn(
                name: "Cost",
                table: "inventories",
                newName: "cost");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "inventories",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "ValidatedDate",
                table: "inventories",
                newName: "validated_date");

            migrationBuilder.RenameColumn(
                name: "ValidatedBy",
                table: "inventories",
                newName: "validated_by");

            migrationBuilder.RenameColumn(
                name: "TotalBalance",
                table: "inventories",
                newName: "total_balance");

            migrationBuilder.RenameColumn(
                name: "ProductId",
                table: "inventories",
                newName: "product_id");

            migrationBuilder.RenameColumn(
                name: "IsValidated",
                table: "inventories",
                newName: "is_validated");

            migrationBuilder.RenameColumn(
                name: "InventoryBalance",
                table: "inventories",
                newName: "inventory_balance");

            migrationBuilder.RenameColumn(
                name: "AverageCost",
                table: "inventories",
                newName: "average_cost");

            migrationBuilder.RenameIndex(
                name: "IX_Inventories_ProductId",
                table: "inventories",
                newName: "ix_inventories_product_id");

            migrationBuilder.RenameColumn(
                name: "Terms",
                table: "customers",
                newName: "terms");

            migrationBuilder.RenameColumn(
                name: "Number",
                table: "customers",
                newName: "number");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "customers",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Address",
                table: "customers",
                newName: "address");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "customers",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "WithHoldingVat",
                table: "customers",
                newName: "with_holding_vat");

            migrationBuilder.RenameColumn(
                name: "WithHoldingTax",
                table: "customers",
                newName: "with_holding_tax");

            migrationBuilder.RenameColumn(
                name: "TinNo",
                table: "customers",
                newName: "tin_no");

            migrationBuilder.RenameColumn(
                name: "CustomerType",
                table: "customers",
                newName: "customer_type");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "customers",
                newName: "created_date");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "customers",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "BusinessStyle",
                table: "customers",
                newName: "business_style");

            migrationBuilder.RenameColumn(
                name: "Value",
                table: "AspNetUserTokens",
                newName: "value");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "AspNetUserTokens",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "LoginProvider",
                table: "AspNetUserTokens",
                newName: "login_provider");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "AspNetUserTokens",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "AspNetUsers",
                newName: "email");

            migrationBuilder.RenameColumn(
                name: "Discriminator",
                table: "AspNetUsers",
                newName: "discriminator");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "AspNetUsers",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserName",
                table: "AspNetUsers",
                newName: "user_name");

            migrationBuilder.RenameColumn(
                name: "TwoFactorEnabled",
                table: "AspNetUsers",
                newName: "two_factor_enabled");

            migrationBuilder.RenameColumn(
                name: "SecurityStamp",
                table: "AspNetUsers",
                newName: "security_stamp");

            migrationBuilder.RenameColumn(
                name: "PhoneNumberConfirmed",
                table: "AspNetUsers",
                newName: "phone_number_confirmed");

            migrationBuilder.RenameColumn(
                name: "PhoneNumber",
                table: "AspNetUsers",
                newName: "phone_number");

            migrationBuilder.RenameColumn(
                name: "PasswordHash",
                table: "AspNetUsers",
                newName: "password_hash");

            migrationBuilder.RenameColumn(
                name: "NormalizedUserName",
                table: "AspNetUsers",
                newName: "normalized_user_name");

            migrationBuilder.RenameColumn(
                name: "NormalizedEmail",
                table: "AspNetUsers",
                newName: "normalized_email");

            migrationBuilder.RenameColumn(
                name: "LockoutEnd",
                table: "AspNetUsers",
                newName: "lockout_end");

            migrationBuilder.RenameColumn(
                name: "LockoutEnabled",
                table: "AspNetUsers",
                newName: "lockout_enabled");

            migrationBuilder.RenameColumn(
                name: "LastName",
                table: "AspNetUsers",
                newName: "last_name");

            migrationBuilder.RenameColumn(
                name: "FirstName",
                table: "AspNetUsers",
                newName: "first_name");

            migrationBuilder.RenameColumn(
                name: "EmailConfirmed",
                table: "AspNetUsers",
                newName: "email_confirmed");

            migrationBuilder.RenameColumn(
                name: "ConcurrencyStamp",
                table: "AspNetUsers",
                newName: "concurrency_stamp");

            migrationBuilder.RenameColumn(
                name: "AccessFailedCount",
                table: "AspNetUsers",
                newName: "access_failed_count");

            migrationBuilder.RenameColumn(
                name: "RoleId",
                table: "AspNetUserRoles",
                newName: "role_id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "AspNetUserRoles",
                newName: "user_id");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                newName: "ix_asp_net_user_roles_role_id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "AspNetUserLogins",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "ProviderDisplayName",
                table: "AspNetUserLogins",
                newName: "provider_display_name");

            migrationBuilder.RenameColumn(
                name: "ProviderKey",
                table: "AspNetUserLogins",
                newName: "provider_key");

            migrationBuilder.RenameColumn(
                name: "LoginProvider",
                table: "AspNetUserLogins",
                newName: "login_provider");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                newName: "ix_asp_net_user_logins_user_id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "AspNetUserClaims",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "AspNetUserClaims",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "ClaimValue",
                table: "AspNetUserClaims",
                newName: "claim_value");

            migrationBuilder.RenameColumn(
                name: "ClaimType",
                table: "AspNetUserClaims",
                newName: "claim_type");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                newName: "ix_asp_net_user_claims_user_id");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "AspNetRoles",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "AspNetRoles",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "NormalizedName",
                table: "AspNetRoles",
                newName: "normalized_name");

            migrationBuilder.RenameColumn(
                name: "ConcurrencyStamp",
                table: "AspNetRoles",
                newName: "concurrency_stamp");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "AspNetRoleClaims",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "RoleId",
                table: "AspNetRoleClaims",
                newName: "role_id");

            migrationBuilder.RenameColumn(
                name: "ClaimValue",
                table: "AspNetRoleClaims",
                newName: "claim_value");

            migrationBuilder.RenameColumn(
                name: "ClaimType",
                table: "AspNetRoleClaims",
                newName: "claim_type");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                newName: "ix_asp_net_role_claims_role_id");

            migrationBuilder.RenameColumn(
                name: "Total",
                table: "service_invoices",
                newName: "total");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "service_invoices",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Period",
                table: "service_invoices",
                newName: "period");

            migrationBuilder.RenameColumn(
                name: "Instructions",
                table: "service_invoices",
                newName: "instructions");

            migrationBuilder.RenameColumn(
                name: "Discount",
                table: "service_invoices",
                newName: "discount");

            migrationBuilder.RenameColumn(
                name: "Balance",
                table: "service_invoices",
                newName: "balance");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "service_invoices",
                newName: "amount");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "service_invoices",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "WithholdingVatAmount",
                table: "service_invoices",
                newName: "withholding_vat_amount");

            migrationBuilder.RenameColumn(
                name: "WithholdingTaxAmount",
                table: "service_invoices",
                newName: "withholding_tax_amount");

            migrationBuilder.RenameColumn(
                name: "VoidedDate",
                table: "service_invoices",
                newName: "voided_date");

            migrationBuilder.RenameColumn(
                name: "VoidedBy",
                table: "service_invoices",
                newName: "voided_by");

            migrationBuilder.RenameColumn(
                name: "VatAmount",
                table: "service_invoices",
                newName: "vat_amount");

            migrationBuilder.RenameColumn(
                name: "UnearnedAmount",
                table: "service_invoices",
                newName: "unearned_amount");

            migrationBuilder.RenameColumn(
                name: "ServicesId",
                table: "service_invoices",
                newName: "services_id");

            migrationBuilder.RenameColumn(
                name: "ServiceNo",
                table: "service_invoices",
                newName: "service_no");

            migrationBuilder.RenameColumn(
                name: "SeriesNumber",
                table: "service_invoices",
                newName: "series_number");

            migrationBuilder.RenameColumn(
                name: "SVNo",
                table: "service_invoices",
                newName: "sv_no");

            migrationBuilder.RenameColumn(
                name: "PostedDate",
                table: "service_invoices",
                newName: "posted_date");

            migrationBuilder.RenameColumn(
                name: "PostedBy",
                table: "service_invoices",
                newName: "posted_by");

            migrationBuilder.RenameColumn(
                name: "NetAmount",
                table: "service_invoices",
                newName: "net_amount");

            migrationBuilder.RenameColumn(
                name: "IsVoided",
                table: "service_invoices",
                newName: "is_voided");

            migrationBuilder.RenameColumn(
                name: "IsPrinted",
                table: "service_invoices",
                newName: "is_printed");

            migrationBuilder.RenameColumn(
                name: "IsPosted",
                table: "service_invoices",
                newName: "is_posted");

            migrationBuilder.RenameColumn(
                name: "IsPaid",
                table: "service_invoices",
                newName: "is_paid");

            migrationBuilder.RenameColumn(
                name: "IsCanceled",
                table: "service_invoices",
                newName: "is_canceled");

            migrationBuilder.RenameColumn(
                name: "DueDate",
                table: "service_invoices",
                newName: "due_date");

            migrationBuilder.RenameColumn(
                name: "CustomerId",
                table: "service_invoices",
                newName: "customer_id");

            migrationBuilder.RenameColumn(
                name: "CurrentAndPreviousAmount",
                table: "service_invoices",
                newName: "current_and_previous_amount");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "service_invoices",
                newName: "created_date");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "service_invoices",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "CanceledDate",
                table: "service_invoices",
                newName: "canceled_date");

            migrationBuilder.RenameColumn(
                name: "CanceledBy",
                table: "service_invoices",
                newName: "canceled_by");

            migrationBuilder.RenameColumn(
                name: "AmountPaid",
                table: "service_invoices",
                newName: "amount_paid");

            migrationBuilder.RenameIndex(
                name: "IX_ServiceInvoices_ServicesId",
                table: "service_invoices",
                newName: "ix_service_invoices_services_id");

            migrationBuilder.RenameIndex(
                name: "IX_ServiceInvoices_CustomerId",
                table: "service_invoices",
                newName: "ix_service_invoices_customer_id");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "sales_invoices",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Remarks",
                table: "sales_invoices",
                newName: "remarks");

            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "sales_invoices",
                newName: "quantity");

            migrationBuilder.RenameColumn(
                name: "Discount",
                table: "sales_invoices",
                newName: "discount");

            migrationBuilder.RenameColumn(
                name: "Balance",
                table: "sales_invoices",
                newName: "balance");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "sales_invoices",
                newName: "amount");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "sales_invoices",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "ZeroRated",
                table: "sales_invoices",
                newName: "zero_rated");

            migrationBuilder.RenameColumn(
                name: "WithHoldingVatAmount",
                table: "sales_invoices",
                newName: "with_holding_vat_amount");

            migrationBuilder.RenameColumn(
                name: "WithHoldingTaxAmount",
                table: "sales_invoices",
                newName: "with_holding_tax_amount");

            migrationBuilder.RenameColumn(
                name: "VoidedDate",
                table: "sales_invoices",
                newName: "voided_date");

            migrationBuilder.RenameColumn(
                name: "VoidedBy",
                table: "sales_invoices",
                newName: "voided_by");

            migrationBuilder.RenameColumn(
                name: "VatableSales",
                table: "sales_invoices",
                newName: "vatable_sales");

            migrationBuilder.RenameColumn(
                name: "VatExempt",
                table: "sales_invoices",
                newName: "vat_exempt");

            migrationBuilder.RenameColumn(
                name: "VatAmount",
                table: "sales_invoices",
                newName: "vat_amount");

            migrationBuilder.RenameColumn(
                name: "UnitPrice",
                table: "sales_invoices",
                newName: "unit_price");

            migrationBuilder.RenameColumn(
                name: "TransactionDate",
                table: "sales_invoices",
                newName: "transaction_date");

            migrationBuilder.RenameColumn(
                name: "SeriesNumber",
                table: "sales_invoices",
                newName: "series_number");

            migrationBuilder.RenameColumn(
                name: "SINo",
                table: "sales_invoices",
                newName: "si_no");

            migrationBuilder.RenameColumn(
                name: "ProductId",
                table: "sales_invoices",
                newName: "product_id");

            migrationBuilder.RenameColumn(
                name: "PostedDate",
                table: "sales_invoices",
                newName: "posted_date");

            migrationBuilder.RenameColumn(
                name: "PostedBy",
                table: "sales_invoices",
                newName: "posted_by");

            migrationBuilder.RenameColumn(
                name: "PoNo",
                table: "sales_invoices",
                newName: "po_no");

            migrationBuilder.RenameColumn(
                name: "OtherRefNo",
                table: "sales_invoices",
                newName: "other_ref_no");

            migrationBuilder.RenameColumn(
                name: "NetDiscount",
                table: "sales_invoices",
                newName: "net_discount");

            migrationBuilder.RenameColumn(
                name: "IsVoided",
                table: "sales_invoices",
                newName: "is_voided");

            migrationBuilder.RenameColumn(
                name: "IsTaxAndVatPaid",
                table: "sales_invoices",
                newName: "is_tax_and_vat_paid");

            migrationBuilder.RenameColumn(
                name: "IsPrinted",
                table: "sales_invoices",
                newName: "is_printed");

            migrationBuilder.RenameColumn(
                name: "IsPosted",
                table: "sales_invoices",
                newName: "is_posted");

            migrationBuilder.RenameColumn(
                name: "IsPaid",
                table: "sales_invoices",
                newName: "is_paid");

            migrationBuilder.RenameColumn(
                name: "IsCanceled",
                table: "sales_invoices",
                newName: "is_canceled");

            migrationBuilder.RenameColumn(
                name: "DueDate",
                table: "sales_invoices",
                newName: "due_date");

            migrationBuilder.RenameColumn(
                name: "CustomerId",
                table: "sales_invoices",
                newName: "customer_id");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "sales_invoices",
                newName: "created_date");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "sales_invoices",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "CanceledDate",
                table: "sales_invoices",
                newName: "canceled_date");

            migrationBuilder.RenameColumn(
                name: "CanceledBy",
                table: "sales_invoices",
                newName: "canceled_by");

            migrationBuilder.RenameColumn(
                name: "AmountPaid",
                table: "sales_invoices",
                newName: "amount_paid");

            migrationBuilder.RenameIndex(
                name: "IX_SalesInvoices_ProductId",
                table: "sales_invoices",
                newName: "ix_sales_invoices_product_id");

            migrationBuilder.RenameIndex(
                name: "IX_SalesInvoices_CustomerId",
                table: "sales_invoices",
                newName: "ix_sales_invoices_customer_id");

            migrationBuilder.RenameColumn(
                name: "Discount",
                table: "sales_books",
                newName: "discount");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "sales_books",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "sales_books",
                newName: "amount");

            migrationBuilder.RenameColumn(
                name: "Address",
                table: "sales_books",
                newName: "address");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "sales_books",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "ZeroRated",
                table: "sales_books",
                newName: "zero_rated");

            migrationBuilder.RenameColumn(
                name: "VatableSales",
                table: "sales_books",
                newName: "vatable_sales");

            migrationBuilder.RenameColumn(
                name: "VatExemptSales",
                table: "sales_books",
                newName: "vat_exempt_sales");

            migrationBuilder.RenameColumn(
                name: "VatAmount",
                table: "sales_books",
                newName: "vat_amount");

            migrationBuilder.RenameColumn(
                name: "TransactionDate",
                table: "sales_books",
                newName: "transaction_date");

            migrationBuilder.RenameColumn(
                name: "TinNo",
                table: "sales_books",
                newName: "tin_no");

            migrationBuilder.RenameColumn(
                name: "SoldTo",
                table: "sales_books",
                newName: "sold_to");

            migrationBuilder.RenameColumn(
                name: "SerialNo",
                table: "sales_books",
                newName: "serial_no");

            migrationBuilder.RenameColumn(
                name: "NetSales",
                table: "sales_books",
                newName: "net_sales");

            migrationBuilder.RenameColumn(
                name: "DueDate",
                table: "sales_books",
                newName: "due_date");

            migrationBuilder.RenameColumn(
                name: "DocumentId",
                table: "sales_books",
                newName: "document_id");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "sales_books",
                newName: "created_date");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "sales_books",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "Remarks",
                table: "receiving_reports",
                newName: "remarks");

            migrationBuilder.RenameColumn(
                name: "Date",
                table: "receiving_reports",
                newName: "date");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "receiving_reports",
                newName: "amount");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "receiving_reports",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "VoidedDate",
                table: "receiving_reports",
                newName: "voided_date");

            migrationBuilder.RenameColumn(
                name: "VoidedBy",
                table: "receiving_reports",
                newName: "voided_by");

            migrationBuilder.RenameColumn(
                name: "VatAmount",
                table: "receiving_reports",
                newName: "vat_amount");

            migrationBuilder.RenameColumn(
                name: "TruckOrVessels",
                table: "receiving_reports",
                newName: "truck_or_vessels");

            migrationBuilder.RenameColumn(
                name: "SupplierInvoiceNumber",
                table: "receiving_reports",
                newName: "supplier_invoice_number");

            migrationBuilder.RenameColumn(
                name: "SupplierInvoiceDate",
                table: "receiving_reports",
                newName: "supplier_invoice_date");

            migrationBuilder.RenameColumn(
                name: "SeriesNumber",
                table: "receiving_reports",
                newName: "series_number");

            migrationBuilder.RenameColumn(
                name: "RRNo",
                table: "receiving_reports",
                newName: "rr_no");

            migrationBuilder.RenameColumn(
                name: "QuantityReceived",
                table: "receiving_reports",
                newName: "quantity_received");

            migrationBuilder.RenameColumn(
                name: "QuantityDelivered",
                table: "receiving_reports",
                newName: "quantity_delivered");

            migrationBuilder.RenameColumn(
                name: "PostedDate",
                table: "receiving_reports",
                newName: "posted_date");

            migrationBuilder.RenameColumn(
                name: "PostedBy",
                table: "receiving_reports",
                newName: "posted_by");

            migrationBuilder.RenameColumn(
                name: "PaidDate",
                table: "receiving_reports",
                newName: "paid_date");

            migrationBuilder.RenameColumn(
                name: "PONo",
                table: "receiving_reports",
                newName: "po_no");

            migrationBuilder.RenameColumn(
                name: "POId",
                table: "receiving_reports",
                newName: "po_id");

            migrationBuilder.RenameColumn(
                name: "OtherRef",
                table: "receiving_reports",
                newName: "other_ref");

            migrationBuilder.RenameColumn(
                name: "NetAmountOfEWT",
                table: "receiving_reports",
                newName: "net_amount_of_ewt");

            migrationBuilder.RenameColumn(
                name: "NetAmount",
                table: "receiving_reports",
                newName: "net_amount");

            migrationBuilder.RenameColumn(
                name: "IsVoided",
                table: "receiving_reports",
                newName: "is_voided");

            migrationBuilder.RenameColumn(
                name: "IsPrinted",
                table: "receiving_reports",
                newName: "is_printed");

            migrationBuilder.RenameColumn(
                name: "IsPosted",
                table: "receiving_reports",
                newName: "is_posted");

            migrationBuilder.RenameColumn(
                name: "IsPaid",
                table: "receiving_reports",
                newName: "is_paid");

            migrationBuilder.RenameColumn(
                name: "IsCanceled",
                table: "receiving_reports",
                newName: "is_canceled");

            migrationBuilder.RenameColumn(
                name: "GainOrLoss",
                table: "receiving_reports",
                newName: "gain_or_loss");

            migrationBuilder.RenameColumn(
                name: "EwtAmount",
                table: "receiving_reports",
                newName: "ewt_amount");

            migrationBuilder.RenameColumn(
                name: "DueDate",
                table: "receiving_reports",
                newName: "due_date");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "receiving_reports",
                newName: "created_date");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "receiving_reports",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "CanceledQuantity",
                table: "receiving_reports",
                newName: "canceled_quantity");

            migrationBuilder.RenameColumn(
                name: "CanceledDate",
                table: "receiving_reports",
                newName: "canceled_date");

            migrationBuilder.RenameColumn(
                name: "CanceledBy",
                table: "receiving_reports",
                newName: "canceled_by");

            migrationBuilder.RenameColumn(
                name: "AmountPaid",
                table: "receiving_reports",
                newName: "amount_paid");

            migrationBuilder.RenameIndex(
                name: "IX_ReceivingReports_POId",
                table: "receiving_reports",
                newName: "ix_receiving_reports_po_id");

            migrationBuilder.RenameColumn(
                name: "Terms",
                table: "purchase_orders",
                newName: "terms");

            migrationBuilder.RenameColumn(
                name: "Remarks",
                table: "purchase_orders",
                newName: "remarks");

            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "purchase_orders",
                newName: "quantity");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "purchase_orders",
                newName: "price");

            migrationBuilder.RenameColumn(
                name: "Date",
                table: "purchase_orders",
                newName: "date");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "purchase_orders",
                newName: "amount");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "purchase_orders",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "VoidedDate",
                table: "purchase_orders",
                newName: "voided_date");

            migrationBuilder.RenameColumn(
                name: "VoidedBy",
                table: "purchase_orders",
                newName: "voided_by");

            migrationBuilder.RenameColumn(
                name: "SupplierNo",
                table: "purchase_orders",
                newName: "supplier_no");

            migrationBuilder.RenameColumn(
                name: "SupplierId",
                table: "purchase_orders",
                newName: "supplier_id");

            migrationBuilder.RenameColumn(
                name: "SeriesNumber",
                table: "purchase_orders",
                newName: "series_number");

            migrationBuilder.RenameColumn(
                name: "ReceivedDate",
                table: "purchase_orders",
                newName: "received_date");

            migrationBuilder.RenameColumn(
                name: "QuantityReceived",
                table: "purchase_orders",
                newName: "quantity_received");

            migrationBuilder.RenameColumn(
                name: "ProductNo",
                table: "purchase_orders",
                newName: "product_no");

            migrationBuilder.RenameColumn(
                name: "ProductId",
                table: "purchase_orders",
                newName: "product_id");

            migrationBuilder.RenameColumn(
                name: "PostedDate",
                table: "purchase_orders",
                newName: "posted_date");

            migrationBuilder.RenameColumn(
                name: "PostedBy",
                table: "purchase_orders",
                newName: "posted_by");

            migrationBuilder.RenameColumn(
                name: "PONo",
                table: "purchase_orders",
                newName: "po_no");

            migrationBuilder.RenameColumn(
                name: "IsVoided",
                table: "purchase_orders",
                newName: "is_voided");

            migrationBuilder.RenameColumn(
                name: "IsReceived",
                table: "purchase_orders",
                newName: "is_received");

            migrationBuilder.RenameColumn(
                name: "IsPrinted",
                table: "purchase_orders",
                newName: "is_printed");

            migrationBuilder.RenameColumn(
                name: "IsPosted",
                table: "purchase_orders",
                newName: "is_posted");

            migrationBuilder.RenameColumn(
                name: "IsCanceled",
                table: "purchase_orders",
                newName: "is_canceled");

            migrationBuilder.RenameColumn(
                name: "FinalPrice",
                table: "purchase_orders",
                newName: "final_price");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "purchase_orders",
                newName: "created_date");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "purchase_orders",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "CanceledDate",
                table: "purchase_orders",
                newName: "canceled_date");

            migrationBuilder.RenameColumn(
                name: "CanceledBy",
                table: "purchase_orders",
                newName: "canceled_by");

            migrationBuilder.RenameIndex(
                name: "IX_PurchaseOrders_SupplierId",
                table: "purchase_orders",
                newName: "ix_purchase_orders_supplier_id");

            migrationBuilder.RenameIndex(
                name: "IX_PurchaseOrders_ProductId",
                table: "purchase_orders",
                newName: "ix_purchase_orders_product_id");

            migrationBuilder.RenameColumn(
                name: "Discount",
                table: "purchase_journal_books",
                newName: "discount");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "purchase_journal_books",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "Date",
                table: "purchase_journal_books",
                newName: "date");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "purchase_journal_books",
                newName: "amount");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "purchase_journal_books",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "WhtAmount",
                table: "purchase_journal_books",
                newName: "wht_amount");

            migrationBuilder.RenameColumn(
                name: "VatAmount",
                table: "purchase_journal_books",
                newName: "vat_amount");

            migrationBuilder.RenameColumn(
                name: "SupplierTin",
                table: "purchase_journal_books",
                newName: "supplier_tin");

            migrationBuilder.RenameColumn(
                name: "SupplierName",
                table: "purchase_journal_books",
                newName: "supplier_name");

            migrationBuilder.RenameColumn(
                name: "SupplierAddress",
                table: "purchase_journal_books",
                newName: "supplier_address");

            migrationBuilder.RenameColumn(
                name: "PONo",
                table: "purchase_journal_books",
                newName: "po_no");

            migrationBuilder.RenameColumn(
                name: "NetPurchases",
                table: "purchase_journal_books",
                newName: "net_purchases");

            migrationBuilder.RenameColumn(
                name: "DueDate",
                table: "purchase_journal_books",
                newName: "due_date");

            migrationBuilder.RenameColumn(
                name: "DocumentNo",
                table: "purchase_journal_books",
                newName: "document_no");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "purchase_journal_books",
                newName: "created_date");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "purchase_journal_books",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "References",
                table: "journal_voucher_headers",
                newName: "references");

            migrationBuilder.RenameColumn(
                name: "Particulars",
                table: "journal_voucher_headers",
                newName: "particulars");

            migrationBuilder.RenameColumn(
                name: "Date",
                table: "journal_voucher_headers",
                newName: "date");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "journal_voucher_headers",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "VoidedDate",
                table: "journal_voucher_headers",
                newName: "voided_date");

            migrationBuilder.RenameColumn(
                name: "VoidedBy",
                table: "journal_voucher_headers",
                newName: "voided_by");

            migrationBuilder.RenameColumn(
                name: "SeriesNumber",
                table: "journal_voucher_headers",
                newName: "series_number");

            migrationBuilder.RenameColumn(
                name: "PostedDate",
                table: "journal_voucher_headers",
                newName: "posted_date");

            migrationBuilder.RenameColumn(
                name: "PostedBy",
                table: "journal_voucher_headers",
                newName: "posted_by");

            migrationBuilder.RenameColumn(
                name: "JVReason",
                table: "journal_voucher_headers",
                newName: "jv_reason");

            migrationBuilder.RenameColumn(
                name: "JVNo",
                table: "journal_voucher_headers",
                newName: "jv_no");

            migrationBuilder.RenameColumn(
                name: "IsVoided",
                table: "journal_voucher_headers",
                newName: "is_voided");

            migrationBuilder.RenameColumn(
                name: "IsPrinted",
                table: "journal_voucher_headers",
                newName: "is_printed");

            migrationBuilder.RenameColumn(
                name: "IsPosted",
                table: "journal_voucher_headers",
                newName: "is_posted");

            migrationBuilder.RenameColumn(
                name: "IsCanceled",
                table: "journal_voucher_headers",
                newName: "is_canceled");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "journal_voucher_headers",
                newName: "created_date");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "journal_voucher_headers",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "CanceledDate",
                table: "journal_voucher_headers",
                newName: "canceled_date");

            migrationBuilder.RenameColumn(
                name: "CanceledBy",
                table: "journal_voucher_headers",
                newName: "canceled_by");

            migrationBuilder.RenameColumn(
                name: "CVId",
                table: "journal_voucher_headers",
                newName: "cv_id");

            migrationBuilder.RenameColumn(
                name: "CRNo",
                table: "journal_voucher_headers",
                newName: "cr_no");

            migrationBuilder.RenameIndex(
                name: "IX_JournalVoucherHeaders_CVId",
                table: "journal_voucher_headers",
                newName: "ix_journal_voucher_headers_cv_id");

            migrationBuilder.RenameColumn(
                name: "Debit",
                table: "journal_voucher_details",
                newName: "debit");

            migrationBuilder.RenameColumn(
                name: "Credit",
                table: "journal_voucher_details",
                newName: "credit");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "journal_voucher_details",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "TransactionNo",
                table: "journal_voucher_details",
                newName: "transaction_no");

            migrationBuilder.RenameColumn(
                name: "AccountNo",
                table: "journal_voucher_details",
                newName: "account_no");

            migrationBuilder.RenameColumn(
                name: "AccountName",
                table: "journal_voucher_details",
                newName: "account_name");

            migrationBuilder.RenameColumn(
                name: "Reference",
                table: "journal_books",
                newName: "reference");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "journal_books",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "Debit",
                table: "journal_books",
                newName: "debit");

            migrationBuilder.RenameColumn(
                name: "Date",
                table: "journal_books",
                newName: "date");

            migrationBuilder.RenameColumn(
                name: "Credit",
                table: "journal_books",
                newName: "credit");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "journal_books",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "journal_books",
                newName: "created_date");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "journal_books",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "AccountTitle",
                table: "journal_books",
                newName: "account_title");

            migrationBuilder.RenameColumn(
                name: "Reference",
                table: "general_ledger_books",
                newName: "reference");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "general_ledger_books",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "Debit",
                table: "general_ledger_books",
                newName: "debit");

            migrationBuilder.RenameColumn(
                name: "Date",
                table: "general_ledger_books",
                newName: "date");

            migrationBuilder.RenameColumn(
                name: "Credit",
                table: "general_ledger_books",
                newName: "credit");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "general_ledger_books",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "IsPosted",
                table: "general_ledger_books",
                newName: "is_posted");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "general_ledger_books",
                newName: "created_date");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "general_ledger_books",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "AccountTitle",
                table: "general_ledger_books",
                newName: "account_title");

            migrationBuilder.RenameColumn(
                name: "AccountNo",
                table: "general_ledger_books",
                newName: "account_no");

            migrationBuilder.RenameColumn(
                name: "Payee",
                table: "disbursement_books",
                newName: "payee");

            migrationBuilder.RenameColumn(
                name: "Particulars",
                table: "disbursement_books",
                newName: "particulars");

            migrationBuilder.RenameColumn(
                name: "Debit",
                table: "disbursement_books",
                newName: "debit");

            migrationBuilder.RenameColumn(
                name: "Date",
                table: "disbursement_books",
                newName: "date");

            migrationBuilder.RenameColumn(
                name: "Credit",
                table: "disbursement_books",
                newName: "credit");

            migrationBuilder.RenameColumn(
                name: "Bank",
                table: "disbursement_books",
                newName: "bank");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "disbursement_books",
                newName: "amount");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "disbursement_books",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "disbursement_books",
                newName: "created_date");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "disbursement_books",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "CheckNo",
                table: "disbursement_books",
                newName: "check_no");

            migrationBuilder.RenameColumn(
                name: "CheckDate",
                table: "disbursement_books",
                newName: "check_date");

            migrationBuilder.RenameColumn(
                name: "ChartOfAccount",
                table: "disbursement_books",
                newName: "chart_of_account");

            migrationBuilder.RenameColumn(
                name: "CVNo",
                table: "disbursement_books",
                newName: "cv_no");

            migrationBuilder.RenameColumn(
                name: "Source",
                table: "debit_memos",
                newName: "source");

            migrationBuilder.RenameColumn(
                name: "Remarks",
                table: "debit_memos",
                newName: "remarks");

            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "debit_memos",
                newName: "quantity");

            migrationBuilder.RenameColumn(
                name: "Period",
                table: "debit_memos",
                newName: "period");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "debit_memos",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "debit_memos",
                newName: "amount");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "debit_memos",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "WithHoldingVatAmount",
                table: "debit_memos",
                newName: "with_holding_vat_amount");

            migrationBuilder.RenameColumn(
                name: "WithHoldingTaxAmount",
                table: "debit_memos",
                newName: "with_holding_tax_amount");

            migrationBuilder.RenameColumn(
                name: "VoidedDate",
                table: "debit_memos",
                newName: "voided_date");

            migrationBuilder.RenameColumn(
                name: "VoidedBy",
                table: "debit_memos",
                newName: "voided_by");

            migrationBuilder.RenameColumn(
                name: "VatableSales",
                table: "debit_memos",
                newName: "vatable_sales");

            migrationBuilder.RenameColumn(
                name: "VatAmount",
                table: "debit_memos",
                newName: "vat_amount");

            migrationBuilder.RenameColumn(
                name: "UnearnedAmount",
                table: "debit_memos",
                newName: "unearned_amount");

            migrationBuilder.RenameColumn(
                name: "TransactionDate",
                table: "debit_memos",
                newName: "transaction_date");

            migrationBuilder.RenameColumn(
                name: "TotalSales",
                table: "debit_memos",
                newName: "total_sales");

            migrationBuilder.RenameColumn(
                name: "ServicesId",
                table: "debit_memos",
                newName: "services_id");

            migrationBuilder.RenameColumn(
                name: "ServiceInvoiceId",
                table: "debit_memos",
                newName: "service_invoice_id");

            migrationBuilder.RenameColumn(
                name: "SeriesNumber",
                table: "debit_memos",
                newName: "series_number");

            migrationBuilder.RenameColumn(
                name: "SalesInvoiceId",
                table: "debit_memos",
                newName: "sales_invoice_id");

            migrationBuilder.RenameColumn(
                name: "PostedDate",
                table: "debit_memos",
                newName: "posted_date");

            migrationBuilder.RenameColumn(
                name: "PostedBy",
                table: "debit_memos",
                newName: "posted_by");

            migrationBuilder.RenameColumn(
                name: "IsVoided",
                table: "debit_memos",
                newName: "is_voided");

            migrationBuilder.RenameColumn(
                name: "IsPrinted",
                table: "debit_memos",
                newName: "is_printed");

            migrationBuilder.RenameColumn(
                name: "IsPosted",
                table: "debit_memos",
                newName: "is_posted");

            migrationBuilder.RenameColumn(
                name: "IsCanceled",
                table: "debit_memos",
                newName: "is_canceled");

            migrationBuilder.RenameColumn(
                name: "DebitAmount",
                table: "debit_memos",
                newName: "debit_amount");

            migrationBuilder.RenameColumn(
                name: "DMNo",
                table: "debit_memos",
                newName: "dm_no");

            migrationBuilder.RenameColumn(
                name: "CurrentAndPreviousAmount",
                table: "debit_memos",
                newName: "current_and_previous_amount");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "debit_memos",
                newName: "created_date");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "debit_memos",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "CanceledDate",
                table: "debit_memos",
                newName: "canceled_date");

            migrationBuilder.RenameColumn(
                name: "CanceledBy",
                table: "debit_memos",
                newName: "canceled_by");

            migrationBuilder.RenameColumn(
                name: "AdjustedPrice",
                table: "debit_memos",
                newName: "adjusted_price");

            migrationBuilder.RenameIndex(
                name: "IX_DebitMemos_ServiceInvoiceId",
                table: "debit_memos",
                newName: "ix_debit_memos_service_invoice_id");

            migrationBuilder.RenameIndex(
                name: "IX_DebitMemos_SalesInvoiceId",
                table: "debit_memos",
                newName: "ix_debit_memos_sales_invoice_id");

            migrationBuilder.RenameColumn(
                name: "Source",
                table: "credit_memos",
                newName: "source");

            migrationBuilder.RenameColumn(
                name: "Remarks",
                table: "credit_memos",
                newName: "remarks");

            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "credit_memos",
                newName: "quantity");

            migrationBuilder.RenameColumn(
                name: "Period",
                table: "credit_memos",
                newName: "period");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "credit_memos",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "credit_memos",
                newName: "amount");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "credit_memos",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "WithHoldingVatAmount",
                table: "credit_memos",
                newName: "with_holding_vat_amount");

            migrationBuilder.RenameColumn(
                name: "WithHoldingTaxAmount",
                table: "credit_memos",
                newName: "with_holding_tax_amount");

            migrationBuilder.RenameColumn(
                name: "VoidedDate",
                table: "credit_memos",
                newName: "voided_date");

            migrationBuilder.RenameColumn(
                name: "VoidedBy",
                table: "credit_memos",
                newName: "voided_by");

            migrationBuilder.RenameColumn(
                name: "VatableSales",
                table: "credit_memos",
                newName: "vatable_sales");

            migrationBuilder.RenameColumn(
                name: "VatAmount",
                table: "credit_memos",
                newName: "vat_amount");

            migrationBuilder.RenameColumn(
                name: "UnearnedAmount",
                table: "credit_memos",
                newName: "unearned_amount");

            migrationBuilder.RenameColumn(
                name: "TransactionDate",
                table: "credit_memos",
                newName: "transaction_date");

            migrationBuilder.RenameColumn(
                name: "TotalSales",
                table: "credit_memos",
                newName: "total_sales");

            migrationBuilder.RenameColumn(
                name: "ServicesId",
                table: "credit_memos",
                newName: "services_id");

            migrationBuilder.RenameColumn(
                name: "ServiceInvoiceId",
                table: "credit_memos",
                newName: "service_invoice_id");

            migrationBuilder.RenameColumn(
                name: "SeriesNumber",
                table: "credit_memos",
                newName: "series_number");

            migrationBuilder.RenameColumn(
                name: "SalesInvoiceId",
                table: "credit_memos",
                newName: "sales_invoice_id");

            migrationBuilder.RenameColumn(
                name: "PostedDate",
                table: "credit_memos",
                newName: "posted_date");

            migrationBuilder.RenameColumn(
                name: "PostedBy",
                table: "credit_memos",
                newName: "posted_by");

            migrationBuilder.RenameColumn(
                name: "IsVoided",
                table: "credit_memos",
                newName: "is_voided");

            migrationBuilder.RenameColumn(
                name: "IsPrinted",
                table: "credit_memos",
                newName: "is_printed");

            migrationBuilder.RenameColumn(
                name: "IsPosted",
                table: "credit_memos",
                newName: "is_posted");

            migrationBuilder.RenameColumn(
                name: "IsCanceled",
                table: "credit_memos",
                newName: "is_canceled");

            migrationBuilder.RenameColumn(
                name: "CurrentAndPreviousAmount",
                table: "credit_memos",
                newName: "current_and_previous_amount");

            migrationBuilder.RenameColumn(
                name: "CreditAmount",
                table: "credit_memos",
                newName: "credit_amount");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "credit_memos",
                newName: "created_date");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "credit_memos",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "CanceledDate",
                table: "credit_memos",
                newName: "canceled_date");

            migrationBuilder.RenameColumn(
                name: "CanceledBy",
                table: "credit_memos",
                newName: "canceled_by");

            migrationBuilder.RenameColumn(
                name: "CMNo",
                table: "credit_memos",
                newName: "cm_no");

            migrationBuilder.RenameColumn(
                name: "AdjustedPrice",
                table: "credit_memos",
                newName: "adjusted_price");

            migrationBuilder.RenameIndex(
                name: "IX_CreditMemos_ServiceInvoiceId",
                table: "credit_memos",
                newName: "ix_credit_memos_service_invoice_id");

            migrationBuilder.RenameIndex(
                name: "IX_CreditMemos_SalesInvoiceId",
                table: "credit_memos",
                newName: "ix_credit_memos_sales_invoice_id");

            migrationBuilder.RenameColumn(
                name: "WVAT",
                table: "collection_receipts",
                newName: "wvat");

            migrationBuilder.RenameColumn(
                name: "Total",
                table: "collection_receipts",
                newName: "total");

            migrationBuilder.RenameColumn(
                name: "Remarks",
                table: "collection_receipts",
                newName: "remarks");

            migrationBuilder.RenameColumn(
                name: "EWT",
                table: "collection_receipts",
                newName: "ewt");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "collection_receipts",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "VoidedDate",
                table: "collection_receipts",
                newName: "voided_date");

            migrationBuilder.RenameColumn(
                name: "VoidedBy",
                table: "collection_receipts",
                newName: "voided_by");

            migrationBuilder.RenameColumn(
                name: "TransactionDate",
                table: "collection_receipts",
                newName: "transaction_date");

            migrationBuilder.RenameColumn(
                name: "ServiceInvoiceId",
                table: "collection_receipts",
                newName: "service_invoice_id");

            migrationBuilder.RenameColumn(
                name: "SeriesNumber",
                table: "collection_receipts",
                newName: "series_number");

            migrationBuilder.RenameColumn(
                name: "SalesInvoiceId",
                table: "collection_receipts",
                newName: "sales_invoice_id");

            migrationBuilder.RenameColumn(
                name: "SVNo",
                table: "collection_receipts",
                newName: "sv_no");

            migrationBuilder.RenameColumn(
                name: "SINo",
                table: "collection_receipts",
                newName: "si_no");

            migrationBuilder.RenameColumn(
                name: "ReferenceNo",
                table: "collection_receipts",
                newName: "reference_no");

            migrationBuilder.RenameColumn(
                name: "PostedDate",
                table: "collection_receipts",
                newName: "posted_date");

            migrationBuilder.RenameColumn(
                name: "PostedBy",
                table: "collection_receipts",
                newName: "posted_by");

            migrationBuilder.RenameColumn(
                name: "ManagerCheckNo",
                table: "collection_receipts",
                newName: "manager_check_no");

            migrationBuilder.RenameColumn(
                name: "ManagerCheckDate",
                table: "collection_receipts",
                newName: "manager_check_date");

            migrationBuilder.RenameColumn(
                name: "ManagerCheckBranch",
                table: "collection_receipts",
                newName: "manager_check_branch");

            migrationBuilder.RenameColumn(
                name: "ManagerCheckBank",
                table: "collection_receipts",
                newName: "manager_check_bank");

            migrationBuilder.RenameColumn(
                name: "ManagerCheckAmount",
                table: "collection_receipts",
                newName: "manager_check_amount");

            migrationBuilder.RenameColumn(
                name: "IsVoided",
                table: "collection_receipts",
                newName: "is_voided");

            migrationBuilder.RenameColumn(
                name: "IsPrinted",
                table: "collection_receipts",
                newName: "is_printed");

            migrationBuilder.RenameColumn(
                name: "IsPosted",
                table: "collection_receipts",
                newName: "is_posted");

            migrationBuilder.RenameColumn(
                name: "IsCertificateUpload",
                table: "collection_receipts",
                newName: "is_certificate_upload");

            migrationBuilder.RenameColumn(
                name: "IsCanceled",
                table: "collection_receipts",
                newName: "is_canceled");

            migrationBuilder.RenameColumn(
                name: "F2307FilePath",
                table: "collection_receipts",
                newName: "f2307file_path");

            migrationBuilder.RenameColumn(
                name: "F2306FilePath",
                table: "collection_receipts",
                newName: "f2306file_path");

            migrationBuilder.RenameColumn(
                name: "CustomerId",
                table: "collection_receipts",
                newName: "customer_id");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "collection_receipts",
                newName: "created_date");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "collection_receipts",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "CheckNo",
                table: "collection_receipts",
                newName: "check_no");

            migrationBuilder.RenameColumn(
                name: "CheckDate",
                table: "collection_receipts",
                newName: "check_date");

            migrationBuilder.RenameColumn(
                name: "CheckBranch",
                table: "collection_receipts",
                newName: "check_branch");

            migrationBuilder.RenameColumn(
                name: "CheckBank",
                table: "collection_receipts",
                newName: "check_bank");

            migrationBuilder.RenameColumn(
                name: "CheckAmount",
                table: "collection_receipts",
                newName: "check_amount");

            migrationBuilder.RenameColumn(
                name: "CashAmount",
                table: "collection_receipts",
                newName: "cash_amount");

            migrationBuilder.RenameColumn(
                name: "CanceledDate",
                table: "collection_receipts",
                newName: "canceled_date");

            migrationBuilder.RenameColumn(
                name: "CanceledBy",
                table: "collection_receipts",
                newName: "canceled_by");

            migrationBuilder.RenameColumn(
                name: "CRNo",
                table: "collection_receipts",
                newName: "cr_no");

            migrationBuilder.RenameIndex(
                name: "IX_CollectionReceipts_ServiceInvoiceId",
                table: "collection_receipts",
                newName: "ix_collection_receipts_service_invoice_id");

            migrationBuilder.RenameIndex(
                name: "IX_CollectionReceipts_SalesInvoiceId",
                table: "collection_receipts",
                newName: "ix_collection_receipts_sales_invoice_id");

            migrationBuilder.RenameColumn(
                name: "Total",
                table: "check_voucher_headers",
                newName: "total");

            migrationBuilder.RenameColumn(
                name: "Reference",
                table: "check_voucher_headers",
                newName: "reference");

            migrationBuilder.RenameColumn(
                name: "Payee",
                table: "check_voucher_headers",
                newName: "payee");

            migrationBuilder.RenameColumn(
                name: "Particulars",
                table: "check_voucher_headers",
                newName: "particulars");

            migrationBuilder.RenameColumn(
                name: "Date",
                table: "check_voucher_headers",
                newName: "date");

            migrationBuilder.RenameColumn(
                name: "Category",
                table: "check_voucher_headers",
                newName: "category");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "check_voucher_headers",
                newName: "amount");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "check_voucher_headers",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "VoidedDate",
                table: "check_voucher_headers",
                newName: "voided_date");

            migrationBuilder.RenameColumn(
                name: "VoidedBy",
                table: "check_voucher_headers",
                newName: "voided_by");

            migrationBuilder.RenameColumn(
                name: "SupplierId",
                table: "check_voucher_headers",
                newName: "supplier_id");

            migrationBuilder.RenameColumn(
                name: "StartDate",
                table: "check_voucher_headers",
                newName: "start_date");

            migrationBuilder.RenameColumn(
                name: "SeriesNumber",
                table: "check_voucher_headers",
                newName: "series_number");

            migrationBuilder.RenameColumn(
                name: "SINo",
                table: "check_voucher_headers",
                newName: "si_no");

            migrationBuilder.RenameColumn(
                name: "RRNo",
                table: "check_voucher_headers",
                newName: "rr_no");

            migrationBuilder.RenameColumn(
                name: "PostedDate",
                table: "check_voucher_headers",
                newName: "posted_date");

            migrationBuilder.RenameColumn(
                name: "PostedBy",
                table: "check_voucher_headers",
                newName: "posted_by");

            migrationBuilder.RenameColumn(
                name: "PONo",
                table: "check_voucher_headers",
                newName: "po_no");

            migrationBuilder.RenameColumn(
                name: "NumberOfMonthsCreated",
                table: "check_voucher_headers",
                newName: "number_of_months_created");

            migrationBuilder.RenameColumn(
                name: "NumberOfMonths",
                table: "check_voucher_headers",
                newName: "number_of_months");

            migrationBuilder.RenameColumn(
                name: "LastCreatedDate",
                table: "check_voucher_headers",
                newName: "last_created_date");

            migrationBuilder.RenameColumn(
                name: "IsVoided",
                table: "check_voucher_headers",
                newName: "is_voided");

            migrationBuilder.RenameColumn(
                name: "IsPrinted",
                table: "check_voucher_headers",
                newName: "is_printed");

            migrationBuilder.RenameColumn(
                name: "IsPosted",
                table: "check_voucher_headers",
                newName: "is_posted");

            migrationBuilder.RenameColumn(
                name: "IsPaid",
                table: "check_voucher_headers",
                newName: "is_paid");

            migrationBuilder.RenameColumn(
                name: "IsComplete",
                table: "check_voucher_headers",
                newName: "is_complete");

            migrationBuilder.RenameColumn(
                name: "IsCanceled",
                table: "check_voucher_headers",
                newName: "is_canceled");

            migrationBuilder.RenameColumn(
                name: "EndDate",
                table: "check_voucher_headers",
                newName: "end_date");

            migrationBuilder.RenameColumn(
                name: "CvType",
                table: "check_voucher_headers",
                newName: "cv_type");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "check_voucher_headers",
                newName: "created_date");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "check_voucher_headers",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "CheckNo",
                table: "check_voucher_headers",
                newName: "check_no");

            migrationBuilder.RenameColumn(
                name: "CheckDate",
                table: "check_voucher_headers",
                newName: "check_date");

            migrationBuilder.RenameColumn(
                name: "CheckAmount",
                table: "check_voucher_headers",
                newName: "check_amount");

            migrationBuilder.RenameColumn(
                name: "CanceledDate",
                table: "check_voucher_headers",
                newName: "canceled_date");

            migrationBuilder.RenameColumn(
                name: "CanceledBy",
                table: "check_voucher_headers",
                newName: "canceled_by");

            migrationBuilder.RenameColumn(
                name: "CVNo",
                table: "check_voucher_headers",
                newName: "cv_no");

            migrationBuilder.RenameColumn(
                name: "BankId",
                table: "check_voucher_headers",
                newName: "bank_id");

            migrationBuilder.RenameColumn(
                name: "AmountPerMonth",
                table: "check_voucher_headers",
                newName: "amount_per_month");

            migrationBuilder.RenameColumn(
                name: "AmountPaid",
                table: "check_voucher_headers",
                newName: "amount_paid");

            migrationBuilder.RenameColumn(
                name: "AccruedType",
                table: "check_voucher_headers",
                newName: "accrued_type");

            migrationBuilder.RenameIndex(
                name: "IX_CheckVoucherHeaders_SupplierId",
                table: "check_voucher_headers",
                newName: "ix_check_voucher_headers_supplier_id");

            migrationBuilder.RenameIndex(
                name: "IX_CheckVoucherHeaders_BankId",
                table: "check_voucher_headers",
                newName: "ix_check_voucher_headers_bank_id");

            migrationBuilder.RenameColumn(
                name: "Debit",
                table: "check_voucher_details",
                newName: "debit");

            migrationBuilder.RenameColumn(
                name: "Credit",
                table: "check_voucher_details",
                newName: "credit");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "check_voucher_details",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "TransactionNo",
                table: "check_voucher_details",
                newName: "transaction_no");

            migrationBuilder.RenameColumn(
                name: "AccountNo",
                table: "check_voucher_details",
                newName: "account_no");

            migrationBuilder.RenameColumn(
                name: "AccountName",
                table: "check_voucher_details",
                newName: "account_name");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "chart_of_accounts",
                newName: "type");

            migrationBuilder.RenameColumn(
                name: "Parent",
                table: "chart_of_accounts",
                newName: "parent");

            migrationBuilder.RenameColumn(
                name: "Number",
                table: "chart_of_accounts",
                newName: "number");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "chart_of_accounts",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Level",
                table: "chart_of_accounts",
                newName: "level");

            migrationBuilder.RenameColumn(
                name: "Category",
                table: "chart_of_accounts",
                newName: "category");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "chart_of_accounts",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "IsMain",
                table: "chart_of_accounts",
                newName: "is_main");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "chart_of_accounts",
                newName: "created_date");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "chart_of_accounts",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "Particulars",
                table: "cash_receipt_books",
                newName: "particulars");

            migrationBuilder.RenameColumn(
                name: "Debit",
                table: "cash_receipt_books",
                newName: "debit");

            migrationBuilder.RenameColumn(
                name: "Date",
                table: "cash_receipt_books",
                newName: "date");

            migrationBuilder.RenameColumn(
                name: "Credit",
                table: "cash_receipt_books",
                newName: "credit");

            migrationBuilder.RenameColumn(
                name: "COA",
                table: "cash_receipt_books",
                newName: "coa");

            migrationBuilder.RenameColumn(
                name: "Bank",
                table: "cash_receipt_books",
                newName: "bank");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "cash_receipt_books",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "RefNo",
                table: "cash_receipt_books",
                newName: "ref_no");

            migrationBuilder.RenameColumn(
                name: "CustomerName",
                table: "cash_receipt_books",
                newName: "customer_name");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "cash_receipt_books",
                newName: "created_date");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "cash_receipt_books",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "CheckNo",
                table: "cash_receipt_books",
                newName: "check_no");

            migrationBuilder.RenameColumn(
                name: "Branch",
                table: "bank_accounts",
                newName: "branch");

            migrationBuilder.RenameColumn(
                name: "Bank",
                table: "bank_accounts",
                newName: "bank");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "bank_accounts",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "SeriesNumber",
                table: "bank_accounts",
                newName: "series_number");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "bank_accounts",
                newName: "created_date");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "bank_accounts",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "BankCode",
                table: "bank_accounts",
                newName: "bank_code");

            migrationBuilder.RenameColumn(
                name: "AccountNoCOA",
                table: "bank_accounts",
                newName: "account_no_coa");

            migrationBuilder.RenameColumn(
                name: "AccountNo",
                table: "bank_accounts",
                newName: "account_no");

            migrationBuilder.RenameColumn(
                name: "AccountName",
                table: "bank_accounts",
                newName: "account_name");

            migrationBuilder.RenameColumn(
                name: "Username",
                table: "audit_trails",
                newName: "username");

            migrationBuilder.RenameColumn(
                name: "Date",
                table: "audit_trails",
                newName: "date");

            migrationBuilder.RenameColumn(
                name: "Activity",
                table: "audit_trails",
                newName: "activity");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "audit_trails",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "MachineName",
                table: "audit_trails",
                newName: "machine_name");

            migrationBuilder.RenameColumn(
                name: "DocumentType",
                table: "audit_trails",
                newName: "document_type");

            migrationBuilder.AddPrimaryKey(
                name: "pk_suppliers",
                table: "suppliers",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_services",
                table: "services",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_products",
                table: "products",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_offsettings",
                table: "offsettings",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_inventories",
                table: "inventories",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_customers",
                table: "customers",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_asp_net_user_tokens",
                table: "AspNetUserTokens",
                columns: new[] { "user_id", "login_provider", "name" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_asp_net_users",
                table: "AspNetUsers",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_asp_net_user_roles",
                table: "AspNetUserRoles",
                columns: new[] { "user_id", "role_id" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_asp_net_user_logins",
                table: "AspNetUserLogins",
                columns: new[] { "login_provider", "provider_key" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_asp_net_user_claims",
                table: "AspNetUserClaims",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_asp_net_roles",
                table: "AspNetRoles",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_asp_net_role_claims",
                table: "AspNetRoleClaims",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_service_invoices",
                table: "service_invoices",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_sales_invoices",
                table: "sales_invoices",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_sales_books",
                table: "sales_books",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_receiving_reports",
                table: "receiving_reports",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_purchase_orders",
                table: "purchase_orders",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_purchase_journal_books",
                table: "purchase_journal_books",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_journal_voucher_headers",
                table: "journal_voucher_headers",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_journal_voucher_details",
                table: "journal_voucher_details",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_journal_books",
                table: "journal_books",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_general_ledger_books",
                table: "general_ledger_books",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_disbursement_books",
                table: "disbursement_books",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_debit_memos",
                table: "debit_memos",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_credit_memos",
                table: "credit_memos",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_collection_receipts",
                table: "collection_receipts",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_check_voucher_headers",
                table: "check_voucher_headers",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_check_voucher_details",
                table: "check_voucher_details",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_chart_of_accounts",
                table: "chart_of_accounts",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_cash_receipt_books",
                table: "cash_receipt_books",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_bank_accounts",
                table: "bank_accounts",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_audit_trails",
                table: "audit_trails",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_asp_net_role_claims_asp_net_roles_role_id",
                table: "AspNetRoleClaims",
                column: "role_id",
                principalTable: "AspNetRoles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_asp_net_user_claims_asp_net_users_user_id",
                table: "AspNetUserClaims",
                column: "user_id",
                principalTable: "AspNetUsers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_asp_net_user_logins_asp_net_users_user_id",
                table: "AspNetUserLogins",
                column: "user_id",
                principalTable: "AspNetUsers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_asp_net_user_roles_asp_net_roles_role_id",
                table: "AspNetUserRoles",
                column: "role_id",
                principalTable: "AspNetRoles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_asp_net_user_roles_asp_net_users_user_id",
                table: "AspNetUserRoles",
                column: "user_id",
                principalTable: "AspNetUsers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_asp_net_user_tokens_asp_net_users_user_id",
                table: "AspNetUserTokens",
                column: "user_id",
                principalTable: "AspNetUsers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_check_voucher_headers_bank_accounts_bank_id",
                table: "check_voucher_headers",
                column: "bank_id",
                principalTable: "bank_accounts",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_check_voucher_headers_suppliers_supplier_id",
                table: "check_voucher_headers",
                column: "supplier_id",
                principalTable: "suppliers",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_collection_receipts_sales_invoices_sales_invoice_id",
                table: "collection_receipts",
                column: "sales_invoice_id",
                principalTable: "sales_invoices",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_collection_receipts_service_invoices_service_invoice_id",
                table: "collection_receipts",
                column: "service_invoice_id",
                principalTable: "service_invoices",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_credit_memos_sales_invoices_sales_invoice_id",
                table: "credit_memos",
                column: "sales_invoice_id",
                principalTable: "sales_invoices",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_credit_memos_service_invoices_service_invoice_id",
                table: "credit_memos",
                column: "service_invoice_id",
                principalTable: "service_invoices",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_debit_memos_sales_invoices_sales_invoice_id",
                table: "debit_memos",
                column: "sales_invoice_id",
                principalTable: "sales_invoices",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_debit_memos_service_invoices_service_invoice_id",
                table: "debit_memos",
                column: "service_invoice_id",
                principalTable: "service_invoices",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_inventories_products_product_id",
                table: "inventories",
                column: "product_id",
                principalTable: "products",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_journal_voucher_headers_check_voucher_headers_cv_id",
                table: "journal_voucher_headers",
                column: "cv_id",
                principalTable: "check_voucher_headers",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_purchase_orders_products_product_id",
                table: "purchase_orders",
                column: "product_id",
                principalTable: "products",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_purchase_orders_suppliers_supplier_id",
                table: "purchase_orders",
                column: "supplier_id",
                principalTable: "suppliers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_receiving_reports_purchase_orders_po_id",
                table: "receiving_reports",
                column: "po_id",
                principalTable: "purchase_orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_invoices_customers_customer_id",
                table: "sales_invoices",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_sales_invoices_products_product_id",
                table: "sales_invoices",
                column: "product_id",
                principalTable: "products",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_service_invoices_customers_customer_id",
                table: "service_invoices",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_service_invoices_services_services_id",
                table: "service_invoices",
                column: "services_id",
                principalTable: "services",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_asp_net_role_claims_asp_net_roles_role_id",
                table: "AspNetRoleClaims");

            migrationBuilder.DropForeignKey(
                name: "fk_asp_net_user_claims_asp_net_users_user_id",
                table: "AspNetUserClaims");

            migrationBuilder.DropForeignKey(
                name: "fk_asp_net_user_logins_asp_net_users_user_id",
                table: "AspNetUserLogins");

            migrationBuilder.DropForeignKey(
                name: "fk_asp_net_user_roles_asp_net_roles_role_id",
                table: "AspNetUserRoles");

            migrationBuilder.DropForeignKey(
                name: "fk_asp_net_user_roles_asp_net_users_user_id",
                table: "AspNetUserRoles");

            migrationBuilder.DropForeignKey(
                name: "fk_asp_net_user_tokens_asp_net_users_user_id",
                table: "AspNetUserTokens");

            migrationBuilder.DropForeignKey(
                name: "fk_check_voucher_headers_bank_accounts_bank_id",
                table: "check_voucher_headers");

            migrationBuilder.DropForeignKey(
                name: "fk_check_voucher_headers_suppliers_supplier_id",
                table: "check_voucher_headers");

            migrationBuilder.DropForeignKey(
                name: "fk_collection_receipts_sales_invoices_sales_invoice_id",
                table: "collection_receipts");

            migrationBuilder.DropForeignKey(
                name: "fk_collection_receipts_service_invoices_service_invoice_id",
                table: "collection_receipts");

            migrationBuilder.DropForeignKey(
                name: "fk_credit_memos_sales_invoices_sales_invoice_id",
                table: "credit_memos");

            migrationBuilder.DropForeignKey(
                name: "fk_credit_memos_service_invoices_service_invoice_id",
                table: "credit_memos");

            migrationBuilder.DropForeignKey(
                name: "fk_debit_memos_sales_invoices_sales_invoice_id",
                table: "debit_memos");

            migrationBuilder.DropForeignKey(
                name: "fk_debit_memos_service_invoices_service_invoice_id",
                table: "debit_memos");

            migrationBuilder.DropForeignKey(
                name: "fk_inventories_products_product_id",
                table: "inventories");

            migrationBuilder.DropForeignKey(
                name: "fk_journal_voucher_headers_check_voucher_headers_cv_id",
                table: "journal_voucher_headers");

            migrationBuilder.DropForeignKey(
                name: "fk_purchase_orders_products_product_id",
                table: "purchase_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_purchase_orders_suppliers_supplier_id",
                table: "purchase_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_receiving_reports_purchase_orders_po_id",
                table: "receiving_reports");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_invoices_customers_customer_id",
                table: "sales_invoices");

            migrationBuilder.DropForeignKey(
                name: "fk_sales_invoices_products_product_id",
                table: "sales_invoices");

            migrationBuilder.DropForeignKey(
                name: "fk_service_invoices_customers_customer_id",
                table: "service_invoices");

            migrationBuilder.DropForeignKey(
                name: "fk_service_invoices_services_services_id",
                table: "service_invoices");

            migrationBuilder.DropPrimaryKey(
                name: "pk_suppliers",
                table: "suppliers");

            migrationBuilder.DropPrimaryKey(
                name: "pk_services",
                table: "services");

            migrationBuilder.DropPrimaryKey(
                name: "pk_products",
                table: "products");

            migrationBuilder.DropPrimaryKey(
                name: "pk_offsettings",
                table: "offsettings");

            migrationBuilder.DropPrimaryKey(
                name: "pk_inventories",
                table: "inventories");

            migrationBuilder.DropPrimaryKey(
                name: "pk_customers",
                table: "customers");

            migrationBuilder.DropPrimaryKey(
                name: "pk_asp_net_user_tokens",
                table: "AspNetUserTokens");

            migrationBuilder.DropPrimaryKey(
                name: "pk_asp_net_users",
                table: "AspNetUsers");

            migrationBuilder.DropPrimaryKey(
                name: "pk_asp_net_user_roles",
                table: "AspNetUserRoles");

            migrationBuilder.DropPrimaryKey(
                name: "pk_asp_net_user_logins",
                table: "AspNetUserLogins");

            migrationBuilder.DropPrimaryKey(
                name: "pk_asp_net_user_claims",
                table: "AspNetUserClaims");

            migrationBuilder.DropPrimaryKey(
                name: "pk_asp_net_roles",
                table: "AspNetRoles");

            migrationBuilder.DropPrimaryKey(
                name: "pk_asp_net_role_claims",
                table: "AspNetRoleClaims");

            migrationBuilder.DropPrimaryKey(
                name: "pk_service_invoices",
                table: "service_invoices");

            migrationBuilder.DropPrimaryKey(
                name: "pk_sales_invoices",
                table: "sales_invoices");

            migrationBuilder.DropPrimaryKey(
                name: "pk_sales_books",
                table: "sales_books");

            migrationBuilder.DropPrimaryKey(
                name: "pk_receiving_reports",
                table: "receiving_reports");

            migrationBuilder.DropPrimaryKey(
                name: "pk_purchase_orders",
                table: "purchase_orders");

            migrationBuilder.DropPrimaryKey(
                name: "pk_purchase_journal_books",
                table: "purchase_journal_books");

            migrationBuilder.DropPrimaryKey(
                name: "pk_journal_voucher_headers",
                table: "journal_voucher_headers");

            migrationBuilder.DropPrimaryKey(
                name: "pk_journal_voucher_details",
                table: "journal_voucher_details");

            migrationBuilder.DropPrimaryKey(
                name: "pk_journal_books",
                table: "journal_books");

            migrationBuilder.DropPrimaryKey(
                name: "pk_general_ledger_books",
                table: "general_ledger_books");

            migrationBuilder.DropPrimaryKey(
                name: "pk_disbursement_books",
                table: "disbursement_books");

            migrationBuilder.DropPrimaryKey(
                name: "pk_debit_memos",
                table: "debit_memos");

            migrationBuilder.DropPrimaryKey(
                name: "pk_credit_memos",
                table: "credit_memos");

            migrationBuilder.DropPrimaryKey(
                name: "pk_collection_receipts",
                table: "collection_receipts");

            migrationBuilder.DropPrimaryKey(
                name: "pk_check_voucher_headers",
                table: "check_voucher_headers");

            migrationBuilder.DropPrimaryKey(
                name: "pk_check_voucher_details",
                table: "check_voucher_details");

            migrationBuilder.DropPrimaryKey(
                name: "pk_chart_of_accounts",
                table: "chart_of_accounts");

            migrationBuilder.DropPrimaryKey(
                name: "pk_cash_receipt_books",
                table: "cash_receipt_books");

            migrationBuilder.DropPrimaryKey(
                name: "pk_bank_accounts",
                table: "bank_accounts");

            migrationBuilder.DropPrimaryKey(
                name: "pk_audit_trails",
                table: "audit_trails");

            migrationBuilder.RenameTable(
                name: "suppliers",
                newName: "Suppliers");

            migrationBuilder.RenameTable(
                name: "services",
                newName: "Services");

            migrationBuilder.RenameTable(
                name: "products",
                newName: "Products");

            migrationBuilder.RenameTable(
                name: "offsettings",
                newName: "Offsettings");

            migrationBuilder.RenameTable(
                name: "inventories",
                newName: "Inventories");

            migrationBuilder.RenameTable(
                name: "customers",
                newName: "Customers");

            migrationBuilder.RenameTable(
                name: "service_invoices",
                newName: "ServiceInvoices");

            migrationBuilder.RenameTable(
                name: "sales_invoices",
                newName: "SalesInvoices");

            migrationBuilder.RenameTable(
                name: "sales_books",
                newName: "SalesBooks");

            migrationBuilder.RenameTable(
                name: "receiving_reports",
                newName: "ReceivingReports");

            migrationBuilder.RenameTable(
                name: "purchase_orders",
                newName: "PurchaseOrders");

            migrationBuilder.RenameTable(
                name: "purchase_journal_books",
                newName: "PurchaseJournalBooks");

            migrationBuilder.RenameTable(
                name: "journal_voucher_headers",
                newName: "JournalVoucherHeaders");

            migrationBuilder.RenameTable(
                name: "journal_voucher_details",
                newName: "JournalVoucherDetails");

            migrationBuilder.RenameTable(
                name: "journal_books",
                newName: "JournalBooks");

            migrationBuilder.RenameTable(
                name: "general_ledger_books",
                newName: "GeneralLedgerBooks");

            migrationBuilder.RenameTable(
                name: "disbursement_books",
                newName: "DisbursementBooks");

            migrationBuilder.RenameTable(
                name: "debit_memos",
                newName: "DebitMemos");

            migrationBuilder.RenameTable(
                name: "credit_memos",
                newName: "CreditMemos");

            migrationBuilder.RenameTable(
                name: "collection_receipts",
                newName: "CollectionReceipts");

            migrationBuilder.RenameTable(
                name: "check_voucher_headers",
                newName: "CheckVoucherHeaders");

            migrationBuilder.RenameTable(
                name: "check_voucher_details",
                newName: "CheckVoucherDetails");

            migrationBuilder.RenameTable(
                name: "chart_of_accounts",
                newName: "ChartOfAccounts");

            migrationBuilder.RenameTable(
                name: "cash_receipt_books",
                newName: "CashReceiptBooks");

            migrationBuilder.RenameTable(
                name: "bank_accounts",
                newName: "BankAccounts");

            migrationBuilder.RenameTable(
                name: "audit_trails",
                newName: "AuditTrails");

            migrationBuilder.RenameColumn(
                name: "validity",
                table: "Suppliers",
                newName: "Validity");

            migrationBuilder.RenameColumn(
                name: "terms",
                table: "Suppliers",
                newName: "Terms");

            migrationBuilder.RenameColumn(
                name: "number",
                table: "Suppliers",
                newName: "Number");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "Suppliers",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "address",
                table: "Suppliers",
                newName: "Address");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Suppliers",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "vat_type",
                table: "Suppliers",
                newName: "VatType");

            migrationBuilder.RenameColumn(
                name: "validity_date",
                table: "Suppliers",
                newName: "ValidityDate");

            migrationBuilder.RenameColumn(
                name: "tin_no",
                table: "Suppliers",
                newName: "TinNo");

            migrationBuilder.RenameColumn(
                name: "tax_type",
                table: "Suppliers",
                newName: "TaxType");

            migrationBuilder.RenameColumn(
                name: "reason_of_exemption",
                table: "Suppliers",
                newName: "ReasonOfExemption");

            migrationBuilder.RenameColumn(
                name: "proof_of_registration_file_path",
                table: "Suppliers",
                newName: "ProofOfRegistrationFilePath");

            migrationBuilder.RenameColumn(
                name: "proof_of_exemption_file_path",
                table: "Suppliers",
                newName: "ProofOfExemptionFilePath");

            migrationBuilder.RenameColumn(
                name: "created_date",
                table: "Suppliers",
                newName: "CreatedDate");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "Suppliers",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "percent",
                table: "Services",
                newName: "Percent");

            migrationBuilder.RenameColumn(
                name: "number",
                table: "Services",
                newName: "Number");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "Services",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Services",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "unearned_title",
                table: "Services",
                newName: "UnearnedTitle");

            migrationBuilder.RenameColumn(
                name: "unearned_no",
                table: "Services",
                newName: "UnearnedNo");

            migrationBuilder.RenameColumn(
                name: "current_and_previous_title",
                table: "Services",
                newName: "CurrentAndPreviousTitle");

            migrationBuilder.RenameColumn(
                name: "current_and_previous_no",
                table: "Services",
                newName: "CurrentAndPreviousNo");

            migrationBuilder.RenameColumn(
                name: "created_date",
                table: "Services",
                newName: "CreatedDate");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "Services",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "unit",
                table: "Products",
                newName: "Unit");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "Products",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "code",
                table: "Products",
                newName: "Code");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Products",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "created_date",
                table: "Products",
                newName: "CreatedDate");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "Products",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "source",
                table: "Offsettings",
                newName: "Source");

            migrationBuilder.RenameColumn(
                name: "reference",
                table: "Offsettings",
                newName: "Reference");

            migrationBuilder.RenameColumn(
                name: "amount",
                table: "Offsettings",
                newName: "Amount");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Offsettings",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "is_removed",
                table: "Offsettings",
                newName: "IsRemoved");

            migrationBuilder.RenameColumn(
                name: "created_date",
                table: "Offsettings",
                newName: "CreatedDate");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "Offsettings",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "account_no",
                table: "Offsettings",
                newName: "AccountNo");

            migrationBuilder.RenameColumn(
                name: "unit",
                table: "Inventories",
                newName: "Unit");

            migrationBuilder.RenameColumn(
                name: "total",
                table: "Inventories",
                newName: "Total");

            migrationBuilder.RenameColumn(
                name: "reference",
                table: "Inventories",
                newName: "Reference");

            migrationBuilder.RenameColumn(
                name: "quantity",
                table: "Inventories",
                newName: "Quantity");

            migrationBuilder.RenameColumn(
                name: "particular",
                table: "Inventories",
                newName: "Particular");

            migrationBuilder.RenameColumn(
                name: "date",
                table: "Inventories",
                newName: "Date");

            migrationBuilder.RenameColumn(
                name: "cost",
                table: "Inventories",
                newName: "Cost");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Inventories",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "validated_date",
                table: "Inventories",
                newName: "ValidatedDate");

            migrationBuilder.RenameColumn(
                name: "validated_by",
                table: "Inventories",
                newName: "ValidatedBy");

            migrationBuilder.RenameColumn(
                name: "total_balance",
                table: "Inventories",
                newName: "TotalBalance");

            migrationBuilder.RenameColumn(
                name: "product_id",
                table: "Inventories",
                newName: "ProductId");

            migrationBuilder.RenameColumn(
                name: "is_validated",
                table: "Inventories",
                newName: "IsValidated");

            migrationBuilder.RenameColumn(
                name: "inventory_balance",
                table: "Inventories",
                newName: "InventoryBalance");

            migrationBuilder.RenameColumn(
                name: "average_cost",
                table: "Inventories",
                newName: "AverageCost");

            migrationBuilder.RenameIndex(
                name: "ix_inventories_product_id",
                table: "Inventories",
                newName: "IX_Inventories_ProductId");

            migrationBuilder.RenameColumn(
                name: "terms",
                table: "Customers",
                newName: "Terms");

            migrationBuilder.RenameColumn(
                name: "number",
                table: "Customers",
                newName: "Number");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "Customers",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "address",
                table: "Customers",
                newName: "Address");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Customers",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "with_holding_vat",
                table: "Customers",
                newName: "WithHoldingVat");

            migrationBuilder.RenameColumn(
                name: "with_holding_tax",
                table: "Customers",
                newName: "WithHoldingTax");

            migrationBuilder.RenameColumn(
                name: "tin_no",
                table: "Customers",
                newName: "TinNo");

            migrationBuilder.RenameColumn(
                name: "customer_type",
                table: "Customers",
                newName: "CustomerType");

            migrationBuilder.RenameColumn(
                name: "created_date",
                table: "Customers",
                newName: "CreatedDate");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "Customers",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "business_style",
                table: "Customers",
                newName: "BusinessStyle");

            migrationBuilder.RenameColumn(
                name: "value",
                table: "AspNetUserTokens",
                newName: "Value");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "AspNetUserTokens",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "login_provider",
                table: "AspNetUserTokens",
                newName: "LoginProvider");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "AspNetUserTokens",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "email",
                table: "AspNetUsers",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "discriminator",
                table: "AspNetUsers",
                newName: "Discriminator");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "AspNetUsers",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_name",
                table: "AspNetUsers",
                newName: "UserName");

            migrationBuilder.RenameColumn(
                name: "two_factor_enabled",
                table: "AspNetUsers",
                newName: "TwoFactorEnabled");

            migrationBuilder.RenameColumn(
                name: "security_stamp",
                table: "AspNetUsers",
                newName: "SecurityStamp");

            migrationBuilder.RenameColumn(
                name: "phone_number_confirmed",
                table: "AspNetUsers",
                newName: "PhoneNumberConfirmed");

            migrationBuilder.RenameColumn(
                name: "phone_number",
                table: "AspNetUsers",
                newName: "PhoneNumber");

            migrationBuilder.RenameColumn(
                name: "password_hash",
                table: "AspNetUsers",
                newName: "PasswordHash");

            migrationBuilder.RenameColumn(
                name: "normalized_user_name",
                table: "AspNetUsers",
                newName: "NormalizedUserName");

            migrationBuilder.RenameColumn(
                name: "normalized_email",
                table: "AspNetUsers",
                newName: "NormalizedEmail");

            migrationBuilder.RenameColumn(
                name: "lockout_end",
                table: "AspNetUsers",
                newName: "LockoutEnd");

            migrationBuilder.RenameColumn(
                name: "lockout_enabled",
                table: "AspNetUsers",
                newName: "LockoutEnabled");

            migrationBuilder.RenameColumn(
                name: "last_name",
                table: "AspNetUsers",
                newName: "LastName");

            migrationBuilder.RenameColumn(
                name: "first_name",
                table: "AspNetUsers",
                newName: "FirstName");

            migrationBuilder.RenameColumn(
                name: "email_confirmed",
                table: "AspNetUsers",
                newName: "EmailConfirmed");

            migrationBuilder.RenameColumn(
                name: "concurrency_stamp",
                table: "AspNetUsers",
                newName: "ConcurrencyStamp");

            migrationBuilder.RenameColumn(
                name: "access_failed_count",
                table: "AspNetUsers",
                newName: "AccessFailedCount");

            migrationBuilder.RenameColumn(
                name: "role_id",
                table: "AspNetUserRoles",
                newName: "RoleId");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "AspNetUserRoles",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "ix_asp_net_user_roles_role_id",
                table: "AspNetUserRoles",
                newName: "IX_AspNetUserRoles_RoleId");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "AspNetUserLogins",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "provider_display_name",
                table: "AspNetUserLogins",
                newName: "ProviderDisplayName");

            migrationBuilder.RenameColumn(
                name: "provider_key",
                table: "AspNetUserLogins",
                newName: "ProviderKey");

            migrationBuilder.RenameColumn(
                name: "login_provider",
                table: "AspNetUserLogins",
                newName: "LoginProvider");

            migrationBuilder.RenameIndex(
                name: "ix_asp_net_user_logins_user_id",
                table: "AspNetUserLogins",
                newName: "IX_AspNetUserLogins_UserId");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "AspNetUserClaims",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "AspNetUserClaims",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "claim_value",
                table: "AspNetUserClaims",
                newName: "ClaimValue");

            migrationBuilder.RenameColumn(
                name: "claim_type",
                table: "AspNetUserClaims",
                newName: "ClaimType");

            migrationBuilder.RenameIndex(
                name: "ix_asp_net_user_claims_user_id",
                table: "AspNetUserClaims",
                newName: "IX_AspNetUserClaims_UserId");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "AspNetRoles",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "AspNetRoles",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "normalized_name",
                table: "AspNetRoles",
                newName: "NormalizedName");

            migrationBuilder.RenameColumn(
                name: "concurrency_stamp",
                table: "AspNetRoles",
                newName: "ConcurrencyStamp");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "AspNetRoleClaims",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "role_id",
                table: "AspNetRoleClaims",
                newName: "RoleId");

            migrationBuilder.RenameColumn(
                name: "claim_value",
                table: "AspNetRoleClaims",
                newName: "ClaimValue");

            migrationBuilder.RenameColumn(
                name: "claim_type",
                table: "AspNetRoleClaims",
                newName: "ClaimType");

            migrationBuilder.RenameIndex(
                name: "ix_asp_net_role_claims_role_id",
                table: "AspNetRoleClaims",
                newName: "IX_AspNetRoleClaims_RoleId");

            migrationBuilder.RenameColumn(
                name: "total",
                table: "ServiceInvoices",
                newName: "Total");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "ServiceInvoices",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "period",
                table: "ServiceInvoices",
                newName: "Period");

            migrationBuilder.RenameColumn(
                name: "instructions",
                table: "ServiceInvoices",
                newName: "Instructions");

            migrationBuilder.RenameColumn(
                name: "discount",
                table: "ServiceInvoices",
                newName: "Discount");

            migrationBuilder.RenameColumn(
                name: "balance",
                table: "ServiceInvoices",
                newName: "Balance");

            migrationBuilder.RenameColumn(
                name: "amount",
                table: "ServiceInvoices",
                newName: "Amount");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "ServiceInvoices",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "withholding_vat_amount",
                table: "ServiceInvoices",
                newName: "WithholdingVatAmount");

            migrationBuilder.RenameColumn(
                name: "withholding_tax_amount",
                table: "ServiceInvoices",
                newName: "WithholdingTaxAmount");

            migrationBuilder.RenameColumn(
                name: "voided_date",
                table: "ServiceInvoices",
                newName: "VoidedDate");

            migrationBuilder.RenameColumn(
                name: "voided_by",
                table: "ServiceInvoices",
                newName: "VoidedBy");

            migrationBuilder.RenameColumn(
                name: "vat_amount",
                table: "ServiceInvoices",
                newName: "VatAmount");

            migrationBuilder.RenameColumn(
                name: "unearned_amount",
                table: "ServiceInvoices",
                newName: "UnearnedAmount");

            migrationBuilder.RenameColumn(
                name: "sv_no",
                table: "ServiceInvoices",
                newName: "SVNo");

            migrationBuilder.RenameColumn(
                name: "services_id",
                table: "ServiceInvoices",
                newName: "ServicesId");

            migrationBuilder.RenameColumn(
                name: "service_no",
                table: "ServiceInvoices",
                newName: "ServiceNo");

            migrationBuilder.RenameColumn(
                name: "series_number",
                table: "ServiceInvoices",
                newName: "SeriesNumber");

            migrationBuilder.RenameColumn(
                name: "posted_date",
                table: "ServiceInvoices",
                newName: "PostedDate");

            migrationBuilder.RenameColumn(
                name: "posted_by",
                table: "ServiceInvoices",
                newName: "PostedBy");

            migrationBuilder.RenameColumn(
                name: "net_amount",
                table: "ServiceInvoices",
                newName: "NetAmount");

            migrationBuilder.RenameColumn(
                name: "is_voided",
                table: "ServiceInvoices",
                newName: "IsVoided");

            migrationBuilder.RenameColumn(
                name: "is_printed",
                table: "ServiceInvoices",
                newName: "IsPrinted");

            migrationBuilder.RenameColumn(
                name: "is_posted",
                table: "ServiceInvoices",
                newName: "IsPosted");

            migrationBuilder.RenameColumn(
                name: "is_paid",
                table: "ServiceInvoices",
                newName: "IsPaid");

            migrationBuilder.RenameColumn(
                name: "is_canceled",
                table: "ServiceInvoices",
                newName: "IsCanceled");

            migrationBuilder.RenameColumn(
                name: "due_date",
                table: "ServiceInvoices",
                newName: "DueDate");

            migrationBuilder.RenameColumn(
                name: "customer_id",
                table: "ServiceInvoices",
                newName: "CustomerId");

            migrationBuilder.RenameColumn(
                name: "current_and_previous_amount",
                table: "ServiceInvoices",
                newName: "CurrentAndPreviousAmount");

            migrationBuilder.RenameColumn(
                name: "created_date",
                table: "ServiceInvoices",
                newName: "CreatedDate");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "ServiceInvoices",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "canceled_date",
                table: "ServiceInvoices",
                newName: "CanceledDate");

            migrationBuilder.RenameColumn(
                name: "canceled_by",
                table: "ServiceInvoices",
                newName: "CanceledBy");

            migrationBuilder.RenameColumn(
                name: "amount_paid",
                table: "ServiceInvoices",
                newName: "AmountPaid");

            migrationBuilder.RenameIndex(
                name: "ix_service_invoices_services_id",
                table: "ServiceInvoices",
                newName: "IX_ServiceInvoices_ServicesId");

            migrationBuilder.RenameIndex(
                name: "ix_service_invoices_customer_id",
                table: "ServiceInvoices",
                newName: "IX_ServiceInvoices_CustomerId");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "SalesInvoices",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "remarks",
                table: "SalesInvoices",
                newName: "Remarks");

            migrationBuilder.RenameColumn(
                name: "quantity",
                table: "SalesInvoices",
                newName: "Quantity");

            migrationBuilder.RenameColumn(
                name: "discount",
                table: "SalesInvoices",
                newName: "Discount");

            migrationBuilder.RenameColumn(
                name: "balance",
                table: "SalesInvoices",
                newName: "Balance");

            migrationBuilder.RenameColumn(
                name: "amount",
                table: "SalesInvoices",
                newName: "Amount");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "SalesInvoices",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "zero_rated",
                table: "SalesInvoices",
                newName: "ZeroRated");

            migrationBuilder.RenameColumn(
                name: "with_holding_vat_amount",
                table: "SalesInvoices",
                newName: "WithHoldingVatAmount");

            migrationBuilder.RenameColumn(
                name: "with_holding_tax_amount",
                table: "SalesInvoices",
                newName: "WithHoldingTaxAmount");

            migrationBuilder.RenameColumn(
                name: "voided_date",
                table: "SalesInvoices",
                newName: "VoidedDate");

            migrationBuilder.RenameColumn(
                name: "voided_by",
                table: "SalesInvoices",
                newName: "VoidedBy");

            migrationBuilder.RenameColumn(
                name: "vatable_sales",
                table: "SalesInvoices",
                newName: "VatableSales");

            migrationBuilder.RenameColumn(
                name: "vat_exempt",
                table: "SalesInvoices",
                newName: "VatExempt");

            migrationBuilder.RenameColumn(
                name: "vat_amount",
                table: "SalesInvoices",
                newName: "VatAmount");

            migrationBuilder.RenameColumn(
                name: "unit_price",
                table: "SalesInvoices",
                newName: "UnitPrice");

            migrationBuilder.RenameColumn(
                name: "transaction_date",
                table: "SalesInvoices",
                newName: "TransactionDate");

            migrationBuilder.RenameColumn(
                name: "si_no",
                table: "SalesInvoices",
                newName: "SINo");

            migrationBuilder.RenameColumn(
                name: "series_number",
                table: "SalesInvoices",
                newName: "SeriesNumber");

            migrationBuilder.RenameColumn(
                name: "product_id",
                table: "SalesInvoices",
                newName: "ProductId");

            migrationBuilder.RenameColumn(
                name: "posted_date",
                table: "SalesInvoices",
                newName: "PostedDate");

            migrationBuilder.RenameColumn(
                name: "posted_by",
                table: "SalesInvoices",
                newName: "PostedBy");

            migrationBuilder.RenameColumn(
                name: "po_no",
                table: "SalesInvoices",
                newName: "PoNo");

            migrationBuilder.RenameColumn(
                name: "other_ref_no",
                table: "SalesInvoices",
                newName: "OtherRefNo");

            migrationBuilder.RenameColumn(
                name: "net_discount",
                table: "SalesInvoices",
                newName: "NetDiscount");

            migrationBuilder.RenameColumn(
                name: "is_voided",
                table: "SalesInvoices",
                newName: "IsVoided");

            migrationBuilder.RenameColumn(
                name: "is_tax_and_vat_paid",
                table: "SalesInvoices",
                newName: "IsTaxAndVatPaid");

            migrationBuilder.RenameColumn(
                name: "is_printed",
                table: "SalesInvoices",
                newName: "IsPrinted");

            migrationBuilder.RenameColumn(
                name: "is_posted",
                table: "SalesInvoices",
                newName: "IsPosted");

            migrationBuilder.RenameColumn(
                name: "is_paid",
                table: "SalesInvoices",
                newName: "IsPaid");

            migrationBuilder.RenameColumn(
                name: "is_canceled",
                table: "SalesInvoices",
                newName: "IsCanceled");

            migrationBuilder.RenameColumn(
                name: "due_date",
                table: "SalesInvoices",
                newName: "DueDate");

            migrationBuilder.RenameColumn(
                name: "customer_id",
                table: "SalesInvoices",
                newName: "CustomerId");

            migrationBuilder.RenameColumn(
                name: "created_date",
                table: "SalesInvoices",
                newName: "CreatedDate");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "SalesInvoices",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "canceled_date",
                table: "SalesInvoices",
                newName: "CanceledDate");

            migrationBuilder.RenameColumn(
                name: "canceled_by",
                table: "SalesInvoices",
                newName: "CanceledBy");

            migrationBuilder.RenameColumn(
                name: "amount_paid",
                table: "SalesInvoices",
                newName: "AmountPaid");

            migrationBuilder.RenameIndex(
                name: "ix_sales_invoices_product_id",
                table: "SalesInvoices",
                newName: "IX_SalesInvoices_ProductId");

            migrationBuilder.RenameIndex(
                name: "ix_sales_invoices_customer_id",
                table: "SalesInvoices",
                newName: "IX_SalesInvoices_CustomerId");

            migrationBuilder.RenameColumn(
                name: "discount",
                table: "SalesBooks",
                newName: "Discount");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "SalesBooks",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "amount",
                table: "SalesBooks",
                newName: "Amount");

            migrationBuilder.RenameColumn(
                name: "address",
                table: "SalesBooks",
                newName: "Address");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "SalesBooks",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "zero_rated",
                table: "SalesBooks",
                newName: "ZeroRated");

            migrationBuilder.RenameColumn(
                name: "vatable_sales",
                table: "SalesBooks",
                newName: "VatableSales");

            migrationBuilder.RenameColumn(
                name: "vat_exempt_sales",
                table: "SalesBooks",
                newName: "VatExemptSales");

            migrationBuilder.RenameColumn(
                name: "vat_amount",
                table: "SalesBooks",
                newName: "VatAmount");

            migrationBuilder.RenameColumn(
                name: "transaction_date",
                table: "SalesBooks",
                newName: "TransactionDate");

            migrationBuilder.RenameColumn(
                name: "tin_no",
                table: "SalesBooks",
                newName: "TinNo");

            migrationBuilder.RenameColumn(
                name: "sold_to",
                table: "SalesBooks",
                newName: "SoldTo");

            migrationBuilder.RenameColumn(
                name: "serial_no",
                table: "SalesBooks",
                newName: "SerialNo");

            migrationBuilder.RenameColumn(
                name: "net_sales",
                table: "SalesBooks",
                newName: "NetSales");

            migrationBuilder.RenameColumn(
                name: "due_date",
                table: "SalesBooks",
                newName: "DueDate");

            migrationBuilder.RenameColumn(
                name: "document_id",
                table: "SalesBooks",
                newName: "DocumentId");

            migrationBuilder.RenameColumn(
                name: "created_date",
                table: "SalesBooks",
                newName: "CreatedDate");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "SalesBooks",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "remarks",
                table: "ReceivingReports",
                newName: "Remarks");

            migrationBuilder.RenameColumn(
                name: "date",
                table: "ReceivingReports",
                newName: "Date");

            migrationBuilder.RenameColumn(
                name: "amount",
                table: "ReceivingReports",
                newName: "Amount");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "ReceivingReports",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "voided_date",
                table: "ReceivingReports",
                newName: "VoidedDate");

            migrationBuilder.RenameColumn(
                name: "voided_by",
                table: "ReceivingReports",
                newName: "VoidedBy");

            migrationBuilder.RenameColumn(
                name: "vat_amount",
                table: "ReceivingReports",
                newName: "VatAmount");

            migrationBuilder.RenameColumn(
                name: "truck_or_vessels",
                table: "ReceivingReports",
                newName: "TruckOrVessels");

            migrationBuilder.RenameColumn(
                name: "supplier_invoice_number",
                table: "ReceivingReports",
                newName: "SupplierInvoiceNumber");

            migrationBuilder.RenameColumn(
                name: "supplier_invoice_date",
                table: "ReceivingReports",
                newName: "SupplierInvoiceDate");

            migrationBuilder.RenameColumn(
                name: "series_number",
                table: "ReceivingReports",
                newName: "SeriesNumber");

            migrationBuilder.RenameColumn(
                name: "rr_no",
                table: "ReceivingReports",
                newName: "RRNo");

            migrationBuilder.RenameColumn(
                name: "quantity_received",
                table: "ReceivingReports",
                newName: "QuantityReceived");

            migrationBuilder.RenameColumn(
                name: "quantity_delivered",
                table: "ReceivingReports",
                newName: "QuantityDelivered");

            migrationBuilder.RenameColumn(
                name: "posted_date",
                table: "ReceivingReports",
                newName: "PostedDate");

            migrationBuilder.RenameColumn(
                name: "posted_by",
                table: "ReceivingReports",
                newName: "PostedBy");

            migrationBuilder.RenameColumn(
                name: "po_no",
                table: "ReceivingReports",
                newName: "PONo");

            migrationBuilder.RenameColumn(
                name: "po_id",
                table: "ReceivingReports",
                newName: "POId");

            migrationBuilder.RenameColumn(
                name: "paid_date",
                table: "ReceivingReports",
                newName: "PaidDate");

            migrationBuilder.RenameColumn(
                name: "other_ref",
                table: "ReceivingReports",
                newName: "OtherRef");

            migrationBuilder.RenameColumn(
                name: "net_amount_of_ewt",
                table: "ReceivingReports",
                newName: "NetAmountOfEWT");

            migrationBuilder.RenameColumn(
                name: "net_amount",
                table: "ReceivingReports",
                newName: "NetAmount");

            migrationBuilder.RenameColumn(
                name: "is_voided",
                table: "ReceivingReports",
                newName: "IsVoided");

            migrationBuilder.RenameColumn(
                name: "is_printed",
                table: "ReceivingReports",
                newName: "IsPrinted");

            migrationBuilder.RenameColumn(
                name: "is_posted",
                table: "ReceivingReports",
                newName: "IsPosted");

            migrationBuilder.RenameColumn(
                name: "is_paid",
                table: "ReceivingReports",
                newName: "IsPaid");

            migrationBuilder.RenameColumn(
                name: "is_canceled",
                table: "ReceivingReports",
                newName: "IsCanceled");

            migrationBuilder.RenameColumn(
                name: "gain_or_loss",
                table: "ReceivingReports",
                newName: "GainOrLoss");

            migrationBuilder.RenameColumn(
                name: "ewt_amount",
                table: "ReceivingReports",
                newName: "EwtAmount");

            migrationBuilder.RenameColumn(
                name: "due_date",
                table: "ReceivingReports",
                newName: "DueDate");

            migrationBuilder.RenameColumn(
                name: "created_date",
                table: "ReceivingReports",
                newName: "CreatedDate");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "ReceivingReports",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "canceled_quantity",
                table: "ReceivingReports",
                newName: "CanceledQuantity");

            migrationBuilder.RenameColumn(
                name: "canceled_date",
                table: "ReceivingReports",
                newName: "CanceledDate");

            migrationBuilder.RenameColumn(
                name: "canceled_by",
                table: "ReceivingReports",
                newName: "CanceledBy");

            migrationBuilder.RenameColumn(
                name: "amount_paid",
                table: "ReceivingReports",
                newName: "AmountPaid");

            migrationBuilder.RenameIndex(
                name: "ix_receiving_reports_po_id",
                table: "ReceivingReports",
                newName: "IX_ReceivingReports_POId");

            migrationBuilder.RenameColumn(
                name: "terms",
                table: "PurchaseOrders",
                newName: "Terms");

            migrationBuilder.RenameColumn(
                name: "remarks",
                table: "PurchaseOrders",
                newName: "Remarks");

            migrationBuilder.RenameColumn(
                name: "quantity",
                table: "PurchaseOrders",
                newName: "Quantity");

            migrationBuilder.RenameColumn(
                name: "price",
                table: "PurchaseOrders",
                newName: "Price");

            migrationBuilder.RenameColumn(
                name: "date",
                table: "PurchaseOrders",
                newName: "Date");

            migrationBuilder.RenameColumn(
                name: "amount",
                table: "PurchaseOrders",
                newName: "Amount");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "PurchaseOrders",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "voided_date",
                table: "PurchaseOrders",
                newName: "VoidedDate");

            migrationBuilder.RenameColumn(
                name: "voided_by",
                table: "PurchaseOrders",
                newName: "VoidedBy");

            migrationBuilder.RenameColumn(
                name: "supplier_no",
                table: "PurchaseOrders",
                newName: "SupplierNo");

            migrationBuilder.RenameColumn(
                name: "supplier_id",
                table: "PurchaseOrders",
                newName: "SupplierId");

            migrationBuilder.RenameColumn(
                name: "series_number",
                table: "PurchaseOrders",
                newName: "SeriesNumber");

            migrationBuilder.RenameColumn(
                name: "received_date",
                table: "PurchaseOrders",
                newName: "ReceivedDate");

            migrationBuilder.RenameColumn(
                name: "quantity_received",
                table: "PurchaseOrders",
                newName: "QuantityReceived");

            migrationBuilder.RenameColumn(
                name: "product_no",
                table: "PurchaseOrders",
                newName: "ProductNo");

            migrationBuilder.RenameColumn(
                name: "product_id",
                table: "PurchaseOrders",
                newName: "ProductId");

            migrationBuilder.RenameColumn(
                name: "posted_date",
                table: "PurchaseOrders",
                newName: "PostedDate");

            migrationBuilder.RenameColumn(
                name: "posted_by",
                table: "PurchaseOrders",
                newName: "PostedBy");

            migrationBuilder.RenameColumn(
                name: "po_no",
                table: "PurchaseOrders",
                newName: "PONo");

            migrationBuilder.RenameColumn(
                name: "is_voided",
                table: "PurchaseOrders",
                newName: "IsVoided");

            migrationBuilder.RenameColumn(
                name: "is_received",
                table: "PurchaseOrders",
                newName: "IsReceived");

            migrationBuilder.RenameColumn(
                name: "is_printed",
                table: "PurchaseOrders",
                newName: "IsPrinted");

            migrationBuilder.RenameColumn(
                name: "is_posted",
                table: "PurchaseOrders",
                newName: "IsPosted");

            migrationBuilder.RenameColumn(
                name: "is_canceled",
                table: "PurchaseOrders",
                newName: "IsCanceled");

            migrationBuilder.RenameColumn(
                name: "final_price",
                table: "PurchaseOrders",
                newName: "FinalPrice");

            migrationBuilder.RenameColumn(
                name: "created_date",
                table: "PurchaseOrders",
                newName: "CreatedDate");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "PurchaseOrders",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "canceled_date",
                table: "PurchaseOrders",
                newName: "CanceledDate");

            migrationBuilder.RenameColumn(
                name: "canceled_by",
                table: "PurchaseOrders",
                newName: "CanceledBy");

            migrationBuilder.RenameIndex(
                name: "ix_purchase_orders_supplier_id",
                table: "PurchaseOrders",
                newName: "IX_PurchaseOrders_SupplierId");

            migrationBuilder.RenameIndex(
                name: "ix_purchase_orders_product_id",
                table: "PurchaseOrders",
                newName: "IX_PurchaseOrders_ProductId");

            migrationBuilder.RenameColumn(
                name: "discount",
                table: "PurchaseJournalBooks",
                newName: "Discount");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "PurchaseJournalBooks",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "date",
                table: "PurchaseJournalBooks",
                newName: "Date");

            migrationBuilder.RenameColumn(
                name: "amount",
                table: "PurchaseJournalBooks",
                newName: "Amount");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "PurchaseJournalBooks",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "wht_amount",
                table: "PurchaseJournalBooks",
                newName: "WhtAmount");

            migrationBuilder.RenameColumn(
                name: "vat_amount",
                table: "PurchaseJournalBooks",
                newName: "VatAmount");

            migrationBuilder.RenameColumn(
                name: "supplier_tin",
                table: "PurchaseJournalBooks",
                newName: "SupplierTin");

            migrationBuilder.RenameColumn(
                name: "supplier_name",
                table: "PurchaseJournalBooks",
                newName: "SupplierName");

            migrationBuilder.RenameColumn(
                name: "supplier_address",
                table: "PurchaseJournalBooks",
                newName: "SupplierAddress");

            migrationBuilder.RenameColumn(
                name: "po_no",
                table: "PurchaseJournalBooks",
                newName: "PONo");

            migrationBuilder.RenameColumn(
                name: "net_purchases",
                table: "PurchaseJournalBooks",
                newName: "NetPurchases");

            migrationBuilder.RenameColumn(
                name: "due_date",
                table: "PurchaseJournalBooks",
                newName: "DueDate");

            migrationBuilder.RenameColumn(
                name: "document_no",
                table: "PurchaseJournalBooks",
                newName: "DocumentNo");

            migrationBuilder.RenameColumn(
                name: "created_date",
                table: "PurchaseJournalBooks",
                newName: "CreatedDate");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "PurchaseJournalBooks",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "references",
                table: "JournalVoucherHeaders",
                newName: "References");

            migrationBuilder.RenameColumn(
                name: "particulars",
                table: "JournalVoucherHeaders",
                newName: "Particulars");

            migrationBuilder.RenameColumn(
                name: "date",
                table: "JournalVoucherHeaders",
                newName: "Date");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "JournalVoucherHeaders",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "voided_date",
                table: "JournalVoucherHeaders",
                newName: "VoidedDate");

            migrationBuilder.RenameColumn(
                name: "voided_by",
                table: "JournalVoucherHeaders",
                newName: "VoidedBy");

            migrationBuilder.RenameColumn(
                name: "series_number",
                table: "JournalVoucherHeaders",
                newName: "SeriesNumber");

            migrationBuilder.RenameColumn(
                name: "posted_date",
                table: "JournalVoucherHeaders",
                newName: "PostedDate");

            migrationBuilder.RenameColumn(
                name: "posted_by",
                table: "JournalVoucherHeaders",
                newName: "PostedBy");

            migrationBuilder.RenameColumn(
                name: "jv_reason",
                table: "JournalVoucherHeaders",
                newName: "JVReason");

            migrationBuilder.RenameColumn(
                name: "jv_no",
                table: "JournalVoucherHeaders",
                newName: "JVNo");

            migrationBuilder.RenameColumn(
                name: "is_voided",
                table: "JournalVoucherHeaders",
                newName: "IsVoided");

            migrationBuilder.RenameColumn(
                name: "is_printed",
                table: "JournalVoucherHeaders",
                newName: "IsPrinted");

            migrationBuilder.RenameColumn(
                name: "is_posted",
                table: "JournalVoucherHeaders",
                newName: "IsPosted");

            migrationBuilder.RenameColumn(
                name: "is_canceled",
                table: "JournalVoucherHeaders",
                newName: "IsCanceled");

            migrationBuilder.RenameColumn(
                name: "cv_id",
                table: "JournalVoucherHeaders",
                newName: "CVId");

            migrationBuilder.RenameColumn(
                name: "created_date",
                table: "JournalVoucherHeaders",
                newName: "CreatedDate");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "JournalVoucherHeaders",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "cr_no",
                table: "JournalVoucherHeaders",
                newName: "CRNo");

            migrationBuilder.RenameColumn(
                name: "canceled_date",
                table: "JournalVoucherHeaders",
                newName: "CanceledDate");

            migrationBuilder.RenameColumn(
                name: "canceled_by",
                table: "JournalVoucherHeaders",
                newName: "CanceledBy");

            migrationBuilder.RenameIndex(
                name: "ix_journal_voucher_headers_cv_id",
                table: "JournalVoucherHeaders",
                newName: "IX_JournalVoucherHeaders_CVId");

            migrationBuilder.RenameColumn(
                name: "debit",
                table: "JournalVoucherDetails",
                newName: "Debit");

            migrationBuilder.RenameColumn(
                name: "credit",
                table: "JournalVoucherDetails",
                newName: "Credit");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "JournalVoucherDetails",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "transaction_no",
                table: "JournalVoucherDetails",
                newName: "TransactionNo");

            migrationBuilder.RenameColumn(
                name: "account_no",
                table: "JournalVoucherDetails",
                newName: "AccountNo");

            migrationBuilder.RenameColumn(
                name: "account_name",
                table: "JournalVoucherDetails",
                newName: "AccountName");

            migrationBuilder.RenameColumn(
                name: "reference",
                table: "JournalBooks",
                newName: "Reference");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "JournalBooks",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "debit",
                table: "JournalBooks",
                newName: "Debit");

            migrationBuilder.RenameColumn(
                name: "date",
                table: "JournalBooks",
                newName: "Date");

            migrationBuilder.RenameColumn(
                name: "credit",
                table: "JournalBooks",
                newName: "Credit");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "JournalBooks",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "created_date",
                table: "JournalBooks",
                newName: "CreatedDate");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "JournalBooks",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "account_title",
                table: "JournalBooks",
                newName: "AccountTitle");

            migrationBuilder.RenameColumn(
                name: "reference",
                table: "GeneralLedgerBooks",
                newName: "Reference");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "GeneralLedgerBooks",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "debit",
                table: "GeneralLedgerBooks",
                newName: "Debit");

            migrationBuilder.RenameColumn(
                name: "date",
                table: "GeneralLedgerBooks",
                newName: "Date");

            migrationBuilder.RenameColumn(
                name: "credit",
                table: "GeneralLedgerBooks",
                newName: "Credit");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "GeneralLedgerBooks",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "is_posted",
                table: "GeneralLedgerBooks",
                newName: "IsPosted");

            migrationBuilder.RenameColumn(
                name: "created_date",
                table: "GeneralLedgerBooks",
                newName: "CreatedDate");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "GeneralLedgerBooks",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "account_title",
                table: "GeneralLedgerBooks",
                newName: "AccountTitle");

            migrationBuilder.RenameColumn(
                name: "account_no",
                table: "GeneralLedgerBooks",
                newName: "AccountNo");

            migrationBuilder.RenameColumn(
                name: "payee",
                table: "DisbursementBooks",
                newName: "Payee");

            migrationBuilder.RenameColumn(
                name: "particulars",
                table: "DisbursementBooks",
                newName: "Particulars");

            migrationBuilder.RenameColumn(
                name: "debit",
                table: "DisbursementBooks",
                newName: "Debit");

            migrationBuilder.RenameColumn(
                name: "date",
                table: "DisbursementBooks",
                newName: "Date");

            migrationBuilder.RenameColumn(
                name: "credit",
                table: "DisbursementBooks",
                newName: "Credit");

            migrationBuilder.RenameColumn(
                name: "bank",
                table: "DisbursementBooks",
                newName: "Bank");

            migrationBuilder.RenameColumn(
                name: "amount",
                table: "DisbursementBooks",
                newName: "Amount");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "DisbursementBooks",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "cv_no",
                table: "DisbursementBooks",
                newName: "CVNo");

            migrationBuilder.RenameColumn(
                name: "created_date",
                table: "DisbursementBooks",
                newName: "CreatedDate");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "DisbursementBooks",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "check_no",
                table: "DisbursementBooks",
                newName: "CheckNo");

            migrationBuilder.RenameColumn(
                name: "check_date",
                table: "DisbursementBooks",
                newName: "CheckDate");

            migrationBuilder.RenameColumn(
                name: "chart_of_account",
                table: "DisbursementBooks",
                newName: "ChartOfAccount");

            migrationBuilder.RenameColumn(
                name: "source",
                table: "DebitMemos",
                newName: "Source");

            migrationBuilder.RenameColumn(
                name: "remarks",
                table: "DebitMemos",
                newName: "Remarks");

            migrationBuilder.RenameColumn(
                name: "quantity",
                table: "DebitMemos",
                newName: "Quantity");

            migrationBuilder.RenameColumn(
                name: "period",
                table: "DebitMemos",
                newName: "Period");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "DebitMemos",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "amount",
                table: "DebitMemos",
                newName: "Amount");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "DebitMemos",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "with_holding_vat_amount",
                table: "DebitMemos",
                newName: "WithHoldingVatAmount");

            migrationBuilder.RenameColumn(
                name: "with_holding_tax_amount",
                table: "DebitMemos",
                newName: "WithHoldingTaxAmount");

            migrationBuilder.RenameColumn(
                name: "voided_date",
                table: "DebitMemos",
                newName: "VoidedDate");

            migrationBuilder.RenameColumn(
                name: "voided_by",
                table: "DebitMemos",
                newName: "VoidedBy");

            migrationBuilder.RenameColumn(
                name: "vatable_sales",
                table: "DebitMemos",
                newName: "VatableSales");

            migrationBuilder.RenameColumn(
                name: "vat_amount",
                table: "DebitMemos",
                newName: "VatAmount");

            migrationBuilder.RenameColumn(
                name: "unearned_amount",
                table: "DebitMemos",
                newName: "UnearnedAmount");

            migrationBuilder.RenameColumn(
                name: "transaction_date",
                table: "DebitMemos",
                newName: "TransactionDate");

            migrationBuilder.RenameColumn(
                name: "total_sales",
                table: "DebitMemos",
                newName: "TotalSales");

            migrationBuilder.RenameColumn(
                name: "services_id",
                table: "DebitMemos",
                newName: "ServicesId");

            migrationBuilder.RenameColumn(
                name: "service_invoice_id",
                table: "DebitMemos",
                newName: "ServiceInvoiceId");

            migrationBuilder.RenameColumn(
                name: "series_number",
                table: "DebitMemos",
                newName: "SeriesNumber");

            migrationBuilder.RenameColumn(
                name: "sales_invoice_id",
                table: "DebitMemos",
                newName: "SalesInvoiceId");

            migrationBuilder.RenameColumn(
                name: "posted_date",
                table: "DebitMemos",
                newName: "PostedDate");

            migrationBuilder.RenameColumn(
                name: "posted_by",
                table: "DebitMemos",
                newName: "PostedBy");

            migrationBuilder.RenameColumn(
                name: "is_voided",
                table: "DebitMemos",
                newName: "IsVoided");

            migrationBuilder.RenameColumn(
                name: "is_printed",
                table: "DebitMemos",
                newName: "IsPrinted");

            migrationBuilder.RenameColumn(
                name: "is_posted",
                table: "DebitMemos",
                newName: "IsPosted");

            migrationBuilder.RenameColumn(
                name: "is_canceled",
                table: "DebitMemos",
                newName: "IsCanceled");

            migrationBuilder.RenameColumn(
                name: "dm_no",
                table: "DebitMemos",
                newName: "DMNo");

            migrationBuilder.RenameColumn(
                name: "debit_amount",
                table: "DebitMemos",
                newName: "DebitAmount");

            migrationBuilder.RenameColumn(
                name: "current_and_previous_amount",
                table: "DebitMemos",
                newName: "CurrentAndPreviousAmount");

            migrationBuilder.RenameColumn(
                name: "created_date",
                table: "DebitMemos",
                newName: "CreatedDate");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "DebitMemos",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "canceled_date",
                table: "DebitMemos",
                newName: "CanceledDate");

            migrationBuilder.RenameColumn(
                name: "canceled_by",
                table: "DebitMemos",
                newName: "CanceledBy");

            migrationBuilder.RenameColumn(
                name: "adjusted_price",
                table: "DebitMemos",
                newName: "AdjustedPrice");

            migrationBuilder.RenameIndex(
                name: "ix_debit_memos_service_invoice_id",
                table: "DebitMemos",
                newName: "IX_DebitMemos_ServiceInvoiceId");

            migrationBuilder.RenameIndex(
                name: "ix_debit_memos_sales_invoice_id",
                table: "DebitMemos",
                newName: "IX_DebitMemos_SalesInvoiceId");

            migrationBuilder.RenameColumn(
                name: "source",
                table: "CreditMemos",
                newName: "Source");

            migrationBuilder.RenameColumn(
                name: "remarks",
                table: "CreditMemos",
                newName: "Remarks");

            migrationBuilder.RenameColumn(
                name: "quantity",
                table: "CreditMemos",
                newName: "Quantity");

            migrationBuilder.RenameColumn(
                name: "period",
                table: "CreditMemos",
                newName: "Period");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "CreditMemos",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "amount",
                table: "CreditMemos",
                newName: "Amount");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "CreditMemos",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "with_holding_vat_amount",
                table: "CreditMemos",
                newName: "WithHoldingVatAmount");

            migrationBuilder.RenameColumn(
                name: "with_holding_tax_amount",
                table: "CreditMemos",
                newName: "WithHoldingTaxAmount");

            migrationBuilder.RenameColumn(
                name: "voided_date",
                table: "CreditMemos",
                newName: "VoidedDate");

            migrationBuilder.RenameColumn(
                name: "voided_by",
                table: "CreditMemos",
                newName: "VoidedBy");

            migrationBuilder.RenameColumn(
                name: "vatable_sales",
                table: "CreditMemos",
                newName: "VatableSales");

            migrationBuilder.RenameColumn(
                name: "vat_amount",
                table: "CreditMemos",
                newName: "VatAmount");

            migrationBuilder.RenameColumn(
                name: "unearned_amount",
                table: "CreditMemos",
                newName: "UnearnedAmount");

            migrationBuilder.RenameColumn(
                name: "transaction_date",
                table: "CreditMemos",
                newName: "TransactionDate");

            migrationBuilder.RenameColumn(
                name: "total_sales",
                table: "CreditMemos",
                newName: "TotalSales");

            migrationBuilder.RenameColumn(
                name: "services_id",
                table: "CreditMemos",
                newName: "ServicesId");

            migrationBuilder.RenameColumn(
                name: "service_invoice_id",
                table: "CreditMemos",
                newName: "ServiceInvoiceId");

            migrationBuilder.RenameColumn(
                name: "series_number",
                table: "CreditMemos",
                newName: "SeriesNumber");

            migrationBuilder.RenameColumn(
                name: "sales_invoice_id",
                table: "CreditMemos",
                newName: "SalesInvoiceId");

            migrationBuilder.RenameColumn(
                name: "posted_date",
                table: "CreditMemos",
                newName: "PostedDate");

            migrationBuilder.RenameColumn(
                name: "posted_by",
                table: "CreditMemos",
                newName: "PostedBy");

            migrationBuilder.RenameColumn(
                name: "is_voided",
                table: "CreditMemos",
                newName: "IsVoided");

            migrationBuilder.RenameColumn(
                name: "is_printed",
                table: "CreditMemos",
                newName: "IsPrinted");

            migrationBuilder.RenameColumn(
                name: "is_posted",
                table: "CreditMemos",
                newName: "IsPosted");

            migrationBuilder.RenameColumn(
                name: "is_canceled",
                table: "CreditMemos",
                newName: "IsCanceled");

            migrationBuilder.RenameColumn(
                name: "current_and_previous_amount",
                table: "CreditMemos",
                newName: "CurrentAndPreviousAmount");

            migrationBuilder.RenameColumn(
                name: "credit_amount",
                table: "CreditMemos",
                newName: "CreditAmount");

            migrationBuilder.RenameColumn(
                name: "created_date",
                table: "CreditMemos",
                newName: "CreatedDate");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "CreditMemos",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "cm_no",
                table: "CreditMemos",
                newName: "CMNo");

            migrationBuilder.RenameColumn(
                name: "canceled_date",
                table: "CreditMemos",
                newName: "CanceledDate");

            migrationBuilder.RenameColumn(
                name: "canceled_by",
                table: "CreditMemos",
                newName: "CanceledBy");

            migrationBuilder.RenameColumn(
                name: "adjusted_price",
                table: "CreditMemos",
                newName: "AdjustedPrice");

            migrationBuilder.RenameIndex(
                name: "ix_credit_memos_service_invoice_id",
                table: "CreditMemos",
                newName: "IX_CreditMemos_ServiceInvoiceId");

            migrationBuilder.RenameIndex(
                name: "ix_credit_memos_sales_invoice_id",
                table: "CreditMemos",
                newName: "IX_CreditMemos_SalesInvoiceId");

            migrationBuilder.RenameColumn(
                name: "wvat",
                table: "CollectionReceipts",
                newName: "WVAT");

            migrationBuilder.RenameColumn(
                name: "total",
                table: "CollectionReceipts",
                newName: "Total");

            migrationBuilder.RenameColumn(
                name: "remarks",
                table: "CollectionReceipts",
                newName: "Remarks");

            migrationBuilder.RenameColumn(
                name: "ewt",
                table: "CollectionReceipts",
                newName: "EWT");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "CollectionReceipts",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "voided_date",
                table: "CollectionReceipts",
                newName: "VoidedDate");

            migrationBuilder.RenameColumn(
                name: "voided_by",
                table: "CollectionReceipts",
                newName: "VoidedBy");

            migrationBuilder.RenameColumn(
                name: "transaction_date",
                table: "CollectionReceipts",
                newName: "TransactionDate");

            migrationBuilder.RenameColumn(
                name: "sv_no",
                table: "CollectionReceipts",
                newName: "SVNo");

            migrationBuilder.RenameColumn(
                name: "si_no",
                table: "CollectionReceipts",
                newName: "SINo");

            migrationBuilder.RenameColumn(
                name: "service_invoice_id",
                table: "CollectionReceipts",
                newName: "ServiceInvoiceId");

            migrationBuilder.RenameColumn(
                name: "series_number",
                table: "CollectionReceipts",
                newName: "SeriesNumber");

            migrationBuilder.RenameColumn(
                name: "sales_invoice_id",
                table: "CollectionReceipts",
                newName: "SalesInvoiceId");

            migrationBuilder.RenameColumn(
                name: "reference_no",
                table: "CollectionReceipts",
                newName: "ReferenceNo");

            migrationBuilder.RenameColumn(
                name: "posted_date",
                table: "CollectionReceipts",
                newName: "PostedDate");

            migrationBuilder.RenameColumn(
                name: "posted_by",
                table: "CollectionReceipts",
                newName: "PostedBy");

            migrationBuilder.RenameColumn(
                name: "manager_check_no",
                table: "CollectionReceipts",
                newName: "ManagerCheckNo");

            migrationBuilder.RenameColumn(
                name: "manager_check_date",
                table: "CollectionReceipts",
                newName: "ManagerCheckDate");

            migrationBuilder.RenameColumn(
                name: "manager_check_branch",
                table: "CollectionReceipts",
                newName: "ManagerCheckBranch");

            migrationBuilder.RenameColumn(
                name: "manager_check_bank",
                table: "CollectionReceipts",
                newName: "ManagerCheckBank");

            migrationBuilder.RenameColumn(
                name: "manager_check_amount",
                table: "CollectionReceipts",
                newName: "ManagerCheckAmount");

            migrationBuilder.RenameColumn(
                name: "is_voided",
                table: "CollectionReceipts",
                newName: "IsVoided");

            migrationBuilder.RenameColumn(
                name: "is_printed",
                table: "CollectionReceipts",
                newName: "IsPrinted");

            migrationBuilder.RenameColumn(
                name: "is_posted",
                table: "CollectionReceipts",
                newName: "IsPosted");

            migrationBuilder.RenameColumn(
                name: "is_certificate_upload",
                table: "CollectionReceipts",
                newName: "IsCertificateUpload");

            migrationBuilder.RenameColumn(
                name: "is_canceled",
                table: "CollectionReceipts",
                newName: "IsCanceled");

            migrationBuilder.RenameColumn(
                name: "f2307file_path",
                table: "CollectionReceipts",
                newName: "F2307FilePath");

            migrationBuilder.RenameColumn(
                name: "f2306file_path",
                table: "CollectionReceipts",
                newName: "F2306FilePath");

            migrationBuilder.RenameColumn(
                name: "customer_id",
                table: "CollectionReceipts",
                newName: "CustomerId");

            migrationBuilder.RenameColumn(
                name: "created_date",
                table: "CollectionReceipts",
                newName: "CreatedDate");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "CollectionReceipts",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "cr_no",
                table: "CollectionReceipts",
                newName: "CRNo");

            migrationBuilder.RenameColumn(
                name: "check_no",
                table: "CollectionReceipts",
                newName: "CheckNo");

            migrationBuilder.RenameColumn(
                name: "check_date",
                table: "CollectionReceipts",
                newName: "CheckDate");

            migrationBuilder.RenameColumn(
                name: "check_branch",
                table: "CollectionReceipts",
                newName: "CheckBranch");

            migrationBuilder.RenameColumn(
                name: "check_bank",
                table: "CollectionReceipts",
                newName: "CheckBank");

            migrationBuilder.RenameColumn(
                name: "check_amount",
                table: "CollectionReceipts",
                newName: "CheckAmount");

            migrationBuilder.RenameColumn(
                name: "cash_amount",
                table: "CollectionReceipts",
                newName: "CashAmount");

            migrationBuilder.RenameColumn(
                name: "canceled_date",
                table: "CollectionReceipts",
                newName: "CanceledDate");

            migrationBuilder.RenameColumn(
                name: "canceled_by",
                table: "CollectionReceipts",
                newName: "CanceledBy");

            migrationBuilder.RenameIndex(
                name: "ix_collection_receipts_service_invoice_id",
                table: "CollectionReceipts",
                newName: "IX_CollectionReceipts_ServiceInvoiceId");

            migrationBuilder.RenameIndex(
                name: "ix_collection_receipts_sales_invoice_id",
                table: "CollectionReceipts",
                newName: "IX_CollectionReceipts_SalesInvoiceId");

            migrationBuilder.RenameColumn(
                name: "total",
                table: "CheckVoucherHeaders",
                newName: "Total");

            migrationBuilder.RenameColumn(
                name: "reference",
                table: "CheckVoucherHeaders",
                newName: "Reference");

            migrationBuilder.RenameColumn(
                name: "payee",
                table: "CheckVoucherHeaders",
                newName: "Payee");

            migrationBuilder.RenameColumn(
                name: "particulars",
                table: "CheckVoucherHeaders",
                newName: "Particulars");

            migrationBuilder.RenameColumn(
                name: "date",
                table: "CheckVoucherHeaders",
                newName: "Date");

            migrationBuilder.RenameColumn(
                name: "category",
                table: "CheckVoucherHeaders",
                newName: "Category");

            migrationBuilder.RenameColumn(
                name: "amount",
                table: "CheckVoucherHeaders",
                newName: "Amount");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "CheckVoucherHeaders",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "voided_date",
                table: "CheckVoucherHeaders",
                newName: "VoidedDate");

            migrationBuilder.RenameColumn(
                name: "voided_by",
                table: "CheckVoucherHeaders",
                newName: "VoidedBy");

            migrationBuilder.RenameColumn(
                name: "supplier_id",
                table: "CheckVoucherHeaders",
                newName: "SupplierId");

            migrationBuilder.RenameColumn(
                name: "start_date",
                table: "CheckVoucherHeaders",
                newName: "StartDate");

            migrationBuilder.RenameColumn(
                name: "si_no",
                table: "CheckVoucherHeaders",
                newName: "SINo");

            migrationBuilder.RenameColumn(
                name: "series_number",
                table: "CheckVoucherHeaders",
                newName: "SeriesNumber");

            migrationBuilder.RenameColumn(
                name: "rr_no",
                table: "CheckVoucherHeaders",
                newName: "RRNo");

            migrationBuilder.RenameColumn(
                name: "posted_date",
                table: "CheckVoucherHeaders",
                newName: "PostedDate");

            migrationBuilder.RenameColumn(
                name: "posted_by",
                table: "CheckVoucherHeaders",
                newName: "PostedBy");

            migrationBuilder.RenameColumn(
                name: "po_no",
                table: "CheckVoucherHeaders",
                newName: "PONo");

            migrationBuilder.RenameColumn(
                name: "number_of_months_created",
                table: "CheckVoucherHeaders",
                newName: "NumberOfMonthsCreated");

            migrationBuilder.RenameColumn(
                name: "number_of_months",
                table: "CheckVoucherHeaders",
                newName: "NumberOfMonths");

            migrationBuilder.RenameColumn(
                name: "last_created_date",
                table: "CheckVoucherHeaders",
                newName: "LastCreatedDate");

            migrationBuilder.RenameColumn(
                name: "is_voided",
                table: "CheckVoucherHeaders",
                newName: "IsVoided");

            migrationBuilder.RenameColumn(
                name: "is_printed",
                table: "CheckVoucherHeaders",
                newName: "IsPrinted");

            migrationBuilder.RenameColumn(
                name: "is_posted",
                table: "CheckVoucherHeaders",
                newName: "IsPosted");

            migrationBuilder.RenameColumn(
                name: "is_paid",
                table: "CheckVoucherHeaders",
                newName: "IsPaid");

            migrationBuilder.RenameColumn(
                name: "is_complete",
                table: "CheckVoucherHeaders",
                newName: "IsComplete");

            migrationBuilder.RenameColumn(
                name: "is_canceled",
                table: "CheckVoucherHeaders",
                newName: "IsCanceled");

            migrationBuilder.RenameColumn(
                name: "end_date",
                table: "CheckVoucherHeaders",
                newName: "EndDate");

            migrationBuilder.RenameColumn(
                name: "cv_type",
                table: "CheckVoucherHeaders",
                newName: "CvType");

            migrationBuilder.RenameColumn(
                name: "cv_no",
                table: "CheckVoucherHeaders",
                newName: "CVNo");

            migrationBuilder.RenameColumn(
                name: "created_date",
                table: "CheckVoucherHeaders",
                newName: "CreatedDate");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "CheckVoucherHeaders",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "check_no",
                table: "CheckVoucherHeaders",
                newName: "CheckNo");

            migrationBuilder.RenameColumn(
                name: "check_date",
                table: "CheckVoucherHeaders",
                newName: "CheckDate");

            migrationBuilder.RenameColumn(
                name: "check_amount",
                table: "CheckVoucherHeaders",
                newName: "CheckAmount");

            migrationBuilder.RenameColumn(
                name: "canceled_date",
                table: "CheckVoucherHeaders",
                newName: "CanceledDate");

            migrationBuilder.RenameColumn(
                name: "canceled_by",
                table: "CheckVoucherHeaders",
                newName: "CanceledBy");

            migrationBuilder.RenameColumn(
                name: "bank_id",
                table: "CheckVoucherHeaders",
                newName: "BankId");

            migrationBuilder.RenameColumn(
                name: "amount_per_month",
                table: "CheckVoucherHeaders",
                newName: "AmountPerMonth");

            migrationBuilder.RenameColumn(
                name: "amount_paid",
                table: "CheckVoucherHeaders",
                newName: "AmountPaid");

            migrationBuilder.RenameColumn(
                name: "accrued_type",
                table: "CheckVoucherHeaders",
                newName: "AccruedType");

            migrationBuilder.RenameIndex(
                name: "ix_check_voucher_headers_supplier_id",
                table: "CheckVoucherHeaders",
                newName: "IX_CheckVoucherHeaders_SupplierId");

            migrationBuilder.RenameIndex(
                name: "ix_check_voucher_headers_bank_id",
                table: "CheckVoucherHeaders",
                newName: "IX_CheckVoucherHeaders_BankId");

            migrationBuilder.RenameColumn(
                name: "debit",
                table: "CheckVoucherDetails",
                newName: "Debit");

            migrationBuilder.RenameColumn(
                name: "credit",
                table: "CheckVoucherDetails",
                newName: "Credit");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "CheckVoucherDetails",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "transaction_no",
                table: "CheckVoucherDetails",
                newName: "TransactionNo");

            migrationBuilder.RenameColumn(
                name: "account_no",
                table: "CheckVoucherDetails",
                newName: "AccountNo");

            migrationBuilder.RenameColumn(
                name: "account_name",
                table: "CheckVoucherDetails",
                newName: "AccountName");

            migrationBuilder.RenameColumn(
                name: "type",
                table: "ChartOfAccounts",
                newName: "Type");

            migrationBuilder.RenameColumn(
                name: "parent",
                table: "ChartOfAccounts",
                newName: "Parent");

            migrationBuilder.RenameColumn(
                name: "number",
                table: "ChartOfAccounts",
                newName: "Number");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "ChartOfAccounts",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "level",
                table: "ChartOfAccounts",
                newName: "Level");

            migrationBuilder.RenameColumn(
                name: "category",
                table: "ChartOfAccounts",
                newName: "Category");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "ChartOfAccounts",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "is_main",
                table: "ChartOfAccounts",
                newName: "IsMain");

            migrationBuilder.RenameColumn(
                name: "created_date",
                table: "ChartOfAccounts",
                newName: "CreatedDate");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "ChartOfAccounts",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "particulars",
                table: "CashReceiptBooks",
                newName: "Particulars");

            migrationBuilder.RenameColumn(
                name: "debit",
                table: "CashReceiptBooks",
                newName: "Debit");

            migrationBuilder.RenameColumn(
                name: "date",
                table: "CashReceiptBooks",
                newName: "Date");

            migrationBuilder.RenameColumn(
                name: "credit",
                table: "CashReceiptBooks",
                newName: "Credit");

            migrationBuilder.RenameColumn(
                name: "coa",
                table: "CashReceiptBooks",
                newName: "COA");

            migrationBuilder.RenameColumn(
                name: "bank",
                table: "CashReceiptBooks",
                newName: "Bank");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "CashReceiptBooks",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "ref_no",
                table: "CashReceiptBooks",
                newName: "RefNo");

            migrationBuilder.RenameColumn(
                name: "customer_name",
                table: "CashReceiptBooks",
                newName: "CustomerName");

            migrationBuilder.RenameColumn(
                name: "created_date",
                table: "CashReceiptBooks",
                newName: "CreatedDate");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "CashReceiptBooks",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "check_no",
                table: "CashReceiptBooks",
                newName: "CheckNo");

            migrationBuilder.RenameColumn(
                name: "branch",
                table: "BankAccounts",
                newName: "Branch");

            migrationBuilder.RenameColumn(
                name: "bank",
                table: "BankAccounts",
                newName: "Bank");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "BankAccounts",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "series_number",
                table: "BankAccounts",
                newName: "SeriesNumber");

            migrationBuilder.RenameColumn(
                name: "created_date",
                table: "BankAccounts",
                newName: "CreatedDate");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "BankAccounts",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "bank_code",
                table: "BankAccounts",
                newName: "BankCode");

            migrationBuilder.RenameColumn(
                name: "account_no_coa",
                table: "BankAccounts",
                newName: "AccountNoCOA");

            migrationBuilder.RenameColumn(
                name: "account_no",
                table: "BankAccounts",
                newName: "AccountNo");

            migrationBuilder.RenameColumn(
                name: "account_name",
                table: "BankAccounts",
                newName: "AccountName");

            migrationBuilder.RenameColumn(
                name: "username",
                table: "AuditTrails",
                newName: "Username");

            migrationBuilder.RenameColumn(
                name: "date",
                table: "AuditTrails",
                newName: "Date");

            migrationBuilder.RenameColumn(
                name: "activity",
                table: "AuditTrails",
                newName: "Activity");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "AuditTrails",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "machine_name",
                table: "AuditTrails",
                newName: "MachineName");

            migrationBuilder.RenameColumn(
                name: "document_type",
                table: "AuditTrails",
                newName: "DocumentType");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Suppliers",
                table: "Suppliers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Services",
                table: "Services",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Products",
                table: "Products",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Offsettings",
                table: "Offsettings",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Inventories",
                table: "Inventories",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Customers",
                table: "Customers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserTokens",
                table: "AspNetUserTokens",
                columns: new[] { "UserId", "LoginProvider", "Name" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUsers",
                table: "AspNetUsers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserRoles",
                table: "AspNetUserRoles",
                columns: new[] { "UserId", "RoleId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserLogins",
                table: "AspNetUserLogins",
                columns: new[] { "LoginProvider", "ProviderKey" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserClaims",
                table: "AspNetUserClaims",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetRoles",
                table: "AspNetRoles",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetRoleClaims",
                table: "AspNetRoleClaims",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ServiceInvoices",
                table: "ServiceInvoices",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SalesInvoices",
                table: "SalesInvoices",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SalesBooks",
                table: "SalesBooks",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ReceivingReports",
                table: "ReceivingReports",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PurchaseOrders",
                table: "PurchaseOrders",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PurchaseJournalBooks",
                table: "PurchaseJournalBooks",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_JournalVoucherHeaders",
                table: "JournalVoucherHeaders",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_JournalVoucherDetails",
                table: "JournalVoucherDetails",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_JournalBooks",
                table: "JournalBooks",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GeneralLedgerBooks",
                table: "GeneralLedgerBooks",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DisbursementBooks",
                table: "DisbursementBooks",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DebitMemos",
                table: "DebitMemos",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CreditMemos",
                table: "CreditMemos",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CollectionReceipts",
                table: "CollectionReceipts",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CheckVoucherHeaders",
                table: "CheckVoucherHeaders",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CheckVoucherDetails",
                table: "CheckVoucherDetails",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChartOfAccounts",
                table: "ChartOfAccounts",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CashReceiptBooks",
                table: "CashReceiptBooks",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BankAccounts",
                table: "BankAccounts",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AuditTrails",
                table: "AuditTrails",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                table: "AspNetUserClaims",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                table: "AspNetUserLogins",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                table: "AspNetUserRoles",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                table: "AspNetUserTokens",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CheckVoucherHeaders_BankAccounts_BankId",
                table: "CheckVoucherHeaders",
                column: "BankId",
                principalTable: "BankAccounts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CheckVoucherHeaders_Suppliers_SupplierId",
                table: "CheckVoucherHeaders",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CollectionReceipts_SalesInvoices_SalesInvoiceId",
                table: "CollectionReceipts",
                column: "SalesInvoiceId",
                principalTable: "SalesInvoices",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CollectionReceipts_ServiceInvoices_ServiceInvoiceId",
                table: "CollectionReceipts",
                column: "ServiceInvoiceId",
                principalTable: "ServiceInvoices",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CreditMemos_SalesInvoices_SalesInvoiceId",
                table: "CreditMemos",
                column: "SalesInvoiceId",
                principalTable: "SalesInvoices",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CreditMemos_ServiceInvoices_ServiceInvoiceId",
                table: "CreditMemos",
                column: "ServiceInvoiceId",
                principalTable: "ServiceInvoices",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DebitMemos_SalesInvoices_SalesInvoiceId",
                table: "DebitMemos",
                column: "SalesInvoiceId",
                principalTable: "SalesInvoices",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DebitMemos_ServiceInvoices_ServiceInvoiceId",
                table: "DebitMemos",
                column: "ServiceInvoiceId",
                principalTable: "ServiceInvoices",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Inventories_Products_ProductId",
                table: "Inventories",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_JournalVoucherHeaders_CheckVoucherHeaders_CVId",
                table: "JournalVoucherHeaders",
                column: "CVId",
                principalTable: "CheckVoucherHeaders",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_Products_ProductId",
                table: "PurchaseOrders",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_Suppliers_SupplierId",
                table: "PurchaseOrders",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ReceivingReports_PurchaseOrders_POId",
                table: "ReceivingReports",
                column: "POId",
                principalTable: "PurchaseOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesInvoices_Customers_CustomerId",
                table: "SalesInvoices",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesInvoices_Products_ProductId",
                table: "SalesInvoices",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceInvoices_Customers_CustomerId",
                table: "ServiceInvoices",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceInvoices_Services_ServicesId",
                table: "ServiceInvoices",
                column: "ServicesId",
                principalTable: "Services",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
