using Accounting_System.Data;
using Accounting_System.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Accounting_System.Repository
{
    public class ReportRepo
    {
        private readonly ApplicationDbContext _dbContext;

        public ReportRepo(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public List<SalesBook> GetSalesBooks(string dateFrom, string dateTo, string? selectedDocument)
        {
            var fromDate = DateTime.Parse(dateFrom);
            var toDate = DateTime.Parse(dateTo);

            if (fromDate > toDate)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            Func<SalesBook, object> orderBy = null;
            Func<SalesBook, bool> query = null;

            switch (selectedDocument)
            {
                case "UnpostedRR":
                case "POLiquidation":
                    query = s => DateTime.Parse(s.TransactionDate) >= fromDate && DateTime.Parse(s.TransactionDate) <= toDate && s.SerialNo.Contains(selectedDocument);
                    break;
                case "DueDate":
                    orderBy = s => s.DueDate;
                    query = s => s.DueDate >= fromDate && s.DueDate <= toDate;
                    break;
                default:
                    orderBy = s => DateTime.Parse(s.TransactionDate);
                    query = s => DateTime.Parse(s.TransactionDate) >= fromDate && DateTime.Parse(s.TransactionDate) <= toDate;
                    break;
            }

            // Add a null check for orderBy
            var salesBooks = _dbContext
                .SalesBooks
                .AsEnumerable()
                .Where(query)
                .OrderBy(orderBy ?? (Func<SalesBook, object>)(s => DateTime.Parse(s.TransactionDate)))
                .ToList();

            return salesBooks;

        }

        public List<CashReceiptBook> GetCashReceiptBooks(string dateFrom, string dateTo)
        {
            var fromDate = DateTime.Parse(dateFrom);
            var toDate = DateTime.Parse(dateTo);

            if (fromDate > toDate)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            var cashReceiptBooks = _dbContext
             .CashReceiptBooks
             .AsEnumerable()
             .Where(cr => DateTime.Parse(cr.Date) >= fromDate && DateTime.Parse(cr.Date) <= toDate)
             .OrderBy(s => s.Id)
             .ToList();

            return cashReceiptBooks;
        }

        public List<PurchaseJournalBook> GetPurchaseBooks(string dateFrom, string dateTo, string? selectedFiltering)
        {
            var fromDate = DateTime.Parse(dateFrom);
            var toDate = DateTime.Parse(dateTo);
            List<PurchaseJournalBook> purchaseBook = new List<PurchaseJournalBook>();

            if (fromDate > toDate)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            Func<PurchaseJournalBook, object> orderBy;

            switch (selectedFiltering)
            {
                case "RRDate":
                    orderBy = p => DateTime.Parse(p.Date);
                    break;
                case "DueDate":
                    orderBy = p => DateTime.Parse(p.DueDate);
                    break;
                case "POLiquidation":
                case "UnpostedRR":
                    orderBy = p => p.Id;
                    break;
                default:
                    orderBy = p => DateTime.Parse(p.Date);
                    break;
            }

            purchaseBook = _dbContext
                .PurchaseJournalBooks
                .AsEnumerable()
                .Where(p => DateTime.Parse(selectedFiltering == "DueDate" || selectedFiltering == "POLiquidation" ? p.DueDate : p.Date) >= fromDate &&
                            DateTime.Parse(selectedFiltering == "DueDate" || selectedFiltering == "POLiquidation" ? p.DueDate : p.Date) <= toDate)
                .OrderBy(orderBy)
                .ToList();

            return purchaseBook;
        }

        public List<ReceivingReport> GetReceivingReport(string dateFrom, string dateTo, string? selectedFiltering)
        {
            var fromDate = DateTime.Parse(dateFrom);
            var toDate = DateTime.Parse(dateTo);

            if (fromDate > toDate)
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
                 .Where(rr => rr.Date >= fromDate && rr.Date <= toDate && !rr.IsPosted)
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
                 .Where(rr => rr.DueDate >= fromDate && rr.DueDate <= toDate && rr.IsPosted)
                 .OrderBy(rr => rr.Id)
                 .ToList();
            }

            return receivingReport;
        }


        public List<InventoryBook> GetInventoryBooks(string dateFrom, string dateTo)
        {
            var fromDate = DateTime.Parse(dateFrom);
            var toDate = DateTime.Parse(dateTo);

            if (fromDate > toDate)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            var inventoryBooks = _dbContext
             .InventoryBooks
             .AsEnumerable()
             .Where(i => DateTime.Parse(i.Date) >= fromDate && DateTime.Parse(i.Date) <= toDate)
             .OrderBy(i => i.Id)
             .ToList();

            return inventoryBooks;
        }

        public List<GeneralLedgerBook> GetGeneralLedgerBooks(string dateFrom, string dateTo)
        {
            var fromDate = DateTime.Parse(dateFrom);
            var toDate = DateTime.Parse(dateTo);

            if (fromDate > toDate)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            Func<GeneralLedgerBook, object> orderBy;

            if (dateFrom != null && dateTo != null)
            {
                orderBy = i => DateTime.Parse(i.Date);
            }
            else
            {
                orderBy = i => i.Date; // Default ordering function
            }

            var generalLedgerBooks = _dbContext
                .GeneralLedgerBooks
                .AsEnumerable()
                .Where(i => DateTime.Parse(i.Date) >= fromDate && DateTime.Parse(i.Date) <= toDate)
                .OrderBy(orderBy)
                .ToList();

            return generalLedgerBooks;
        }

        public List<DisbursementBook> GetDisbursementBooks(string dateFrom, string dateTo)
        {
            var fromDate = DateTime.Parse(dateFrom);
            var toDate = DateTime.Parse(dateTo);

            if (fromDate > toDate)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            var disbursementBooks = _dbContext
             .DisbursementBooks
             .AsEnumerable()
             .Where(d => DateTime.Parse(d.Date) >= fromDate && DateTime.Parse(d.Date) <= toDate)
             .OrderBy(d => d.Id)
             .ToList();

            return disbursementBooks;
        }

        public List<JournalBook> GetJournalBooks(string dateFrom, string dateTo)
        {
            var fromDate = DateTime.Parse(dateFrom);
            var toDate = DateTime.Parse(dateTo);

            if (fromDate > toDate)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            var disbursementBooks = _dbContext
             .JournalBooks
             .AsEnumerable()
             .Where(d => DateTime.Parse(d.Date) >= fromDate && DateTime.Parse(d.Date) <= toDate)
             .OrderBy(d => d.Id)
             .ToList();

            return disbursementBooks;
        }

        public List<AuditTrail> GetAuditTrails(string dateFrom, string dateTo)
        {
            var fromDate = DateTime.Parse(dateFrom);
            var toDate = DateTime.Parse(dateTo);

            if (fromDate > toDate)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            var auditTrail = _dbContext
                .AuditTrails
                .AsEnumerable()
                .Where(a => DateOnly.FromDateTime(a.Date) >= DateOnly.FromDateTime(fromDate) && DateOnly.FromDateTime(a.Date) <= DateOnly.FromDateTime(toDate))
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