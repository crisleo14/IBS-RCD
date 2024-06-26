using Accounting_System.Data;
using Accounting_System.Models.Reports;
using Microsoft.AspNetCore.Mvc.Rendering;
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

        public async Task<List<SelectListItem>> GetSupplierListAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.Suppliers
                .Select(supp => new SelectListItem
                {
                    Value = supp.Id.ToString(),
                    Text = supp.Name
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetPurchaseOrderListAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.PurchaseOrders
                .Select(po => new SelectListItem
                {
                    Value = po.PONo.ToString(),
                    Text = po.PONo
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetReceivingReportListAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.ReceivingReports
                .Select(rr => new SelectListItem
                {
                    Value = rr.RRNo.ToString(),
                    Text = rr.RRNo
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetBankAccountListAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.BankAccounts
                .Select(ba => new SelectListItem
                {
                    Value = ba.Id.ToString(),
                    Text = ba.AccountName
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<SelectListItem>> GetCOAListAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.ChartOfAccounts
                .Where(coa => !new[] { "2010102", "2010101", "1010101" }.Any(excludedNumber => coa.Number.Contains(excludedNumber)) && coa.Level == 4 || coa.Level == 5)
                .Select(s => new SelectListItem
                {
                    Value = s.Number,
                    Text = s.Number + " " + s.Name
                })
                .ToListAsync(cancellationToken);
        }
    }
}