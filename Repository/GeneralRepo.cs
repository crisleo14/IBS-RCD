using Accounting_System.Data;
using Accounting_System.Models.Reports;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Accounting_System.Repository
{
    public class GeneralRepo
    {
        private readonly ApplicationDbContext _dbContext;

        public GeneralRepo(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<int> RemoveRecords<TEntity>(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
       where TEntity : class
        {
            var entitySet = _dbContext.Set<TEntity>();
            var entitiesToRemove = await entitySet.Where(predicate).ToListAsync(cancellationToken);

            if (entitiesToRemove.Any())
            {
                foreach (var entity in entitiesToRemove)
                {
                    entitySet.Remove(entity);
                }

                try
                {
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    return entitiesToRemove.Count;
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
            else
            {
                throw new ArgumentException($"No entities found with identifier value: '{predicate.Body.ToString}'");
            }
        }

        public bool IsDebitCreditBalanced(IEnumerable<GeneralLedgerBook> ledgers)
        {
            try
            {
                decimal totalDebit = ledgers.Sum(j => j.Debit);
                decimal totalCredit = ledgers.Sum(j => j.Credit);

                return totalDebit == totalCredit;
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.Message);
            }
        }
    }
}