using Accounting_System.Data;
using Accounting_System.Models;
using Microsoft.EntityFrameworkCore;

namespace Accounting_System.Repository
{
    public class CreditMemoRepo
    {
        private readonly ApplicationDbContext _dbContext;

        public CreditMemoRepo(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<long> GetLastSeriesNumber(CancellationToken cancellationToken = default)
        {
            var lastInvoice = await _dbContext
                .CreditMemos
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

        public async Task<string> GenerateCMNo(CancellationToken cancellationToken = default)
        {
            var creditMemo = await _dbContext
                .CreditMemos
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (creditMemo != null)
            {
                var generatedCM = creditMemo.SeriesNumber + 1;
                return $"CM{generatedCM.ToString("D10")}";
            }
            else
            {
                return $"CM{1.ToString("D10")}";
            }
        }


        public async Task<string> GetSINoAsync(int? id, CancellationToken cancellationToken = default)
        {
            if (id != 0)
            {
                var si = await _dbContext
                                .SalesInvoices
                                .FirstOrDefaultAsync(po => po.Id == id, cancellationToken);
                return si.SINo;
            }
            else
            {
                throw new ArgumentException("No record found in supplier.");
            }
        }

        public async Task<string> GetSOANoAsync(int? id, CancellationToken cancellationToken = default)
        {
            if (id != 0)
            {
                var soa = await _dbContext
                                .ServiceInvoices
                                .FirstOrDefaultAsync(po => po.Id == id, cancellationToken);
                return soa.SVNo;
            }
            else
            {
                throw new ArgumentException("No record found in supplier.");
            }
        }

        public async Task<CreditMemo> FindCM(int id, CancellationToken cancellationToken = default)
        {
            var creditMemo = await _dbContext
                .CreditMemos
                .Include(c => c.SalesInvoice)
                .Include(c => c.StatementOfAccount)
                .ThenInclude(soa => soa.Customer)
                .Include(c => c.StatementOfAccount)
                .ThenInclude(soa => soa.Service)
                .FirstOrDefaultAsync(creditMemo => creditMemo.Id == id, cancellationToken);

            if (creditMemo != null)
            {
                return creditMemo;
            }
            else
            {
                throw new ArgumentException("Invalid id value. The id must be greater than 0.");
            }
        }

        public async Task<Services> GetServicesAsync(int id, CancellationToken cancellationToken = default)
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