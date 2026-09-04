using System.Diagnostics;
using System.Globalization;
using Accounting_System.Data;
using Accounting_System.Models;
using Accounting_System.Models.AccountsPayable;
using Accounting_System.Models.Reports;
using Accounting_System.Repository;
using Accounting_System.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Linq.Dynamic.Core;
using Microsoft.IdentityModel.Tokens;

namespace Accounting_System.Controllers
{
    [Authorize]
    public class ReceivingReportController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly AasDbContext _aasDbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly ReceivingReportRepo _receivingReportRepo;

        private readonly PurchaseOrderRepo _purchaseOrderRepo;

        private readonly GeneralRepo _generalRepo;

        private readonly InventoryRepo _inventoryRepo;

        public ReceivingReportController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, ReceivingReportRepo receivingReportRepo, GeneralRepo generalRepo, InventoryRepo inventoryRepo, PurchaseOrderRepo purchaseOrderRepo, AasDbContext aasDbContext)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _receivingReportRepo = receivingReportRepo;
            _purchaseOrderRepo = purchaseOrderRepo;
            _generalRepo = generalRepo;
            _inventoryRepo = inventoryRepo;
            _aasDbContext = aasDbContext;
        }

        public IActionResult Index(string? view)
        {
            if (view == nameof(DynamicView.ReceivingReport))
            {
                return View("ImportExportIndex");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetReceivingReports([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var receivingReports = await _receivingReportRepo.GetReceivingReportsAsync(cancellationToken);
                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();
                    receivingReports = receivingReports
                        .Where(rr =>
                            rr.ReceivingReportNo?.ToLower().Contains(searchValue) == true ||
                            rr.Date.ToString("MMM dd, yyyy").ToLower().Contains(searchValue) ||
                            rr.PONo?.ToLower().Contains(searchValue) == true ||
                            rr.QuantityDelivered.ToString(CultureInfo.InvariantCulture).Contains(searchValue) ||
                            rr.QuantityReceived.ToString(CultureInfo.InvariantCulture).Contains(searchValue) ||
                            rr.Amount.ToString(CultureInfo.InvariantCulture).Contains(searchValue) ||
                            rr.Remarks.ToString().ToLower().Contains(searchValue) ||
                            rr.CreatedBy!.ToLower().Contains(searchValue)
                            )
                        .ToList();
                }
                // Sorting
                if (parameters.Order != null && parameters.Order.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Data;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";
                    receivingReports = receivingReports
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}")
                        .ToList();
                }
                var totalRecords = receivingReports.Count();
                var pagedData = receivingReports
                    .Skip(parameters.Start)
                    .Take(parameters.Length)
                    .ToList();
                return Json(new
                {
                    draw = parameters.Draw,
                    recordsTotal = totalRecords,
                    recordsFiltered = totalRecords,
                    data = pagedData
                });
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllReceivingReportIds(CancellationToken cancellationToken)
        {
            var receivingReportIds = await _dbContext.ReceivingReports
                                     .Select(rr => rr.ReceivingReportId) // Assuming Id is the primary key
                                     .ToListAsync(cancellationToken);
            return Json(receivingReportIds);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var viewModel = new ReceivingReport
            {
                PurchaseOrders = await _dbContext.PurchaseOrders
                    .Where(po => !po.IsReceived && po.IsPosted && !po.IsClosed)
                    .Select(po => new SelectListItem
                    {
                        Value = po.PurchaseOrderId.ToString(),
                        Text = po.PurchaseOrderNo
                    })
                    .ToListAsync(cancellationToken)
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ReceivingReport model, CancellationToken cancellationToken)
        {
            model.PurchaseOrders = await _dbContext.PurchaseOrders
                .Where(po => !po.IsReceived && po.IsPosted)
                .Select(po => new SelectListItem
                {
                    Value = po.PurchaseOrderId.ToString(),
                    Text = po.PurchaseOrderNo
                })
                .ToListAsync(cancellationToken);
            if (ModelState.IsValid)
            {
                var createdBy = await _generalRepo.GetUserFullNameAsync(User.Identity!.Name!);

                #region --Retrieve PO

                var existingPo = await _dbContext
                            .PurchaseOrders
                            .Include(po => po.Supplier)
                            .Include(po => po.Product)
                            .FirstOrDefaultAsync(po => po.PurchaseOrderId == model.POId, cancellationToken);

                #endregion --Retrieve PO

                var totalAmountRr = existingPo!.Quantity - existingPo.QuantityReceived;

                if (model.QuantityDelivered > totalAmountRr)
                {
                    TempData["error"] = "Input is exceed to remaining quantity delivered";
                    return View(model);
                }

                #region --Validating Series

                var generatedRr = await _receivingReportRepo.GenerateRRNo(cancellationToken);
                var getLastNumber = long.Parse(generatedRr.Substring(2));

                if (getLastNumber > 9999999999)
                {
                    TempData["error"] = "You reach the maximum Series Number";
                    return View(model);
                }
                var totalRemainingSeries = 9999999999 - getLastNumber;
                if (getLastNumber >= 9999999899)
                {
                    TempData["warning"] = $"Receiving Report created successfully, Warning {totalRemainingSeries} series number remaining";
                }
                else
                {
                    TempData["success"] = "Receiving Report created successfully";
                }

                #endregion --Validating Series

                model.ReceivingReportNo = generatedRr;
                model.CreatedBy = createdBy;
                model.GainOrLoss = model.QuantityReceived - model.QuantityDelivered;
                model.PONo = await _receivingReportRepo.GetPONoAsync(model.POId, cancellationToken);
                model.DueDate = await _receivingReportRepo.ComputeDueDateAsync(model.POId, model.Date, cancellationToken);

                model.Amount = model.QuantityReceived * existingPo.Price;

                #region --Audit Trail Recording

                if (model.OriginalSeriesNumber.IsNullOrEmpty() && model.OriginalDocumentId == 0)
                {
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    AuditTrail auditTrailBook = new(createdBy, $"Create new receiving report# {model.ReceivingReportNo}", "Receiving Report", ipAddress!);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                }

                #endregion --Audit Trail Recording

                await _dbContext.AddAsync(model, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError("", "The information you submitted is not valid!");
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
            if (id == null || !_dbContext.ReceivingReports.Any())
            {
                return NotFound();
            }

            var receivingReport = await _dbContext.ReceivingReports.FirstOrDefaultAsync(x => x.ReceivingReportId == id, cancellationToken);
            if (receivingReport == null)
            {
                return NotFound();
            }

            receivingReport.PurchaseOrders = await _dbContext.PurchaseOrders
                .Select(s => new SelectListItem
                {
                    Value = s.PurchaseOrderId.ToString(),
                    Text = s.PurchaseOrderNo
                })
                .ToListAsync(cancellationToken);

            return View(receivingReport);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(ReceivingReport model, CancellationToken cancellationToken)
        {
            var existingModel = await _dbContext.ReceivingReports.FirstOrDefaultAsync(x => x.ReceivingReportId == model.ReceivingReportId, cancellationToken);

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                if (ModelState.IsValid)
                {
                    if (existingModel == null)
                    {
                        return NotFound();
                    }
                    var createdBy = await _generalRepo.GetUserFullNameAsync(User.Identity!.Name!);

                    #region --Retrieve PO

                    var po = await _dbContext
                                .PurchaseOrders
                                .Include(po => po.Supplier)
                                .Include(po => po.Product)
                                .FirstOrDefaultAsync(po => po.PurchaseOrderId == model.POId, cancellationToken);

                    #endregion --Retrieve PO

                    var totalAmountRr = po!.Quantity - po.QuantityReceived;

                    if (model.QuantityDelivered > totalAmountRr && !existingModel.IsPosted)
                    {
                        TempData["error"] = "Input is exceed to remaining quantity delivered";
                        existingModel.PurchaseOrders = await _dbContext.PurchaseOrders
                            .Select(s => new SelectListItem
                            {
                                Value = s.PurchaseOrderId.ToString(),
                                Text = s.PurchaseOrderNo
                            })
                            .ToListAsync(cancellationToken);
                        return View(existingModel);
                    }

                    existingModel.Date = model.Date;
                    existingModel.POId = model.POId;
                    existingModel.PONo = await _receivingReportRepo.GetPONoAsync(model.POId, cancellationToken);
                    existingModel.DueDate = await _receivingReportRepo.ComputeDueDateAsync(model.POId, model.Date, cancellationToken);
                    existingModel.SupplierInvoiceNumber = model.SupplierInvoiceNumber;
                    existingModel.SupplierInvoiceDate = model.SupplierInvoiceDate;
                    existingModel.TruckOrVessels = model.TruckOrVessels;
                    existingModel.QuantityDelivered = model.QuantityDelivered;
                    existingModel.QuantityReceived = model.QuantityReceived;
                    existingModel.GainOrLoss = model.QuantityReceived - model.QuantityDelivered;
                    existingModel.OtherRef = model.OtherRef;
                    existingModel.Remarks = model.Remarks;
                    existingModel.ReceivedDate = model.ReceivedDate;
                    existingModel.Amount = model.QuantityReceived * po.Price;

                    if (_dbContext.ChangeTracker.HasChanges())
                    {
                        #region --Audit Trail Recording

                        if (existingModel.OriginalSeriesNumber.IsNullOrEmpty() && existingModel.OriginalDocumentId == 0)
                        {
                            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                            AuditTrail auditTrailBook = new(createdBy, $"Edited receiving report# {existingModel.ReceivingReportNo}", "Receiving Report", ipAddress!);
                            await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                        }

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        TempData["success"] = "Receiving Report updated successfully";
                        await transaction.CommitAsync(cancellationToken);
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        throw new InvalidOperationException("No data changes!");
                    }
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                existingModel!.PurchaseOrders = await _dbContext.PurchaseOrders
                    .Select(s => new SelectListItem
                    {
                        Value = s.PurchaseOrderId.ToString(),
                        Text = s.PurchaseOrderNo
                    })
                    .ToListAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return View(existingModel);
            }

            existingModel!.PurchaseOrders = await _dbContext.PurchaseOrders
                .Select(s => new SelectListItem
                {
                    Value = s.PurchaseOrderId.ToString(),
                    Text = s.PurchaseOrderNo
                })
                .ToListAsync(cancellationToken);
            return View(existingModel);
        }

        [HttpGet]
        public async Task<IActionResult> Print(int id, CancellationToken cancellationToken)
        {
            if (id == 0 || !_dbContext.ReceivingReports.Any())
            {
                return NotFound();
            }

            var receivingReport = await _receivingReportRepo.FindRR(id, cancellationToken);

            return View(receivingReport);
        }

        public async Task<IActionResult> Printed(int id, CancellationToken cancellationToken)
        {
            var rr = await _dbContext.ReceivingReports.FirstOrDefaultAsync(x => x.ReceivingReportId == id, cancellationToken);
            var createdBy = await _generalRepo.GetUserFullNameAsync(User.Identity!.Name!);

            if (rr != null && !rr.IsPrinted)
            {

                #region --Audit Trail Recording

                if (rr.OriginalSeriesNumber.IsNullOrEmpty() && rr.OriginalDocumentId == 0)
                {
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    AuditTrail auditTrailBook = new(createdBy, $"Printed original copy of rr# {rr.ReceivingReportNo}", "Receiving Report", ipAddress!);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                }

                #endregion --Audit Trail Recording

                rr.IsPrinted = true;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            return RedirectToAction(nameof(Print), new { id });
        }

        public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
        {
            var model = await _receivingReportRepo.FindRR(id, cancellationToken);

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            var createdBy = !model.OriginalSeriesNumber.IsNullOrEmpty() && model.OriginalDocumentId != 0 ? model.PostedBy : await _generalRepo.GetUserFullNameAsync(User.Identity!.Name!);
            var date = !model.OriginalSeriesNumber.IsNullOrEmpty() && model.OriginalDocumentId != 0 ? model.PostedDate : DateTime.Now;

            try
            {
                if (model.ReceivedDate == null)
                {
                    TempData["error"] = "Please indicate the received date.";
                    return RedirectToAction(nameof(Index));
                }

                if (!model.IsPosted)
                {
                    model.IsPosted = true;
                    model.PostedBy = createdBy;
                    model.PostedDate = date;

                    await _receivingReportRepo.PostAsync(model, User, cancellationToken);

                     #region --Audit Trail Recording

                    if (model.OriginalSeriesNumber.IsNullOrEmpty() && model.OriginalDocumentId == 0)
                    {
                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        AuditTrail auditTrailBook = new(createdBy, $"Posted rr# {model.ReceivingReportNo}", "Receiving Report", ipAddress!);
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                    }

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    TempData["success"] = "Receiving Report has been Posted.";
                    return RedirectToAction(nameof(Print), new { id });
                }
                else
                {
                    return RedirectToAction(nameof(Print), new { id });
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Print), new { id });
            }
        }

        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {
            var model = await _dbContext.ReceivingReports
                .FirstOrDefaultAsync(x => x.ReceivingReportId == id, cancellationToken);

            var existingInventory = await _dbContext.Inventories
                .Include(i => i.Product)
                .FirstOrDefaultAsync(i => i.Reference == model!.ReceivingReportNo, cancellationToken: cancellationToken);

            if (model != null && existingInventory != null)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                var createdBy = !model.OriginalSeriesNumber.IsNullOrEmpty() && model.OriginalDocumentId != 0 ? model.VoidedBy : await _generalRepo.GetUserFullNameAsync(User.Identity!.Name!);
                var date = !model.OriginalSeriesNumber.IsNullOrEmpty() && model.OriginalDocumentId != 0 ? model.VoidedDate : DateTime.Now;
                try
                {
                    if (!model.IsVoided)
                    {
                        if (model.IsPosted)
                        {
                            model.IsPosted = false;
                        }
                        model.IsVoided = true;
                        model.VoidedBy = createdBy;
                        model.VoidedDate = date;

                        await _generalRepo.RemoveRecords<PurchaseJournalBook>(pb => pb.DocumentNo == model.ReceivingReportNo, cancellationToken);
                        await _generalRepo.RemoveRecords<GeneralLedgerBook>(gl => gl.Reference == model.ReceivingReportNo, cancellationToken);
                        await _inventoryRepo.VoidInventory(existingInventory, cancellationToken);
                        await _receivingReportRepo.RemoveQuantityReceived(model.POId, model.QuantityReceived, cancellationToken);
                        model.QuantityReceived = 0;

                        #region --Audit Trail Recording

                        if (model.OriginalSeriesNumber.IsNullOrEmpty() && model.OriginalDocumentId == 0)
                        {
                            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                            AuditTrail auditTrailBook = new(createdBy, $"Voided rr# {model.ReceivingReportNo}", "Receiving Report", ipAddress!);
                            await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                        }

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Receiving Report has been Voided.";
                    }
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(Index));
                }
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        public async Task<IActionResult> Cancel(int id, string cancellationRemarks, CancellationToken cancellationToken)
        {
            var model = await _dbContext.ReceivingReports.FirstOrDefaultAsync(x => x.ReceivingReportId == id, cancellationToken);

            if (model != null)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                var createdBy = !model.OriginalSeriesNumber.IsNullOrEmpty() && model.OriginalDocumentId != 0 ? model.CanceledBy : await _generalRepo.GetUserFullNameAsync(User.Identity!.Name!);
                var date = !model.OriginalSeriesNumber.IsNullOrEmpty() && model.OriginalDocumentId != 0 ? model.CanceledDate : DateTime.Now;
                try
                {
                    if (!model.IsCanceled)
                    {
                        model.IsCanceled = true;
                        model.CanceledBy = createdBy;
                        model.CanceledDate = date;
                        model.CanceledQuantity = model.QuantityDelivered < model.QuantityReceived ? model.QuantityDelivered : model.QuantityReceived;
                        model.QuantityDelivered = 0;
                        model.QuantityReceived = 0;
                        model.CancellationRemarks = cancellationRemarks;

                        #region --Audit Trail Recording

                        if (model.OriginalSeriesNumber.IsNullOrEmpty() && model.OriginalDocumentId == 0)
                        {
                            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                            AuditTrail auditTrailBook = new(createdBy, $"Cancelled rr# {model.ReceivingReportNo}", "Receiving Report", ipAddress!);
                            await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                        }

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Receiving Report has been Cancelled.";
                    }
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(Index));
                }
            }

            return NotFound();
        }

        [HttpGet]
        public async Task<IActionResult> GetLiquidations(int id, CancellationToken cancellationToken)
        {
            var po = await _receivingReportRepo.GetPurchaseOrderAsync(id, cancellationToken);

            var rrPostedOnly = await _dbContext
                .ReceivingReports
                .Where(rr => rr.PONo == po.PurchaseOrderNo && rr.IsPosted)
                .ToListAsync(cancellationToken);

            var rr = await _dbContext
                .ReceivingReports
                .Where(rr => rr.PONo == po.PurchaseOrderNo)
                .ToListAsync(cancellationToken);

            var rrNotPosted = await _dbContext
                .ReceivingReports
                .Where(x => x.PONo == po.PurchaseOrderNo && !x.IsPosted && !x.IsCanceled)
                .ToListAsync(cancellationToken);

            var rrCanceled = await _dbContext
                .ReceivingReports
                .Where(x => x.PONo == po.PurchaseOrderNo && x.IsCanceled)
                .ToListAsync(cancellationToken);

            if (po.PurchaseOrderId != 0)
            {
                return Json(new
                {
                    poNo = po.PurchaseOrderNo,
                    poQuantity = po.Quantity.ToString("N2"),
                    rrList = rr,
                    rrListPostedOnly = rrPostedOnly,
                    rrListNotPosted = rrNotPosted,
                    rrListCanceled = rrCanceled
                });
            }

            return Json(null);
        }

        [HttpPost]
        public async Task<IActionResult> GetReceivingReportList(CancellationToken cancellationToken)
        {
            try
            {
                var receivingReports = await _receivingReportRepo.GetReceivingReportsAsync(cancellationToken);

                return Json(new
                {
                    data = receivingReports
                });
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        //Download as .xlsx file.(Export)
        #region -- export xlsx record --

        [HttpPost]
        public async Task<IActionResult> Export(string selectedRecord, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(selectedRecord))
            {
                // Handle the case where no invoices are selected
                return RedirectToAction(nameof(Index));
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
		    {
                var recordIds = selectedRecord.Split(',').Select(int.Parse).ToList();

                // Retrieve the selected records from the database
                var selectedList = await _dbContext.ReceivingReports
                    .Where(rr => recordIds.Contains(rr.ReceivingReportId))
                    .Include(rr => rr.PurchaseOrder)
                    .OrderBy(rr => rr.ReceivingReportNo)
                    .ToListAsync(cancellationToken: cancellationToken);

                // Create the Excel package
                using var package = new ExcelPackage();
                // Add a new worksheet to the Excel package
                #region -- Purchase Order Table Header --

                var worksheet2 = package.Workbook.Worksheets.Add("PurchaseOrder");

                worksheet2.Cells["A1"].Value = "Date";
                worksheet2.Cells["B1"].Value = "Terms";
                worksheet2.Cells["C1"].Value = "Quantity";
                worksheet2.Cells["D1"].Value = "Price";
                worksheet2.Cells["E1"].Value = "Amount";
                worksheet2.Cells["F1"].Value = "FinalPrice";
                worksheet2.Cells["G1"].Value = "QuantityReceived";
                worksheet2.Cells["H1"].Value = "IsReceived";
                worksheet2.Cells["I1"].Value = "ReceivedDate";
                worksheet2.Cells["J1"].Value = "Remarks";
                worksheet2.Cells["K1"].Value = "CreatedBy";
                worksheet2.Cells["L1"].Value = "CreatedDate";
                worksheet2.Cells["M1"].Value = "IsClosed";
                worksheet2.Cells["N1"].Value = "CancellationRemarks";
                worksheet2.Cells["O1"].Value = "OriginalProductId";
                worksheet2.Cells["P1"].Value = "OriginalPONo";
                worksheet2.Cells["Q1"].Value = "OriginalSupplierId";
                worksheet2.Cells["R1"].Value = "OriginalDocumentId";
                worksheet2.Cells["S1"].Value = "EditedBy";
                worksheet2.Cells["T1"].Value = "EditedDate";
                worksheet2.Cells["U1"].Value = "CanceledBy";
                worksheet2.Cells["V1"].Value = "CanceledDate";
                worksheet2.Cells["W1"].Value = "VoidedBy";
                worksheet2.Cells["X1"].Value = "VoidedDate";

                #endregion -- Purchase Order Table Header --

                #region -- Receving Report Table Header --

                var worksheet = package.Workbook.Worksheets.Add("ReceivingReport");

                worksheet.Cells["A1"].Value = "Date";
                worksheet.Cells["B1"].Value = "DueDate";
                worksheet.Cells["C1"].Value = "SupplierInvoiceNumber";
                worksheet.Cells["D1"].Value = "SupplierInvoiceDate";
                worksheet.Cells["E1"].Value = "TruckOrVessels";
                worksheet.Cells["F1"].Value = "QuantityDelivered";
                worksheet.Cells["G1"].Value = "QuantityReceived";
                worksheet.Cells["H1"].Value = "GainOrLoss";
                worksheet.Cells["I1"].Value = "Amount";
                worksheet.Cells["J1"].Value = "OtherRef";
                worksheet.Cells["K1"].Value = "Remarks";
                worksheet.Cells["L1"].Value = "AmountPaid";
                worksheet.Cells["M1"].Value = "IsPaid";
                worksheet.Cells["N1"].Value = "PaidDate";
                worksheet.Cells["O1"].Value = "CanceledQuantity";
                worksheet.Cells["P1"].Value = "CreatedBy";
                worksheet.Cells["Q1"].Value = "CreatedDate";
                worksheet.Cells["R1"].Value = "CancellationRemarks";
                worksheet.Cells["S1"].Value = "ReceivedDate";
                worksheet.Cells["T1"].Value = "OriginalPOId";
                worksheet.Cells["U1"].Value = "OriginalRRNo";
                worksheet.Cells["V1"].Value = "OriginalDocumentId";
                worksheet.Cells["W1"].Value = "EditedBy";
                worksheet.Cells["X1"].Value = "EditedDate";
                worksheet.Cells["Y1"].Value = "CanceledBy";
                worksheet.Cells["Z1"].Value = "CanceledDate";
                worksheet.Cells["AA1"].Value = "VoidedBy";
                worksheet.Cells["AB1"].Value = "VoidedDate";

                #endregion -- Receving Report Table Header --

                #region -- Receiving Report Export --

                int row = 2;

                foreach (var item in selectedList)
                {
                    worksheet.Cells[row, 1].Value = item.Date.ToString("yyyy-MM-dd");
                    worksheet.Cells[row, 2].Value = item.DueDate.ToString("yyyy-MM-dd");
                    worksheet.Cells[row, 3].Value = item.SupplierInvoiceNumber;
                    worksheet.Cells[row, 4].Value = item.SupplierInvoiceDate;
                    worksheet.Cells[row, 5].Value = item.TruckOrVessels;
                    worksheet.Cells[row, 6].Value = item.QuantityDelivered;
                    worksheet.Cells[row, 7].Value = item.QuantityReceived;
                    worksheet.Cells[row, 8].Value = item.GainOrLoss;
                    worksheet.Cells[row, 9].Value = item.Amount;
                    worksheet.Cells[row, 10].Value = item.OtherRef;
                    worksheet.Cells[row, 11].Value = item.Remarks;
                    worksheet.Cells[row, 12].Value = item.AmountPaid;
                    worksheet.Cells[row, 13].Value = item.IsPaid;
                    worksheet.Cells[row, 14].Value = item.PaidDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                    worksheet.Cells[row, 15].Value = item.CanceledQuantity;
                    worksheet.Cells[row, 16].Value = item.CreatedBy;
                    worksheet.Cells[row, 17].Value = item.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                    worksheet.Cells[row, 18].Value = item.CancellationRemarks;
                    worksheet.Cells[row, 19].Value = item.ReceivedDate?.ToString("yyyy-MM-dd");
                    worksheet.Cells[row, 20].Value = item.POId;
                    worksheet.Cells[row, 21].Value = item.ReceivingReportNo;
                    worksheet.Cells[row, 22].Value = item.ReceivingReportId;
                    worksheet.Cells[row, 23].Value = item.EditedBy;
                    worksheet.Cells[row, 24].Value = item.EditedDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                    worksheet.Cells[row, 25].Value = item.CanceledBy;
                    worksheet.Cells[row, 26].Value = item.CanceledDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                    worksheet.Cells[row, 27].Value = item.VoidedBy;
                    worksheet.Cells[row, 28].Value = item.VoidedDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff");

                    row++;
                }

                #endregion -- Receiving Report Export --

                #region -- Purchase Order Export --

                int poRow = 2;

                foreach (var item in selectedList)
                {
                    if (item.PurchaseOrder == null)
                    {
                        continue;
                    }
                    worksheet2.Cells[poRow, 1].Value = item.PurchaseOrder.Date.ToString("yyyy-MM-dd");
                    worksheet2.Cells[poRow, 2].Value = item.PurchaseOrder.Terms;
                    worksheet2.Cells[poRow, 3].Value = item.PurchaseOrder.Quantity;
                    worksheet2.Cells[poRow, 4].Value = item.PurchaseOrder.Price;
                    worksheet2.Cells[poRow, 5].Value = item.PurchaseOrder.Amount;
                    worksheet2.Cells[poRow, 6].Value = item.PurchaseOrder.FinalPrice;
                    worksheet2.Cells[poRow, 7].Value = item.PurchaseOrder.QuantityReceived;
                    worksheet2.Cells[poRow, 8].Value = item.PurchaseOrder.IsReceived;
                    worksheet2.Cells[poRow, 9].Value = item.PurchaseOrder.ReceivedDate != default
                        ? item.PurchaseOrder.ReceivedDate.ToString("yyyy-MM-dd HH:mm:ss.ffffff zzz")
                        : null;
                    worksheet2.Cells[poRow, 10].Value = item.PurchaseOrder.Remarks;
                    worksheet2.Cells[poRow, 11].Value = item.PurchaseOrder.CreatedBy;
                    worksheet2.Cells[poRow, 12].Value =
                        item.PurchaseOrder.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                    worksheet2.Cells[poRow, 13].Value = item.PurchaseOrder.IsClosed;
                    worksheet2.Cells[poRow, 14].Value = item.PurchaseOrder.CancellationRemarks;
                    worksheet2.Cells[poRow, 15].Value = item.PurchaseOrder.ProductId;
                    worksheet2.Cells[poRow, 16].Value = item.PurchaseOrder.PurchaseOrderNo;
                    worksheet2.Cells[poRow, 17].Value = item.PurchaseOrder.SupplierId;
                    worksheet2.Cells[poRow, 18].Value = item.PurchaseOrder.PurchaseOrderId;
                    worksheet2.Cells[poRow, 19].Value = item.PurchaseOrder.EditedBy;
                    worksheet2.Cells[poRow, 20].Value = item.PurchaseOrder.EditedDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                    worksheet2.Cells[poRow, 21].Value = item.PurchaseOrder.CanceledBy;
                    worksheet2.Cells[poRow, 22].Value = item.PurchaseOrder.CanceledDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                    worksheet2.Cells[poRow, 23].Value = item.PurchaseOrder.VoidedBy;
                    worksheet2.Cells[poRow, 24].Value = item.PurchaseOrder.VoidedDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff");

                    poRow++;
                }

                #endregion -- Purchase Order Export --

                // Convert the Excel package to a byte array
                var excelBytes = await package.GetAsByteArrayAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"ReceivingReportList_IBS-RCD_{DateTime.Now:yyyyddMMHHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index), new { view = DynamicView.BankAccount });
            }
        }

        #endregion -- export xlsx record --

        //Upload as .xlsx file.(Import)
        #region -- import xlsx record from IBS --

        [HttpPost]
        public async Task<IActionResult> Import(IFormFile file, CancellationToken cancellationToken)
        {
            if (file.Length == 0)
            {
                TempData["error"] = "The Excel file length is zero!.";
                return RedirectToAction(nameof(Index), new { view = DynamicView.ReceivingReport });
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            var timer = Stopwatch.StartNew();
            try
            {
                await using var stream = file.OpenReadStream();
                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "ReceivingReport");

                var worksheet2 = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "PurchaseOrder");

                if (worksheet == null)
                {
                    TempData["error"] = "The Excel file contains no worksheets.";
                    return RedirectToAction(nameof(Index), new { view = DynamicView.ReceivingReport });
                }
                if (worksheet.ToString() != nameof(DynamicView.ReceivingReport))
                {
                    TempData["error"] = "The Excel file is not related to receiving report.";
                    return RedirectToAction(nameof(Index), new { view = DynamicView.ReceivingReport });
                }

                if (worksheet2 != null)
                {
                    var rows = _purchaseOrderRepo.ParseWorksheet(worksheet2);
                    var lookup = await _purchaseOrderRepo.BuildLookupPurchaseOrderContextAsync(rows, cancellationToken);

                    var purchaseOrders = new List<PurchaseOrder>();
                    var auditTrails = new List<AuditTrail>();
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    var checkingDuplicateSeriesNo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var row in rows)
                    {
                        if (!lookup.ExistingPurchaseOrder.TryGetValue(row.OriginalSeriesNumber, out var existing))
                        {
                            if (!checkingDuplicateSeriesNo.Add(row.OriginalSeriesNumber))
                            {
                                continue;
                            }

                            purchaseOrders.Add(_purchaseOrderRepo.MapToPurchaseOrderEntity(row, lookup));
                            auditTrails.AddRange(_purchaseOrderRepo.AuditTrails(row, ipAddress ?? string.Empty));
                        }
                        else
                        {
                            var changes = _purchaseOrderRepo.Detect(existing, row, lookup.ExistingLogs);
                            if (changes.Any())
                            {
                                await _purchaseOrderRepo.LogChangesAsync(
                                    existing.OriginalDocumentId,
                                    changes,
                                    await _generalRepo.GetUserFullNameAsync(User.Identity!.Name!),
                                    existing.PurchaseOrderNo,
                                    "IBS-RCD");
                            }
                        }
                    }

                    _dbContext.PurchaseOrders.AddRange(purchaseOrders);
                    _dbContext.AuditTrails.AddRange(auditTrails);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }

                if (worksheet != null)
                {
                    var rows = _receivingReportRepo.ParseWorksheet(worksheet);
                    var lookup = await _receivingReportRepo.BuildLookupReceivingReportContextAsync(rows, cancellationToken);

                    var receivingReports = new List<ReceivingReport>();
                    var auditTrails = new List<AuditTrail>();
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    var checkingDuplicateSeriesNo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var row in rows)
                    {
                        if (!lookup.ExistingReceivingReport.TryGetValue(row.OriginalSeriesNumber, out var existing))
                        {
                            if (!checkingDuplicateSeriesNo.Add(row.OriginalSeriesNumber))
                            {
                                continue;
                            }

                            receivingReports.Add(_receivingReportRepo.MapToReceivingReportEntity(row, lookup));
                            auditTrails.AddRange(_receivingReportRepo.AuditTrails(row, ipAddress ?? string.Empty));
                        }
                        else
                        {
                            var changes = _receivingReportRepo.Detect(existing, row, lookup.ExistingLogs);
                            if (changes.Any())
                            {
                                await _receivingReportRepo.LogChangesAsync(
                                    existing.OriginalDocumentId,
                                    changes,
                                    await _generalRepo.GetUserFullNameAsync(User.Identity!.Name!),
                                    existing.ReceivingReportNo,
                                    "IBS-RCD");
                            }
                        }
                    }

                    _dbContext.ReceivingReports.AddRange(receivingReports);
                    _dbContext.AuditTrails.AddRange(auditTrails);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }

                var checkChangesOfRecord = await _dbContext.ImportExportLogs
                    .Where(iel => iel.Action == string.Empty).ToListAsync(cancellationToken);
                if (checkChangesOfRecord.Any())
                {
                    TempData["importChanges"] = "";
                }

                await transaction.CommitAsync(cancellationToken);
            }
            catch (OperationCanceledException oce)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = oce.Message;
                return RedirectToAction(nameof(Index), new { view = DynamicView.ReceivingReport });
            }
            catch (InvalidOperationException ioe)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["warning"] = ioe.Message;
                return RedirectToAction(nameof(Index), new { view = DynamicView.ReceivingReport });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index), new { view = DynamicView.ReceivingReport });
            }

            TempData["success"] = $"Uploading Success!{timer.Elapsed}";
            return RedirectToAction(nameof(Index), new { view = DynamicView.ReceivingReport });
        }

        #endregion

        //Upload as .xlsx file.(Import)
        #region -- import xlsx record to AAS --

        [HttpPost]
        public async Task<IActionResult> AasImport(IFormFile file, CancellationToken cancellationToken)
        {
            if (file.Length == 0)
            {
                TempData["error"] = "The Excel file length is zero!.";
                return RedirectToAction(nameof(Index), new { view = DynamicView.ReceivingReport });
            }

            await using var transaction = await _aasDbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                await using var stream = file.OpenReadStream();
                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "ReceivingReport");
                var worksheet2 = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "PurchaseOrder");

                if (worksheet == null)
                {
                    TempData["error"] = "The Excel file contains no worksheets.";
                    return RedirectToAction(nameof(Index), new { view = DynamicView.ReceivingReport });
                }
                if (worksheet.ToString() != nameof(DynamicView.ReceivingReport))
                {
                    TempData["error"] = "The Excel file is not related to receiving report.";
                    return RedirectToAction(nameof(Index), new { view = DynamicView.ReceivingReport });
                }

                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

                #region -- Purchase Order

                if (worksheet2 != null)
                {
                    var rows = _purchaseOrderRepo.ParseWorksheet(worksheet2);
                    var lookup = await _purchaseOrderRepo.BuildLookupPurchaseOrderContextForAasAsync(rows, cancellationToken);

                    var purchaseOrders = new List<PurchaseOrder>();
                    var auditTrails = new List<AuditTrail>();
                    var checkingDuplicateSeriesNo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var row in rows)
                    {
                        if (!lookup.ExistingPurchaseOrder.TryGetValue(row.OriginalSeriesNumber, out var existing))
                        {
                            if (!checkingDuplicateSeriesNo.Add(row.OriginalSeriesNumber))
                            {
                                continue;
                            }

                            purchaseOrders.Add(_purchaseOrderRepo.MapToPurchaseOrderEntity(row, lookup));
                            auditTrails.AddRange(_purchaseOrderRepo.AuditTrails(row, ipAddress ?? string.Empty));
                        }
                        else
                        {
                            var changes = _purchaseOrderRepo.Detect(existing, row, lookup.ExistingLogs);
                            if (changes.Any())
                            {
                                await _purchaseOrderRepo.LogChangesAsync(
                                    existing.OriginalDocumentId,
                                    changes,
                                    await _generalRepo.GetUserFullNameAsync(User.Identity!.Name!),
                                    existing.PurchaseOrderNo,
                                    "AAS");
                            }
                        }
                    }

                    _aasDbContext.PurchaseOrders.AddRange(purchaseOrders);
                    _aasDbContext.AuditTrails.AddRange(auditTrails);
                    await _aasDbContext.SaveChangesAsync(cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }

                #endregion

                #region -- Receiving Report

                var rrRows = _receivingReportRepo.ParseWorksheet(worksheet);
                var rrLookup = await _receivingReportRepo.BuildLookupReceivingReportContextForAasAsync(rrRows, cancellationToken);

                var receivingReports = new List<ReceivingReport>();
                var rrAuditTrails = new List<AuditTrail>();
                var rrCheckingDuplicateSeriesNo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var row in rrRows)
                {
                    if (!rrLookup.ExistingReceivingReport.TryGetValue(row.OriginalSeriesNumber, out var existing))
                    {
                        if (!rrCheckingDuplicateSeriesNo.Add(row.OriginalSeriesNumber))
                        {
                            continue;
                        }

                        receivingReports.Add(_receivingReportRepo.MapToReceivingReportEntity(row, rrLookup));
                        rrAuditTrails.AddRange(_receivingReportRepo.AuditTrails(row, ipAddress ?? string.Empty));
                    }
                    else
                    {
                        var changes = _receivingReportRepo.Detect(existing, row, rrLookup.ExistingLogs);
                        if (changes.Any())
                        {
                            await _receivingReportRepo.LogChangesAsync(
                                existing.OriginalDocumentId,
                                changes,
                                await _generalRepo.GetUserFullNameAsync(User.Identity!.Name!),
                                existing.ReceivingReportNo,
                                "AAS");
                        }
                    }
                }

                _aasDbContext.ReceivingReports.AddRange(receivingReports);
                _aasDbContext.AuditTrails.AddRange(rrAuditTrails);
                await _aasDbContext.SaveChangesAsync(cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                #endregion

                var checkChangesOfRecord = await _dbContext.ImportExportLogs
                    .Where(iel => iel.Action == string.Empty).ToListAsync(cancellationToken);
                if (checkChangesOfRecord.Any())
                {
                    TempData["importChanges"] = "";
                }

                await transaction.CommitAsync(cancellationToken);
            }
            catch (OperationCanceledException oce)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = oce.Message;
                return RedirectToAction(nameof(Index), new { view = DynamicView.ReceivingReport });
            }
            catch (InvalidOperationException ioe)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["warning"] = ioe.Message;
                return RedirectToAction(nameof(Index), new { view = DynamicView.ReceivingReport });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index), new { view = DynamicView.ReceivingReport });
            }

            TempData["success"] = "Uploading Success!";
            return RedirectToAction(nameof(Index), new { view = DynamicView.ReceivingReport });
        }
        #endregion
    }
}
