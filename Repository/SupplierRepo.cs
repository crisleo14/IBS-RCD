using Accounting_System.Data;
using Microsoft.EntityFrameworkCore;

namespace Accounting_System.Repository
{
    public class SupplierRepo
    {
        private readonly ApplicationDbContext _dbContext;

        public SupplierRepo(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<int> GetLastNumber(CancellationToken cancellationToken = default)
        {
            var lastNumber = await _dbContext
                .Suppliers
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (lastNumber != null)
            {
                return lastNumber.Number + 1;
            }
            else
            {
                return 3001;
            }
        }

        public async Task<bool> IsSupplierNameExist(string supplierName, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Suppliers
                .AnyAsync(s => s.Name.ToUpper() == supplierName.ToUpper(), cancellationToken);
        }

        public async Task<bool> IsSupplierTinExist(string supplierName, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Suppliers
                .AnyAsync(s => s.Name.ToUpper() == supplierName.ToUpper(), cancellationToken);
        }
    }
}