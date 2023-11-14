using Accounting_System.Data;
using Microsoft.EntityFrameworkCore;

namespace Accounting_System.Repository
{
    public class CollectionReceiptRepo
    {
        private readonly ApplicationDbContext _dbContext;
        public CollectionReceiptRepo(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<string> GenerateCRNo()
        {
            var collectionReceipt = await _dbContext
                .CollectionReceipts
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync();

            if (collectionReceipt != null)
            {
                var generatedCR = collectionReceipt.Id + 1;
                return $"CR{generatedCR.ToString("D8")}";
            }
            else
            {
                return $"CR{1.ToString("D8")}";
            }

        }
    }
}
