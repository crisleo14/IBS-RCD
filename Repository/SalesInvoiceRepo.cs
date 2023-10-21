using Accounting_System.Data;
using Accounting_System.Models;
using Microsoft.EntityFrameworkCore;

namespace Accounting_System.Repository
{
    public class SalesInvoiceRepo
    {
        private readonly ApplicationDbContext _dbContext;

        public SalesInvoiceRepo(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<SalesInvoice>> GetSalesInvoicesAsync()
        {
            return await _dbContext
                .SalesInvoices
                .ToListAsync();
        }

        public async Task<List<Customer>> GetCustomersAsync()
        {
            return await _dbContext
                .Customers
                .ToListAsync();
        }

        public async Task<int> GetLastSerialNo()
        {
            var lastInvoice = await _dbContext
                .SalesInvoices
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync();

            if (lastInvoice != null)
            {
                // Increment the last serial by one and return it
                return lastInvoice.SerialNo + 1;
            }
            else
            {
                // If there are no existing records, you can start with a default value like 1
                return 1;
            }
        }
    }
}