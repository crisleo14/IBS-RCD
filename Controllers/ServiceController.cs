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
using OfficeOpenXml;

namespace Accounting_System.Controllers
{
    [Authorize]
    public class ServiceController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly ServiceRepo _serviceRepo;

        public ServiceController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, ServiceRepo serviceRepo)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _serviceRepo = serviceRepo;
        }

        public async Task<IActionResult> Index(string? view, CancellationToken cancellationToken)
        {
            var data = await _dbContext.Services.ToListAsync(cancellationToken);

            if (view == nameof(DynamicView.Service))
            {
                return View("ImportExportIndex", data);
            }

            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllServiceIds(CancellationToken cancellationToken)
        {
            var serviceIds = await _dbContext.Services
                                     .Select(s => s.Id) // Assuming Id is the primary key
                                     .ToListAsync(cancellationToken);
            return Json(serviceIds);
        }

        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var viewModel = new Services();

            viewModel.CurrentAndPreviousTitles = await _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == 4 || coa.Level == 5)
                .OrderBy(coa => coa.AccountId)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountId.ToString(),
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            viewModel.UnearnedTitles = await _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == 4 || coa.Level == 5)
                .OrderBy(coa => coa.AccountId)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountId.ToString(),
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Services services, CancellationToken cancellationToken)
        {
            services.CurrentAndPreviousTitles = await _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == 4 || coa.Level == 5)
                .OrderBy(coa => coa.AccountId)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountId.ToString(),
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            services.UnearnedTitles = await _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == 4 || coa.Level == 5)
                .OrderBy(coa => coa.AccountId)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountId.ToString(),
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            if (ModelState.IsValid)
            {
                if (await _serviceRepo.IsServicesExist(services.Name, cancellationToken))
                {
                    ModelState.AddModelError("Name", "Services already exist!");
                    return View(services);
                }

                if (services.Percent == 0)
                {
                    ModelState.AddModelError("Percent", "Please input percent!");
                    return View(services);
                }

                var currentAndPrevious = await _dbContext.ChartOfAccounts
                    .FindAsync(services.CurrentAndPreviousId, cancellationToken);

                var unearned = await _dbContext.ChartOfAccounts
                    .FindAsync(services.UnearnedId, cancellationToken);

                services.CurrentAndPreviousNo = currentAndPrevious.AccountNumber;
                services.CurrentAndPreviousTitle = currentAndPrevious.AccountName;

                services.UnearnedNo = unearned.AccountNumber;
                services.UnearnedTitle = unearned.AccountName;

                services.CreatedBy = _userManager.GetUserName(this.User).ToUpper();
                services.Number = await _serviceRepo.GetLastNumber(cancellationToken);

                TempData["success"] = "Services created successfully";

                #region --Audit Trail Recording

                if (services.OriginalServiceId == 0)
                {
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    AuditTrail auditTrailBook = new(services.CreatedBy, $"Created new service {services.Name}", "Service", ipAddress);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                }

                #endregion --Audit Trail Recording

                await _dbContext.AddAsync(services, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                return RedirectToAction(nameof(Index));
            }
            return View(services);
        }

        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
            if (id == null || _dbContext.Services == null)
            {
                return NotFound();
            }

            var services = await _dbContext.Services.FindAsync(id, cancellationToken);
            if (services == null)
            {
                return NotFound();
            }
            return View(services);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Name,Percent,Id,CreatedBy,CreatedDate")] Services services, CancellationToken cancellationToken)
        {
            if (id != services.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                if (services.Percent == 0)
                {
                    ModelState.AddModelError("Percent", "Please input percent!");
                    return View(services);
                }
                try
                {
                    _dbContext.Update(services);

                    TempData["success"] = "Services updated successfully";

                    #region --Audit Trail Recording

                    if (services.OriginalServiceId == 0)
                    {
                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        AuditTrail auditTrailBook = new(_userManager.GetUserName(this.User), $"Update service {services.Name}", "Service", ipAddress);
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                    }

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ServicesExists(services.Id))
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
            return View(services);
        }

        private bool ServicesExists(int id)
        {
            return (_dbContext.Services?.Any(e => e.Id == id)).GetValueOrDefault();
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
            var selectedList = await _dbContext.Services
                .Where(service => recordIds.Contains(service.Id))
                .OrderBy(service => service.Id)
                .ToListAsync();

            // Create the Excel package
            using var package = new ExcelPackage();
            // Add a new worksheet to the Excel package
            var worksheet = package.Workbook.Worksheets.Add("Services");

            worksheet.Cells["A1"].Value = "CurrentAndPreviousTitle";
            worksheet.Cells["B1"].Value = "UneranedTitle";
            worksheet.Cells["C1"].Value = "Name";
            worksheet.Cells["D1"].Value = "Percent";
            worksheet.Cells["E1"].Value = "CreatedBy";
            worksheet.Cells["F1"].Value = "CreatedDate";
            worksheet.Cells["G1"].Value = "CurrentAndPreviousNo";
            worksheet.Cells["H1"].Value = "UnearnedNo";
            worksheet.Cells["I1"].Value = "OriginalServiceId";

            int row = 2;

            foreach (var item in selectedList)
            {
                worksheet.Cells[row, 1].Value = item.CurrentAndPreviousTitle;
                worksheet.Cells[row, 2].Value = item.UnearnedTitle;
                worksheet.Cells[row, 3].Value = item.Name;
                worksheet.Cells[row, 4].Value = item.Percent;
                worksheet.Cells[row, 5].Value = item.CreatedBy;
                worksheet.Cells[row, 6].Value = item.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                worksheet.Cells[row, 7].Value = item.CurrentAndPreviousNo;
                worksheet.Cells[row, 8].Value = item.UnearnedNo;
                worksheet.Cells[row, 9].Value = item.Id;

                row++;
            }

            // Convert the Excel package to a byte array
            var excelBytes = await package.GetAsByteArrayAsync();

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ServiceList.xlsx");
        }

        #endregion -- export xlsx record --

        //Upload as .xlsx file.(Import)
        #region -- import xlsx record --

        [HttpPost]
        public async Task<IActionResult> Import(IFormFile file, CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0)
            {
                return RedirectToAction(nameof(Index));
            }

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                stream.Position = 0;
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                        if (worksheet == null)
                        {
                            TempData["error"] = "The Excel file contains no worksheets.";
                            return RedirectToAction(nameof(Index), new { view = DynamicView.Service });
                        }
                        if (worksheet.ToString() != "Services")
                        {
                            TempData["error"] = "The Excel file is not related to service master file.";
                            return RedirectToAction(nameof(Index), new { view = DynamicView.Service });
                        }

                        var rowCount = worksheet.Dimension.Rows;
                        var servicesList = await _dbContext
                            .Services
                            .ToListAsync(cancellationToken);

                        for (int row = 2; row <= rowCount; row++)  // Assuming the first row is the header
                        {
                            var services = new Services
                            {
                                Number = await _serviceRepo.GetLastNumber(),
                                CurrentAndPreviousTitle = worksheet.Cells[row, 1].Text,
                                UnearnedTitle = worksheet.Cells[row, 2].Text,
                                Name = worksheet.Cells[row, 3].Text,
                                Percent = int.TryParse(worksheet.Cells[row, 4].Text, out int percent) ? percent : 0,
                                CreatedBy = worksheet.Cells[row, 5].Text,
                                CreatedDate = DateTime.TryParse(worksheet.Cells[row, 6].Text, out DateTime createdDate) ? createdDate : default,
                                CurrentAndPreviousNo = worksheet.Cells[row, 7].Text,
                                UnearnedNo = worksheet.Cells[row, 8].Text,
                                OriginalServiceId = int.TryParse(worksheet.Cells[row, 9].Text, out int originalServiceId) ? originalServiceId : 0,
                            };

                            if (servicesList.Any(s => s.OriginalServiceId == services.OriginalServiceId))
                            {
                                continue;
                            }

                            await _dbContext.Services.AddAsync(services, cancellationToken);
                        }
                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                    }
                }
                catch (OperationCanceledException oce)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = oce.Message;
                    return RedirectToAction(nameof(Index), new { view = DynamicView.Service });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(Index), new { view = DynamicView.Service });
                }
            }
            TempData["success"] = "Uploading Success!";
            return RedirectToAction(nameof(Index), new { view = DynamicView.Service });
        }

        #endregion -- import xlsx record --
    }
}
