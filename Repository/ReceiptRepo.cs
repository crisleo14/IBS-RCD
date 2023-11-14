using Accounting_System.Data;
using Microsoft.EntityFrameworkCore;

namespace Accounting_System.Repository
{
    public class ReceiptRepo
    {
        private readonly ApplicationDbContext _dbContext;
        public ReceiptRepo(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<string> GenerateCRNo()
        {
            var collectionReceipt = await _dbContext
                .CollectionReceipts
                .Include(s => s.SalesInvoice)
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync();

            if (collectionReceipt != null)
            {
                var generatedCR = collectionReceipt.Id + 1;
                return $"CR{generatedCR.ToString("D10")}";
            }
            else
            {
                return $"CR{1.ToString("D10")}";
            }

        }
    }
}
