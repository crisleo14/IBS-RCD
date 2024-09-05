using Accounting_System.Data;
using Accounting_System.Models.AccountsPayable;
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

        public async Task<List<PurchaseOrder>> GetPurchaseOrderAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext
                .PurchaseOrders
                .Include(p => p.Supplier)
                .Include(p => p.Product)
                .ToListAsync(cancellationToken);
        }

        public async Task<long> GetLastSeriesNumber(CancellationToken cancellationToken = default)
        {
            var lastInvoice = await _dbContext
                .PurchaseOrders
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
                return 1L;
            }
        }

        public async Task<string> GeneratePONo(CancellationToken cancellationToken = default)
        {
            var purchaseOrder = await _dbContext
                .PurchaseOrders
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (purchaseOrder != null)
            {
                var generatedCR = purchaseOrder.SeriesNumber + 1;
                return $"PO{generatedCR.ToString("D10")}";
            }
            else
            {
                return $"PO{1.ToString("D10")}";
            }
        }

        public async Task<int> GetSupplierNoAsync(int? id, CancellationToken cancellationToken = default)
        {
            if (id != 0)
            {
                var supplier = await _dbContext
                                .Suppliers
                                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
                return supplier.Number;
            }
            else
            {
                throw new ArgumentException("No record found in supplier.");
            }
        }

        public async Task<string> GetProductNoAsync(int? id, CancellationToken cancellationToken = default)
        {
            if (id != 0)
            {
                var product = await _dbContext
                                .Products
                                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
                return product.Code;
            }
            else
            {
                throw new ArgumentException("No record found in supplier.");
            }
        }

        public async Task<PurchaseOrder> FindPurchaseOrder(int? id, CancellationToken cancellationToken = default)
        {
            var po = await _dbContext
                .PurchaseOrders
                .Include(p => p.Supplier)
                .Include(p => p.Product)
                .FirstOrDefaultAsync(po => po.Id == id, cancellationToken);

            if (po != null)
            {
                return po;
            }
            else
            {
                throw new ArgumentException("Invalid id value. The id must be greater than 0.");
            }
        }
    }
}