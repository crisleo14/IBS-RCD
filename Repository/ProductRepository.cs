using Accounting_System.Data;
using Microsoft.EntityFrameworkCore;

namespace Accounting_System.Repository
{
    public class ProductRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public ProductRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<bool> IsProductCodeExist(string productCode, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Products
                .AnyAsync(p => p.ProductCode!.ToUpper() == productCode.ToUpper(), cancellationToken);
        }

        public async Task<bool> IsProductNameExist(string productName, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Products
                .AnyAsync(p => p.ProductName.ToUpper() == productName.ToUpper(), cancellationToken);
        }
    }
}
