using Accounting_System.Data;
using Accounting_System.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.Expressions.Internal;

namespace Accounting_System.Repository
{
    public class StatementOfAccountRepo
    {
        private readonly ApplicationDbContext _dbContext;

        public StatementOfAccountRepo(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<StatementOfAccount>> GetSOAListAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext
                .StatementOfAccounts
                .Include(soa => soa.Customer)
                .Include(soa => soa.Service)
                .ToListAsync(cancellationToken);
        }

        public async Task<long> GetLastSeriesNumber(CancellationToken cancellationToken = default)
        {
            var lastInvoice = await _dbContext
                .StatementOfAccounts
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (lastInvoice != null)
            {
                // Increment the last serial by one and return it
                return lastInvoice.SeriesNumber + 1;
            }
            else
            {
                // If there are no existing records, you can start with a default value like 1
                return 1;
            }
        }

        public async Task<string> GenerateSOANo(CancellationToken cancellationToken = default)
        {
            var statementOfAccount = await _dbContext
                .StatementOfAccounts
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (statementOfAccount != null)
            {
                var generatedSOA = statementOfAccount.SeriesNumber + 1;
                return $"SV{generatedSOA.ToString("D10")}";
            }
            else
            {
                return $"SV{1.ToString("D10")}";
            }
        }

        public async Task<StatementOfAccount> FindSOA(int id, CancellationToken cancellationToken = default)
        {
            var statementOfAccount = await _dbContext
                .StatementOfAccounts
                .Include(s => s.Customer)
                .Include(s => s.Service)
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

            if (statementOfAccount != null)
            {
                return statementOfAccount;
            }
            else
            {
                throw new ArgumentException("Invalid id value. The id must be greater than 0.");
            }
        }

        public async Task<Services> GetServicesAsync(int id, CancellationToken  cancellationToken = default)
        {
            var services = await _dbContext
                .Services
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

            if (services != null)
            {
                return services;
            }
            else
            {
                throw new ArgumentException("Invalid id value. The id must be greater than 0.");
            }
        }

        public async Task<Customer> FindCustomerAsync(int id, CancellationToken cancellationToken = default)
        {
            var customer = await _dbContext
                .Customers
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

            if (customer != null)
            {
                return customer;
            }
            else
            {
                throw new ArgumentException("Invalid id value. The id must be greater than 0.");
            }
        }
    }
}