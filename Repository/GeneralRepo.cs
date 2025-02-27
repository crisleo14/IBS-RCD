using Accounting_System.Data;
using Accounting_System.Models.Reports;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Accounting_System.Models;
using Accounting_System.Models.AccountsReceivable;
using Accounting_System.Models.DTOs;

namespace Accounting_System.Repository
{
    public class GeneralRepo
    {
        private readonly ApplicationDbContext _dbContext;

        private const decimal VatRate = 0.12m;

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
                decimal totalDebit = Math.Round(ledgers.Sum(j => j.Debit), 2);
                decimal totalCredit = Math.Round(ledgers.Sum(j => j.Credit), 2);

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

        public async Task<List<SelectListItem>> GetReceivingReportListAsync(string[] rrNos, CancellationToken cancellationToken = default)
        {
            var rrNoHashSet = new HashSet<string>(rrNos);

            var rrList = await _dbContext.ReceivingReports
                .OrderBy(rr => rrNoHashSet.Contains(rr.RRNo) ? Array.IndexOf(rrNos, rr.RRNo) : int.MaxValue) // Order by index in rrNos array if present in HashSet
                .ThenBy(rr => rr.Id) // Secondary ordering by Id
                .Select(rr => new SelectListItem
                {
                    Value = rr.RRNo.ToString(),
                    Text = rr.RRNo
                })
                .ToListAsync(cancellationToken);

            return rrList;
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
                .Where(coa => !new[] { "202010200", "202010100", "101010100" }.Any(excludedNumber => coa.AccountNumber.Contains(excludedNumber)) && coa.Level == 4 || coa.Level == 5)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);
        }

        public decimal ComputeNetOfVat(decimal grossAmount)
        {
            if (grossAmount <= 0)
            {
                throw new ArgumentException("Gross amount cannot be negative or zero.");
            }

            return grossAmount / (1 + VatRate);
        }

        public decimal ComputeVatAmount(decimal netOfVatAmount)
        {
            if (netOfVatAmount <= 0)
            {
                throw new ArgumentException("Net of vat amount cannot be negative or zero.");
            }

            return netOfVatAmount * VatRate;
        }

        public decimal ComputeEwtAmount(decimal netOfVatAmount, decimal percent)
        {
            if (netOfVatAmount <= 0)
            {
                throw new ArgumentException("Net of vat amount cannot be negative or zero.");
            }

            if (percent <= 0)
            {
                throw new ArgumentException("Ewt percent cannot be negative or zero.");
            }

            return netOfVatAmount * percent;
        }

        public decimal ComputeNetOfEwt(decimal grossAmount, decimal ewtAmount)
        {
            if (grossAmount <= 0)
            {
                throw new ArgumentException("Gross amount cannot be negative or zero.");
            }

            if (ewtAmount <= 0)
            {
                throw new ArgumentException("Ewt amount cannot be negative or zero.");
            }

            return grossAmount - ewtAmount;
        }

        public async Task<List<AccountTitleDto>> GetListOfAccountTitleDto(CancellationToken cancellationToken = default)
        {
            return await _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == 4 || coa.Level == 5)
                .Select(coa => new AccountTitleDto
                {
                    AccountId = coa.AccountId,
                    AccountNumber = coa.AccountNumber,
                    AccountName = coa.AccountName
                })
                .ToListAsync(cancellationToken);
        }
    }
}
