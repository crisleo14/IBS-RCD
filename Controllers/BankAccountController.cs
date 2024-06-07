using Accounting_System.Data;
using Accounting_System.Models;
using Accounting_System.Models.MasterFile;
using Accounting_System.Models.Reports;
using Accounting_System.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Accounting_System.Controllers
{
    [Authorize]
    public class BankAccountController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly BankAccountRepo _bankAccountRepo;

        public BankAccountController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, BankAccountRepo bankAccountRepo)
        {
            _dbContext = dbContext;
            this._userManager = userManager;
            _bankAccountRepo = bankAccountRepo;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var ba = await _bankAccountRepo.GetBankAccountAsync(cancellationToken);

            return View(ba);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(BankAccount model, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                if (await _bankAccountRepo.IsBankAccountNoExist(model.AccountNo, cancellationToken))
                {
                    ModelState.AddModelError("AccountNo", "Bank account no already exist!");
                    return View(model);
                }

                if (await _bankAccountRepo.IsBankAccountNameExist(model.AccountName, cancellationToken))
                {
                    ModelState.AddModelError("AccountName", "Bank account name already exist!");
                    return View(model);
                }

                var checkLastAccountNo = await _dbContext
                .BankAccounts
                .OrderBy(bank => bank.Id)
                .LastOrDefaultAsync(cancellationToken);

                #region -- Generate AccountNo --

                var generatedAccountNo = 0L;
                if (checkLastAccountNo != null)
                {
                    // Increment the last serial by one and return it
                    generatedAccountNo = checkLastAccountNo.SeriesNumber + 1L;
                }
                else
                {
                    // If there are no existing records, you can start with a default value like 1
                    generatedAccountNo = 1L;
                }
                model.SeriesNumber = generatedAccountNo;
                model.AccountNoCOA = "1010101" + generatedAccountNo.ToString("D2");

                #endregion -- Generate AccountNo --

                model.CreatedBy = _userManager.GetUserName(this.User);

                #region -- COA Entry --

                var coa = new ChartOfAccount
                {
                    IsMain = false,
                    Number = model.AccountNoCOA,
                    Name = "Cash in Bank" + " - " + model.AccountNo + " " + model.AccountName,
                    Type = "Asset",
                    Category = "Debit",
                    Parent = "1010101",
                    CreatedBy = _userManager.GetUserName(this.User),
                    CreatedDate = DateTime.Now,
                    Level = 5
                };

                await _dbContext.ChartOfAccounts.AddAsync(coa, cancellationToken);

                #endregion -- COA Entry --

                #region --Audit Trail Recording

                AuditTrail auditTrail = new(model.CreatedBy, $"Created new bank {model.AccountName}", "Bank Account");
                await _dbContext.AddAsync(auditTrail, cancellationToken);

                #endregion --Audit Trail Recording

                await _dbContext.AddAsync(model, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                TempData["success"] = "Bank created successfully.";
                return RedirectToAction("Index");
            }
            else
            {
                ModelState.AddModelError("", "The information you submitted is not valid!");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var existingModel = await _bankAccountRepo.FindBankAccount(id, cancellationToken);
            return View(existingModel);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(BankAccount model, CancellationToken cancellationToken)
        {
            var existingModel = await _bankAccountRepo.FindBankAccount(model.Id, cancellationToken);
            if (existingModel == null)
            {
                return NotFound();
            }
            if (ModelState.IsValid)
            {
                existingModel.AccountNo = model.AccountNo;
                existingModel.AccountName = model.AccountName;
                existingModel.Bank = model.Bank;
                existingModel.Branch = model.Branch;

                TempData["success"] = "Bank edited successfully.";

                #region --Audit Trail Recording

                AuditTrail auditTrail = new(_userManager.GetUserName(this.User), $"Updated bank {model.AccountName}", "Bank Account");
                await _dbContext.AddAsync(auditTrail, cancellationToken);

                #endregion --Audit Trail Recording
            }
            else
            {
                ModelState.AddModelError("", "The information you submitted is not valid!");
                return View(existingModel);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            return RedirectToAction("Index");
        }
    }
}