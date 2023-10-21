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
    }
}