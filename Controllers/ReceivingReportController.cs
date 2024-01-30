using Accounting_System.Data;
using Accounting_System.Models;
using Accounting_System.Repository;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Accounting_System.Controllers
{
    public class ReceivingReportController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly ReceivingReportRepo _receivingReportRepo;

        public ReceivingReportController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, ReceivingReportRepo receivingReportRepo)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _receivingReportRepo = receivingReportRepo;
        }

        public async Task<IActionResult> Index()
        {
            var rr = await _dbContext.ReceivingReports
                .Include(p => p.PurchaseOrder)
                .ThenInclude(s => s.Supplier)
                .Include(p => p.PurchaseOrder)
                .ThenInclude(prod => prod.Product)
                .OrderBy(r => r.Id)
                .ToListAsync();

            return View(rr);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var viewModel = new ReceivingReport();
            viewModel.PurchaseOrders = await _dbContext.PurchaseOrders
                .Where(po => !po.IsReceived)
                .Select(po => new SelectListItem
                {
                    Value = po.Id.ToString(),
                    Text = po.PONo
                })
                .ToListAsync();

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ReceivingReport model)
        {
            model.PurchaseOrders = await _dbContext.PurchaseOrders
                .Where(po => !po.IsReceived)
                .Select(po => new SelectListItem
                {
                    Value = po.Id.ToString(),
                    Text = po.PONo
                })
                .ToListAsync();
            if (ModelState.IsValid)
            {
                #region --Validating Series

                var getLastNumber = await _receivingReportRepo.GetLastSeriesNumber();

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

                var generatedRR = await _receivingReportRepo.GenerateRRNo();
                model.SeriesNumber = getLastNumber;
                model.RRNo = generatedRR;
                model.CreatedBy = _userManager.GetUserName(this.User);
                model.GainOrLoss = model.QuantityDelivered - model.QuantityReceived;
                model.PONo = await _receivingReportRepo.GetPONoAsync(model.POId);
                model.DueDate = await _receivingReportRepo.ComputeDueDateAsync(model.POId, model.Date);

                _dbContext.Add(model);

                #region --Audit Trail Recording

                AuditTrail auditTrail = new(model.CreatedBy, $"Create new rr# {model.RRNo}", "Receiving Report");
                _dbContext.Add(auditTrail);

                #endregion --Audit Trail Recording

                await _dbContext.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            ModelState.AddModelError("", "The information you submitted is not valid!");
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _dbContext.ReceivingReports == null)
            {
                return NotFound();
            }

            var receivingReport = await _dbContext.ReceivingReports.FindAsync(id);
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
                .ToListAsync();

            return View(receivingReport);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(ReceivingReport model)
        {
            var receivingReport = await _dbContext.ReceivingReports.FindAsync(model.Id);

            receivingReport.PurchaseOrders = await _dbContext.PurchaseOrders
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.PONo
                })
                .ToListAsync();

            if (ModelState.IsValid)
            {
                var existingModel = await _dbContext.ReceivingReports.FindAsync(model.Id);

                if (existingModel == null)
                {
                    return NotFound();
                }

                existingModel.Date = model.Date;
                existingModel.POId = model.POId;
                existingModel.PONo = await _receivingReportRepo.GetPONoAsync(model.POId);
                existingModel.DueDate = await _receivingReportRepo.ComputeDueDateAsync(model.POId, model.Date);
                existingModel.InvoiceOrDate = model.InvoiceOrDate;
                existingModel.TruckOrVessels = model.TruckOrVessels;
                existingModel.QuantityDelivered = model.QuantityDelivered;
                existingModel.QuantityReceived = model.QuantityReceived;
                existingModel.GainOrLoss = model.QuantityDelivered - model.QuantityReceived;
                existingModel.OtherRef = model.OtherRef;
                existingModel.Remarks = model.Remarks;

                #region --Audit Trail Recording

                AuditTrail auditTrail = new(existingModel.CreatedBy, $"Edit rr# {existingModel.RRNo}", "Receiving Report");
                _dbContext.Add(auditTrail);

                #endregion --Audit Trail Recording

                await _dbContext.SaveChangesAsync();

                TempData["success"] = "Receiving Report updated successfully";
                return RedirectToAction("Index");
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Print(int id)
        {
            if (id == null || _dbContext.ReceivingReports == null)
            {
                return NotFound();
            }

            var receivingReport = await _receivingReportRepo.FindRR(id);

            if (receivingReport == null)
            {
                return NotFound();
            }

            return View(receivingReport);
        }

        public async Task<IActionResult> Printed(int id)
        {
            var rr = await _dbContext.ReceivingReports.FindAsync(id);
            if (rr != null && !rr.IsPrinted)
            {
                rr.IsPrinted = true;
                await _dbContext.SaveChangesAsync();
            }
            return RedirectToAction("Print", new { id = id });
        }

        public async Task<IActionResult> Post(int id)
        {
            var model = await _receivingReportRepo.FindRR(id);

            if (model != null)
            {
                if (!model.IsPosted)
                {
                    model.IsPosted = true;
                    model.PostedBy = _userManager.GetUserName(this.User);
                    model.PostedDate = DateTime.Now;

                    #region --General Ledger Recording

                    var ledger = new List<GeneralLedgerBook>();

                    //ledger.Add(new GeneralLedgerBook
                    //{
                    //    Date = model.CreatedDate.ToShortDateString(),
                    //    Reference = model.RRNo,
                    //    Description = "Receipt of Goods",
                    //    AccountTitle = model.PurchaseOrder.Product.Name,
                    //    Debit =
                    //});

                    #endregion --General Ledger Recording

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(model.PostedBy, $"Posted receiving# {model.RRNo}", "Receiving Report");
                    _dbContext.Add(auditTrail);

                    #endregion --Audit Trail Recording

                    await _receivingReportRepo.UpdatePOAsync(model.PurchaseOrder.Id, model.QuantityReceived);

                    await _dbContext.SaveChangesAsync();
                    TempData["success"] = "Receiving Report has been Posted.";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    return RedirectToAction(nameof(Index));
                }
            }

            return NotFound();
        }

        public async Task<IActionResult> Void(int id)
        {
            var model = await _dbContext.ReceivingReports.FindAsync(id);

            if (model != null)
            {
                if (!model.IsVoided)
                {
                    model.IsVoided = true;
                    model.VoidedBy = _userManager.GetUserName(this.User);
                    model.VoidedDate = DateTime.Now;

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(model.VoidedBy, $"Voided receiving# {model.RRNo}", "Receiving Report");
                    _dbContext.Add(auditTrail);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync();
                    TempData["success"] = "Receiving Report has been Voided.";
                }
                return RedirectToAction("Index");
            }

            return NotFound();
        }

        public async Task<IActionResult> Cancel(int id)
        {
            var model = await _dbContext.ReceivingReports.FindAsync(id);

            if (model != null)
            {
                if (!model.IsCanceled)
                {
                    model.IsCanceled = true;
                    model.CanceledBy = _userManager.GetUserName(this.User);
                    model.CanceledDate = DateTime.Now;
                    //model.Status = "Canceled";

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(model.CanceledBy, $"Canceled receiving# {model.RRNo}", "Receiving Report");
                    _dbContext.Add(auditTrail);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync();
                    TempData["success"] = "Receiving Report has been Canceled.";
                }
                return RedirectToAction("Index");
            }

            return NotFound();
        }

        [HttpGet]
        public async Task<IActionResult> GetLiquidations(int id)
        {
            var po = await _receivingReportRepo.GetPurchaseOrderAsync(id);
            var rr = await _dbContext
                .ReceivingReports
                .Where(rr => rr.PONo == po.PONo && rr.IsPosted)
                .ToListAsync();

            if (po != null)
            {
                return Json(new
                {
                    poNo = po.PONo,
                    poQuantity = po.Quantity.ToString(),
                    rrList =  rr
                });
            }
            else
            {
                return Json(null);
            }
        }
    }
}