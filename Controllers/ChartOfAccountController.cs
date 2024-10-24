using Accounting_System.Data;
using Accounting_System.Models;
using Accounting_System.Models.AccountsReceivable;
using Accounting_System.Models.MasterFile;
using Accounting_System.Models.Reports;
using Accounting_System.Repository;
using Accounting_System.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using OfficeOpenXml;

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

        public async Task<IActionResult> Index(string? view, CancellationToken cancellationToken)
        {
            var chartOfAccounts = await _coaRepo.GetChartOfAccountAsync(cancellationToken);

            if (view == nameof(DynamicView.ChartOfAccount))
            {
                return View("ImportExportIndex", chartOfAccounts);
            }
            return View(chartOfAccounts);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllChartOfAccountIds(CancellationToken cancellationToken)
        {
            var coaIds = await _dbContext.ChartOfAccounts
                                     .Select(coa => coa.Id) // Assuming Id is the primary key
                                     .ToListAsync(cancellationToken);
            return Json(coaIds);
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

                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    AuditTrail auditTrailBook = new(_userManager.GetUserName(this.User), $"Updated chart of account {chartOfAccount.Number} {chartOfAccount.Name}", "Chart of Account", ipAddress);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);

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
            var selectedList = await _dbContext.ChartOfAccounts
                .Where(coa => recordIds.Contains(coa.Id))
                .OrderBy(coa => coa.Id)
                .ToListAsync();

            // Create the Excel package
            using var package = new ExcelPackage();
            // Add a new worksheet to the Excel package
            var worksheet = package.Workbook.Worksheets.Add("ChartOfAccount");

            worksheet.Cells["A1"].Value = "IsMain";
            worksheet.Cells["B1"].Value = "AccountNumber";
            worksheet.Cells["C1"].Value = "AccountName";
            worksheet.Cells["D1"].Value = "Type";
            worksheet.Cells["E1"].Value = "Category";
            worksheet.Cells["F1"].Value = "Parent";
            worksheet.Cells["G1"].Value = "CreatedBy";
            worksheet.Cells["H1"].Value = "CreatedDate";
            worksheet.Cells["I1"].Value = "Level";
            worksheet.Cells["J1"].Value = "OriginalChartOfAccount";

            int row = 2;

            foreach (var item in selectedList)
            {
                worksheet.Cells[row, 1].Value = item.IsMain;
                worksheet.Cells[row, 2].Value = item.Number;
                worksheet.Cells[row, 3].Value = item.Name;
                worksheet.Cells[row, 4].Value = item.Type;
                worksheet.Cells[row, 5].Value = item.Category;
                worksheet.Cells[row, 6].Value = item.Parent;
                worksheet.Cells[row, 7].Value = item.CreatedBy;
                worksheet.Cells[row, 8].Value = item.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                worksheet.Cells[row, 9].Value = item.Level;
                worksheet.Cells[row, 10].Value = item.Id;

                row++;
            }

            // Convert the Excel package to a byte array
            var excelBytes = await package.GetAsByteArrayAsync();

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ChartOfAccountList.xlsx");
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
                            return RedirectToAction(nameof(Index), new { view = DynamicView.ChartOfAccount });
                        }
                        if (worksheet.ToString() != nameof(DynamicView.ChartOfAccount))
                        {
                            TempData["error"] = "The Excel file is not related to chart of account.";
                            return RedirectToAction(nameof(Index), new { view = DynamicView.ChartOfAccount });
                        }

                        var rowCount = worksheet.Dimension.Rows;

                        for (int row = 2; row <= rowCount; row++)  // Assuming the first row is the header
                        {
                            var coa = new ChartOfAccount
                            {
                                IsMain = bool.TryParse(worksheet.Cells[row, 1].Text, out bool isMain) ? isMain : false,
                                Number = worksheet.Cells[row, 2].Text,
                                Name = worksheet.Cells[row, 3].Text,
                                Type = worksheet.Cells[row, 4].Text,
                                Category = worksheet.Cells[row, 5].Text,
                                Parent = worksheet.Cells[row, 6].Text,
                                CreatedBy = worksheet.Cells[row, 7].Text,
                                CreatedDate = DateTime.TryParse(worksheet.Cells[row, 8].Text, out DateTime createdDate) ? createdDate : default,
                                Level = int.TryParse(worksheet.Cells[row, 9].Text, out int level) ? level : 0,
                                OriginalChartOfAccountId = int.TryParse(worksheet.Cells[row, 10].Text, out int originalChartOfAccountId) ? originalChartOfAccountId : 0,
                            };

                            var chartOfAccountList = _dbContext
                            .ChartOfAccounts
                            .Where(c => c.OriginalChartOfAccountId == coa.OriginalChartOfAccountId || c.Id == coa.OriginalChartOfAccountId)
                            .ToList();

                            if (chartOfAccountList.Any())
                            {
                                continue;
                            }

                            await _dbContext.ChartOfAccounts.AddAsync(coa);
                            await _dbContext.SaveChangesAsync();
                        }
                    }
                }
                catch (OperationCanceledException oce)
                {
                    TempData["error"] = oce.Message;
                    return RedirectToAction(nameof(Index), new { view = DynamicView.ChartOfAccount });
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(Index), new { view = DynamicView.ChartOfAccount });
                }
            }
            TempData["success"] = "Uploading Success!";
            return RedirectToAction(nameof(Index), new { view = DynamicView.ChartOfAccount });
        }

        #endregion -- import xlsx record --
    }
}