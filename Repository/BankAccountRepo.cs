using Accounting_System.Data;
using Accounting_System.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
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
                .OrderByDescending(b => b.Id)
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
    }
}
