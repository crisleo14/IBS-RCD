using Accounting_System.Data;
using Accounting_System.Models;
using Accounting_System.Models.Reports;
using Accounting_System.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Accounting_System.Controllers
{
    [Authorize]
    public class ChartOfAccountController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly ChartOfAccountRepo _coaRepo;

        public ChartOfAccountController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, ChartOfAccountRepo coaRepo)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _coaRepo = coaRepo;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            return _dbContext.ChartOfAccounts != null ?
                        View(await _dbContext.ChartOfAccounts.ToListAsync(cancellationToken)) :
                        Problem("Entity set 'ApplicationDbContext.ChartOfAccounts'  is null.");
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var viewModel = new ChartOfAccount();

            viewModel.Main = await _dbContext.ChartOfAccounts
                .OrderBy(coa => coa.Id)
                .Where(coa => coa.IsMain)
                .Select(s => new SelectListItem
                {
                    Value = s.Number,
                    Text = s.Number + " " + s.Name
                })
               .ToListAsync(cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ChartOfAccount chartOfAccount, string thirdLevel, string? fourthLevel, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                chartOfAccount.CreatedBy = _userManager.GetUserName(this.User);

                if (fourthLevel == "create-new" || fourthLevel == null)
                {
                    var existingCoa = await _dbContext
                        .ChartOfAccounts
                        .OrderBy(coa => coa.Id)
                        .FirstOrDefaultAsync(coa => coa.Number == thirdLevel, cancellationToken);

                    if (existingCoa == null)
                    {
                        return NotFound();
                    }

                    chartOfAccount.Type = existingCoa.Type;
                    chartOfAccount.Category = existingCoa.Category;
                    chartOfAccount.Level = existingCoa.Level + 1;
                    chartOfAccount.Parent = thirdLevel;
                }
                else
                {
                    var existingCoa = await _dbContext
                        .ChartOfAccounts
                        .OrderBy(coa => coa.Id)
                        .FirstOrDefaultAsync(coa => coa.Number == fourthLevel, cancellationToken);

                    if (existingCoa == null)
                    {
                        return NotFound();
                    }

                    chartOfAccount.Type = existingCoa.Type;
                    chartOfAccount.Category = existingCoa.Category;
                    chartOfAccount.Level = existingCoa.Level + 1;
                    chartOfAccount.Parent = fourthLevel;
                }

                await _dbContext.AddAsync(chartOfAccount, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                return RedirectToAction(nameof(Index));
            }
            return View(chartOfAccount);
        }

        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
            if (id == null || _dbContext.ChartOfAccounts == null)
            {
                return NotFound();
            }

            var chartOfAccount = await _dbContext.ChartOfAccounts.FindAsync(id, cancellationToken);
            if (chartOfAccount == null)
            {
                return NotFound();
            }
            return View(chartOfAccount);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IsMain,Number,Name,Type,Category,Level,Id,CreatedBy,CreatedDate")] ChartOfAccount chartOfAccount, CancellationToken cancellationToken)
        {
            if (id != chartOfAccount.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _dbContext.Update(chartOfAccount);

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(_userManager.GetUserName(this.User), $"Updated chart of account {chartOfAccount.Number} {chartOfAccount.Name}", "Chart of Account");
                    await _dbContext.AddAsync(auditTrail, cancellationToken);

                    #endregion --Audit Trail Recording

                    TempData["success"] = "Chart of account updated successfully";

                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ChartOfAccountExists(chartOfAccount.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(chartOfAccount);
        }

        private bool ChartOfAccountExists(int id)
        {
            return (_dbContext.ChartOfAccounts?.Any(e => e.Id == id)).GetValueOrDefault();
        }

        [HttpGet]
        public async Task<IActionResult> GetChartOfAccount(string number, CancellationToken cancellationToken)
        {
            return Json(await _coaRepo.FindAccountsAsync(number, cancellationToken));
        }

        [HttpGet]
        public async Task<IActionResult> GenerateNumber(string parent, CancellationToken cancellationToken)
        {
            return Json(await _coaRepo.GenerateNumberAsync(parent, cancellationToken));
        }
    }
}