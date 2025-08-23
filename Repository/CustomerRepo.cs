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

        public async Task<List<Customer>> GetCustomersAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext
                .Customers
                .ToListAsync(cancellationToken);
        }

        public async Task<Customer> FindCustomerAsync(int? id, CancellationToken cancellationToken = default)
        {
            var customer = await _dbContext
                .Customers
                .FirstOrDefaultAsync(c => c.CustomerId == id, cancellationToken);

            if (customer != null)
            {
                return customer;
            }
            else
            {
                throw new ArgumentException("Invalid id value. The id must be greater than 0.");
            }
        }

        public async Task<bool> IsCustomerExist(string name, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Customers.
                AnyAsync(c => c.CustomerName.ToUpper() == name.ToUpper(), cancellationToken);
        }

        public async Task<bool> IsTinNoExist(string tin, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Customers.
                AnyAsync(c => c.CustomerTin.ToUpper() == tin.ToUpper(), cancellationToken);
        }

        public async Task<int> GetLastNumber(CancellationToken cancellationToken = default)
        {
            var lastNumber = await _dbContext
                .Customers
                .OrderByDescending(s => s.CustomerId)
                .FirstOrDefaultAsync(cancellationToken);

            if (lastNumber != null)
            {
                return lastNumber.Number + 1;
            }
            else
            {
                return 1001;
            }
        }
    }
}