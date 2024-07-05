using Accounting_System.Data;
using Accounting_System.Models;
using Accounting_System.Models.AccountsPayable;
using Accounting_System.Models.Reports;
using Accounting_System.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

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

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var rr = await _dbContext.ReceivingReports
                .Include(p => p.PurchaseOrder)
                .ThenInclude(s => s.Supplier)
                .Include(p => p.PurchaseOrder)
                .ThenInclude(prod => prod.Product)
                .ToListAsync(cancellationToken);

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

                var rr = _dbContext.ReceivingReports
                .Where(rr => rr.PONo == po.PONo)
                .ToList();

                var totalAmountRR = po.Quantity - rr.Sum(r => r.QuantityReceived);

                if (totalAmountRR < model.QuantityReceived)
                {
                    TempData["error"] = "Input is exceed to remaining quantity received";
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
                model.GainOrLoss = model.QuantityDelivered - model.QuantityReceived;
                model.PONo = await _receivingReportRepo.GetPONoAsync(model.POId, cancellationToken);
                model.DueDate = await _receivingReportRepo.ComputeDueDateAsync(model.POId, model.Date, cancellationToken);

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

                return RedirectToAction("Index");
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

                existingModel.Date = model.Date;
                existingModel.POId = model.POId;
                existingModel.PONo = await _receivingReportRepo.GetPONoAsync(model.POId, cancellationToken);
                existingModel.DueDate = await _receivingReportRepo.ComputeDueDateAsync(model.POId, model.Date, cancellationToken);
                existingModel.SupplierInvoiceNumber = model.SupplierInvoiceNumber;
                existingModel.SupplierInvoiceDate = model.SupplierInvoiceDate;
                existingModel.TruckOrVessels = model.TruckOrVessels;
                existingModel.QuantityDelivered = model.QuantityDelivered;
                existingModel.QuantityReceived = model.QuantityReceived;
                existingModel.GainOrLoss = model.QuantityDelivered - model.QuantityReceived;
                existingModel.OtherRef = model.OtherRef;
                existingModel.Remarks = model.Remarks;

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
                return RedirectToAction("Index");
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
            return RedirectToAction("Print", new { id = id });
        }

        public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
        {
            var model = await _receivingReportRepo.FindRR(id, cancellationToken);

            if (model != null)
            {
                try
                {
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
            var model = await _dbContext.ReceivingReports.FindAsync(id, cancellationToken);

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

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(model.VoidedBy, $"Voided receiving# {model.RRNo}", "Receiving Report");
                    await _dbContext.AddAsync(auditTrail, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Receiving Report has been Voided.";
                }
                return RedirectToAction("Index");
            }

            return NotFound();
        }

        public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
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

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(model.CanceledBy, $"Cancelled receiving# {model.RRNo}", "Receiving Report");
                    await _dbContext.AddAsync(auditTrail, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Receiving Report has been Cancelled.";
                }
                return RedirectToAction("Index");
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
    }
}