using Accounting_System.Data;
using Accounting_System.Models;
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

        public List<SalesBook> GetSalesBooks(string dateFrom, string dateTo, string? selectedDocument)
        {
            var fromDate = DateTime.Parse(dateFrom);
            var toDate = DateTime.Parse(dateTo);
            List<SalesBook> salesBooks = new List<SalesBook>();

            if (fromDate > toDate)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            if (selectedDocument == null)
            {
                salesBooks = _dbContext
                 .SalesBooks
                 .AsEnumerable()
                 .Where(s => DateTime.Parse(s.TransactionDate) >= fromDate && DateTime.Parse(s.TransactionDate) <= toDate)
                 .OrderBy(s => s.Id)
                 .ToList();
            }
            else
            {
                salesBooks = _dbContext
                 .SalesBooks
                 .AsEnumerable()
                 .Where(s => DateTime.Parse(s.TransactionDate) >= fromDate && DateTime.Parse(s.TransactionDate) <= toDate && s.SerialNo.Contains(selectedDocument))
                 .OrderBy(s => s.Id)
                 .ToList();   
            }

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

        public List<PurchaseJournalBook> GetPurchaseBooks(string dateFrom, string dateTo, string? selectedAging)
        {
            var fromDate = DateTime.Parse(dateFrom);
            var toDate = DateTime.Parse(dateTo);
            List<PurchaseJournalBook> purchaseBook = new List<PurchaseJournalBook>();

            if (fromDate > toDate)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            if (selectedAging != "DueDate")
            {
              purchaseBook = _dbContext
             .PurchaseJournalBooks
             .AsEnumerable()
             .Where(p => DateTime.Parse(p.Date) >= fromDate && DateTime.Parse(p.Date) <= toDate)
             .OrderBy(s => s.Id)
             .ToList();
            }
            else
            {
              purchaseBook = _dbContext
             .PurchaseJournalBooks
             .AsEnumerable()
             .Where(p => DateTime.Parse(p.DueDate) >= fromDate && DateTime.Parse(p.DueDate) <= toDate)
             .OrderBy(s => s.Id)
             .ToList();
            }

            return purchaseBook;
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

            var generalLedgerBooks = _dbContext
             .GeneralLedgerBooks
             .AsEnumerable()
             .Where(i => DateTime.Parse(i.Date) >= fromDate && DateTime.Parse(i.Date) <= toDate)
             .OrderBy(i => i.Id)
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

        public List<PurchaseJournalBook> GetMaturityAging(string dateFrom, string dateTo)
        {
            var fromDate = DateTime.Parse(dateFrom);
            var toDate = DateTime.Parse(dateTo);

            if (fromDate > toDate)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            var maturityAging = _dbContext
             .PurchaseJournalBooks
             .AsEnumerable()
             .Where(p => DateTime.Parse(p.DueDate) >= fromDate && DateTime.Parse(p.DueDate) <= toDate)
             .OrderBy(s => s.Id)
             .ToList();

            return maturityAging;
        }
    }
}