using Accounting_System.Data;
using Accounting_System.Models;
using Accounting_System.Models.AccountsReceivable;
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
    public class ServiceInvoiceController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly ServiceInvoiceRepo _serviceInvoiceRepo;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly GeneralRepo _generalRepo;

        public ServiceInvoiceController(ApplicationDbContext dbContext, ServiceInvoiceRepo statementOfAccountRepo, UserManager<IdentityUser> userManager, GeneralRepo generalRepo)
        {
            _dbContext = dbContext;
            _serviceInvoiceRepo = statementOfAccountRepo;
            _userManager = userManager;
            _generalRepo = generalRepo;
        }

        public async Task<IActionResult> Index(string? view, CancellationToken cancellationToken)
        {
            var results = await _serviceInvoiceRepo
                .GetSvListAsync(cancellationToken);

            if (view == nameof(DynamicView.ServiceInvoice))
            {
                return View("ImportExportIndex", results);
            }

            return View(results);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var viewModel = new ServiceInvoice();
            viewModel.Customers = await _dbContext.Customers
                .OrderBy(c => c.Id)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                })
                .ToListAsync(cancellationToken);
            viewModel.Services = await _dbContext.Services
                .OrderBy(s => s.Id)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Name
                })
                .ToListAsync(cancellationToken);
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(ServiceInvoice model, CancellationToken cancellationToken)
        {
            model.Customers = await _dbContext.Customers
                .OrderBy(c => c.Id)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                })
                .ToListAsync(cancellationToken);
            model.Services = await _dbContext.Services
                .OrderBy(s => s.Id)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Name
                })
                .ToListAsync(cancellationToken);
            if (ModelState.IsValid)
            {
                #region --Validating the series

                var getLastNumber = await _serviceInvoiceRepo.GetLastSeriesNumber(cancellationToken);

                if (getLastNumber > 9999999999)
                {
                    TempData["error"] = "You reach the maximum Series Number";
                    return View(model);
                }
                var totalRemainingSeries = 9999999999 - getLastNumber;
                if (getLastNumber >= 9999999899)
                {
                    TempData["warning"] = $"Service invoice created successfully, Warning {totalRemainingSeries} series number remaining";
                }
                else
                {
                    TempData["success"] = "Service invoice created successfully";
                }

                #endregion --Validating the series

                #region --Retrieval of Services

                var services = await _serviceInvoiceRepo.GetServicesAsync(model?.ServicesId, cancellationToken);

                #endregion --Retrieval of Services

                #region --Retrieval of Customer

                var customer = await _serviceInvoiceRepo.FindCustomerAsync(model?.CustomerId, cancellationToken);

                #endregion --Retrieval of Customer

                #region --Saving the default properties

                model.SeriesNumber = getLastNumber;

                model.SVNo = await _serviceInvoiceRepo.GenerateSvNo(cancellationToken);

                model.CreatedBy = _userManager.GetUserName(this.User);

                model.ServiceNo = services.Number;

                model.Total = model.Amount;

                if (customer.CustomerType == "Vatable")
                {
                    model.NetAmount = (model.Total - model.Discount) / 1.12m;
                    model.VatAmount = (model.Total - model.Discount) - model.NetAmount;
                    model.WithholdingTaxAmount = model.NetAmount * (services.Percent / 100m);
                    if (customer.WithHoldingVat)
                    {
                        model.WithholdingVatAmount = model.NetAmount * 0.05m;
                    }
                }
                else
                {
                    model.NetAmount = model.Total - model.Discount;
                    model.WithholdingTaxAmount = model.NetAmount * (services.Percent / 100m);
                    if (customer.WithHoldingVat)
                    {
                        model.WithholdingVatAmount = model.NetAmount * 0.05m;
                    }
                }

                if (DateOnly.FromDateTime(model.CreatedDate) < model.Period)
                {
                    model.UnearnedAmount += model.Amount;
                }
                else
                {
                    model.CurrentAndPreviousAmount += model.Amount;
                }

                if (customer.CustomerType == "Vatable")
                {
                    model.CurrentAndPreviousAmount = Math.Round(model.CurrentAndPreviousAmount / 1.12m, 2);
                    model.UnearnedAmount = Math.Round(model.UnearnedAmount / 1.12m, 2);

                    var total = model.CurrentAndPreviousAmount + model.UnearnedAmount;

                    var roundedNetAmount = Math.Round(model.NetAmount, 2);

                    if (roundedNetAmount > total)
                    {
                        var shortAmount = model.NetAmount - total;

                        model.CurrentAndPreviousAmount += shortAmount;
                    }
                }

                await _dbContext.AddAsync(model, cancellationToken);

                #endregion --Saving the default properties

                //#region --Audit Trail Recording

                //AuditTrail auditTrail = new(model.CreatedBy, $"Create new service invoice# {model.SVNo}", "Service Invoice");
                //await _dbContext.AddAsync(auditTrail, cancellationToken);

                //#endregion --Audit Trail Recording

                await _dbContext.SaveChangesAsync(cancellationToken);
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        public async Task<IActionResult> Generate(int id, CancellationToken cancellationToken)
        {
            var soa = await _serviceInvoiceRepo
                .FindSv(id, cancellationToken);

            return View(soa);
        }

        public async Task<IActionResult> PrintedSOA(int id, CancellationToken cancellationToken)
        {
            var findIdOfSOA = await _serviceInvoiceRepo.FindSv(id, cancellationToken);
            if (findIdOfSOA != null && !findIdOfSOA.IsPrinted)
            {
                //#region --Audit Trail Recording

                //var printedBy = _userManager.GetUserName(this.User);
                //AuditTrail auditTrail = new(printedBy, $"Printed original copy of sv# {findIdOfSOA.SVNo}", "Service Invoice");
                //await _dbContext.AddAsync(auditTrail, cancellationToken);

                //#endregion --Audit Trail Recording

                findIdOfSOA.IsPrinted = true;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            return RedirectToAction(nameof(Generate), new { id });
        }

        public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
        {
            var model = await _serviceInvoiceRepo.FindSv(id, cancellationToken);

            try
            {
                if (model != null)
                {
                    if (!model.IsPosted)
                    {
                        model.IsPosted = true;
                        model.PostedBy = _userManager.GetUserName(this.User);
                        model.PostedDate = DateTime.Now;

                        #region --Retrieval of Services

                        var services = await _serviceInvoiceRepo.GetServicesAsync(model.ServicesId, cancellationToken);

                        #endregion --Retrieval of Services

                        #region --Retrieval of Customer

                        var customer = await _serviceInvoiceRepo.FindCustomerAsync(model.CustomerId, cancellationToken);

                        #endregion --Retrieval of Customer

                        #region --SV Computation--

                        var postedDate = DateOnly.FromDateTime(model.CreatedDate) >= model.Period ? DateOnly.FromDateTime(model.CreatedDate) : model.Period.AddMonths(1).AddDays(-1);

                        if (customer.CustomerType == "Vatable")
                        {
                            model.Total = model.Amount;
                            model.NetAmount = (model.Amount - model.Discount) / 1.12m;
                            model.VatAmount = (model.Amount - model.Discount) - model.NetAmount;
                            model.WithholdingTaxAmount = Math.Round(model.NetAmount * (customer.WithHoldingTax ? services.Percent / 100m : 0), 2);
                            if (customer.WithHoldingVat)
                            {
                                model.WithholdingVatAmount = Math.Round(model.NetAmount * 0.05m, 2);
                            }
                        }
                        else
                        {
                            model.NetAmount = model.Amount - model.Discount;
                            model.WithholdingTaxAmount = model.NetAmount * (customer.WithHoldingTax ? services.Percent / 100m : 0);
                            if (customer.WithHoldingVat)
                            {
                                model.WithholdingVatAmount = model.NetAmount * 0.05m;
                            }
                        }

                        if (customer.CustomerType == "Vatable")
                        {
                            var total = Math.Round(model.Amount / 1.12m, 2);

                            var roundedNetAmount = Math.Round(model.NetAmount, 2);

                            if (roundedNetAmount > total)
                            {
                                model.Amount = model.NetAmount - total;
                            }
                        }

                        #endregion --SV Computation--

                        #region --Sales Book Recording

                        var sales = new SalesBook();

                        if (model.Customer.CustomerType == "Vatable")
                        {
                            sales.TransactionDate = postedDate;
                            sales.SerialNo = model.SVNo;
                            sales.SoldTo = model.Customer.Name;
                            sales.TinNo = model.Customer.TinNo;
                            sales.Address = model.Customer.Address;
                            sales.Description = model.Service.Name;
                            sales.Amount = model.Total;
                            sales.VatAmount = model.VatAmount;
                            sales.VatableSales = model.Total / 1.12m;
                            sales.Discount = model.Discount;
                            sales.NetSales = model.NetAmount;
                            sales.CreatedBy = model.CreatedBy;
                            sales.CreatedDate = model.CreatedDate;
                            sales.DueDate = model.DueDate;
                            sales.DocumentId = model.Id;
                        }
                        else if (model.Customer.CustomerType == "Exempt")
                        {
                            sales.TransactionDate = postedDate;
                            sales.SerialNo = model.SVNo;
                            sales.SoldTo = model.Customer.Name;
                            sales.TinNo = model.Customer.TinNo;
                            sales.Address = model.Customer.Address;
                            sales.Description = model.Service.Name;
                            sales.Amount = model.Total;
                            sales.VatExemptSales = model.Total;
                            sales.Discount = model.Discount;
                            sales.NetSales = model.NetAmount;
                            sales.CreatedBy = model.CreatedBy;
                            sales.CreatedDate = model.CreatedDate;
                            sales.DueDate = model.DueDate;
                            sales.DocumentId = model.Id;
                        }
                        else
                        {
                            sales.TransactionDate = postedDate;
                            sales.SerialNo = model.SVNo;
                            sales.SoldTo = model.Customer.Name;
                            sales.TinNo = model.Customer.TinNo;
                            sales.Address = model.Customer.Address;
                            sales.Description = model.Service.Name;
                            sales.Amount = model.Total;
                            sales.ZeroRated = model.Total;
                            sales.Discount = model.Discount;
                            sales.NetSales = model.NetAmount;
                            sales.CreatedBy = model.CreatedBy;
                            sales.CreatedDate = model.CreatedDate;
                            sales.DueDate = model.DueDate;
                            sales.DocumentId = model.Id;
                        }

                        await _dbContext.AddAsync(sales, cancellationToken);

                        #endregion --Sales Book Recording

                        #region --General Ledger Book Recording

                        var ledgers = new List<GeneralLedgerBook>();

                        ledgers.Add(
                                new GeneralLedgerBook
                                {
                                    Date = postedDate,
                                    Reference = model.SVNo,
                                    Description = model.Service.Name,
                                    AccountNo = "1010204",
                                    AccountTitle = "AR-Non Trade Receivable",
                                    Debit = Math.Round(model.Total - (model.WithholdingTaxAmount + model.WithholdingVatAmount), 2),
                                    Credit = 0,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        if (model.WithholdingTaxAmount > 0)
                        {
                            ledgers.Add(
                                new GeneralLedgerBook
                                {
                                    Date = postedDate,
                                    Reference = model.SVNo,
                                    Description = model.Service.Name,
                                    AccountNo = "1010202",
                                    AccountTitle = "Deferred Creditable Withholding Tax",
                                    Debit = model.WithholdingTaxAmount,
                                    Credit = 0,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }
                        if (model.WithholdingVatAmount > 0)
                        {
                            ledgers.Add(
                                new GeneralLedgerBook
                                {
                                    Date = postedDate,
                                    Reference = model.SVNo,
                                    Description = model.Service.Name,
                                    AccountNo = "1010203",
                                    AccountTitle = "Deferred Creditable Withholding Vat",
                                    Debit = model.WithholdingVatAmount,
                                    Credit = 0,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }

                        ledgers.Add(
                               new GeneralLedgerBook
                               {
                                   Date = postedDate,
                                   Reference = model.SVNo,
                                   Description = model.Service.Name,
                                   AccountNo = model.Service.CurrentAndPreviousNo,
                                   AccountTitle = model.Service.CurrentAndPreviousTitle,
                                   Debit = 0,
                                   Credit = Math.Round(model.Total / 1.12m, 2),
                                   CreatedBy = model.CreatedBy,
                                   CreatedDate = model.CreatedDate
                               }
                           );

                        if (model.VatAmount > 0)
                        {
                            ledgers.Add(
                                new GeneralLedgerBook
                                {
                                    Date = postedDate,
                                    Reference = model.SVNo,
                                    Description = model.Service.Name,
                                    AccountNo = "2010301",
                                    AccountTitle = "Vat Output",
                                    Debit = 0,
                                    Credit = Math.Round((model.VatAmount), 2),
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }

                        if (!_generalRepo.IsDebitCreditBalanced(ledgers))
                        {
                            throw new ArgumentException("Debit and Credit is not equal, check your entries.");
                        }

                        await _dbContext.GeneralLedgerBooks.AddRangeAsync(ledgers, cancellationToken);

                        #endregion --General Ledger Book Recording

                        //#region --Audit Trail Recording

                        //AuditTrail auditTrail = new(model.PostedBy, $"Posted service invoice# {model.SVNo}", "Service Invoice");
                        //await _dbContext.AddAsync(auditTrail, cancellationToken);

                        //#endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        TempData["success"] = "Service invoice has been posted.";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        return RedirectToAction(nameof(Index));
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }

            return null;
        }

        public async Task<IActionResult> Cancel(int id, string cancellationRemarks, CancellationToken cancellationToken)
        {
            var model = await _dbContext.ServiceInvoices.FindAsync(id, cancellationToken);

            if (model != null)
            {
                if (!model.IsCanceled)
                {
                    model.IsCanceled = true;
                    model.CanceledBy = _userManager.GetUserName(this.User);
                    model.CanceledDate = DateTime.Now;
                    model.CancellationRemarks = cancellationRemarks;

                    //#region --Audit Trail Recording

                    //AuditTrail auditTrail = new(model.CanceledBy, $"Cancelled service invoice# {model.SVNo}", "Service Invoice");
                    //await _dbContext.AddAsync(auditTrail, cancellationToken);

                    //#endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Service invoice has been Cancelled.";
                }
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {
            var model = await _dbContext.ServiceInvoices.FindAsync(id, cancellationToken);

            if (model != null)
            {
                if (!model.IsVoided)
                {
                    if (model.IsPosted)
                    {
                        model.IsPosted = false;
                    }

                    model.IsVoided = true;
                    model.VoidedBy = _userManager.GetUserName(this.User);
                    model.VoidedDate = DateTime.Now;

                    //#region --Audit Trail Recording

                    //AuditTrail auditTrail = new(model.VoidedBy, $"Voided service invoice# {model.SVNo}", "Service Invoice");
                    //await _dbContext.AddAsync(auditTrail, cancellationToken);

                    //#endregion --Audit Trail Recording

                    await _generalRepo.RemoveRecords<SalesBook>(gl => gl.SerialNo == model.SVNo, cancellationToken);
                    await _generalRepo.RemoveRecords<GeneralLedgerBook>(gl => gl.Reference == model.SVNo, cancellationToken);

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Service invoice has been voided.";
                }
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }
            var existingModel = await _serviceInvoiceRepo.FindSv(id, cancellationToken);

            if (existingModel == null)
            {
                return NotFound();
            }

            existingModel.Customers = await _dbContext.Customers
                .OrderBy(c => c.Id)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                })
                .ToListAsync(cancellationToken);
            existingModel.Services = await _dbContext.Services
                .OrderBy(s => s.Id)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Name
                })
                .ToListAsync(cancellationToken);

            return View(existingModel);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(ServiceInvoice model, CancellationToken cancellationToken)
        {
            var existingModel = await _serviceInvoiceRepo.FindSv(model.Id, cancellationToken);

            if (existingModel == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                #region --Validating the series

                var getLastNumber = await _serviceInvoiceRepo.GetLastSeriesNumber(cancellationToken);

                if (getLastNumber > 9999999999)
                {
                    TempData["error"] = "You reach the maximum Series Number";
                    return View(model);
                }
                var totalRemainingSeries = 9999999999 - getLastNumber;
                if (getLastNumber >= 9999999899)
                {
                    TempData["warning"] = $"Service invoice created successfully, Warning {totalRemainingSeries} series number remaining";
                }
                else
                {
                    TempData["success"] = "Service invoice created successfully";
                }

                #endregion --Validating the series

                #region --Retrieval of Services

                var services = await _serviceInvoiceRepo.GetServicesAsync(model.ServicesId, cancellationToken);

                #endregion --Retrieval of Services

                #region --Retrieval of Customer

                var customer = await _serviceInvoiceRepo.FindCustomerAsync(model.CustomerId, cancellationToken);

                #endregion --Retrieval of Customer

                #region --Saving the default properties

                existingModel.Discount = model.Discount;
                existingModel.Amount = model.Amount;
                existingModel.Period = model.Period;
                existingModel.DueDate = model.DueDate;
                existingModel.Instructions = model.Instructions;

                decimal total = 0;
                total += model.Amount;
                existingModel.Total = total;

                if (customer.CustomerType == "Vatable")
                {
                    existingModel.NetAmount = (existingModel.Total - existingModel.Discount) / 1.12m;
                    existingModel.VatAmount = (existingModel.Total - existingModel.Discount) - existingModel.NetAmount;
                    existingModel.WithholdingTaxAmount = existingModel.NetAmount * (services.Percent / 100m);
                    if (customer.WithHoldingVat)
                    {
                        existingModel.WithholdingVatAmount = existingModel.NetAmount * 0.05m;
                    }
                }
                else
                {
                    existingModel.NetAmount = existingModel.Total - existingModel.Discount;
                    existingModel.WithholdingTaxAmount = existingModel.NetAmount * (services.Percent / 100m);
                    if (customer.WithHoldingVat)
                    {
                        existingModel.WithholdingVatAmount = existingModel.NetAmount * 0.05m;
                    }
                }

                #endregion --Saving the default properties

                //#region --Audit Trail Recording

                //AuditTrail auditTrail = new(existingModel.CreatedBy, $"Edit service invoice# {existingModel.SVNo}", "Service Invoice");
                //await _dbContext.AddAsync(auditTrail, cancellationToken);

                //#endregion --Audit Trail Recording

                await _dbContext.SaveChangesAsync(cancellationToken);
                return RedirectToAction(nameof(Index));
            }

            return View(existingModel);
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
            var selectedList = await _dbContext.ServiceInvoices
                .Where(sv => recordIds.Contains(sv.Id))
                .OrderBy(sv => sv.SVNo)
                .ToListAsync();

            // Create the Excel package
            using var package = new ExcelPackage();
            // Add a new worksheet to the Excel package
            var worksheet = package.Workbook.Worksheets.Add("ServiceInvoices");

            worksheet.Cells["A1"].Value = "DueDate";
            worksheet.Cells["B1"].Value = "Period";
            worksheet.Cells["C1"].Value = "Amount";
            worksheet.Cells["D1"].Value = "VatAmount";
            worksheet.Cells["E1"].Value = "NetAmount";
            worksheet.Cells["F1"].Value = "Total";
            worksheet.Cells["G1"].Value = "Discount";
            worksheet.Cells["H1"].Value = "WithholdingTaxAmount";
            worksheet.Cells["I1"].Value = "WithholdingVatAmount";
            worksheet.Cells["J1"].Value = "CurrentAndPreviousMonth";
            worksheet.Cells["K1"].Value = "UnearnedAmount";
            worksheet.Cells["L1"].Value = "Status";
            worksheet.Cells["M1"].Value = "AmountPaid";
            worksheet.Cells["N1"].Value = "Balance";
            worksheet.Cells["O1"].Value = "Instructions";
            worksheet.Cells["P1"].Value = "IsPaid";
            worksheet.Cells["Q1"].Value = "CreatedBy";
            worksheet.Cells["R1"].Value = "CreatedDate";
            worksheet.Cells["S1"].Value = "CancellationRemarks";
            worksheet.Cells["T1"].Value = "OriginalCustomerId";
            worksheet.Cells["U1"].Value = "OriginalSeriesNumber";
            worksheet.Cells["V1"].Value = "OriginalServicesId";
            worksheet.Cells["W1"].Value = "OriginalDocumentId";

            int row = 2;

            foreach (var item in selectedList)
            {
                worksheet.Cells[row, 1].Value = item.DueDate.ToString("yyyy-MM-dd");
                worksheet.Cells[row, 2].Value = item.Period.ToString("yyyy-MM-dd");
                worksheet.Cells[row, 3].Value = item.Amount;
                worksheet.Cells[row, 4].Value = item.VatAmount;
                worksheet.Cells[row, 5].Value = item.NetAmount;
                worksheet.Cells[row, 6].Value = item.Total;
                worksheet.Cells[row, 7].Value = item.Discount;
                worksheet.Cells[row, 8].Value = item.WithholdingTaxAmount;
                worksheet.Cells[row, 9].Value = item.WithholdingVatAmount;
                worksheet.Cells[row, 10].Value = item.CurrentAndPreviousAmount;
                worksheet.Cells[row, 11].Value = item.UnearnedAmount;
                worksheet.Cells[row, 12].Value = item.Status;
                worksheet.Cells[row, 13].Value = item.AmountPaid;
                worksheet.Cells[row, 14].Value = item.Balance;
                worksheet.Cells[row, 15].Value = item.Instructions;
                worksheet.Cells[row, 16].Value = item.IsPaid;
                worksheet.Cells[row, 17].Value = item.CreatedBy;
                worksheet.Cells[row, 18].Value = item.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                worksheet.Cells[row, 19].Value = item.CancellationRemarks;
                worksheet.Cells[row, 20].Value = item.CustomerId;
                worksheet.Cells[row, 21].Value = item.SVNo;
                worksheet.Cells[row, 22].Value = item.ServicesId;
                worksheet.Cells[row, 23].Value = item.Id;

                row++;
            }

            // Convert the Excel package to a byte array
            var excelBytes = await package.GetAsByteArrayAsync();

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ServiceInvoiceList.xlsx");
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
                            var serviceInvoice = new ServiceInvoice
                            {
                                SVNo = await _serviceInvoiceRepo.GenerateSvNo(),
                                SeriesNumber = await _serviceInvoiceRepo.GetLastSeriesNumber(),
                                DueDate = DateOnly.TryParse(worksheet.Cells[row, 1].Text, out DateOnly dueDate) ? dueDate : default,
                                Period = DateOnly.TryParse(worksheet.Cells[row, 2].Text, out DateOnly period) ? period : default,
                                Amount = decimal.TryParse(worksheet.Cells[row, 3].Text, out decimal amount) ? amount : 0,
                                VatAmount = decimal.TryParse(worksheet.Cells[row, 4].Text, out decimal vatAmount) ? vatAmount : 0,
                                NetAmount = decimal.TryParse(worksheet.Cells[row, 5].Text, out decimal netAmount) ? netAmount : 0,
                                Total = decimal.TryParse(worksheet.Cells[row, 6].Text, out decimal total) ? total : 0,
                                Discount = decimal.TryParse(worksheet.Cells[row, 7].Text, out decimal discount) ? discount : 0,
                                WithholdingTaxAmount = decimal.TryParse(worksheet.Cells[row, 8].Text, out decimal withholdingTaxAmount) ? withholdingTaxAmount : 0,
                                WithholdingVatAmount = decimal.TryParse(worksheet.Cells[row, 9].Text, out decimal withholdingVatAmount) ? withholdingVatAmount : 0,
                                CurrentAndPreviousAmount = decimal.TryParse(worksheet.Cells[row, 10].Text, out decimal currentAndPreviousAmount) ? currentAndPreviousAmount : 0,
                                UnearnedAmount = decimal.TryParse(worksheet.Cells[row, 11].Text, out decimal unearnedAmount) ? unearnedAmount : 0,
                                Status = worksheet.Cells[row, 12].Text,
                                AmountPaid = decimal.TryParse(worksheet.Cells[row, 13].Text, out decimal amountPaid) ? amountPaid : 0,
                                Balance = decimal.TryParse(worksheet.Cells[row, 14].Text, out decimal balance) ? balance : 0,
                                Instructions = worksheet.Cells[row, 15].Text,
                                IsPaid = bool.TryParse(worksheet.Cells[row, 16].Text, out bool isPaid) ? isPaid : false,
                                CreatedBy = worksheet.Cells[row, 17].Text,
                                CreatedDate = DateTime.TryParse(worksheet.Cells[row, 18].Text, out DateTime createdDate) ? createdDate : default,
                                CancellationRemarks = worksheet.Cells[row, 19].Text,
                                OriginalCustomerId = int.TryParse(worksheet.Cells[row, 20].Text, out int originalCustomerId) ? originalCustomerId : 0,
                                OriginalSeriesNumber = worksheet.Cells[row, 21].Text,
                                OriginalServicesId = int.TryParse(worksheet.Cells[row, 22].Text, out int originalServicesId) ? originalServicesId : 0,
                                OriginalDocumentId = int.TryParse(worksheet.Cells[row, 23].Text, out int originalDocumentId) ? originalDocumentId : 0,
                            };
                            serviceInvoice.CustomerId = await _dbContext.Customers
                                .Where(sv => sv.OriginalCustomerId == serviceInvoice.OriginalCustomerId)
                                .Select(sv => sv.Id)
                                .FirstOrDefaultAsync();

                            serviceInvoice.ServicesId = await _dbContext.Services
                                .Where(sv => sv.OriginalServiceId == serviceInvoice.OriginalServicesId)
                                .Select(sv => sv.Id)
                                .FirstOrDefaultAsync();

                            await _dbContext.ServiceInvoices.AddAsync(serviceInvoice);
                            await _dbContext.SaveChangesAsync();
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