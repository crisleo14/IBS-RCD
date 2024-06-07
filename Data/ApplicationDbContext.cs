using Accounting_System.Models;
using Accounting_System.Models.AccountsPayable;
using Accounting_System.Models.AccountsReceivable;
using Accounting_System.Models.MasterFile;
using Accounting_System.Models.Reports;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Accounting_System.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<SalesInvoice> SalesInvoices { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<ChartOfAccount> ChartOfAccounts { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<AuditTrail> AuditTrails { get; set; }
        public DbSet<Inventory> Inventories { get; set; }
        public DbSet<ServiceInvoice> ServiceInvoices { get; set; }
        public DbSet<CollectionReceipt> CollectionReceipts { get; set; }
        public DbSet<Services> Services { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
        public DbSet<DebitMemo> DebitMemos { get; set; }
        public DbSet<ReceivingReport> ReceivingReports { get; set; }
        public DbSet<CreditMemo> CreditMemos { get; set; }
        public DbSet<CheckVoucherHeader> CheckVoucherHeaders { get; set; }
        public DbSet<CheckVoucherDetail> CheckVoucherDetails { get; set; }
        public DbSet<Offsetting> Offsettings { get; set; }
        public DbSet<JournalVoucherHeader> JournalVoucherHeaders { get; set; }
        public DbSet<JournalVoucherDetail> JournalVoucherDetails { get; set; }

        // Book Context
        public DbSet<CashReceiptBook> CashReceiptBooks { get; set; }

        public DbSet<GeneralLedgerBook> GeneralLedgerBooks { get; set; }
        public DbSet<DisbursementBook> DisbursementBooks { get; set; }
        public DbSet<JournalBook> JournalBooks { get; set; }
        public DbSet<PurchaseJournalBook> PurchaseJournalBooks { get; set; }
        public DbSet<SalesBook> SalesBooks { get; set; }
        public DbSet<BankAccount> BankAccounts { get; set; }
    }
}