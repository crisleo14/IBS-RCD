using Accounting_System.Data;
using Accounting_System.Models;
using Microsoft.EntityFrameworkCore;

namespace Accounting_System.Repository
{
    public class CheckVoucherRepo
    {
        private readonly ApplicationDbContext _dbContext;

        public CheckVoucherRepo(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<CheckVoucherHeader>> GetCheckVouchers()
        {
            return await _dbContext
                .CheckVoucherHeaders
                .OrderByDescending(cv => cv.Id)
                .ToListAsync();
        }
    }
}