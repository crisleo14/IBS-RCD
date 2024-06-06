using Accounting_System.Data;
using Microsoft.EntityFrameworkCore;

namespace Accounting_System.Repository
{
    public class ServiceRepo
    {
        private readonly ApplicationDbContext _dbContext;

        public ServiceRepo(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<int> GetLastNumber(CancellationToken cancellationToken = default)
        {
            var lastNumber = await _dbContext
                .Services
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (lastNumber != null)
            {
                return lastNumber.Number + 1;
            }
            else
            {
                return 2001;
            }
        }

        public async Task<bool> IsServicesExist(string serviceName, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Services
                .AnyAsync(s => s.Name.ToUpper() == serviceName.ToUpper(), cancellationToken);
        }
    }
}