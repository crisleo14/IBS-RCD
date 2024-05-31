using Accounting_System.Data;
using Accounting_System.Models;
using Accounting_System.Models.AccountsPayable;
using Accounting_System.Models.MasterFile;
using Accounting_System.Models.Reports;
using Microsoft.EntityFrameworkCore;

namespace Accounting_System.Repository
{
    public class ReportRepo
    {
        private readonly ApplicationDbContext _dbContext;

        public ReportRepo(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public List<SalesBook> GetSalesBooks(DateOnly? dateFrom, DateOnly? dateTo, string? selectedDocument)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            Func<SalesBook, object> orderBy = null;
            Func<SalesBook, bool> query = null;

            switch (selectedDocument)
            {
                case "UnpostedRR":
                case "POLiquidation":
                    query = s => s.TransactionDate >= dateFrom && s.TransactionDate <= dateTo && s.SerialNo.Contains(selectedDocument);
                    break;
                case "DueDate":
                    orderBy = s => s.DueDate;
                    query = s => s.DueDate >= dateFrom && s.DueDate <= dateTo;
                    break;
                default:
                    orderBy = s => s.TransactionDate;
                    query = s => s.TransactionDate >= dateFrom && s.TransactionDate <= dateTo;
                    break;
            }

            // Add a null check for orderBy
            var salesBooks = _dbContext
                .SalesBooks
                .AsEnumerable()
                .Where(query)
                .OrderBy(orderBy ?? (Func<SalesBook, object>)(s => s.TransactionDate))
                .ToList();

            return salesBooks;

        }

        public List<CashReceiptBook> GetCashReceiptBooks(DateOnly? dateFrom, DateOnly? dateTo)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            var cashReceiptBooks = _dbContext
             .CashReceiptBooks
             .AsEnumerable()
             .Where(cr => cr.Date >= dateFrom && cr.Date <= dateTo)
             .OrderBy(s => s.Id)
             .ToList();

            return cashReceiptBooks;
        }

        public List<PurchaseJournalBook> GetPurchaseBooks(DateOnly? dateFrom, DateOnly? dateTo, string? selectedFiltering)
        {
            List<PurchaseJournalBook> purchaseBook = new List<PurchaseJournalBook>();

            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            Func<PurchaseJournalBook, object> orderBy;

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
                    orderBy = p => p.Id;
                    break;
                default:
                    orderBy = p => p.Date;
                    break;
            }

            purchaseBook = _dbContext
                .PurchaseJournalBooks
                .AsEnumerable()
                .Where(p => (selectedFiltering == "DueDate" || selectedFiltering == "POLiquidation" ? p.DueDate : p.Date) >= dateFrom &&
                            (selectedFiltering == "DueDate" || selectedFiltering == "POLiquidation" ? p.DueDate : p.Date) <= dateTo)
                .OrderBy(orderBy)
                .ToList();

            return purchaseBook;
        }

        public List<ReceivingReport> GetReceivingReport(DateOnly? dateFrom, DateOnly? dateTo, string? selectedFiltering)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            List<ReceivingReport> receivingReport = new List<ReceivingReport>();

            if (selectedFiltering == "UnpostedRR")
            {
                receivingReport = _dbContext
                 .ReceivingReports
                 .Include(rr => rr.PurchaseOrder)
                 .ThenInclude(po => po.Supplier)
                 .Include(rr => rr.PurchaseOrder)
                 .ThenInclude(po => po.Product)
                 .AsEnumerable()
                 .Where(rr => rr.Date >= dateFrom && rr.Date <= dateTo && !rr.IsPosted)
                 .OrderBy(rr => rr.Id)
                 .ToList();
            }
            else if (selectedFiltering == "POLiquidation")
            {
                receivingReport = _dbContext
                 .ReceivingReports
                 .Include(rr => rr.PurchaseOrder)
                 .ThenInclude(po => po.Supplier)
                 .Include(rr => rr.PurchaseOrder)
                 .ThenInclude(po => po.Product)
                 .AsEnumerable()
                 .Where(rr => rr.DueDate >= dateFrom && rr.DueDate <= dateTo && rr.IsPosted)
                 .OrderBy(rr => rr.Id)
                 .ToList();
            }

            return receivingReport;
        }


        public List<Inventory> GetInventoryBooks(DateOnly? dateFrom, DateOnly? dateTo)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            var inventoryBooks = _dbContext
             .Inventories
             .Include(i => i.Product)
             .AsEnumerable()
             .Where(i => i.Date >= dateFrom && i.Date <= dateTo)
             .OrderBy(i => i.Id)
             .ToList();

            return inventoryBooks;
        }

        public List<GeneralLedgerBook> GetGeneralLedgerBooks(DateOnly? dateFrom, DateOnly? dateTo)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            Func<GeneralLedgerBook, object> orderBy;

            if (dateFrom != null && dateTo != null)
            {
                orderBy = i => i.Date;
            }
            else
            {
                orderBy = i => i.Date; // Default ordering function
            }

            var generalLedgerBooks = _dbContext
                .GeneralLedgerBooks
                .AsEnumerable()
                .Where(i => i.Date >= dateFrom && i.Date <= dateTo)
                .OrderBy(orderBy)
                .ToList();

            return generalLedgerBooks;
        }

        public List<DisbursementBook> GetDisbursementBooks(DateOnly? dateFrom, DateOnly? dateTo)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            var disbursementBooks = _dbContext
             .DisbursementBooks
             .AsEnumerable()
             .Where(d => d.Date >= dateFrom && d.Date <= dateTo)
             .OrderBy(d => d.Id)
             .ToList();

            return disbursementBooks;
        }

        public List<JournalBook> GetJournalBooks(DateOnly? dateFrom, DateOnly? dateTo)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            var disbursementBooks = _dbContext
             .JournalBooks
             .AsEnumerable()
             .Where(d => d.Date >= dateFrom && d.Date <= dateTo)
             .OrderBy(d => d.Id)
             .ToList();

            return disbursementBooks;
        }

        public List<AuditTrail> GetAuditTrails(DateOnly? dateFrom, DateOnly? dateTo)
        {
            if (dateFrom > dateTo)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            var auditTrail = _dbContext
                .AuditTrails
                .AsEnumerable()
                .Where(a => DateOnly.FromDateTime(a.Date) >= dateFrom && DateOnly.FromDateTime(a.Date) <= dateTo)
                .OrderBy(a => a.Date)
                .ToList();

            return auditTrail;
        }

        public async Task<List<Customer>> GetCustomersAsync()
        {
            return await _dbContext
                .Customers
                .ToListAsync();
        }

        public async Task<List<Product>> GetProductsAsync()
        {
            return await _dbContext
                .Products
                .ToListAsync();
        }
    }
}