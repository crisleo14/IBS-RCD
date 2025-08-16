using Accounting_System.Data;
using Accounting_System.Models;
using Accounting_System.Models.AccountsPayable;
using Accounting_System.Models.MasterFile;
using Accounting_System.Models.Reports;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Accounting_System.Repository
{
    public class ReportRepo
    {
        private readonly ApplicationDbContext _dbContext;

        public ReportRepo(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<SalesBook>> GetSalesBooksAsync(DateOnly dateFrom, DateOnly dateTo, string? selectedDocument, CancellationToken cancellationToken = default)
        {
            Expression<Func<SalesBook, bool>> query;
            Expression<Func<SalesBook, object>> orderBy = null!;

            switch (selectedDocument)
            {
                case null:
                case "TransactionDate":
                    query = s => s.TransactionDate >= dateFrom && s.TransactionDate <= dateTo;
                    break;

                case "DueDate":
                    orderBy = s => s.DueDate;
                    query = s => s.DueDate >= dateFrom && s.DueDate <= dateTo;
                    break;

                default:
                    orderBy = s => s.TransactionDate;
                    query = s => s.TransactionDate >= dateFrom && s.TransactionDate <= dateTo && s.SerialNo.Contains(selectedDocument);
                    break;
            }

            // Add a null check for orderBy
            var salesBooks = await _dbContext
                .SalesBooks
                .Where(query)
                .OrderBy(orderBy ?? (s => s.TransactionDate))
                .ToListAsync(cancellationToken);

            return salesBooks;

        }

        public async Task<List<CashReceiptBook>> GetCashReceiptBooks(DateOnly? dateFrom, DateOnly? dateTo, CancellationToken cancellationToken = default)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            var cashReceiptBooks = await _dbContext
             .CashReceiptBooks
             .Where(cr => cr.Date >= dateFrom && cr.Date <= dateTo)
             .OrderBy(s => s.CashReceiptBookId)
             .ToListAsync(cancellationToken);

            return cashReceiptBooks;
        }

        public async Task<List<PurchaseJournalBook>> GetPurchaseBooks(DateOnly? dateFrom, DateOnly? dateTo, string? selectedFiltering, CancellationToken cancellationToken = default)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            Expression<Func<PurchaseJournalBook, object>> orderBy;

            switch (selectedFiltering)
            {
                case "RRDate":
                    orderBy = p => p.Date;
                    break;

                case "DueDate":
                    orderBy = p => p.DueDate;
                    break;

                case "POLiquidation":
                case "UnpostedRR":
                    orderBy = p => p.PurchaseBookId;
                    break;

                default:
                    orderBy = p => p.Date;
                    break;
            }

            var purchaseBook = await _dbContext
                .PurchaseJournalBooks
                .Where(p => (selectedFiltering == "DueDate" || selectedFiltering == "POLiquidation" ? p.DueDate : p.Date) >= dateFrom &&
                            (selectedFiltering == "DueDate" || selectedFiltering == "POLiquidation" ? p.DueDate : p.Date) <= dateTo)
                .OrderBy(orderBy)
                .ToListAsync();

            return purchaseBook;
        }

        public async Task<List<ReceivingReport>> GetReceivingReport(DateOnly? dateFrom, DateOnly? dateTo, string? selectedFiltering, CancellationToken cancellationToken = default)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            List<ReceivingReport> receivingReport = new List<ReceivingReport>();

            if (selectedFiltering == "UnpostedRR")
            {
                receivingReport = await _dbContext
                 .ReceivingReports
                 .Include(rr => rr.PurchaseOrder)
                 .ThenInclude(po => po!.Supplier)
                 .Include(rr => rr.PurchaseOrder)
                 .ThenInclude(po => po!.Product)
                 .Where(rr => rr.Date >= dateFrom && rr.Date <= dateTo && !rr.IsPosted)
                 .OrderBy(rr => rr.ReceivingReportId)
                 .ToListAsync(cancellationToken);
            }
            else if (selectedFiltering == "POLiquidation")
            {
                receivingReport = await _dbContext
                 .ReceivingReports
                 .Include(rr => rr.PurchaseOrder)
                 .ThenInclude(po => po!.Supplier)
                 .Include(rr => rr.PurchaseOrder)
                 .ThenInclude(po => po!.Product)
                 .Where(rr => rr.DueDate >= dateFrom && rr.DueDate <= dateTo && rr.IsPosted)
                 .OrderBy(rr => rr.ReceivingReportId)
                 .ToListAsync(cancellationToken);
            }

            return receivingReport;
        }

        public async Task<List<Inventory>> GetInventoryBooks(DateOnly dateFrom, DateOnly dateTo, CancellationToken cancellationToken = default)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            var inventoryBooks = await _dbContext
             .Inventories
             .Include(i => i.Product)
             .Where(i => i.Date >= dateFrom && i.Date <= dateTo)
             .OrderBy(i => i.Id)
             .ToListAsync(cancellationToken);

            return inventoryBooks;
        }

        public async Task<List<GeneralLedgerBook>> GetGeneralLedgerBooks(DateOnly? dateFrom, DateOnly? dateTo, CancellationToken cancellationToken = default)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            Expression<Func<GeneralLedgerBook, object>> orderBy;

            if (dateFrom != null && dateTo != null)
            {
                orderBy = i => i.Date;
            }
            else
            {
                orderBy = i => i.Date; // Default ordering function
            }

            var generalLedgerBooks = await _dbContext
                .GeneralLedgerBooks
                .Where(i => i.Date >= dateFrom && i.Date <= dateTo && i.IsPosted)
                .OrderBy(orderBy)
                .ToListAsync(cancellationToken);

            return generalLedgerBooks;
        }

        public async Task<List<DisbursementBook>> GetDisbursementBooks(DateOnly? dateFrom, DateOnly? dateTo, CancellationToken cancellationToken = default)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            var disbursementBooks = await _dbContext
             .DisbursementBooks
             .Where(d => d.Date >= dateFrom && d.Date <= dateTo)
             .OrderBy(d => d.DisbursementBookId)
             .ToListAsync();

            return disbursementBooks;
        }

        public async Task<List<JournalBook>> GetJournalBooks(DateOnly? dateFrom, DateOnly? dateTo, CancellationToken cancellationToken = default)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            var disbursementBooks = await _dbContext
             .JournalBooks
             .Where(d => d.Date >= dateFrom && d.Date <= dateTo)
             .OrderBy(d => d.JournalBookId)
             .ToListAsync(cancellationToken);

            return disbursementBooks;
        }

        public async Task<List<AuditTrail>> GetAuditTrails(DateOnly? dateFrom, DateOnly? dateTo, CancellationToken cancellationToken = default)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            var auditTrail = await _dbContext
                .AuditTrails
                .Where(a => DateOnly.FromDateTime(a.Date) >= dateFrom && DateOnly.FromDateTime(a.Date) <= dateTo)
                .OrderBy(a => a.Date)
                .ToListAsync(cancellationToken);

            return auditTrail;
        }

        public async Task<List<Customer>> GetCustomersAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext
                .Customers
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Product>> GetProductsAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext
                .Products
                .ToListAsync(cancellationToken);
        }
    }
}
