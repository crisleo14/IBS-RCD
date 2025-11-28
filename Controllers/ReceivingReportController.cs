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

        public async Task<IActionResult> Index(string? view, CancellationToken cancellationToken)
        {
            var rr = await _receivingReportRepo.GetReceivingReportsAsync(cancellationToken);

            if (view == nameof(DynamicView.ReceivingReport))
            {
                return View("ImportExportIndex", rr);
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
            var createdBy = await _generalRepo.GetUserFullNameAsync(User.Identity!.Name!);

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
                    model.PostedDate = DateTime.Now;

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
                var createdBy = await _generalRepo.GetUserFullNameAsync(User.Identity!.Name!);
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
                        model.VoidedDate = DateTime.Now;

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
                var createdBy = await _generalRepo.GetUserFullNameAsync(User.Identity!.Name!);
                try
                {
                    if (!model.IsCanceled)
                    {
                        model.IsCanceled = true;
                        model.CanceledBy = createdBy;
                        model.CanceledDate = DateTime.Now;
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
                return RedirectToAction(nameof(Index));
            }

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream, cancellationToken);
                stream.Position = 0;
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                var createdBy = await _generalRepo.GetUserFullNameAsync(User.Identity!.Name!);
                try
                {
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

                    #region -- Purchase Order Import --

                    var poRowCount = worksheet2?.Dimension?.Rows ?? 0;
                    var poDictionary = new Dictionary<string, bool>();
                    var purchaseOrderList = await _dbContext
                        .PurchaseOrders
                        .ToListAsync(cancellationToken);


                    for (int row = 2; row <= poRowCount; row++)  // Assuming the first row is the header
                    {
                        if (worksheet2 == null || poRowCount == 0)
                        {
                            continue;
                        }
                        var purchaseOrder = new PurchaseOrder
                        {
                            PurchaseOrderNo = worksheet2.Cells[row, 16].Text,
                            Date = DateOnly.TryParse(worksheet2.Cells[row, 1].Text, out DateOnly dueDate) ? dueDate : default,
                            Terms = worksheet2.Cells[row, 2].Text,
                            Quantity = decimal.TryParse(worksheet2.Cells[row, 3].Text, out decimal quantity) ? quantity : 0,
                            Price = decimal.TryParse(worksheet2.Cells[row, 4].Text, out decimal price) ? price : 0,
                            Amount = decimal.TryParse(worksheet2.Cells[row, 5].Text, out decimal amount) ? amount : 0,
                            FinalPrice = decimal.TryParse(worksheet2.Cells[row, 6].Text, out decimal finalPrice) ? finalPrice : 0,
                            // QuantityReceived = decimal.TryParse(worksheet.Cells[row, 7].Text, out decimal quantityReceived) ? quantityReceived : 0,
                            // IsReceived = bool.TryParse(worksheet.Cells[row, 8].Text, out bool isReceived) ? isReceived : default,
                            // ReceivedDate = DateTime.TryParse(worksheet.Cells[row, 9].Text, out DateTime receivedDate) ? receivedDate : default,
                            Remarks = worksheet2.Cells[row, 10].Text,
                            CreatedBy = worksheet2.Cells[row, 11].Text,
                            CreatedDate = DateTime.TryParse(worksheet2.Cells[row, 12].Text, out DateTime createdDate) ? createdDate : default,
                            PostedBy = worksheet2.Cells[row, 19].Text,
                            PostedDate = DateTime.TryParse(worksheet2.Cells[row, 20].Text, out DateTime postedDate) ? postedDate : default,
                            IsClosed = bool.TryParse(worksheet2.Cells[row, 13].Text, out bool isClosed) && isClosed,
                            CancellationRemarks = worksheet2.Cells[row, 14].Text != "" ? worksheet2.Cells[row, 14].Text : null,
                            OriginalProductId = int.TryParse(worksheet2.Cells[row, 15].Text, out int originalProductId) ? originalProductId : 0,
                            OriginalSeriesNumber = worksheet2.Cells[row, 16].Text,
                            OriginalSupplierId = int.TryParse(worksheet2.Cells[row, 17].Text, out int originalSupplierId) ? originalSupplierId : 0,
                            OriginalDocumentId = int.TryParse(worksheet2.Cells[row, 18].Text, out int originalDocumentId) ? originalDocumentId : 0,
                        };

                        if (!poDictionary.TryAdd(purchaseOrder.OriginalSeriesNumber, true))
                        {
                            continue;
                        }

                        if (purchaseOrderList.Any(po => po.OriginalDocumentId == purchaseOrder.OriginalDocumentId))
                        {
                            var poChanges = new Dictionary<string, (string OriginalValue, string NewValue)>();
                            var existingPo = await _dbContext.PurchaseOrders.FirstOrDefaultAsync(si => si.OriginalDocumentId == purchaseOrder.OriginalDocumentId, cancellationToken);
                            var existingPoInLogs = await _dbContext.ImportExportLogs
                                .Where(x => x.DocumentNo == existingPo.PurchaseOrderNo)
                                .ToListAsync(cancellationToken);

                            if (existingPo!.PurchaseOrderNo!.Trim().ReplaceLineEndings(" ") != worksheet2.Cells[row, 16].Text.Trim().ReplaceLineEndings(" "))
                            {
                                var originalValue = existingPo.PurchaseOrderNo.Trim().ReplaceLineEndings(" ");
                                var adjustedValue = worksheet2.Cells[row, 16].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingPoInLogs
                                    .Where(x => x.ColumnName == "PONo" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    poChanges["PONo"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingPo.Date.ToString("yyyy-MM-dd").Trim().ReplaceLineEndings(" ") != worksheet2.Cells[row, 1].Text.Trim().ReplaceLineEndings(" "))
                            {
                                var originalValue = existingPo.Date.ToString("yyyy-MM-dd").Trim().ReplaceLineEndings(" ");
                                var adjustedValue = worksheet2.Cells[row, 1].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingPoInLogs
                                    .Where(x => x.ColumnName == "Date" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    poChanges["Date"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingPo.Terms.Trim().ReplaceLineEndings(" ") != worksheet2.Cells[row, 2].Text.Trim().ReplaceLineEndings(" "))
                            {
                                var originalValue = existingPo.Terms.Trim().ReplaceLineEndings(" ");
                                var adjustedValue = worksheet2.Cells[row, 2].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingPoInLogs
                                    .Where(x => x.ColumnName == "Terms" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    poChanges["Terms"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingPo.Quantity.ToString("F2").Trim().ReplaceLineEndings(" ") != decimal.Parse(worksheet2.Cells[row, 3].Text).ToString("F2").Trim().ReplaceLineEndings(" "))
                            {
                                var originalValue = existingPo.Quantity.ToString("F2").Trim().ReplaceLineEndings(" ");
                                var adjustedValue = decimal.Parse(worksheet2.Cells[row, 3].Text).ToString("F2").Trim().ReplaceLineEndings(" ");
                                var find  = existingPoInLogs
                                    .Where(x => x.ColumnName == "Quantity" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    poChanges["Quantity"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingPo.Price.ToString("F2").Trim().ReplaceLineEndings(" ") != decimal.Parse(worksheet2.Cells[row, 4].Text).ToString("F2").Trim().ReplaceLineEndings(" "))
                            {
                                var originalValue = existingPo.Price.ToString("F2").Trim().ReplaceLineEndings(" ");
                                var adjustedValue = decimal.Parse(worksheet2.Cells[row, 4].Text).ToString("F2").Trim().ReplaceLineEndings(" ");
                                var find  = existingPoInLogs
                                    .Where(x => x.ColumnName == "Price" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    poChanges["Price"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingPo.Amount.ToString("F2").Trim().ReplaceLineEndings(" ") != decimal.Parse(worksheet2.Cells[row, 5].Text).ToString("F2").Trim().ReplaceLineEndings(" "))
                            {
                                var originalValue = existingPo.Amount.ToString("F2").Trim().ReplaceLineEndings(" ");
                                var adjustedValue = decimal.Parse(worksheet2.Cells[row, 5].Text).ToString("F2").Trim().ReplaceLineEndings(" ");
                                var find  = existingPoInLogs
                                    .Where(x => x.ColumnName == "Amount" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    poChanges["Amount"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingPo.FinalPrice?.ToString("F2").Trim().ReplaceLineEndings(" ") != decimal.Parse(worksheet2.Cells[row, 6].Text).ToString("F2").Trim().ReplaceLineEndings(" "))
                            {
                                var originalValue = existingPo.FinalPrice?.ToString("F2").Trim().ReplaceLineEndings(" ");
                                var adjustedValue = decimal.Parse(worksheet2.Cells[row, 6].Text).ToString("F2").Trim().ReplaceLineEndings(" ");
                                var find  = existingPoInLogs
                                    .Where(x => x.ColumnName == "FinalPrice" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    poChanges["FinalPrice"] = (originalValue, adjustedValue)!;
                                }
                            }

                            if (existingPo.Remarks.Trim().ReplaceLineEndings(" ") != worksheet2.Cells[row, 10].Text.Trim().ReplaceLineEndings(" "))
                            {
                                var originalValue = existingPo.Remarks.Trim().ReplaceLineEndings(" ");
                                var adjustedValue = worksheet2.Cells[row, 10].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingPoInLogs
                                    .Where(x => x.ColumnName == "Remarks" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    poChanges["Remarks"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingPo.CreatedBy!.Trim().ReplaceLineEndings(" ") != worksheet2.Cells[row, 11].Text.Trim().ReplaceLineEndings(" "))
                            {
                                var originalValue = existingPo.CreatedBy.Trim().ReplaceLineEndings(" ");
                                var adjustedValue = worksheet2.Cells[row, 11].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingPoInLogs
                                    .Where(x => x.ColumnName == "CreatedBy" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    poChanges["CreatedBy"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingPo.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff").Trim().ReplaceLineEndings(" ") != worksheet2.Cells[row, 12].Text.Trim().ReplaceLineEndings(" "))
                            {
                                var originalValue = existingPo.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff").Trim().ReplaceLineEndings(" ");
                                var adjustedValue = worksheet2.Cells[row, 12].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingPoInLogs
                                    .Where(x => x.ColumnName == "CreatedDate" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    poChanges["CreatedDate"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingPo.IsClosed.ToString().ToUpper().Trim().ReplaceLineEndings(" ") != worksheet2.Cells[row, 13].Text.Trim().ReplaceLineEndings(" "))
                            {
                                var originalValue = existingPo.IsClosed.ToString().ToUpper().Trim().ReplaceLineEndings(" ");
                                var adjustedValue = worksheet2.Cells[row, 13].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingPoInLogs
                                    .Where(x => x.ColumnName == "IsClosed" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    poChanges["IsClosed"] = (originalValue, adjustedValue);
                                }
                            }

                            if ((string.IsNullOrWhiteSpace(existingPo.CancellationRemarks?.Trim().ReplaceLineEndings(" ")) ? "" : existingPo.CancellationRemarks.Trim().ReplaceLineEndings(" ")) != worksheet2.Cells[row, 14].Text.Trim().ReplaceLineEndings(" "))
                            {
                                var originalValue = existingPo.CancellationRemarks?.Trim().ReplaceLineEndings(" ");
                                var adjustedValue = worksheet2.Cells[row, 14].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingPoInLogs
                                    .Where(x => x.ColumnName == "CancellationRemarks" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    poChanges["CancellationRemarks"] = (originalValue, adjustedValue)!;
                                }
                            }

                            if (existingPo.OriginalProductId.ToString()!.Trim().ReplaceLineEndings(" ") != (worksheet2.Cells[row, 15].Text.Trim().ReplaceLineEndings(" ") == "" ? 0.ToString() : worksheet2.Cells[row, 15].Text.Trim().ReplaceLineEndings(" ")))
                            {
                                var originalValue = existingPo.OriginalProductId.ToString()!.Trim().ReplaceLineEndings(" ");
                                var adjustedValue = worksheet2.Cells[row, 15].Text.Trim().ReplaceLineEndings(" ") == ""
                                    ? 0.ToString()
                                    : worksheet2.Cells[row, 15].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingPoInLogs
                                    .Where(x => x.ColumnName == "OriginalProductId" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    poChanges["OriginalProductId"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingPo.OriginalSeriesNumber!.Trim().ReplaceLineEndings(" ") != worksheet2.Cells[row, 16].Text.Trim().ReplaceLineEndings(" "))
                            {
                                var originalValue = existingPo.OriginalSeriesNumber.Trim().ReplaceLineEndings(" ");
                                var adjustedValue = worksheet2.Cells[row, 16].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingPoInLogs
                                    .Where(x => x.ColumnName == "OriginalSeriesNumber" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    poChanges["OriginalSeriesNumber"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingPo.OriginalSupplierId.ToString()!.Trim().ReplaceLineEndings(" ") != (worksheet2.Cells[row, 17].Text.Trim().ReplaceLineEndings(" ") == "" ? 0.ToString() : worksheet2.Cells[row, 17].Text.Trim().ReplaceLineEndings(" ")))
                            {
                                var originalValue = existingPo.SupplierId.ToString()!.Trim().ReplaceLineEndings(" ");
                                var adjustedValue = worksheet2.Cells[row, 17].Text.Trim().ReplaceLineEndings(" ") == ""
                                    ? 0.ToString()
                                    : worksheet2.Cells[row, 17].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingPoInLogs
                                    .Where(x => x.ColumnName == "SupplierId" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    poChanges["SupplierId"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingPo.OriginalDocumentId.ToString().Trim().ReplaceLineEndings(" ") != (worksheet2.Cells[row, 18].Text.Trim().ReplaceLineEndings(" ") == "" ? 0.ToString() : worksheet2.Cells[row, 18].Text.Trim().ReplaceLineEndings(" ")))
                            {
                                var originalValue = existingPo.OriginalDocumentId.ToString().Trim().ReplaceLineEndings(" ");
                                var adjustedValue = worksheet2.Cells[row, 18].Text.Trim().ReplaceLineEndings(" ") == ""
                                    ? 0.ToString()
                                    : worksheet2.Cells[row, 18].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingPoInLogs
                                    .Where(x => x.ColumnName == "OriginalDocumentId" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    poChanges["OriginalDocumentId"] = (originalValue, adjustedValue);
                                }
                            }

                            if (poChanges.Any())
                            {
                                await _purchaseOrderRepo.LogChangesAsync(existingPo.OriginalDocumentId, poChanges, createdBy, existingPo.PurchaseOrderNo, "IBS-RCD");
                            }

                            continue;
                        }
                        else
                        {
                            #region --Audit Trail Recording

                            if (!purchaseOrder.CreatedBy.IsNullOrEmpty())
                            {
                                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                                AuditTrail auditTrailBook = new(purchaseOrder.CreatedBy, $"Create new purchase order# {purchaseOrder.PurchaseOrderNo}", "Purchase Order", ipAddress!, purchaseOrder.CreatedDate);
                                await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                            }
                            if (!purchaseOrder.PostedBy.IsNullOrEmpty())
                            {
                                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                                AuditTrail auditTrailBook = new(purchaseOrder.PostedBy, $"Posted purchase order# {purchaseOrder.PurchaseOrderNo}", "Purchase Order", ipAddress!, purchaseOrder.PostedDate);
                                await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                            }

                            #endregion --Audit Trail Recording
                        }

                        var getProduct = await _dbContext.Products
                            .Where(p => p.OriginalProductId == purchaseOrder.OriginalProductId)
                            .FirstOrDefaultAsync(cancellationToken);

                        if (getProduct != null)
                        {
                            purchaseOrder.ProductId = getProduct.ProductId;

                            purchaseOrder.ProductNo = getProduct.ProductCode;
                        }
                        else
                        {
                            throw new InvalidOperationException("Please upload the Excel file for the product master file first.");
                        }

                        var getSupplier = await _dbContext.Suppliers
                            .Where(c => c.OriginalSupplierId == purchaseOrder.OriginalSupplierId)
                            .FirstOrDefaultAsync(cancellationToken);

                        if (getSupplier != null)
                        {
                            purchaseOrder.SupplierId = getSupplier.SupplierId;

                            purchaseOrder.SupplierNo = getSupplier.Number;
                        }
                        else
                        {
                            throw new InvalidOperationException("Please upload the Excel file for the supplier master file first.");
                        }

                        await _dbContext.PurchaseOrders.AddAsync(purchaseOrder, cancellationToken);
                    }

                    await _dbContext.SaveChangesAsync(cancellationToken);

                    #endregion -- Purchase Order Import --

                    #region -- Receiving Report Import --

                    var rowCount = worksheet.Dimension.Rows;
                    var rrDictionary = new Dictionary<string, bool>();
                    var receivingReportList = await _dbContext
                        .ReceivingReports
                        .ToListAsync(cancellationToken);
                    for (int row = 2; row <= rowCount; row++)  // Assuming the first row is the header
                    {
                        var receivingReport = new ReceivingReport
                        {
                            ReceivingReportNo = worksheet.Cells[row, 21].Text,
                            Date = DateOnly.TryParse(worksheet.Cells[row, 1].Text, out DateOnly date) ? date : default,
                            DueDate = DateOnly.TryParse(worksheet.Cells[row, 2].Text, out DateOnly dueDate) ? dueDate : default,
                            SupplierInvoiceNumber = worksheet.Cells[row, 3].Text != "" ? worksheet.Cells[row, 3].Text : null,
                            SupplierInvoiceDate = worksheet.Cells[row, 4].Text,
                            TruckOrVessels = worksheet.Cells[row, 5].Text,
                            QuantityDelivered = decimal.TryParse(worksheet.Cells[row, 6].Text, out decimal quantityDelivered) ? quantityDelivered : 0,
                            QuantityReceived = decimal.TryParse(worksheet.Cells[row, 7].Text, out decimal quantityReceived) ? quantityReceived : 0,
                            GainOrLoss = decimal.TryParse(worksheet.Cells[row, 8].Text, out decimal gainOrLoss) ? gainOrLoss : 0,
                            Amount = decimal.TryParse(worksheet.Cells[row, 9].Text, out decimal amount) ? amount : 0,
                            OtherRef = worksheet.Cells[row, 10].Text != "" ? worksheet.Cells[row, 10].Text : null,
                            Remarks = worksheet.Cells[row, 11].Text,
                            // AmountPaid = decimal.TryParse(worksheet.Cells[row, 12].Text, out decimal amountPaid) ? amountPaid : 0,
                            // IsPaid = bool.TryParse(worksheet.Cells[row, 13].Text, out bool IsPaid) ? IsPaid : default,
                            // PaidDate = DateTime.TryParse(worksheet.Cells[row, 14].Text, out DateTime paidDate) ? paidDate : DateTime.MinValue,
                            // CanceledQuantity = decimal.TryParse(worksheet.Cells[row, 15].Text, out decimal netAmountOfEWT) ? netAmountOfEWT : 0,
                            CreatedBy = worksheet.Cells[row, 16].Text,
                            CreatedDate = DateTime.TryParse(worksheet.Cells[row, 17].Text, out DateTime createdDate) ? createdDate : default,
                            PostedBy = worksheet.Cells[row, 23].Text,
                            PostedDate = DateTime.TryParse(worksheet.Cells[row, 24].Text, out DateTime postedDate) ? postedDate : default,
                            CancellationRemarks = worksheet.Cells[row, 18].Text != "" ? worksheet.Cells[row, 18].Text : null,
                            ReceivedDate = DateOnly.TryParse(worksheet.Cells[row, 19].Text, out DateOnly receivedDate) ? receivedDate : default,
                            OriginalPOId = int.TryParse(worksheet.Cells[row, 20].Text, out int originalPoId) ? originalPoId : 0,
                            OriginalSeriesNumber = worksheet.Cells[row, 21].Text,
                            OriginalDocumentId = int.TryParse(worksheet.Cells[row, 22].Text, out int originalDocumentId) ? originalDocumentId : 0,
                        };

                        if (!rrDictionary.TryAdd(receivingReport.OriginalSeriesNumber, true))
                        {
                            continue;
                        }

                        //Checking for duplicate record
                        if (receivingReportList.Any(rr => rr.OriginalDocumentId == receivingReport.OriginalDocumentId))
                        {
                            var rrChanges = new Dictionary<string, (string OriginalValue, string NewValue)>();
                            var existingRr = await _dbContext.ReceivingReports.FirstOrDefaultAsync(rr => rr.OriginalDocumentId == receivingReport.OriginalDocumentId, cancellationToken);
                            var existingRrInLogs = await _dbContext.ImportExportLogs
                                .Where(x => x.DocumentNo == existingRr.ReceivingReportNo)
                                .ToListAsync(cancellationToken);

                            if (existingRr!.ReceivingReportNo!.Trim().ReplaceLineEndings(" ") != worksheet.Cells[row, 21].Text.Trim().ReplaceLineEndings(" "))
                            {
                                var originalValue = existingRr.ReceivingReportNo.Trim().ReplaceLineEndings(" ");
                                var adjustedValue = worksheet.Cells[row, 21].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingRrInLogs
                                    .Where(x => x.ColumnName == "RRNo" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["RRNo"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingRr.Date.ToString("yyyy-MM-dd").Trim().ReplaceLineEndings(" ") != worksheet.Cells[row, 1].Text.Trim().ReplaceLineEndings(" "))
                            {
                                var originalValue = existingRr.Date.ToString("yyyy-MM-dd").Trim().ReplaceLineEndings(" ");
                                var adjustedValue = worksheet.Cells[row, 1].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingRrInLogs
                                    .Where(x => x.ColumnName == "Date" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["Date"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingRr.PurchaseOrder?.OriginalDocumentId.ToString().Trim() != worksheet.Cells[row, 20].Text.Trim())
                            {
                                var originalValue = existingRr.PurchaseOrder?.OriginalDocumentId.ToString().Trim();
                                var adjustedValue = worksheet.Cells[row, 20].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingRrInLogs
                                    .Where(x => x.ColumnName == "SuppliersPoId" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["SuppliersPoId"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingRr.DueDate.ToString("yyyy-MM-dd").Trim().ReplaceLineEndings(" ") != worksheet.Cells[row, 2].Text.Trim().ReplaceLineEndings(" "))
                            {
                                var originalValue = existingRr.DueDate.ToString("yyyy-MM-dd").Trim().ReplaceLineEndings(" ");
                                var adjustedValue = worksheet.Cells[row, 2].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingRrInLogs
                                    .Where(x => x.ColumnName == "DueDate" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["DueDate"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingRr.SupplierInvoiceNumber?.Trim().ReplaceLineEndings(" ") != (worksheet.Cells[row, 3].Text.Trim().ReplaceLineEndings(" ") == "" ? null : worksheet.Cells[row, 3].Text.Trim().ReplaceLineEndings(" ")))
                            {
                                var originalValue = existingRr.SupplierInvoiceNumber?.Trim().ReplaceLineEndings(" ");
                                var adjustedValue = worksheet.Cells[row, 3].Text.Trim().ReplaceLineEndings(" ") == ""
                                    ? null
                                    : worksheet.Cells[row, 3].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingRrInLogs
                                    .Where(x => x.ColumnName == "SupplierInvoiceNumber" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["SupplierInvoiceNumber"] = (originalValue, adjustedValue);
                                }
                            }

                            if ((string.IsNullOrWhiteSpace(existingRr.SupplierInvoiceDate?.Trim().ReplaceLineEndings(" ")) ? null : existingRr.SupplierInvoiceDate?.Trim().ReplaceLineEndings(" ")) != (string.IsNullOrWhiteSpace(worksheet.Cells[row, 4].Text) ? null : worksheet.Cells[row, 4].Text.Trim().ReplaceLineEndings(" ")))
                            {
                                var originalValue = existingRr.SupplierInvoiceDate?.Trim().ReplaceLineEndings(" ");
                                var adjustedValue = worksheet.Cells[row, 4].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingRrInLogs
                                    .Where(x => x.ColumnName == "SupplierInvoiceDate" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["SupplierInvoiceDate"] = (originalValue, adjustedValue)!;
                                }
                            }

                            if (existingRr.TruckOrVessels.Trim().ReplaceLineEndings(" ") != worksheet.Cells[row, 5].Text.Trim().ReplaceLineEndings(" "))
                            {
                                var originalValue = existingRr.TruckOrVessels.Trim().ReplaceLineEndings(" ");
                                var adjustedValue = worksheet.Cells[row, 5].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingRrInLogs
                                    .Where(x => x.ColumnName == "TruckOrVessels" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["TruckOrVessels"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingRr.QuantityDelivered.ToString("F2").Trim().ReplaceLineEndings(" ") != decimal.Parse(worksheet.Cells[row, 6].Text).ToString("F2").Trim().ReplaceLineEndings(" "))
                            {
                                var originalValue = existingRr.QuantityDelivered.ToString("F2").Trim().ReplaceLineEndings(" ");
                                var adjustedValue = decimal.Parse(worksheet.Cells[row, 6].Text).ToString("F2").Trim().ReplaceLineEndings(" ");
                                var find  = existingRrInLogs
                                    .Where(x => x.ColumnName == "QuantityDelivered" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["QuantityDelivered"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingRr.QuantityReceived.ToString("F2").Trim().ReplaceLineEndings(" ") != decimal.Parse(worksheet.Cells[row, 7].Text).ToString("F2").Trim().ReplaceLineEndings(" "))
                            {
                                var originalValue = existingRr.QuantityReceived.ToString("F2").Trim().ReplaceLineEndings(" ");
                                var adjustedValue = decimal.Parse(worksheet.Cells[row, 7].Text).ToString("F2").Trim().ReplaceLineEndings(" ");
                                var find  = existingRrInLogs
                                    .Where(x => x.ColumnName == "QuantityReceived" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["QuantityReceived"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingRr.GainOrLoss.ToString("F2").Trim().ReplaceLineEndings(" ") != decimal.Parse(worksheet.Cells[row, 8].Text).ToString("F2").Trim().ReplaceLineEndings(" "))
                            {
                                var originalValue = existingRr.GainOrLoss.ToString("F2").Trim().ReplaceLineEndings(" ");
                                var adjustedValue = decimal.Parse(worksheet.Cells[row, 8].Text).ToString("F2").Trim().ReplaceLineEndings(" ");
                                var find  = existingRrInLogs
                                    .Where(x => x.ColumnName == "GainOrLoss" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["GainOrLoss"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingRr.Amount.ToString("F2").Trim().ReplaceLineEndings(" ") != decimal.Parse(worksheet.Cells[row, 9].Text).ToString("F2").Trim().ReplaceLineEndings(" "))
                            {
                                var originalValue = existingRr.Amount.ToString("F2").Trim().ReplaceLineEndings(" ");
                                var adjustedValue = decimal.Parse(worksheet.Cells[row, 9].Text).ToString("F2").Trim().ReplaceLineEndings(" ");
                                var find  = existingRrInLogs
                                    .Where(x => x.ColumnName == "Amount" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["Amount"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingRr.OtherRef?.Trim().ReplaceLineEndings(" ") != (worksheet.Cells[row, 10].Text.Trim().ReplaceLineEndings(" ") == "" ? null : worksheet.Cells[row, 10].Text.Trim().ReplaceLineEndings(" ")))
                            {
                                var originalValue = existingRr.OtherRef?.Trim().ReplaceLineEndings(" ");
                                var adjustedValue = worksheet.Cells[row, 10].Text.Trim().ReplaceLineEndings(" ") == ""
                                    ? null
                                    : worksheet.Cells[row, 10].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingRrInLogs
                                    .Where(x => x.ColumnName == "OtherRef" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["OtherRef"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingRr.Remarks.Trim().ReplaceLineEndings(" ") != worksheet.Cells[row, 11].Text.Trim().ReplaceLineEndings(" "))
                            {
                                var originalValue = existingRr.Remarks.Trim().ReplaceLineEndings(" ");
                                var adjustedValue = worksheet.Cells[row, 11].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingRrInLogs
                                    .Where(x => x.ColumnName == "Remarks" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["Remarks"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingRr.CreatedBy?.Trim().ReplaceLineEndings(" ") != worksheet.Cells[row, 16].Text.Trim().ReplaceLineEndings(" "))
                            {
                                var originalValue = existingRr.CreatedBy?.Trim().ReplaceLineEndings(" ");
                                var adjustedValue = worksheet.Cells[row, 16].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingRrInLogs
                                    .Where(x => x.ColumnName == "CreatedBy" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["CreatedBy"] = (originalValue, adjustedValue)!;
                                }
                            }

                            if (existingRr.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff").Trim().ReplaceLineEndings(" ") != worksheet.Cells[row, 17].Text.Trim().ReplaceLineEndings(" "))
                            {
                                var originalValue = existingRr.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff").Trim().ReplaceLineEndings(" ");
                                var adjustedValue = worksheet.Cells[row, 17].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingRrInLogs
                                    .Where(x => x.ColumnName == "CreatedDate" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["CreatedDate"] = (originalValue, adjustedValue);
                                }
                            }

                            if ((string.IsNullOrWhiteSpace(existingRr.CancellationRemarks?.Trim().ReplaceLineEndings(" ")) ? "" : existingRr.CancellationRemarks.Trim().ReplaceLineEndings(" ")) != worksheet.Cells[row, 18].Text.Trim().ReplaceLineEndings(" "))
                            {
                                var originalValue = existingRr.CancellationRemarks?.Trim().ReplaceLineEndings(" ");
                                var adjustedValue = worksheet.Cells[row, 18].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingRrInLogs
                                    .Where(x => x.ColumnName == "CancellationRemarks" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["CancellationRemarks"] = (originalValue, adjustedValue)!;
                                }
                            }

                            if (existingRr.ReceivedDate?.ToString("yyyy-MM-dd").Trim().ReplaceLineEndings(" ") != (worksheet.Cells[row, 19].Text.Trim().ReplaceLineEndings(" ") == "" ? DateOnly.MinValue.ToString("yyyy-MM-dd").Trim().ReplaceLineEndings(" ") : worksheet.Cells[row, 19].Text.Trim().ReplaceLineEndings(" ")))
                            {
                                var originalValue = existingRr.ReceivedDate?.ToString("yyyy-MM-dd").Trim().ReplaceLineEndings(" ");
                                var adjustedValue = worksheet.Cells[row, 19].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingRrInLogs
                                    .Where(x => x.ColumnName == "ReceivedDate" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["ReceivedDate"] = (originalValue, adjustedValue)!;
                                }
                            }

                            if (existingRr.OriginalPOId?.ToString().Trim().ReplaceLineEndings(" ") != (worksheet.Cells[row, 20].Text.Trim().ReplaceLineEndings(" ") == "" ? 0.ToString() : worksheet.Cells[row, 20].Text.Trim().ReplaceLineEndings(" ")))
                            {
                                var originalValue = existingRr.OriginalPOId?.ToString().Trim().ReplaceLineEndings(" ");
                                var adjustedValue = worksheet.Cells[row, 20].Text.Trim().ReplaceLineEndings(" ") == "" ? 0.ToString() : worksheet.Cells[row, 20].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingRrInLogs
                                    .Where(x => x.ColumnName == "OriginalPOId" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["OriginalPOId"] = (originalValue, adjustedValue)!;
                                }
                            }

                            if (existingRr.OriginalSeriesNumber?.Trim().ReplaceLineEndings(" ") != worksheet.Cells[row, 21].Text.Trim().ReplaceLineEndings(" "))
                            {
                                var originalValue = existingRr.OriginalSeriesNumber?.Trim().ReplaceLineEndings(" ");
                                var adjustedValue = worksheet.Cells[row, 21].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingRrInLogs
                                    .Where(x => x.ColumnName == "OriginalSeriesNumber" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["OriginalSeriesNumber"] = (originalValue, adjustedValue)!;
                                }
                            }

                            if (existingRr.OriginalDocumentId.ToString().Trim().ReplaceLineEndings(" ") != (worksheet.Cells[row, 22].Text.Trim().ReplaceLineEndings(" ") == "" ? 0.ToString() : worksheet.Cells[row, 22].Text.Trim().ReplaceLineEndings(" ")))
                            {
                                var originalValue = existingRr.OriginalDocumentId.ToString().Trim().ReplaceLineEndings(" ");
                                var adjustedValue = worksheet.Cells[row, 22].Text.Trim().ReplaceLineEndings(" ") == ""
                                    ? 0.ToString()
                                    : worksheet.Cells[row, 22].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingRrInLogs
                                    .Where(x => x.ColumnName == "OriginalDocumentId" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["OriginalDocumentId"] = (originalValue, adjustedValue);
                                }
                            }

                            if (rrChanges.Any())
                            {
                                await _receivingReportRepo.LogChangesAsync(existingRr.OriginalDocumentId, rrChanges, createdBy, existingRr.ReceivingReportNo, "IBS-RCD");
                            }

                            continue;
                        }
                        else
                        {
                            #region --Audit Trail Recording

                            if (!receivingReport.CreatedBy.IsNullOrEmpty())
                            {
                                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                                AuditTrail auditTrailBook = new(receivingReport.CreatedBy, $"Create new receiving report# {receivingReport.ReceivingReportNo}", "Receiving Report", ipAddress!, receivingReport.CreatedDate);
                                await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                            }
                            if (!receivingReport.PostedBy.IsNullOrEmpty())
                            {
                                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                                AuditTrail auditTrailBook = new(receivingReport.PostedBy, $"Posted receiving report# {receivingReport.ReceivingReportNo}", "Receiving report", ipAddress!, receivingReport.PostedDate);
                                await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                            }

                            #endregion --Audit Trail Recording
                        }

                        var getPo = await _dbContext
                            .PurchaseOrders
                            .Where(po => po.OriginalDocumentId == receivingReport.OriginalPOId)
                            .FirstOrDefaultAsync(cancellationToken);

                        receivingReport.POId = getPo?.PurchaseOrderId;
                        receivingReport.PONo = getPo?.PurchaseOrderNo;

                        await _dbContext.ReceivingReports.AddAsync(receivingReport, cancellationToken);
                    }

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    var checkChangesOfRecord = await _dbContext.ImportExportLogs
                        .Where(iel => iel.Action == string.Empty).ToListAsync(cancellationToken);
                    if (checkChangesOfRecord.Any())
                    {
                        TempData["importChanges"] = "";
                    }

                    #endregion -- Receiving Report Import --
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
            }
            TempData["success"] = "Uploading Success!";
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
                return RedirectToAction(nameof(Index));
            }

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream, cancellationToken);
                stream.Position = 0;
                await using var transaction = await _aasDbContext.Database.BeginTransactionAsync(cancellationToken);
                var createdBy = await _generalRepo.GetUserFullNameAsync(User.Identity!.Name!);
                try
                {
                    if (file.FileName.Contains(CS.Name))
                    {
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

                    #region -- Purchase Order Import --

                    var poRowCount = worksheet2?.Dimension?.Rows ?? 0;
                    var poDictionary = new Dictionary<string, bool>();
                    var purchaseOrderList = await _aasDbContext
                        .PurchaseOrders
                        .ToListAsync(cancellationToken);


                    for (int row = 2; row <= poRowCount; row++)  // Assuming the first row is the header
                    {
                        if (worksheet2 == null || poRowCount == 0)
                        {
                            continue;
                        }
                        var purchaseOrder = new PurchaseOrder
                        {
                            PurchaseOrderNo = worksheet2.Cells[row, 16].Text,
                            Date = DateOnly.TryParse(worksheet2.Cells[row, 1].Text, out DateOnly dueDate) ? dueDate : default,
                            Terms = worksheet2.Cells[row, 2].Text,
                            Quantity = decimal.TryParse(worksheet2.Cells[row, 3].Text, out decimal quantity) ? quantity : 0,
                            Price = decimal.TryParse(worksheet2.Cells[row, 4].Text, out decimal price) ? price : 0,
                            Amount = decimal.TryParse(worksheet2.Cells[row, 5].Text, out decimal amount) ? amount : 0,
                            FinalPrice = decimal.TryParse(worksheet2.Cells[row, 6].Text, out decimal finalPrice) ? finalPrice : 0,
                            // QuantityReceived = decimal.TryParse(worksheet.Cells[row, 7].Text, out decimal quantityReceived) ? quantityReceived : 0,
                            // IsReceived = bool.TryParse(worksheet.Cells[row, 8].Text, out bool isReceived) ? isReceived : default,
                            // ReceivedDate = DateTime.TryParse(worksheet.Cells[row, 9].Text, out DateTime receivedDate) ? receivedDate : default,
                            Remarks = worksheet2.Cells[row, 10].Text,
                            CreatedBy = worksheet2.Cells[row, 11].Text,
                            CreatedDate = DateTime.TryParse(worksheet2.Cells[row, 12].Text, out DateTime createdDate) ? createdDate : default,
                            PostedBy = worksheet2.Cells[row, 19].Text,
                            PostedDate = DateTime.TryParse(worksheet2.Cells[row, 20].Text, out DateTime postedDate) ? postedDate : default,
                            IsClosed = bool.TryParse(worksheet2.Cells[row, 13].Text, out bool isClosed) && isClosed,
                            CancellationRemarks = worksheet2.Cells[row, 14].Text != "" ? worksheet2.Cells[row, 14].Text : null,
                            OriginalProductId = int.TryParse(worksheet2.Cells[row, 15].Text, out int originalProductId) ? originalProductId : 0,
                            OriginalSeriesNumber = worksheet2.Cells[row, 16].Text,
                            OriginalSupplierId = int.TryParse(worksheet2.Cells[row, 17].Text, out int originalSupplierId) ? originalSupplierId : 0,
                            OriginalDocumentId = int.TryParse(worksheet2.Cells[row, 18].Text, out int originalDocumentId) ? originalDocumentId : 0,
                        };

                        if (!poDictionary.TryAdd(purchaseOrder.OriginalSeriesNumber, true))
                        {
                            continue;
                        }

                        if (purchaseOrderList.Any(po => po.OriginalDocumentId == purchaseOrder.OriginalDocumentId))
                        {
                            var poChanges = new Dictionary<string, (string OriginalValue, string NewValue)>();
                            var existingPo = await _aasDbContext.PurchaseOrders.FirstOrDefaultAsync(si => si.OriginalDocumentId == purchaseOrder.OriginalDocumentId, cancellationToken);
                            var existingPoInLogs = await _dbContext.ImportExportLogs
                                .Where(x => x.DocumentNo == existingPo.PurchaseOrderNo)
                                .ToListAsync(cancellationToken);

                            if (existingPo!.PurchaseOrderNo!.Trim().ReplaceLineEndings(" ") != worksheet2.Cells[row, 16].Text.Trim().ReplaceLineEndings(" "))
                            {
                                var originalValue = existingPo.PurchaseOrderNo.Trim().ReplaceLineEndings(" ");
                                var adjustedValue = worksheet2.Cells[row, 16].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingPoInLogs
                                    .Where(x => x.ColumnName == "PONo" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    poChanges["PONo"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingPo.Date.ToString("yyyy-MM-dd").Trim().ReplaceLineEndings(" ") != worksheet2.Cells[row, 1].Text.Trim().ReplaceLineEndings(" "))
                            {
                                var originalValue = existingPo.Date.ToString("yyyy-MM-dd").Trim().ReplaceLineEndings(" ");
                                var adjustedValue = worksheet2.Cells[row, 1].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingPoInLogs
                                    .Where(x => x.ColumnName == "Date" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    poChanges["Date"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingPo.Terms.Trim().ReplaceLineEndings(" ") != worksheet2.Cells[row, 2].Text.Trim().ReplaceLineEndings(" "))
                            {
                                var originalValue = existingPo.Terms.Trim().ReplaceLineEndings(" ");
                                var adjustedValue = worksheet2.Cells[row, 2].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingPoInLogs
                                    .Where(x => x.ColumnName == "Terms" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    poChanges["Terms"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingPo.Quantity.ToString("F2").Trim().ReplaceLineEndings(" ") != decimal.Parse(worksheet2.Cells[row, 3].Text).ToString("F2").Trim().ReplaceLineEndings(" "))
                            {
                                var originalValue = existingPo.Quantity.ToString("F2").Trim().ReplaceLineEndings(" ");
                                var adjustedValue = decimal.Parse(worksheet2.Cells[row, 3].Text).ToString("F2").Trim().ReplaceLineEndings(" ");
                                var find  = existingPoInLogs
                                    .Where(x => x.ColumnName == "Quantity" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    poChanges["Quantity"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingPo.Price.ToString("F2").Trim().ReplaceLineEndings(" ") != decimal.Parse(worksheet2.Cells[row, 4].Text).ToString("F2").Trim().ReplaceLineEndings(" "))
                            {
                                var originalValue = existingPo.Price.ToString("F2").Trim().ReplaceLineEndings(" ");
                                var adjustedValue = decimal.Parse(worksheet2.Cells[row, 4].Text).ToString("F2").Trim().ReplaceLineEndings(" ");
                                var find  = existingPoInLogs
                                    .Where(x => x.ColumnName == "Price" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    poChanges["Price"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingPo.Amount.ToString("F2").Trim().ReplaceLineEndings(" ") != decimal.Parse(worksheet2.Cells[row, 5].Text).ToString("F2").Trim().ReplaceLineEndings(" "))
                            {
                                var originalValue = existingPo.Amount.ToString("F2").Trim().ReplaceLineEndings(" ");
                                var adjustedValue = decimal.Parse(worksheet2.Cells[row, 5].Text).ToString("F2").Trim().ReplaceLineEndings(" ");
                                var find  = existingPoInLogs
                                    .Where(x => x.ColumnName == "Amount" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    poChanges["Amount"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingPo.FinalPrice?.ToString("F2").Trim().ReplaceLineEndings(" ") != decimal.Parse(worksheet2.Cells[row, 6].Text).ToString("F2").Trim().ReplaceLineEndings(" "))
                            {
                                var originalValue = existingPo.FinalPrice?.ToString("F2").Trim().ReplaceLineEndings(" ");
                                var adjustedValue = decimal.Parse(worksheet2.Cells[row, 6].Text).ToString("F2").Trim().ReplaceLineEndings(" ");
                                var find  = existingPoInLogs
                                    .Where(x => x.ColumnName == "FinalPrice" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    poChanges["FinalPrice"] = (originalValue, adjustedValue)!;
                                }
                            }

                            if (existingPo.Remarks.Trim().ReplaceLineEndings(" ") != worksheet2.Cells[row, 10].Text.Trim().ReplaceLineEndings(" "))
                            {
                                var originalValue = existingPo.Remarks.Trim().ReplaceLineEndings(" ");
                                var adjustedValue = worksheet2.Cells[row, 10].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingPoInLogs
                                    .Where(x => x.ColumnName == "Remarks" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    poChanges["Remarks"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingPo.CreatedBy!.Trim().ReplaceLineEndings(" ") != worksheet2.Cells[row, 11].Text.Trim().ReplaceLineEndings(" "))
                            {
                                var originalValue = existingPo.CreatedBy.Trim().ReplaceLineEndings(" ");
                                var adjustedValue = worksheet2.Cells[row, 11].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingPoInLogs
                                    .Where(x => x.ColumnName == "CreatedBy" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    poChanges["CreatedBy"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingPo.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff").Trim().ReplaceLineEndings(" ") != worksheet2.Cells[row, 12].Text.Trim().ReplaceLineEndings(" "))
                            {
                                var originalValue = existingPo.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff").Trim().ReplaceLineEndings(" ");
                                var adjustedValue = worksheet2.Cells[row, 12].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingPoInLogs
                                    .Where(x =>x.ColumnName == "CreatedDate" &&  x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    poChanges["CreatedDate"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingPo.IsClosed.ToString().ToUpper().Trim().ReplaceLineEndings(" ") != worksheet2.Cells[row, 13].Text.Trim().ReplaceLineEndings(" "))
                            {
                                var originalValue = existingPo.IsClosed.ToString().ToUpper().Trim().ReplaceLineEndings(" ");
                                var adjustedValue = worksheet2.Cells[row, 13].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingPoInLogs
                                    .Where(x => x.ColumnName == "IsClosed" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    poChanges["IsClosed"] = (originalValue, adjustedValue);
                                }
                            }

                            if ((string.IsNullOrWhiteSpace(existingPo.CancellationRemarks?.Trim().ReplaceLineEndings(" ")) ? "" : existingPo.CancellationRemarks.Trim().ReplaceLineEndings(" ")) != worksheet2.Cells[row, 14].Text.Trim().ReplaceLineEndings(" "))
                            {
                                var originalValue = existingPo.CancellationRemarks?.Trim().ReplaceLineEndings(" ");
                                var adjustedValue = worksheet2.Cells[row, 14].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingPoInLogs
                                    .Where(x => x.ColumnName == "CancellationRemarks" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    poChanges["CancellationRemarks"] = (originalValue, adjustedValue)!;
                                }
                            }

                            if (existingPo.OriginalProductId.ToString()!.Trim().ReplaceLineEndings(" ") != (worksheet2.Cells[row, 15].Text.Trim().ReplaceLineEndings(" ") == "" ? 0.ToString() : worksheet2.Cells[row, 15].Text.Trim().ReplaceLineEndings(" ")))
                            {
                                var originalValue = existingPo.OriginalProductId.ToString()!.Trim().ReplaceLineEndings(" ");
                                var adjustedValue = worksheet2.Cells[row, 15].Text.Trim().ReplaceLineEndings(" ") == ""
                                    ? 0.ToString()
                                    : worksheet2.Cells[row, 15].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingPoInLogs
                                    .Where(x => x.ColumnName == "OriginalProductId" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    poChanges["OriginalProductId"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingPo.OriginalSeriesNumber!.Trim().ReplaceLineEndings(" ") != worksheet2.Cells[row, 16].Text.Trim().ReplaceLineEndings(" "))
                            {
                                var originalValue = existingPo.OriginalSeriesNumber.Trim().ReplaceLineEndings(" ");
                                var adjustedValue = worksheet2.Cells[row, 16].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingPoInLogs
                                    .Where(x => x.ColumnName == "OriginalSeriesNumber" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    poChanges["OriginalSeriesNumber"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingPo.OriginalSupplierId.ToString()!.Trim().ReplaceLineEndings(" ") != (worksheet2.Cells[row, 17].Text.Trim().ReplaceLineEndings(" ") == "" ? 0.ToString() : worksheet2.Cells[row, 17].Text.Trim().ReplaceLineEndings(" ")))
                            {
                                var originalValue = existingPo.SupplierId.ToString()!.Trim().ReplaceLineEndings(" ");
                                var adjustedValue = worksheet2.Cells[row, 17].Text.Trim().ReplaceLineEndings(" ") == ""
                                    ? 0.ToString()
                                    : worksheet2.Cells[row, 17].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingPoInLogs
                                    .Where(x => x.ColumnName == "SupplierId" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    poChanges["SupplierId"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingPo.OriginalDocumentId.ToString().Trim().ReplaceLineEndings(" ") != (worksheet2.Cells[row, 18].Text.Trim().ReplaceLineEndings(" ") == "" ? 0.ToString() : worksheet2.Cells[row, 18].Text.Trim().ReplaceLineEndings(" ")))
                            {
                                var originalValue = existingPo.OriginalDocumentId.ToString().Trim().ReplaceLineEndings(" ");
                                var adjustedValue = worksheet2.Cells[row, 18].Text.Trim().ReplaceLineEndings(" ") == ""
                                    ? 0.ToString()
                                    : worksheet2.Cells[row, 18].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingPoInLogs
                                    .Where(x => x.ColumnName == "OriginalDocumentId" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    poChanges["OriginalDocumentId"] = (originalValue, adjustedValue);
                                }
                            }

                            if (poChanges.Any())
                            {
                                await _purchaseOrderRepo.LogChangesAsync(existingPo.OriginalDocumentId, poChanges, createdBy, existingPo.PurchaseOrderNo, "AAS");
                            }

                            continue;
                        }
                        else
                        {
                            #region --Audit Trail Recording

                            if (!purchaseOrder.CreatedBy.IsNullOrEmpty())
                            {
                                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                                AuditTrail auditTrailBook = new(purchaseOrder.CreatedBy, $"Create new purchase order# {purchaseOrder.PurchaseOrderNo}", "Purchase Order", ipAddress!, purchaseOrder.CreatedDate);
                                await _aasDbContext.AddAsync(auditTrailBook, cancellationToken);
                            }
                            if (!purchaseOrder.PostedBy.IsNullOrEmpty())
                            {
                                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                                AuditTrail auditTrailBook = new(purchaseOrder.PostedBy, $"Posted purchase order# {purchaseOrder.PurchaseOrderNo}", "Purchase Order", ipAddress!, purchaseOrder.PostedDate);
                                await _aasDbContext.AddAsync(auditTrailBook, cancellationToken);
                            }

                            #endregion --Audit Trail Recording
                        }

                        var getProduct = await _aasDbContext.Products
                            .Where(p => p.OriginalProductId == purchaseOrder.OriginalProductId)
                            .FirstOrDefaultAsync(cancellationToken);

                        if (getProduct != null)
                        {
                            purchaseOrder.ProductId = getProduct.ProductId;

                            purchaseOrder.ProductNo = getProduct.ProductCode;
                        }
                        else
                        {
                            throw new InvalidOperationException("Please upload the Excel file for the product master file first.");
                        }

                        var getSupplier = await _aasDbContext.Suppliers
                            .Where(c => c.OriginalSupplierId == purchaseOrder.OriginalSupplierId)
                            .FirstOrDefaultAsync(cancellationToken);

                        if (getSupplier != null)
                        {
                            purchaseOrder.SupplierId = getSupplier.SupplierId;

                            purchaseOrder.SupplierNo = getSupplier.Number;
                        }
                        else
                        {
                            throw new InvalidOperationException("Please upload the Excel file for the supplier master file first.");
                        }

                        await _aasDbContext.PurchaseOrders.AddAsync(purchaseOrder, cancellationToken);
                    }

                    await _aasDbContext.SaveChangesAsync(cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    #endregion -- Purchase Order Import --

                    #region -- Receiving Report Import --

                    var rowCount = worksheet.Dimension.Rows;
                    var rrDictionary = new Dictionary<string, bool>();
                    var receivingReportList = await _aasDbContext
                        .ReceivingReports
                        .ToListAsync(cancellationToken);
                    for (int row = 2; row <= rowCount; row++)  // Assuming the first row is the header
                    {
                        var receivingReport = new ReceivingReport
                        {
                            ReceivingReportNo = worksheet.Cells[row, 21].Text,
                            Date = DateOnly.TryParse(worksheet.Cells[row, 1].Text, out DateOnly date) ? date : default,
                            DueDate = DateOnly.TryParse(worksheet.Cells[row, 2].Text, out DateOnly dueDate) ? dueDate : default,
                            SupplierInvoiceNumber = worksheet.Cells[row, 3].Text != "" ? worksheet.Cells[row, 3].Text : null,
                            SupplierInvoiceDate = worksheet.Cells[row, 4].Text,
                            TruckOrVessels = worksheet.Cells[row, 5].Text,
                            QuantityDelivered = decimal.TryParse(worksheet.Cells[row, 6].Text, out decimal quantityDelivered) ? quantityDelivered : 0,
                            QuantityReceived = decimal.TryParse(worksheet.Cells[row, 7].Text, out decimal quantityReceived) ? quantityReceived : 0,
                            GainOrLoss = decimal.TryParse(worksheet.Cells[row, 8].Text, out decimal gainOrLoss) ? gainOrLoss : 0,
                            Amount = decimal.TryParse(worksheet.Cells[row, 9].Text, out decimal amount) ? amount : 0,
                            OtherRef = worksheet.Cells[row, 10].Text != "" ? worksheet.Cells[row, 10].Text : null,
                            Remarks = worksheet.Cells[row, 11].Text,
                            // AmountPaid = decimal.TryParse(worksheet.Cells[row, 12].Text, out decimal amountPaid) ? amountPaid : 0,
                            // IsPaid = bool.TryParse(worksheet.Cells[row, 13].Text, out bool IsPaid) ? IsPaid : default,
                            // PaidDate = DateTime.TryParse(worksheet.Cells[row, 14].Text, out DateTime paidDate) ? paidDate : DateTime.MinValue,
                            // CanceledQuantity = decimal.TryParse(worksheet.Cells[row, 15].Text, out decimal netAmountOfEWT) ? netAmountOfEWT : 0,
                            CreatedBy = worksheet.Cells[row, 16].Text,
                            CreatedDate = DateTime.TryParse(worksheet.Cells[row, 17].Text, out DateTime createdDate) ? createdDate : default,
                            PostedBy = worksheet.Cells[row, 23].Text,
                            PostedDate = DateTime.TryParse(worksheet.Cells[row, 24].Text, out DateTime postedDate) ? postedDate : default,
                            CancellationRemarks = worksheet.Cells[row, 18].Text != "" ? worksheet.Cells[row, 18].Text : null,
                            ReceivedDate = DateOnly.TryParse(worksheet.Cells[row, 19].Text, out DateOnly receivedDate) ? receivedDate : default,
                            OriginalPOId = int.TryParse(worksheet.Cells[row, 20].Text, out int originalPoId) ? originalPoId : 0,
                            OriginalSeriesNumber = worksheet.Cells[row, 21].Text,
                            OriginalDocumentId = int.TryParse(worksheet.Cells[row, 22].Text, out int originalDocumentId) ? originalDocumentId : 0,
                        };

                        if (!rrDictionary.TryAdd(receivingReport.OriginalSeriesNumber, true))
                        {
                            continue;
                        }

                        //Checking for duplicate record
                        if (receivingReportList.Any(rr => rr.OriginalDocumentId == receivingReport.OriginalDocumentId))
                        {
                            var rrChanges = new Dictionary<string, (string OriginalValue, string NewValue)>();
                            var existingRr = await _aasDbContext.ReceivingReports.FirstOrDefaultAsync(rr => rr.OriginalDocumentId == receivingReport.OriginalDocumentId, cancellationToken);
                            var existingRrInLogs = await _dbContext.ImportExportLogs
                                .Where(x => x.DocumentNo == existingRr.ReceivingReportNo)
                                .ToListAsync(cancellationToken);

                            if (existingRr!.ReceivingReportNo!.Trim().ReplaceLineEndings(" ") != worksheet.Cells[row, 21].Text.Trim().ReplaceLineEndings(" "))
                            {
                                var originalValue = existingRr.ReceivingReportNo.Trim().ReplaceLineEndings(" ");
                                var adjustedValue = worksheet.Cells[row, 21].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingRrInLogs
                                    .Where(x => x.ColumnName == "RRNo" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["RRNo"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingRr.Date.ToString("yyyy-MM-dd").Trim().ReplaceLineEndings(" ") != worksheet.Cells[row, 1].Text.Trim().ReplaceLineEndings(" "))
                            {
                                var originalValue = existingRr.Date.ToString("yyyy-MM-dd").Trim().ReplaceLineEndings(" ");
                                var adjustedValue = worksheet.Cells[row, 1].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingRrInLogs
                                    .Where(x => x.ColumnName == "Date" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["Date"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingRr.PurchaseOrder?.OriginalDocumentId.ToString().Trim() != worksheet.Cells[row, 20].Text.Trim())
                            {
                                var originalValue = existingRr.PurchaseOrder?.OriginalDocumentId.ToString().Trim();
                                var adjustedValue = worksheet.Cells[row, 20].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingRrInLogs
                                    .Where(x => x.ColumnName == "SuppliersPoId" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["SuppliersPoId"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingRr.DueDate.ToString("yyyy-MM-dd").Trim().ReplaceLineEndings(" ") != worksheet.Cells[row, 2].Text.Trim().ReplaceLineEndings(" "))
                            {
                                var originalValue = existingRr.DueDate.ToString("yyyy-MM-dd").Trim().ReplaceLineEndings(" ");
                                var adjustedValue = worksheet.Cells[row, 2].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingRrInLogs
                                    .Where(x => x.ColumnName == "DueDate" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["DueDate"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingRr.SupplierInvoiceNumber?.Trim().ReplaceLineEndings(" ") != (worksheet.Cells[row, 3].Text.Trim().ReplaceLineEndings(" ") == "" ? null : worksheet.Cells[row, 3].Text.Trim().ReplaceLineEndings(" ")))
                            {
                                var originalValue = existingRr.SupplierInvoiceNumber?.Trim().ReplaceLineEndings(" ");
                                var adjustedValue = worksheet.Cells[row, 3].Text.Trim().ReplaceLineEndings(" ") == ""
                                    ? null
                                    : worksheet.Cells[row, 3].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingRrInLogs
                                    .Where(x => x.ColumnName == "SupplierInvoiceNumber" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["SupplierInvoiceNumber"] = (originalValue, adjustedValue);
                                }
                            }

                            if ((string.IsNullOrWhiteSpace(existingRr.SupplierInvoiceDate?.Trim().ReplaceLineEndings(" ")) ? null : existingRr.SupplierInvoiceDate?.Trim().ReplaceLineEndings(" ")) != (string.IsNullOrWhiteSpace(worksheet.Cells[row, 4].Text) ? null : worksheet.Cells[row, 4].Text.Trim().ReplaceLineEndings(" ")))
                            {
                                var originalValue = existingRr.SupplierInvoiceDate?.Trim().ReplaceLineEndings(" ");
                                var adjustedValue = worksheet.Cells[row, 4].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingRrInLogs
                                    .Where(x => x.ColumnName == "SupplierInvoiceDate" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["SupplierInvoiceDate"] = (originalValue, adjustedValue)!;
                                }
                            }

                            if (existingRr.TruckOrVessels.Trim().ReplaceLineEndings(" ") != worksheet.Cells[row, 5].Text.Trim().ReplaceLineEndings(" "))
                            {
                                var originalValue = existingRr.TruckOrVessels.Trim().ReplaceLineEndings(" ");
                                var adjustedValue = worksheet.Cells[row, 5].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingRrInLogs
                                    .Where(x => x.ColumnName == "TruckOrVessels" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["TruckOrVessels"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingRr.QuantityDelivered.ToString("F2").Trim().ReplaceLineEndings(" ") != decimal.Parse(worksheet.Cells[row, 6].Text).ToString("F2").Trim().ReplaceLineEndings(" "))
                            {
                                var originalValue = existingRr.QuantityDelivered.ToString("F2").Trim().ReplaceLineEndings(" ");
                                var adjustedValue = decimal.Parse(worksheet.Cells[row, 6].Text).ToString("F2").Trim().ReplaceLineEndings(" ");
                                var find  = existingRrInLogs
                                    .Where(x => x.ColumnName == "QuantityDelivered" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["QuantityDelivered"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingRr.QuantityReceived.ToString("F2").Trim().ReplaceLineEndings(" ") != decimal.Parse(worksheet.Cells[row, 7].Text).ToString("F2").Trim().ReplaceLineEndings(" "))
                            {
                                var originalValue = existingRr.QuantityReceived.ToString("F2").Trim().ReplaceLineEndings(" ");
                                var adjustedValue = decimal.Parse(worksheet.Cells[row, 7].Text).ToString("F2").Trim().ReplaceLineEndings(" ");
                                var find  = existingRrInLogs
                                    .Where(x => x.ColumnName == "QuantityReceived" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["QuantityReceived"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingRr.GainOrLoss.ToString("F2").Trim().ReplaceLineEndings(" ") != decimal.Parse(worksheet.Cells[row, 8].Text).ToString("F2").Trim().ReplaceLineEndings(" "))
                            {
                                var originalValue = existingRr.GainOrLoss.ToString("F2").Trim().ReplaceLineEndings(" ");
                                var adjustedValue = decimal.Parse(worksheet.Cells[row, 8].Text).ToString("F2").Trim().ReplaceLineEndings(" ");
                                var find  = existingRrInLogs
                                    .Where(x => x.ColumnName == "GainOrLoss" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["GainOrLoss"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingRr.Amount.ToString("F2").Trim().ReplaceLineEndings(" ") != decimal.Parse(worksheet.Cells[row, 9].Text).ToString("F2").Trim().ReplaceLineEndings(" "))
                            {
                                var originalValue = existingRr.Amount.ToString("F2").Trim().ReplaceLineEndings(" ");
                                var adjustedValue = decimal.Parse(worksheet.Cells[row, 9].Text).ToString("F2").Trim().ReplaceLineEndings(" ");
                                var find  = existingRrInLogs
                                    .Where(x => x.ColumnName == "Amount" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["Amount"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingRr.OtherRef?.Trim().ReplaceLineEndings(" ") != (worksheet.Cells[row, 10].Text.Trim().ReplaceLineEndings(" ") == "" ? null : worksheet.Cells[row, 10].Text.Trim().ReplaceLineEndings(" ")))
                            {
                                var originalValue = existingRr.OtherRef?.Trim().ReplaceLineEndings(" ");
                                var adjustedValue = worksheet.Cells[row, 10].Text.Trim().ReplaceLineEndings(" ") == ""
                                    ? null
                                    : worksheet.Cells[row, 10].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingRrInLogs
                                    .Where(x => x.ColumnName == "OtherRef" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["OtherRef"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingRr.Remarks.Trim().ReplaceLineEndings(" ") != worksheet.Cells[row, 11].Text.Trim().ReplaceLineEndings(" "))
                            {
                                var originalValue = existingRr.Remarks.Trim().ReplaceLineEndings(" ");
                                var adjustedValue = worksheet.Cells[row, 11].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingRrInLogs
                                    .Where(x => x.ColumnName == "Remarks" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["Remarks"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingRr.CreatedBy?.Trim().ReplaceLineEndings(" ") != worksheet.Cells[row, 16].Text.Trim().ReplaceLineEndings(" "))
                            {
                                var originalValue = existingRr.CreatedBy?.Trim().ReplaceLineEndings(" ");
                                var adjustedValue = worksheet.Cells[row, 16].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingRrInLogs
                                    .Where(x => x.ColumnName == "CreatedBy" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["CreatedBy"] = (originalValue, adjustedValue)!;
                                }
                            }

                            if (existingRr.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff").Trim().ReplaceLineEndings(" ") != worksheet.Cells[row, 17].Text.Trim().ReplaceLineEndings(" "))
                            {
                                var originalValue = existingRr.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff").Trim().ReplaceLineEndings(" ");
                                var adjustedValue = worksheet.Cells[row, 17].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingRrInLogs
                                    .Where(x => x.ColumnName == "CreatedDate" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["CreatedDate"] = (originalValue, adjustedValue);
                                }
                            }

                            if ((string.IsNullOrWhiteSpace(existingRr.CancellationRemarks?.Trim().ReplaceLineEndings(" ")) ? "" : existingRr.CancellationRemarks.Trim().ReplaceLineEndings(" ")) != worksheet.Cells[row, 18].Text.Trim().ReplaceLineEndings(" "))
                            {
                                var originalValue = existingRr.CancellationRemarks?.Trim().ReplaceLineEndings(" ");
                                var adjustedValue = worksheet.Cells[row, 18].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingRrInLogs
                                    .Where(x => x.ColumnName == "CancellationRemarks" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["CancellationRemarks"] = (originalValue, adjustedValue)!;
                                }
                            }

                            if (existingRr.ReceivedDate?.ToString("yyyy-MM-dd").Trim().ReplaceLineEndings(" ") != (worksheet.Cells[row, 19].Text.Trim().ReplaceLineEndings(" ") == "" ? DateOnly.MinValue.ToString("yyyy-MM-dd").Trim().ReplaceLineEndings(" ") : worksheet.Cells[row, 19].Text.Trim().ReplaceLineEndings(" ")))
                            {
                                var originalValue = existingRr.ReceivedDate?.ToString("yyyy-MM-dd").Trim().ReplaceLineEndings(" ");
                                var adjustedValue = worksheet.Cells[row, 19].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingRrInLogs
                                    .Where(x => x.ColumnName == "ReceivedDate" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["ReceivedDate"] = (originalValue, adjustedValue)!;
                                }
                            }

                            if (existingRr.OriginalPOId?.ToString().Trim().ReplaceLineEndings(" ") != (worksheet.Cells[row, 20].Text.Trim().ReplaceLineEndings(" ") == "" ? 0.ToString() : worksheet.Cells[row, 20].Text.Trim().ReplaceLineEndings(" ")))
                            {
                                var originalValue = existingRr.OriginalPOId?.ToString().Trim().ReplaceLineEndings(" ");
                                var adjustedValue = worksheet.Cells[row, 20].Text.Trim().ReplaceLineEndings(" ") == "" ? 0.ToString() : worksheet.Cells[row, 20].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingRrInLogs
                                    .Where(x => x.ColumnName == "OriginalPOId" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["OriginalPOId"] = (originalValue, adjustedValue)!;
                                }
                            }

                            if (existingRr.OriginalSeriesNumber?.Trim().ReplaceLineEndings(" ") != worksheet.Cells[row, 21].Text.Trim().ReplaceLineEndings(" "))
                            {
                                var originalValue = existingRr.OriginalSeriesNumber?.Trim().ReplaceLineEndings(" ");
                                var adjustedValue = worksheet.Cells[row, 21].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingRrInLogs
                                    .Where(x => x.ColumnName == "OriginalSeriesNumber" && x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["OriginalSeriesNumber"] = (originalValue, adjustedValue)!;
                                }
                            }

                            if (existingRr.OriginalDocumentId.ToString().Trim().ReplaceLineEndings(" ") != (worksheet.Cells[row, 22].Text.Trim().ReplaceLineEndings(" ") == "" ? 0.ToString() : worksheet.Cells[row, 22].Text.Trim().ReplaceLineEndings(" ")))
                            {
                                var originalValue = existingRr.OriginalDocumentId.ToString().Trim().ReplaceLineEndings(" ");
                                var adjustedValue = worksheet.Cells[row, 22].Text.Trim().ReplaceLineEndings(" ") == ""
                                    ? 0.ToString()
                                    : worksheet.Cells[row, 22].Text.Trim().ReplaceLineEndings(" ");
                                var find  = existingRrInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["OriginalDocumentId"] = (originalValue, adjustedValue);
                                }
                            }

                            if (rrChanges.Any())
                            {
                                await _receivingReportRepo.LogChangesAsync(existingRr.OriginalDocumentId, rrChanges, createdBy, existingRr.ReceivingReportNo, "AAS");
                            }

                            continue;
                        }
                        else
                        {
                            #region --Audit Trail Recording

                            if (!receivingReport.CreatedBy.IsNullOrEmpty())
                            {
                                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                                AuditTrail auditTrailBook = new(receivingReport.CreatedBy, $"Create new receiving report# {receivingReport.ReceivingReportNo}", "Receiving Report", ipAddress!, receivingReport.CreatedDate);
                                await _aasDbContext.AddAsync(auditTrailBook, cancellationToken);
                            }
                            if (!receivingReport.PostedBy.IsNullOrEmpty())
                            {
                                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                                AuditTrail auditTrailBook = new(receivingReport.PostedBy, $"Posted receiving report# {receivingReport.ReceivingReportNo}", "Receiving report", ipAddress!, receivingReport.PostedDate);
                                await _aasDbContext.AddAsync(auditTrailBook, cancellationToken);
                            }

                            #endregion --Audit Trail Recording
                        }

                        var getPo = await _aasDbContext
                            .PurchaseOrders
                            .Where(po => po.OriginalDocumentId == receivingReport.OriginalPOId)
                            .FirstOrDefaultAsync(cancellationToken);

                        receivingReport.POId = getPo?.PurchaseOrderId;
                        receivingReport.PONo = getPo?.PurchaseOrderNo;

                        await _aasDbContext.ReceivingReports.AddAsync(receivingReport, cancellationToken);
                    }

                    await _aasDbContext.SaveChangesAsync(cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    var checkChangesOfRecord = await _dbContext.ImportExportLogs
                        .Where(iel => iel.Action == string.Empty).ToListAsync(cancellationToken);
                    if (checkChangesOfRecord.Any())
                    {
                        TempData["importChanges"] = "";
                    }

                    #endregion -- Receiving Report Import --
                    }
                    else
                    {
                        TempData["warning"] = "The Uploaded Excel file is not related to AAS.";
                    }
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
            }
            TempData["success"] = "Uploading Success!";
            return RedirectToAction(nameof(Index), new { view = DynamicView.ReceivingReport });
        }

        #endregion
    }
}
