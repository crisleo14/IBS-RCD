using Accounting_System.Data;
using Accounting_System.Models;
using Microsoft.EntityFrameworkCore;

namespace Accounting_System.Repository
{
    public class CustomerRepo
    {
        private readonly ApplicationDbContext _dbContext;

        public CustomerRepo(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<Customer>> GetCustomersAsync()
        {
            return await _dbContext
                .Customers
                .ToListAsync();
        }
    }
}