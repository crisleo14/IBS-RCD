using Accounting_System.Data;
using Accounting_System.Models.AccountsPayable;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Security.Cryptography.Xml;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Accounting_System.Repository
{
    public class CheckVoucherRepo
    {
        private readonly ApplicationDbContext _dbContext;

        public CheckVoucherRepo(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<CheckVoucherHeader>> GetCheckVouchers(CancellationToken cancellationToken = default)
        {
            return await _dbContext
                .CheckVoucherHeaders
                .OrderByDescending(cv => cv.Id)
                .ToListAsync(cancellationToken);
        }
        public async Task<string> GenerateCVNo(CancellationToken cancellationToken = default)
        {
            var checkVoucher = await _dbContext
                .CheckVoucherHeaders
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (checkVoucher != null)
            {
                var generatedCV = checkVoucher.SeriesNumber + 1;
                return $"CV{generatedCV.ToString("D10")}";
            }
            else
            {
                return $"CV{1.ToString("D10")}";
            }
        }
        public async Task<long> GetLastSeriesNumberCV(CancellationToken cancellationToken = default)
        {
            var lastNumber = await _dbContext
                .CheckVoucherHeaders
                .OrderByDescending(s => s.Id)
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
        public async Task<long> GetLastSequenceNumberCV(CancellationToken cancellationToken)
        {
            var lastSequence = await _dbContext
                .CheckVoucherHeaders
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync(s => s.AccruedType == "Payment", cancellationToken);

            if (lastSequence != null)
            {
                // Increment the last serial by one and return it
                return lastSequence.Sequence + 1;
            }
            else
            {
                // If there are no existing records, you can start with a default value like 1
                return 1L;
            }
        }
    }
}