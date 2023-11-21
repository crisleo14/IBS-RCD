using Accounting_System.Data;
using Accounting_System.Models;
using Microsoft.EntityFrameworkCore;

namespace Accounting_System.Repository
{
    public class DMCMRepo
    {
        private readonly ApplicationDbContext _dbContext;

        public DMCMRepo(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }
        public async Task<List<DMCM>> GetDMAsync()
        {
            return await _dbContext
                .DMCM
                .OrderByDescending(c => c.Id)
                .ToListAsync();
        }
        public async Task<DMCM> FindDM(int id)
        {
            var debitMemo = await _dbContext
                .DMCM
                .Include(s => s.SalesInvoice)
                .FirstOrDefaultAsync(debitMemo => debitMemo.Id == id);

            if (debitMemo != null)
            {
                return debitMemo;
            }
            else
            {
                throw new ArgumentException("Invalid id value. The id must be greater than 0.");
            }
        }
        public async Task<string> GenerateDMCMNo()
        {
            var dmcm = await _dbContext
                .DMCM
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync();

            if (dmcm != null)
            {
                var generatedCR = dmcm.Id + 1;
                return $"DMCM{generatedCR.ToString("D10")}";
            }
            else
            {
                return $"DMCM{1.ToString("D10")}";
            }
        }
    }
}
