using Accounting_System.Data;
using Accounting_System.Models;
using Microsoft.EntityFrameworkCore;

namespace Accounting_System.Repository
{
    public class JournalVoucherRepo
    {
        private readonly ApplicationDbContext _dbContext;

        public JournalVoucherRepo(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<JournalVoucherHeader>> GetJournalVouchersAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.JournalVoucherHeaders
                .Include(j => j.CheckVoucherHeader)
                .ThenInclude(cv => cv.Supplier)
                .ToListAsync(cancellationToken);
        }

        public async Task<string> GenerateJVNo(CancellationToken cancellationToken = default)
        {
            var journalVoucher = await _dbContext
                .JournalVoucherHeaders
                .OrderByDescending(j => j.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (journalVoucher != null)
            {
                var generatedJV = journalVoucher.SeriesNumber + 1;
                return $"JV{generatedJV.ToString("D10")}";
            }
            else
            {
                return $"JV{1.ToString("D10")}";
            }
        }

        public async Task<long> GetLastSeriesNumberJV(CancellationToken cancellationToken = default)
        {
            var lastNumber = await _dbContext
                .JournalVoucherHeaders
                .OrderByDescending(j => j.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (lastNumber != null)
            {
                // Increment the last serial by one and return it
                return lastNumber.SeriesNumber + 1;
            }
            else
            {
                // If there are no existing records, you can start with a default value like 1
                return 1L;
            }
        }
    }
}