using Accounting_System.Data;
using Accounting_System.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Accounting_System.Repository
{
    public class ChartOfAccountRepo
    {
        private readonly ApplicationDbContext _dbContext;

        public ChartOfAccountRepo(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<SelectListItem>> FindAccountsAsync(string accountNo, CancellationToken cancellationToken = default)
        {
            var coa = await _dbContext
                .ChartOfAccounts
                .Where(coa => coa.Parent == accountNo)
                .OrderBy(coa => coa.Id)
                .ToListAsync(cancellationToken);

            var list = coa.Select(s => new SelectListItem
            {
                Value = s.Number,
                Text = s.Number + " " + s.Name
            }).ToList();

            return list;
        }

        public async Task<string> GenerateNumberAsync(string parent, CancellationToken cancellationToken = default)
        {
            var lastAccount = await _dbContext
                .ChartOfAccounts
                .OrderBy(coa => coa.Id)
                .LastOrDefaultAsync(coa => coa.Parent == parent, cancellationToken);

            if (lastAccount != null)
            {
                var accountNo = Int32.Parse(lastAccount.Number);

                var generatedNo = accountNo + 1;

                return generatedNo.ToString();
            }
            else
            {
                return parent + "01";
            }
        }
    }
}