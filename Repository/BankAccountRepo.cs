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
                .FirstOrDefaultAsync(ba => ba.BankAccountId == id, cancellationToken);

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
    }
}
