using Accounting_System.Data;
using Accounting_System.Models;
using Accounting_System.Models.AccountsReceivable;
using Microsoft.EntityFrameworkCore;

namespace Accounting_System.Repository
{
    public class ServiceInvoiceRepo
    {
        private readonly ApplicationDbContext _dbContext;

        public ServiceInvoiceRepo(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<ServiceInvoice>> GetSvListAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext
                .ServiceInvoices
                .Include(sv => sv.Customer)
                .Include(sv => sv.Service)
                .ToListAsync(cancellationToken);
        }

        public async Task<long> GetLastSeriesNumber(CancellationToken cancellationToken = default)
        {
            var lastInvoice = await _dbContext
                .ServiceInvoices
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

        public async Task<string> GenerateSvNo(CancellationToken cancellationToken = default)
        {
            var serviceInvoice = await _dbContext
                .ServiceInvoices
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (serviceInvoice != null)
            {
                var generatedInvoice = serviceInvoice.SeriesNumber + 1;
                return $"SV{generatedInvoice.ToString("D10")}";
            }
            else
            {
                return $"SV{1.ToString("D10")}";
            }
        }

        public async Task<ServiceInvoice> FindSv(int id, CancellationToken cancellationToken = default)
        {
            var serviceInvoice = await _dbContext
                .ServiceInvoices
                .Include(s => s.Customer)
                .Include(s => s.Service)
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

            if (serviceInvoice != null)
            {
                return serviceInvoice;
            }
            else
            {
                throw new ArgumentException("Invalid id value. The id must be greater than 0.");
            }
        }

        public async Task<Services> GetServicesAsync(int? id, CancellationToken cancellationToken = default)
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

        public async Task<Customer> FindCustomerAsync(int? id, CancellationToken cancellationToken = default)
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