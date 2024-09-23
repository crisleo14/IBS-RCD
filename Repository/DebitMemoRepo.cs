using Accounting_System.Data;
using Accounting_System.Models;
using Accounting_System.Models.AccountsReceivable;
using Microsoft.EntityFrameworkCore;

namespace Accounting_System.Repository
{
    public class DebitMemoRepo
    {
        private readonly ApplicationDbContext _dbContext;

        public DebitMemoRepo(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<List<DebitMemo>> GetDebitMemosAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.DebitMemos
                .Include(dm => dm.SalesInvoice)
                .ThenInclude(s => s.Customer)
                .Include(dm => dm.SalesInvoice)
                .ThenInclude(s => s.Product)
                .Include(dm => dm.ServiceInvoice)
                .ThenInclude(sv => sv.Customer)
                .Include(dm => dm.ServiceInvoice)
                .ThenInclude(sv => sv.Service)
                .ToListAsync(cancellationToken);
        }

        public async Task<long> GetLastSeriesNumber(CancellationToken cancellationToken = default)
        {
            var lastInvoice = await _dbContext
                .DebitMemos
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

        public async Task<string> GenerateDMNo(CancellationToken cancellationToken = default)
        {
            var debitMemo = await _dbContext
                .DebitMemos
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (debitMemo != null)
            {
                var generatedCR = debitMemo.SeriesNumber + 1;
                return $"DM{generatedCR.ToString("D10")}";
            }
            else
            {
                return $"DM{1.ToString("D10")}";
            }
        }

        public async Task<DebitMemo> FindDM(int id, CancellationToken cancellationToken = default)
        {
            var debitMemo = await _dbContext
                .DebitMemos
                .Include(s => s.SalesInvoice)
                .ThenInclude(s => s.Customer)
                .Include(s => s.SalesInvoice)
                .ThenInclude(s => s.Product)
                .Include(soa => soa.ServiceInvoice)
                .ThenInclude(soa => soa.Customer)
                .Include(c => c.ServiceInvoice)
                .ThenInclude(soa => soa.Service)
                .FirstOrDefaultAsync(debitMemo => debitMemo.Id == id, cancellationToken);

            if (debitMemo != null)
            {
                return debitMemo;
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
    }
}