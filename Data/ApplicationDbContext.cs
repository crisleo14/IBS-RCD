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

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSnakeCaseNamingConvention();
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

        public DbSet<ImportExportLog> ImportExportLogs { get; set; }

        public DbSet<MultipleCheckVoucherPayment> MultipleCheckVoucherPayments { get; set; }

        public DbSet<CVTradePayment> CVTradePayments { get; set; }

        #region--Fluent API Implementation

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            #region -- Implement ICollection

            #region --CheckVoucher

            builder.Entity<CheckVoucherDetail>(cvd =>
            {
                cvd.HasOne(cvh => cvh.CheckVoucherHeader)
                .WithMany(cvd => cvd.Details)
                .HasForeignKey(cv => cv.CheckVoucherHeaderId)
                .OnDelete(DeleteBehavior.Restrict);
            });

            #endregion --CheckVoucher

            #region --JournalVoucher

            builder.Entity<JournalVoucherDetail>(cvd =>
            {
                cvd.HasOne(cvh => cvh.JournalVoucherHeader)
                .WithMany(cvd => cvd.Details)
                .HasForeignKey(cv => cv.JournalVoucherHeaderId)
                .OnDelete(DeleteBehavior.Restrict);
            });

            #endregion --JournalVoucher

            #endregion --Filpride

            #region--Filpride

            #region -- Accounts Receivable --

            #region -- Sales Invoice --

            builder.Entity<SalesInvoice>(si =>
            {
                si.HasOne(si => si.Product)
                .WithMany()
                .HasForeignKey(si => si.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

                si.HasOne(si => si.Customer)
                .WithMany()
                .HasForeignKey(si => si.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
            });

            #endregion -- Sales Invoice --

            #region -- Service Invoice --

            builder.Entity<ServiceInvoice>(sv =>
            {
                sv.HasOne(sv => sv.Customer)
                .WithMany()
                .HasForeignKey(sv => sv.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

                sv.HasOne(sv => sv.Service)
                .WithMany()
                .HasForeignKey(sv => sv.ServicesId)
                .OnDelete(DeleteBehavior.Restrict);
            });

            #endregion -- Service Invoice --

            #region -- Collection Receipt --

            builder.Entity<CollectionReceipt>(cr =>
            {
                cr.HasOne(cr => cr.SalesInvoice)
                .WithMany()
                .HasForeignKey(cr => cr.SalesInvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

                cr.HasOne(cr => cr.ServiceInvoice)
                .WithMany()
                .HasForeignKey(cr => cr.ServiceInvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

                cr.HasOne(cr => cr.Customer)
                .WithMany()
                .HasForeignKey(cr => cr.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
            });

            #endregion -- Collection Receipt --

            #region -- Debit Memo --

            builder.Entity<DebitMemo>(cr =>
            {
                cr.HasOne(cr => cr.SalesInvoice)
                .WithMany()
                .HasForeignKey(cr => cr.SalesInvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

                cr.HasOne(cr => cr.ServiceInvoice)
                .WithMany()
                .HasForeignKey(cr => cr.ServiceInvoiceId)
                .OnDelete(DeleteBehavior.Restrict);
            });

            #endregion -- Debit Memo --

            #region -- Credit Memo --

            builder.Entity<CreditMemo>(cr =>
            {
                cr.HasOne(cr => cr.SalesInvoice)
                .WithMany()
                .HasForeignKey(cr => cr.SalesInvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

                cr.HasOne(cr => cr.ServiceInvoice)
                .WithMany()
                .HasForeignKey(cr => cr.ServiceInvoiceId)
                .OnDelete(DeleteBehavior.Restrict);
            });

            #endregion -- Credit Memo --

            #endregion -- Accounts Receivable --

            #region -- Accounts Payable --

            #region -- Purchase Order --

            builder.Entity<PurchaseOrder>(po =>
            {
                po.HasOne(po => po.Supplier)
                .WithMany()
                .HasForeignKey(po => po.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

                po.HasOne(po => po.Product)
                .WithMany()
                .HasForeignKey(po => po.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
            });

            #endregion -- Purchase Order --

            #region -- Receving Report --

            builder.Entity<ReceivingReport>(rr =>
            {
                rr.HasOne(rr => rr.PurchaseOrder)
                .WithMany()
                .HasForeignKey(rr => rr.POId)
                .OnDelete(DeleteBehavior.Restrict);
            });

            #endregion -- Receving Report --

            #region -- Check Voucher --

            builder.Entity<CheckVoucherHeader>(cv =>
            {
                cv.HasOne(cv => cv.Supplier)
                .WithMany()
                .HasForeignKey(cv => cv.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

                cv.HasOne(cv => cv.BankAccount)
                .WithMany()
                .HasForeignKey(cv => cv.BankId)
                .OnDelete(DeleteBehavior.Restrict);
            });

            #endregion -- Check Voucher --

            #region -- Check Voucher --

            builder.Entity<JournalVoucherHeader>(jv =>
            {
                jv.HasOne(jv => jv.CheckVoucherHeader)
                .WithMany()
                .HasForeignKey(jv => jv.CVId)
                .OnDelete(DeleteBehavior.Restrict);
            });

            #endregion -- Check Voucher --

            #region -- Check Voucher Trade Payment --

            builder.Entity<CVTradePayment>(cv =>
            {
                cv.HasOne(cv => cv.CV)
                    .WithMany()
                    .HasForeignKey(cv => cv.CheckVoucherId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            #endregion -- Check Voucher Trade Payment --

            #region -- Multiple Check Voucher Payment --

            builder.Entity<MultipleCheckVoucherPayment>(mcvp =>
            {
                mcvp.HasOne(mcvp => mcvp.CheckVoucherHeaderPayment)
                    .WithMany()
                    .HasForeignKey(mcvp => mcvp.CheckVoucherHeaderPaymentId)
                    .OnDelete(DeleteBehavior.Restrict);

                mcvp.HasOne(mcvp => mcvp.CheckVoucherHeaderInvoice)
                    .WithMany()
                    .HasForeignKey(mcvp => mcvp.CheckVoucherHeaderInvoiceId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            #endregion -- Multiple Check Voucher Payment --

            #endregion -- Accounts Payable --

            #endregion

        }

        #endregion
    }
}
