using Accounting_System.Data;
using Accounting_System.Models.ViewModels;
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

        public IEnumerable<ChartOfAccountSummary> GetSummaryReportView(CancellationToken cancellationToken = default)
        {
            var query = from c in _dbContext.ChartOfAccounts
                        join gl in _dbContext.GeneralLedgerBooks on c.Number equals gl.AccountNo into glGroup
                        from gl in glGroup.DefaultIfEmpty()
                        group new { c, gl } by new { Level = c.Level, AccountNumber = c.Number, AccountName = c.Name, AccountType = c.Type, Parent = c.Parent } into g
                        select new ChartOfAccountSummary
                        {
                            Level = g.Key.Level,
                            AccountNumber = g.Key.AccountNumber,
                            AccountName = g.Key.AccountName,
                            AccountType = g.Key.AccountType,
                            Parent = g.Key.Parent,
                            Debit = g.Sum(x => x.gl.Debit),
                            Credit = g.Sum(x => x.gl.Credit),
                            Balance = g.Sum(x => x.gl.Debit) - g.Sum(x => x.gl.Credit),
                            Children = new List<ChartOfAccountSummary>()
                        };

            // Dictionary to store account information by level and account number (key)
            var accountDictionary = query.ToDictionary(x => new { x.Level, x.AccountNumber }, x => x);

            // Loop through all levels (ascending order to include level 1)
            foreach (var level in query.Select(x => x.Level).Distinct().OrderByDescending(x => x))
            {
                // Loop through accounts within the current level
                foreach (var account in accountDictionary.Where(x => x.Key.Level == level))
                {
                    // Update parent account if it exists and handle potential null reference
                    if (account.Value.Parent != null && accountDictionary.TryGetValue(new { Level = level - 1, AccountNumber = account.Value.Parent }, out var parentAccount))
                    {
                        parentAccount.Debit += account.Value.Debit;
                        parentAccount.Credit += account.Value.Credit;
                        parentAccount.Balance += account.Value.Balance;
                        parentAccount.Children.Add(account.Value);
                    }
                }

            }

            // Return the modified accounts
            return accountDictionary.Values.Where(x => x.Level == 1);
        }
    }
}