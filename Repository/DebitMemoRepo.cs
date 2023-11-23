using Accounting_System.Data;
using Accounting_System.Models;
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

        public async Task<List<DebitMemo>> GetDMAsync()
        {
            return await _dbContext
                .DebitMemos
                .OrderByDescending(d => d.Id)
                .ToListAsync();
        }
        public async Task<string> GenerateDMNo()
        {
            var debitMemo = await _dbContext
                .DebitMemos
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync();

            if (debitMemo != null)
            {
                var generatedCR = debitMemo.Id + 1;
                return $"DM{generatedCR.ToString("D10")}";
            }
            else
            {
                return $"DM{1.ToString("D10")}";
            }
        }
    }
}
