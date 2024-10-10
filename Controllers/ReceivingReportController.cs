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

namespace Accounting_System.Controllers
{
    [Authorize]
    public class ReceivingReportController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly ReceivingReportRepo _receivingReportRepo;

        private readonly GeneralRepo _generalRepo;

        private readonly InventoryRepo _inventoryRepo;

        public ReceivingReportController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, ReceivingReportRepo receivingReportRepo, GeneralRepo generalRepo, InventoryRepo inventoryRepo)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _receivingReportRepo = receivingReportRepo;
            _generalRepo = generalRepo;
            _inventoryRepo = inventoryRepo;
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
                if (!string.IsNullOrEmpty(parameters.Search?.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();
                    receivingReports = receivingReports
                        .Where(rr =>
                            rr.RRNo.ToLower().Contains(searchValue) ||
                            rr.Date.ToString("MMM dd, yyyy").ToLower().Contains(searchValue) ||
                            rr.PONo.ToLower().Contains(searchValue) ||
                            rr.QuantityDelivered.ToString().ToLower().Contains(searchValue) ||
                            rr.QuantityReceived.ToString().ToLower().Contains(searchValue) ||
                            rr.CreatedBy.ToLower().Contains(searchValue)
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
                                     .Select(rr => rr.Id) // Assuming Id is the primary key
                                     .ToListAsync(cancellationToken);
            return Json(receivingReportIds);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var viewModel = new ReceivingReport();
            viewModel.PurchaseOrders = await _dbContext.PurchaseOrders
                .Where(po => !po.IsReceived && po.IsPosted && !po.IsClosed)
                .Select(po => new SelectListItem
                {
                    Value = po.Id.ToString(),
                    Text = po.PONo
                })
                .ToListAsync(cancellationToken);

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
                    Value = po.Id.ToString(),
                    Text = po.PONo
                })
                .ToListAsync(cancellationToken);
            if (ModelState.IsValid)
            {
                #region --Retrieve PO

                var existingPo = await _dbContext
                            .PurchaseOrders
                            .Include(po => po.Supplier)
                            .Include(po => po.Product)
                            .FirstOrDefaultAsync(po => po.Id == model.POId, cancellationToken);

                #endregion --Retrieve PO

                var totalAmountRR = existingPo.Quantity - existingPo.QuantityReceived;

                if (model.QuantityDelivered > totalAmountRR)
                {
                    TempData["error"] = "Input is exceed to remaining quantity delivered";
                    return View(model);
                }

                #region --Validating Series

                var getLastNumber = await _receivingReportRepo.GetLastSeriesNumber(cancellationToken);

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

                var generatedRR = await _receivingReportRepo.GenerateRRNo(cancellationToken);
                model.SeriesNumber = getLastNumber;
                model.RRNo = generatedRR;
                model.CreatedBy = _userManager.GetUserName(this.User);
                model.GainOrLoss = model.QuantityReceived - model.QuantityDelivered;
                model.PONo = await _receivingReportRepo.GetPONoAsync(model?.POId, cancellationToken);
                model.DueDate = await _receivingReportRepo.ComputeDueDateAsync(model?.POId, model.Date, cancellationToken);

                model.Amount = model.QuantityReceived * existingPo.Price;

                #region --Audit Trail Recording

                if (model.OriginalSeriesNumber == null && model.OriginalDocumentId == 0)
                {
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    AuditTrail auditTrailBook = new(model.CreatedBy, $"Create new rr# {model.RRNo}", "Receiving Report", ipAddress);
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
            if (id == null || _dbContext.ReceivingReports == null)
            {
                return NotFound();
            }

            var receivingReport = await _dbContext.ReceivingReports.FindAsync(id, cancellationToken);
            if (receivingReport == null)
            {
                return NotFound();
            }

            receivingReport.PurchaseOrders = await _dbContext.PurchaseOrders
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.PONo
                })
                .ToListAsync(cancellationToken);

            return View(receivingReport);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(ReceivingReport model, CancellationToken cancellationToken)
        {
            var existingModel = await _dbContext.ReceivingReports.FindAsync(model.Id, cancellationToken);

            existingModel.PurchaseOrders = await _dbContext.PurchaseOrders
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.PONo
                })
                .ToListAsync(cancellationToken);

            if (ModelState.IsValid)
            {
                if (existingModel == null)
                {
                    return NotFound();
                }

                #region --Retrieve PO

                var po = await _dbContext
                            .PurchaseOrders
                            .Include(po => po.Supplier)
                            .Include(po => po.Product)
                            .FirstOrDefaultAsync(po => po.Id == model.POId, cancellationToken);

                #endregion --Retrieve PO

                var rr = await _dbContext.ReceivingReports
                .Where(rr => rr.PONo == po.PONo)
                .ToListAsync(cancellationToken);

                var totalAmountRR = po.Quantity - po.QuantityReceived;

                if (model.QuantityDelivered > totalAmountRR && !existingModel.IsPosted)
                {
                    TempData["error"] = "Input is exceed to remaining quantity delivered";
                    return View(model);
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

                #region --Audit Trail Recording

                if (model.OriginalSeriesNumber == null && model.OriginalDocumentId == 0)
                {
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    AuditTrail auditTrailBook = new(existingModel.CreatedBy, $"Edit rr# {existingModel.RRNo}", "Receiving Report", ipAddress);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                }

                #endregion --Audit Trail Recording

                await _dbContext.SaveChangesAsync(cancellationToken);

                TempData["success"] = "Receiving Report updated successfully";
                return RedirectToAction(nameof(Index));
            }

            return View(existingModel);
        }

        [HttpGet]
        public async Task<IActionResult> Print(int id, CancellationToken cancellationToken)
        {
            if (id == null || _dbContext.ReceivingReports == null)
            {
                return NotFound();
            }

            var receivingReport = await _receivingReportRepo.FindRR(id, cancellationToken);

            if (receivingReport == null)
            {
                return NotFound();
            }

            return View(receivingReport);
        }

        public async Task<IActionResult> Printed(int id, CancellationToken cancellationToken)
        {
            var rr = await _dbContext.ReceivingReports.FindAsync(id, cancellationToken);
            if (rr != null && !rr.IsPrinted)
            {

                #region --Audit Trail Recording

                if (rr.OriginalSeriesNumber == null && rr.OriginalDocumentId == 0)
                {
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    var printedBy = _userManager.GetUserName(this.User);
                    AuditTrail auditTrailBook = new(printedBy, $"Printed original copy of rr# {rr.RRNo}", "Receiving Report", ipAddress);
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

            if (model != null)
            {
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
                        model.PostedBy = _userManager.GetUserName(this.User);
                        model.PostedDate = DateTime.Now;

                        #region --General Ledger Recording

                        var ledgers = new List<GeneralLedgerBook>();

                        decimal netOfVatAmount = 0;
                        decimal vatAmount = 0;
                        decimal ewtAmount = 0;
                        decimal netOfEwtAmount = 0;

                        if (model.PurchaseOrder.Supplier.VatType == CS.VatType_Vatable)
                        {
                            netOfVatAmount = _generalRepo.ComputeNetOfVat(model.Amount);
                            vatAmount = _generalRepo.ComputeVatAmount(netOfVatAmount);
                        }
                        else
                        {
                            netOfVatAmount = model.Amount;
                        }

                        if (model.PurchaseOrder.Supplier.TaxType == CS.TaxType_WithTax)
                        {
                            ewtAmount = _generalRepo.ComputeEwtAmount(netOfVatAmount, 0.01m);
                            netOfEwtAmount = _generalRepo.ComputeNetOfEwt(model.Amount, ewtAmount);
                        }
                        else
                        {
                            netOfEwtAmount = model.Amount;
                        }

                        if (model.PurchaseOrder.Product.Name == "Biodiesel")
                        {
                            ledgers.Add(new GeneralLedgerBook
                            {
                                Date = model.Date,
                                Reference = model.RRNo,
                                Description = "Receipt of Goods",
                                AccountNo = "1010401",
                                AccountTitle = "Inventory - Biodiesel",
                                Debit = netOfVatAmount,
                                Credit = 0,
                                CreatedBy = model.CreatedBy,
                                CreatedDate = model.CreatedDate
                            });
                        }
                        else if (model.PurchaseOrder.Product.Name == "Econogas")
                        {
                            ledgers.Add(new GeneralLedgerBook
                            {
                                Date = model.Date,
                                Reference = model.RRNo,
                                Description = "Receipt of Goods",
                                AccountNo = "1010402",
                                AccountTitle = "Inventory - Econogas",
                                Debit = netOfVatAmount,
                                Credit = 0,
                                CreatedBy = model.CreatedBy,
                                CreatedDate = model.CreatedDate
                            });
                        }
                        else
                        {
                            ledgers.Add(new GeneralLedgerBook
                            {
                                Date = model.Date,
                                Reference = model.RRNo,
                                Description = "Receipt of Goods",
                                AccountNo = "1010403",
                                AccountTitle = "Inventory - Envirogas",
                                Debit = netOfVatAmount,
                                Credit = 0,
                                CreatedBy = model.CreatedBy,
                                CreatedDate = model.CreatedDate
                            });
                        }

                        if (vatAmount > 0)
                        {
                            ledgers.Add(new GeneralLedgerBook
                            {
                                Date = model.Date,
                                Reference = model.RRNo,
                                Description = "Receipt of Goods",
                                AccountNo = "1010602",
                                AccountTitle = "Vat Input",
                                Debit = vatAmount,
                                Credit = 0,
                                CreatedBy = model.CreatedBy,
                                CreatedDate = model.CreatedDate
                            });
                        }

                        if (ewtAmount > 0)
                        {
                            ledgers.Add(new GeneralLedgerBook
                            {
                                Date = model.Date,
                                Reference = model.RRNo,
                                Description = "Receipt of Goods",
                                AccountNo = "2010302",
                                AccountTitle = "Expanded Withholding Tax 1%",
                                Debit = 0,
                                Credit = ewtAmount,
                                CreatedBy = model.CreatedBy,
                                CreatedDate = model.CreatedDate
                            });
                        }

                        ledgers.Add(new GeneralLedgerBook
                        {
                            Date = model.Date,
                            Reference = model.RRNo,
                            Description = "Receipt of Goods",
                            AccountNo = "2010101",
                            AccountTitle = "AP-Trade Payable",
                            Debit = 0,
                            Credit = netOfEwtAmount,
                            CreatedBy = model.CreatedBy,
                            CreatedDate = model.CreatedDate
                        });


                        if (!_generalRepo.IsDebitCreditBalanced(ledgers))
                        {
                            throw new ArgumentException("Debit and Credit is not equal, check your entries.");
                        }

                        await _dbContext.AddRangeAsync(ledgers, cancellationToken);

                        #endregion --General Ledger Recording

                        #region--Inventory Recording

                        await _inventoryRepo.AddPurchaseToInventoryAsync(model, User, cancellationToken);

                        #endregion

                        await _receivingReportRepo.UpdatePOAsync(model.PurchaseOrder.Id, model.QuantityReceived, cancellationToken);

                        #region --Purchase Book Recording

                        var purchaseBook = new List<PurchaseJournalBook>();

                        purchaseBook.Add(new PurchaseJournalBook
                        {
                            Date = model.Date,
                            SupplierName = model.PurchaseOrder.Supplier.Name,
                            SupplierTin = model.PurchaseOrder.Supplier.TinNo,
                            SupplierAddress = model.PurchaseOrder.Supplier.Address,
                            DocumentNo = model.RRNo,
                            Description = model.PurchaseOrder.Product.Name,
                            Amount = model.Amount,
                            VatAmount = vatAmount,
                            WhtAmount = ewtAmount,
                            NetPurchases = netOfVatAmount,
                            CreatedBy = model.CreatedBy,
                            PONo = model.PurchaseOrder.PONo,
                            DueDate = model.DueDate
                        });

                        await _dbContext.AddRangeAsync(purchaseBook, cancellationToken);
                        #endregion --Purchase Book Recording

                        #region --Audit Trail Recording

                        if (model.OriginalSeriesNumber == null && model.OriginalDocumentId == 0)
                        {
                            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                            AuditTrail auditTrailBook = new(model.PostedBy, $"Posted rr# {model.RRNo}", "Receiving Report", ipAddress);
                            await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                        }

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        TempData["success"] = "Receiving Report has been Posted.";
                        return RedirectToAction(nameof(Print), new { id = id });
                    }
                    else
                    {
                        return RedirectToAction(nameof(Print), new { id = id });
                    }
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(Print), new { id = id });
                }
            }

            return NotFound();
        }

        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {
            var model = await _dbContext.ReceivingReports
                .FindAsync(id, cancellationToken);

            var existingInventory = await _dbContext.Inventories
                .Include(i => i.Product)
                .FirstOrDefaultAsync(i => i.Reference == model.RRNo);

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

                    await _generalRepo.RemoveRecords<PurchaseJournalBook>(pb => pb.DocumentNo == model.RRNo, cancellationToken);
                    await _generalRepo.RemoveRecords<GeneralLedgerBook>(gl => gl.Reference == model.RRNo, cancellationToken);
                    await _inventoryRepo.VoidInventory(existingInventory, cancellationToken);
                    await _receivingReportRepo.RemoveQuantityReceived(model?.POId, model.QuantityReceived, cancellationToken);
                    model.QuantityReceived = 0;

                    #region --Audit Trail Recording

                    if (model.OriginalSeriesNumber == null && model.OriginalDocumentId == 0)
                    {
                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        AuditTrail auditTrailBook = new(model.VoidedBy, $"Voided rr# {model.RRNo}", "Receiving Report", ipAddress);
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                    }

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Receiving Report has been Voided.";
                }
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        public async Task<IActionResult> Cancel(int id, string cancellationRemarks, CancellationToken cancellationToken)
        {
            var model = await _dbContext.ReceivingReports.FindAsync(id, cancellationToken);

            if (model != null)
            {
                if (!model.IsCanceled)
                {
                    model.IsCanceled = true;
                    model.CanceledBy = _userManager.GetUserName(this.User);
                    model.CanceledDate = DateTime.Now;
                    model.CanceledQuantity = model.QuantityDelivered < model.QuantityReceived ? model.QuantityDelivered : model.QuantityReceived;
                    model.QuantityDelivered = 0;
                    model.QuantityReceived = 0;
                    model.CancellationRemarks = cancellationRemarks;

                    #region --Audit Trail Recording

                    if (model.OriginalSeriesNumber == null && model.OriginalDocumentId == 0)
                    {
                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        AuditTrail auditTrailBook = new(model.CanceledBy, $"Cancelled rr# {model.RRNo}", "Receiving Report", ipAddress);
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                    }

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Receiving Report has been Cancelled.";
                }
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        [HttpGet]
        public async Task<IActionResult> GetLiquidations(int id, CancellationToken cancellationToken)
        {
            var po = await _receivingReportRepo.GetPurchaseOrderAsync(id, cancellationToken);

            var rrPostedOnly = await _dbContext
                .ReceivingReports
                .Where(rr => rr.PONo == po.PONo && rr.IsPosted)
                .ToListAsync(cancellationToken);

            var rr = await _dbContext
                .ReceivingReports
                .Where(rr => rr.PONo == po.PONo)
                .ToListAsync(cancellationToken);

            var rrNotPosted = await _dbContext
                .ReceivingReports
                .Where(rr => rr.PONo == po.PONo && !rr.IsPosted && !rr.IsCanceled)
                .ToListAsync(cancellationToken);

            var rrCanceled = await _dbContext
                .ReceivingReports
                .Where(rr => rr.PONo == po.PONo && rr.IsCanceled)
                .ToListAsync(cancellationToken);

            if (po != null)
            {
                return Json(new
                {
                    poNo = po.PONo,
                    poQuantity = po.Quantity.ToString("N2"),
                    rrList = rr,
                    rrListPostedOnly = rrPostedOnly,
                    rrListNotPosted = rrNotPosted,
                    rrListCanceled = rrCanceled
                });
            }
            else
            {
                return Json(null);
            }
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

            // Retrieve the selected records from the database
            var selectedList = await _dbContext.ReceivingReports
                .Where(rr => recordIds.Contains(rr.Id))
                .OrderBy(rr => rr.RRNo)
                .ToListAsync();

            // Create the Excel package
            using var package = new ExcelPackage();
            // Add a new worksheet to the Excel package
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
            worksheet.Cells["U1"].Value = "OriginalSeriesNumber";
            worksheet.Cells["V1"].Value = "OriginalDocumentId";

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
                worksheet.Cells[row, 21].Value = item.RRNo;
                worksheet.Cells[row, 22].Value = item.Id;

                row++;
            }

            // Convert the Excel package to a byte array
            var excelBytes = await package.GetAsByteArrayAsync();

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ReceivingReportList.xlsx");
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
                            return RedirectToAction(nameof(Index), new { view = DynamicView.ReceivingReport });
                        }
                        if (worksheet.ToString() != nameof(DynamicView.ReceivingReport))
                        {
                            TempData["error"] = "The Excel file is not related to receiving report.";
                            return RedirectToAction(nameof(Index), new { view = DynamicView.ReceivingReport });
                        }

                        var rowCount = worksheet.Dimension.Rows;

                        for (int row = 2; row <= rowCount; row++)  // Assuming the first row is the header
                        {
                            var receivingReport = new ReceivingReport
                            {
                                RRNo = await _receivingReportRepo.GenerateRRNo(),
                                SeriesNumber = await _receivingReportRepo.GetLastSeriesNumber(),
                                Date = DateOnly.TryParse(worksheet.Cells[row, 1].Text, out DateOnly date) ? date : default,
                                DueDate = DateOnly.TryParse(worksheet.Cells[row, 2].Text, out DateOnly dueDate) ? dueDate : default,
                                SupplierInvoiceNumber = worksheet.Cells[row, 3].Text != "" ? worksheet.Cells[row, 3].Text : null,
                                SupplierInvoiceDate = worksheet.Cells[row, 4].Text,
                                TruckOrVessels = worksheet.Cells[row, 5].Text,
                                QuantityDelivered = decimal.TryParse(worksheet.Cells[row, 6].Text, out decimal quantityDelivered) ? quantityDelivered : 0,
                                QuantityReceived = decimal.TryParse(worksheet.Cells[row, 7].Text, out decimal quantityReceived) ? quantityReceived : 0,
                                GainOrLoss = decimal.TryParse(worksheet.Cells[row, 8].Text, out decimal gainOrLoss) ? gainOrLoss : 0,
                                Amount = decimal.TryParse(worksheet.Cells[row, 9].Text, out decimal amount) ? amount : 0,
                                OtherRef = worksheet.Cells[row, 10].Text != "" ? worksheet.Cells[row, 13].Text : null,
                                Remarks = worksheet.Cells[row, 11].Text,
                                AmountPaid = decimal.TryParse(worksheet.Cells[row, 12].Text, out decimal amountPaid) ? amountPaid : 0,
                                IsPaid = bool.TryParse(worksheet.Cells[row, 13].Text, out bool IsPaid) ? IsPaid : default,
                                PaidDate = DateTime.TryParse(worksheet.Cells[row, 14].Text, out DateTime paidDate) ? paidDate : default,
                                CanceledQuantity = decimal.TryParse(worksheet.Cells[row, 15].Text, out decimal netAmountOfEWT) ? netAmountOfEWT : 0,
                                CreatedBy = worksheet.Cells[row, 16].Text,
                                CreatedDate = DateTime.TryParse(worksheet.Cells[row, 17].Text, out DateTime createdDate) ? createdDate : default,
                                CancellationRemarks = worksheet.Cells[row, 18].Text != "" ? worksheet.Cells[row, 18].Text : null,
                                ReceivedDate = DateOnly.TryParse(worksheet.Cells[row, 19].Text, out DateOnly receivedDate) ? receivedDate : default,
                                OriginalPOId = int.TryParse(worksheet.Cells[row, 20].Text, out int OriginalPOId) ? OriginalPOId : 0,
                                OriginalSeriesNumber = worksheet.Cells[row, 21].Text,
                                OriginalDocumentId = int.TryParse(worksheet.Cells[row, 22].Text, out int originalDocumentId) ? originalDocumentId : 0,
                            };
                            var getPO = await _dbContext.PurchaseOrders
                                .Where(c => c.OriginalDocumentId == receivingReport.OriginalPOId)
                                .FirstOrDefaultAsync();

                            receivingReport.POId = getPO.Id;
                            receivingReport.PONo = getPO.PONo;

                            await _dbContext.ReceivingReports.AddAsync(receivingReport);
                            await _dbContext.SaveChangesAsync();
                        }
                    }
                }
                catch (OperationCanceledException oce)
                {
                    TempData["error"] = oce.Message;
                    return RedirectToAction(nameof(Index), new { view = DynamicView.ReceivingReport });
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(Index), new { view = DynamicView.ReceivingReport });
                }
            }
            TempData["success"] = "Uploading Success!";
            return RedirectToAction(nameof(Index), new { view = DynamicView.ReceivingReport });
        }

        #endregion -- import xlsx record --
    }
}