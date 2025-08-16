using Accounting_System.Data;
using Accounting_System.Models.MasterFile;
using Microsoft.EntityFrameworkCore;

namespace Accounting_System.Repository
{
    public class BankAccountRepo
    {
        private readonly ApplicationDbContext _dbContext;

        public BankAccountRepo(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
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

            throw new ArgumentException("Invalid id value. The id must be greater than 0.");
        }

        public async Task<bool> IsBankAccountNameExist(string accountName,
            CancellationToken cancellationToken = default)
        {
            return await _dbContext.BankAccounts
                .AnyAsync(b => b.AccountName.ToUpper() == accountName.ToUpper());
        }
    }
}
