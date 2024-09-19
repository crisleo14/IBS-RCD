using Accounting_System.Data;
using Accounting_System.Models.MasterFile;
using Accounting_System.Repository;
using Accounting_System.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

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

        public async Task<IActionResult> Index(string? view, CancellationToken cancellationToken)
        {
            var ba = await _bankAccountRepo.GetBankAccountAsync(cancellationToken);

            if (view == nameof(DynamicView.BankAccount))
            {
                return View("ImportExportIndex", ba);
            }

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

                model.SeriesNumber = await _bankAccountRepo.GetLastSeriesNumber(cancellationToken);
                model.AccountNoCOA = "1010101" + model.SeriesNumber.ToString("D2");

                #endregion -- Generate AccountNo --

                model.CreatedBy = _userManager.GetUserName(this.User);

                #region -- COA Entry --

                var coa = _bankAccountRepo.COAEntry(model, User, cancellationToken);
                await _dbContext.AddAsync(coa, cancellationToken);

                #endregion -- COA Entry --

                //#region --Audit Trail Recording

                //AuditTrail auditTrail = new(model.CreatedBy, $"Created new bank {model.AccountName}", "Bank Account");
                //await _dbContext.AddAsync(auditTrail, cancellationToken);

                //#endregion --Audit Trail Recording

                await _dbContext.AddAsync(model, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                TempData["success"] = "Bank created successfully.";
                return RedirectToAction(nameof(Index));
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

                //#region --Audit Trail Recording

                //AuditTrail auditTrail = new(_userManager.GetUserName(this.User), $"Updated bank {model.AccountName}", "Bank Account");
                //await _dbContext.AddAsync(auditTrail, cancellationToken);

                //#endregion --Audit Trail Recording
            }
            else
            {
                ModelState.AddModelError("", "The information you submitted is not valid!");
                return View(existingModel);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            return RedirectToAction(nameof(Index));
        }

        //Download as .xlsx file.(Export)
        #region -- export xlsx record --

        [HttpPost]
        public async Task<IActionResult> Export(string selectedRecord)
        {
            if (string.IsNullOrEmpty(selectedRecord))
            {
                // Handle the case where no invoices are selected
                return RedirectToAction(nameof(Index));
            }

            var recordIds = selectedRecord.Split(',').Select(int.Parse).ToList();

            // Retrieve the selected invoices from the database
            var selectedList = await _dbContext.BankAccounts
                .Where(bank => recordIds.Contains(bank.Id))
                .OrderBy(bank => bank.Id)
                .ToListAsync();

            // Create the Excel package
            using var package = new ExcelPackage();
            // Add a new worksheet to the Excel package
            var worksheet = package.Workbook.Worksheets.Add("BankAccount");

            worksheet.Cells["A1"].Value = "Branch";
            worksheet.Cells["B1"].Value = "CreatedBy";
            worksheet.Cells["C1"].Value = "CreatedDate";
            worksheet.Cells["D1"].Value = "AccountName";
            worksheet.Cells["E1"].Value = "AccountNo";
            worksheet.Cells["F1"].Value = "Bank";
            worksheet.Cells["G1"].Value = "OriginalBankId";

            int row = 2;

            foreach (var item in selectedList)
            {
                worksheet.Cells[row, 1].Value = item.Branch;
                worksheet.Cells[row, 2].Value = item.CreatedBy;
                worksheet.Cells[row, 3].Value = item.CreatedDate;
                worksheet.Cells[row, 4].Value = item.AccountName;
                worksheet.Cells[row, 5].Value = item.AccountNo;
                worksheet.Cells[row, 6].Value = item.Bank;
                worksheet.Cells[row, 7].Value = item.Id;

                row++;
            }

            // Convert the Excel package to a byte array
            var excelBytes = await package.GetAsByteArrayAsync();

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "BankAccountList.xlsx");
        }

        #endregion -- export xlsx record --

        //Upload as .xlsx file.(Import)
        #region -- import xlsx record --

        [HttpPost]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return RedirectToAction(nameof(Index));
            }

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                stream.Position = 0;

                try
                {
                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                        if (worksheet == null)
                        {
                            TempData["error"] = "The Excel file contains no worksheets.";
                            return RedirectToAction(nameof(Index), new { view = DynamicView.BankAccount });
                        }
                        if (worksheet.ToString() != nameof(DynamicView.BankAccount))
                        {
                            TempData["error"] = "The Excel file is not related to bank account master file.";
                            return RedirectToAction(nameof(Index), new { view = DynamicView.BankAccount });
                        }

                        var rowCount = worksheet.Dimension.Rows;

                        for (int row = 2; row <= rowCount; row++)  // Assuming the first row is the header
                        {
                            var bankAccount = new BankAccount
                            {

                                SeriesNumber = await _bankAccountRepo.GetLastSeriesNumber(),
                                Branch = worksheet.Cells[row, 1].Text,
                                CreatedBy = worksheet.Cells[row, 2].Text,
                                CreatedDate = DateTime.TryParse(worksheet.Cells[row, 3].Text, out DateTime createdDate) ? createdDate : default,
                                AccountName = worksheet.Cells[row, 4].Text,
                                AccountNo = worksheet.Cells[row, 5].Text,
                                Bank = worksheet.Cells[row, 6].Text,
                                OriginalBankId = int.TryParse(worksheet.Cells[row, 7].Text, out int originalBankId) ? originalBankId : 0,
                            };
                            bankAccount.AccountNoCOA = "1010101" + bankAccount.SeriesNumber.ToString("D2");

                            #region -- COA Entry --

                            var coa = _bankAccountRepo.COAEntry(bankAccount, User);
                            await _dbContext.AddAsync(coa);

                            #endregion -- COA Entry --

                            await _dbContext.BankAccounts.AddAsync(bankAccount);
                            await _dbContext.SaveChangesAsync();
                        }

                    }
                }
                catch (OperationCanceledException oce)
                {
                    TempData["error"] = oce.Message;
                    return RedirectToAction(nameof(Index), new { view = DynamicView.BankAccount });
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(Index), new { view = DynamicView.BankAccount });
                }
            }
            TempData["success"] = "Uploading Success!";
            return RedirectToAction(nameof(Index), new { view = DynamicView.BankAccount });
        }

        #endregion -- import xlsx record --
    }
}