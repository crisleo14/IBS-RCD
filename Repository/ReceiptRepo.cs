using Accounting_System.Data;
using Accounting_System.Models;
using Microsoft.EntityFrameworkCore;

namespace Accounting_System.Repository
{
    public class ReceiptRepo
    {
        private readonly ApplicationDbContext _dbContext;

        public ReceiptRepo(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
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
                return $"CR{generatedCR.ToString("D10")}";
            }
            else
            {
                return $"CR{1.ToString("D10")}";
            }
        }

        public async Task<string> GenerateORNo()
        {
            var officialReceipt = await _dbContext
                .OfficialReceipts
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync();

            if (officialReceipt != null)
            {
                var generatedCR = officialReceipt.Id + 1;
                return $"OR{generatedCR.ToString("D10")}";
            }
            else
            {
                return $"OR{1.ToString("D10")}";
            }
        }

        public async Task<List<CollectionReceipt>> GetCRAsync()
        {
            return await _dbContext
                .CollectionReceipts
                .OrderByDescending(c => c.Id)
                .ToListAsync();
        }

        public async Task<List<OfficialReceipt>> GetORAsync()
        {
            return await _dbContext
                .OfficialReceipts
                .OrderByDescending(c => c.Id)
                .ToListAsync();
        }

        public async Task<CollectionReceipt> FindCR(int id)
        {
            var collectionReceipt = await _dbContext
                .CollectionReceipts
                .Include(s => s.SalesInvoice)
                .FirstOrDefaultAsync(collectionReceipt => collectionReceipt.Id == id);

            if (collectionReceipt != null)
            {
                return collectionReceipt;
            }
            else
            {
                throw new ArgumentException("Invalid id value. The id must be greater than 0.");
            }
        }

        public async Task<OfficialReceipt> FindOR(int id)
        {
            var officialReceipt = await _dbContext
                .OfficialReceipts
                .Include(s => s.StatementOfAccount)
                .ThenInclude(soa => soa.Customer)
                .Include(s => s.StatementOfAccount)
                .ThenInclude(soa => soa.Service)
                .FirstOrDefaultAsync(collectionReceipt => collectionReceipt.Id == id);

            if (officialReceipt != null)
            {
                return officialReceipt;
            }
            else
            {
                throw new ArgumentException("Invalid id value. The id must be greater than 0.");
            }
        }
    }
}