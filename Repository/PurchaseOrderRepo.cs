using Accounting_System.Data;
using Accounting_System.Models;
using Microsoft.EntityFrameworkCore;

namespace Accounting_System.Repository
{
    public class PurchaseOrderRepo
    {
        private readonly ApplicationDbContext _dbContext;

        public PurchaseOrderRepo(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<PurchaseOrder>> GetPurchaseOrderAsync()
        {
            return await _dbContext
                .PurchaseOrders
                .Include(p => p.Supplier)
                .OrderBy(s => s.Id)
                .ToListAsync();
        }

        public async Task<string> GeneratePONo()
        {
            var purchaseOrder = await _dbContext
                .PurchaseOrders
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync();

            if (purchaseOrder != null)
            {
                var generatedCR = purchaseOrder.Id + 1;
                return $"PO{generatedCR.ToString("D10")}";
            }
            else
            {
                return $"PO{1.ToString("D10")}";
            }
        }
    }
}