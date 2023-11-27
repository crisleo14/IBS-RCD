using Accounting_System.Data;
using Microsoft.EntityFrameworkCore;

namespace Accounting_System.Repository
{
    public class CreditMemoRepo
    {
        private readonly ApplicationDbContext _dbContext;

        public CreditMemoRepo(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<string> GenerateCMNo()
        {
            var creditMemo = await _dbContext
                .CreditMemos
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync();

            if (creditMemo != null)
            {
                var generatedCM = creditMemo.Id + 1;
                return $"CM{generatedCM.ToString("D10")}";
            }
            else
            {
                return $"CM{1.ToString("D10")}";
            }
        }
    }
}