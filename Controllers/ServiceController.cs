using Accounting_System.Data;
using Accounting_System.Models;
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

        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var viewModel = new Services();

            viewModel.CurrentAndPreviousTitles = await _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == 4 || coa.Level == 5)
                .OrderBy(coa => coa.Id)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Number + " " + s.Name
                })
                .ToListAsync(cancellationToken);

            viewModel.UnearnedTitles = await _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == 4 || coa.Level == 5)
                .OrderBy(coa => coa.Id)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Number + " " + s.Name
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
                .OrderBy(coa => coa.Id)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Number + " " + s.Name
                })
                .ToListAsync(cancellationToken);

            services.UnearnedTitles = await _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == 4 || coa.Level == 5)
                .OrderBy(coa => coa.Id)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Number + " " + s.Name
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

                services.CurrentAndPreviousNo = currentAndPrevious.Number;
                services.CurrentAndPreviousTitle = currentAndPrevious.Name;

                services.UnearnedNo = unearned.Number;
                services.UnearnedTitle = unearned.Name;

                services.CreatedBy = _userManager.GetUserName(this.User).ToUpper();
                services.Number = await _serviceRepo.GetLastNumber(cancellationToken);

                TempData["success"] = "Services created successfully";

                #region --Audit Trail Recording

                AuditTrail auditTrail = new(services.CreatedBy, $"Created new service {services.Name}", "Service");
                await _dbContext.AddAsync(auditTrail, cancellationToken);

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

                    AuditTrail auditTrail = new(_userManager.GetUserName(this.User), $"Update service {services.Name}", "Service");
                    await _dbContext.AddAsync(auditTrail, cancellationToken);

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
        public IActionResult Export(string selectedRecord)
        {
            if (string.IsNullOrEmpty(selectedRecord))
            {
                // Handle the case where no invoices are selected
                return RedirectToAction(nameof(Index));
            }

            var recordIds = selectedRecord.Split(',').Select(int.Parse).ToList();

            // Retrieve the selected invoices from the database
            var selectedList = _dbContext.Services
                .Where(service => recordIds.Contains(service.Id))
                .OrderBy(service => service.Id)
                .ToList();

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
            var excelBytes = package.GetAsByteArray();

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ServiceList.xlsx");
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
                            return RedirectToAction(nameof(Index), new { errorMessage = "The Excel file contains no worksheets." });
                        }

                        var rowCount = worksheet.Dimension.Rows;

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
                            await _dbContext.Services.AddAsync(services);
                            await _dbContext.SaveChangesAsync();

                            //var svcs = await _dbContext
                            //    .Services
                            //    .FirstOrDefaultAsync(svcs => svcs.Id == services.Id);

                            //sv.CustomerId = await _dbContext.Customers
                            //    .Where(sv => sv.OriginalCustomerId == serviceInvoice.OriginalCustomerId)
                            //    .Select(sv => sv.Id)
                            //    .FirstOrDefaultAsync();

                            //sv.ServicesId = await _dbContext.Services
                            //    .Where(sv => sv.OriginalServiceId == serviceInvoice.OriginalServicesId)
                            //    .Select(sv => sv.Id)
                            //    .FirstOrDefaultAsync();

                            //await _dbContext.SaveChangesAsync();
                        }

                    }
                }
                catch (OperationCanceledException oce)
                {
                    TempData["error"] = oce.Message;
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(Index));
                }
            }

            return RedirectToAction(nameof(Index));
        }

        #endregion -- import xlsx record --
    }
}