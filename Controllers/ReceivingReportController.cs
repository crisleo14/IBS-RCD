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
            var rr = await _dbContext.ReceivingReports
                .Include(p => p.PurchaseOrder)
                .ThenInclude(s => s.Supplier)
                .Include(p => p.PurchaseOrder)
            .ThenInclude(prod => prod.Product)
                .ToListAsync(cancellationToken);

            if (view == nameof(DynamicView.ReceivingReport))
            {
                return View("ImportExportIndex", rr);
            }

            return View(rr);
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

                if (po.Supplier.VatType == "Vatable")
                {
                    model.Amount = model.QuantityDelivered < model.QuantityReceived ? model.QuantityDelivered * po.Price : model.QuantityReceived * po.Price;
                    model.NetAmount = model.Amount / 1.12m;
                    model.VatAmount = model.NetAmount * .12m;
                }
                else
                {
                    model.Amount = model.QuantityDelivered < model.QuantityReceived ? model.QuantityDelivered * po.Price : model.QuantityReceived * po.Price;
                    model.NetAmount = model.Amount;
                }

                if (po.Supplier.TaxType == "Withholding Tax")
                {
                    model.EwtAmount = model.NetAmount * .01m;
                    model.NetAmountOfEWT = model.Amount - model.EwtAmount;
                }

                #region --Audit Trail Recording

                AuditTrail auditTrail = new(model.CreatedBy, $"Create new rr# {model.RRNo}", "Receiving Report");
                await _dbContext.AddAsync(auditTrail, cancellationToken);

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


                if (po.Supplier.VatType == "Vatable")
                {
                    existingModel.Amount = model.QuantityReceived * po.Price;
                    existingModel.NetAmount = existingModel.Amount / 1.12m;
                    existingModel.VatAmount = existingModel.NetAmount * .12m;
                }
                else
                {
                    existingModel.Amount = model.QuantityReceived * po.Price;
                    existingModel.NetAmount = existingModel.Amount;
                }

                if (po.Supplier.TaxType == "Withholding Tax")
                {
                    existingModel.EwtAmount = existingModel.NetAmount * .01m;
                }

                #region --Audit Trail Recording

                AuditTrail auditTrail = new(existingModel.CreatedBy, $"Edit rr# {existingModel.RRNo}", "Receiving Report");
                await _dbContext.AddAsync(auditTrail, cancellationToken);

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

                var printedBy = _userManager.GetUserName(this.User);
                AuditTrail auditTrail = new(printedBy, $"Printed original copy of rr# {rr.RRNo}", "Receiving Report");
                await _dbContext.AddAsync(auditTrail, cancellationToken);

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

                        if (model.PurchaseOrder.Product.Name == "Biodiesel")
                        {
                            ledgers.Add(new GeneralLedgerBook
                            {
                                Date = model.Date,
                                Reference = model.RRNo,
                                Description = "Receipt of Goods",
                                AccountNo = "1010401",
                                AccountTitle = "Inventory - Biodiesel",
                                Debit = model.NetAmount,
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
                                Debit = model.NetAmount,
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
                                Debit = model.NetAmount,
                                Credit = 0,
                                CreatedBy = model.CreatedBy,
                                CreatedDate = model.CreatedDate
                            });
                        }

                        if (model.VatAmount > 0)
                        {
                            ledgers.Add(new GeneralLedgerBook
                            {
                                Date = model.Date,
                                Reference = model.RRNo,
                                Description = "Receipt of Goods",
                                AccountNo = "1010602",
                                AccountTitle = "Vat Input",
                                Debit = model.VatAmount,
                                Credit = 0,
                                CreatedBy = model.CreatedBy,
                                CreatedDate = model.CreatedDate
                            });
                        }

                        if (model.EwtAmount > 0)
                        {
                            ledgers.Add(new GeneralLedgerBook
                            {
                                Date = model.Date,
                                Reference = model.RRNo,
                                Description = "Receipt of Goods",
                                AccountNo = "2010302",
                                AccountTitle = "Expanded Withholding Tax 1%",
                                Debit = 0,
                                Credit = model.EwtAmount,
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
                            Credit = model.Amount - model.EwtAmount,
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
                            VatAmount = model.VatAmount,
                            WhtAmount = model.EwtAmount,
                            NetPurchases = model.NetAmount,
                            CreatedBy = model.CreatedBy,
                            PONo = model.PurchaseOrder.PONo,
                            DueDate = model.DueDate
                        });

                        await _dbContext.AddRangeAsync(purchaseBook, cancellationToken);
                        #endregion --Purchase Book Recording

                        #region --Audit Trail Recording

                        AuditTrail auditTrail = new(model.PostedBy, $"Posted receiving# {model.RRNo}", "Receiving Report");
                        await _dbContext.AddAsync(auditTrail, cancellationToken);

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        TempData["success"] = "Receiving Report has been Posted.";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        return RedirectToAction(nameof(Index));
                    }
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(Index));
                }
            }

            return NotFound();
        }

        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {
            var model = await _dbContext.ReceivingReports
                .FindAsync(id, cancellationToken);

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
                    await _generalRepo.RemoveRecords<Inventory>(i => i.Reference == model.RRNo, cancellationToken);
                    await _receivingReportRepo.RemoveQuantityReceived(model?.POId, model.QuantityReceived, cancellationToken);
                    model.QuantityReceived = 0;

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(model.VoidedBy, $"Voided receiving# {model.RRNo}", "Receiving Report");
                    await _dbContext.AddAsync(auditTrail, cancellationToken);

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

                    AuditTrail auditTrail = new(model.CanceledBy, $"Cancelled receiving# {model.RRNo}", "Receiving Report");
                    await _dbContext.AddAsync(auditTrail, cancellationToken);

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
            worksheet.Cells["J1"].Value = "NetAmount";
            worksheet.Cells["K1"].Value = "VatAmount";
            worksheet.Cells["L1"].Value = "EWTAmount";
            worksheet.Cells["M1"].Value = "OtherRef";
            worksheet.Cells["N1"].Value = "Remarks";
            worksheet.Cells["O1"].Value = "AmountPaid";
            worksheet.Cells["P1"].Value = "IsPaid";
            worksheet.Cells["Q1"].Value = "PaidDate";
            worksheet.Cells["R1"].Value = "CanceledQuantity";
            worksheet.Cells["S1"].Value = "NetAmountOfEWT";
            worksheet.Cells["T1"].Value = "CreatedBy";
            worksheet.Cells["U1"].Value = "CreatedDate";
            worksheet.Cells["V1"].Value = "CancellationRemarks";
            worksheet.Cells["W1"].Value = "ReceivedDate";
            worksheet.Cells["X1"].Value = "OriginalPOId";
            worksheet.Cells["Y1"].Value = "OriginalSeriesNumber";
            worksheet.Cells["Z1"].Value = "OriginalDocumentId";

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
                worksheet.Cells[row, 10].Value = item.NetAmount;
                worksheet.Cells[row, 11].Value = item.VatAmount;
                worksheet.Cells[row, 12].Value = item.EwtAmount;
                worksheet.Cells[row, 13].Value = item.OtherRef;
                worksheet.Cells[row, 14].Value = item.Remarks;
                worksheet.Cells[row, 15].Value = item.AmountPaid;
                worksheet.Cells[row, 16].Value = item.IsPaid;
                worksheet.Cells[row, 17].Value = item.PaidDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                worksheet.Cells[row, 18].Value = item.CanceledQuantity;
                worksheet.Cells[row, 19].Value = item.NetAmountOfEWT;
                worksheet.Cells[row, 20].Value = item.CreatedBy;
                worksheet.Cells[row, 21].Value = item.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                worksheet.Cells[row, 22].Value = item.CancellationRemarks;
                worksheet.Cells[row, 23].Value = item.ReceivedDate?.ToString("yyyy-MM-dd");
                worksheet.Cells[row, 24].Value = item.POId;
                worksheet.Cells[row, 25].Value = item.RRNo;
                worksheet.Cells[row, 26].Value = item.Id;

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
                            return RedirectToAction(nameof(Index), new { errorMessage = "The Excel file contains no worksheets." });
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
                                NetAmount = decimal.TryParse(worksheet.Cells[row, 10].Text, out decimal netAmount) ? netAmount : 0,
                                VatAmount = decimal.TryParse(worksheet.Cells[row, 11].Text, out decimal vatAmount) ? vatAmount : 0,
                                EwtAmount = decimal.TryParse(worksheet.Cells[row, 12].Text, out decimal ewtAmount) ? ewtAmount : 0,
                                OtherRef = worksheet.Cells[row, 13].Text != "" ? worksheet.Cells[row, 13].Text : null,
                                Remarks = worksheet.Cells[row, 14].Text,
                                AmountPaid = decimal.TryParse(worksheet.Cells[row, 15].Text, out decimal amountPaid) ? amountPaid : 0,
                                IsPaid = bool.TryParse(worksheet.Cells[row, 16].Text, out bool IsPaid) ? IsPaid : default,
                                PaidDate = DateTime.TryParse(worksheet.Cells[row, 17].Text, out DateTime paidDate) ? paidDate : default,
                                CanceledQuantity = decimal.TryParse(worksheet.Cells[row, 18].Text, out decimal netAmountOfEWT) ? netAmountOfEWT : 0,
                                NetAmountOfEWT = decimal.TryParse(worksheet.Cells[row, 19].Text, out decimal balance) ? balance : 0,
                                CreatedBy = worksheet.Cells[row, 20].Text,
                                CreatedDate = DateTime.TryParse(worksheet.Cells[row, 21].Text, out DateTime createdDate) ? createdDate : default,
                                CancellationRemarks = worksheet.Cells[row, 22].Text != "" ? worksheet.Cells[row, 22].Text : null,
                                ReceivedDate = DateOnly.TryParse(worksheet.Cells[row, 23].Text, out DateOnly receivedDate) ? receivedDate : default,
                                OriginalPOId = int.TryParse(worksheet.Cells[row, 24].Text, out int OriginalPOId) ? OriginalPOId : 0,
                                OriginalSeriesNumber = worksheet.Cells[row, 25].Text,
                                OriginalDocumentId = int.TryParse(worksheet.Cells[row, 26].Text, out int originalDocumentId) ? originalDocumentId : 0,
                            };
                            await _dbContext.ReceivingReports.AddAsync(receivingReport);
                            await _dbContext.SaveChangesAsync();

                            var rr = await _dbContext
                                .ReceivingReports
                                .FirstOrDefaultAsync(s => s.Id == receivingReport.Id);

                            var getPO = await _dbContext.PurchaseOrders
                                .Where(c => c.OriginalDocumentId == receivingReport.OriginalPOId)
                                .FirstOrDefaultAsync();

                            rr.POId = getPO.Id;
                            rr.PONo = getPO.PONo;

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