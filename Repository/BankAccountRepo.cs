using Accounting_System.Data;
using Accounting_System.Models;
using Accounting_System.Models.MasterFile;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Accounting_System.Repository
{
    public class BankAccountRepo
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<IdentityUser> _userManager;

        public BankAccountRepo(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
        }

        public async Task<List<BankAccount>> GetBankAccountAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext
                .BankAccounts
                .ToListAsync(cancellationToken);
        }

        public async Task<BankAccount> FindBankAccount(int? id, CancellationToken cancellationToken = default)
        {
            var bankAccount = await _dbContext
                .BankAccounts
                .FirstOrDefaultAsync(ba => ba.Id == id, cancellationToken);

            if (bankAccount != null)
            {
                return bankAccount;
            }
            else
            {
                throw new ArgumentException("Invalid id value. The id must be greater than 0.");
            }
        }

        public async Task<bool> IsBankAccountNameExist(string accountName,
            CancellationToken cancellationToken = default)
        {
            return await _dbContext.BankAccounts
                .AnyAsync(b => b.AccountName.ToUpper() == accountName.ToUpper());
        }

        public async Task<long> GetLastSeriesNumber(CancellationToken cancellationToken = default)
        {
            var lastRecord = await _dbContext
                .BankAccounts
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (lastRecord != null)
            {
                // Increment the last serial by one and return it
                return lastRecord.SeriesNumber + 1;
            }
            else
            {
                // If there are no existing records, you can start with a default value like 1
                return 1;
            }
        }

        public ChartOfAccount COAEntry(BankAccount model, ClaimsPrincipal user,
            CancellationToken cancellationToken = default)
        {
            var coa = new ChartOfAccount
            {
                IsMain = false,
                Number = model.AccountNoCOA,
                Name = "Cash in Bank" + " - " + model.AccountName,
                Type = "Asset",
                Category = "Debit",
                Parent = "1010101",
                CreatedBy = _userManager.GetUserName(user),
                CreatedDate = DateTime.Now,
                Level = 5
            };

            if (coa != null)
            {
                return coa;
            }
            else
            {
                throw new ArgumentException("Invalid model chart of account.");
            }
        }
    }
}