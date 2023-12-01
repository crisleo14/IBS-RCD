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
                .OrderByDescending(c => c.Id)
                .ToListAsync();
        }

        public async Task<Customer> FindCustomerAsync(int? id)
        {
            var customer = await _dbContext
                .Customers
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer != null)
            {
                return customer;
            }
            else
            {
                throw new ArgumentException("Invalid id value. The id must be greater than 0.");
            }
        }

        public bool CustomerExist(int id)
        {
            return _dbContext.Customers.Any(c => c.Id == id);
        }

        public async Task<int> GetLastNumber()
        {
            var lastNumber = await _dbContext
                .Customers
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync();

            if (lastNumber != null)
            {
                return lastNumber.Number + 1;
            }
            else
            {
                return 1001;
            }
        }

        public async Task<Customer?> CheckIfTinNoExist(string tin)
        {
            return await _dbContext
                .Customers
                .Where(c => c.TinNo == tin)
                .FirstOrDefaultAsync();
        }
    }
}