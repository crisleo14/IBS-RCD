using Accounting_System.Data;
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
    public class PurchaseOrderController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly PurchaseOrderRepo _purchaseOrderRepo;

        private readonly UserManager<IdentityUser> _userManager;

        public PurchaseOrderController(ApplicationDbContext dbContext, PurchaseOrderRepo purchaseOrderRepo, UserManager<IdentityUser> userManager)
        {
            _dbContext = dbContext;
            _purchaseOrderRepo = purchaseOrderRepo;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var purchaseOrder = await _purchaseOrderRepo.GetPurchaseOrderAsync(cancellationToken);

            return View(purchaseOrder);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var viewModel = new PurchaseOrder();
            viewModel.Suppliers = await _dbContext.Suppliers
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Name
                })
                .ToListAsync(cancellationToken);

            viewModel.Products = await _dbContext.Products
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Name
                })
                .ToListAsync(cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PurchaseOrder model, CancellationToken cancellationToken)
        {
            model.Suppliers = await _dbContext.Suppliers
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Name
                })
                .ToListAsync(cancellationToken);

            model.Products = await _dbContext.Products
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Name
                })
                .ToListAsync(cancellationToken);

            if (ModelState.IsValid)
            {
                var getLastNumber = await _purchaseOrderRepo.GetLastSeriesNumber(cancellationToken);

                if (getLastNumber > 9999999999)
                {
                    TempData["error"] = "You reach the maximum Series Number";
                    return View(model);
                }
                var totalRemainingSeries = 9999999999 - getLastNumber;
                if (getLastNumber >= 9999999899)
                {
                    TempData["warning"] = $"Purchase Order created successfully, Warning {totalRemainingSeries} series number remaining";
                }
                else
                {
                    TempData["success"] = "Purchase Order created successfully";
                }

                var generatedPO = await _purchaseOrderRepo.GeneratePONo(cancellationToken);

                model.SeriesNumber = getLastNumber;
                model.PONo = generatedPO;
                model.CreatedBy = _userManager.GetUserName(this.User);
                model.Amount = model.Quantity * model.Price;
                model.SupplierNo = await _purchaseOrderRepo.GetSupplierNoAsync(model.SupplierId, cancellationToken);
                model.ProductNo = await _purchaseOrderRepo.GetProductNoAsync(model.ProductId, cancellationToken);

                await _dbContext.AddAsync(model, cancellationToken);

                #region --Audit Trail Recording

                AuditTrail auditTrail = new(model.CreatedBy, $"Create new purchase order# {model.PONo}", "Purchase Order");
                await _dbContext.AddAsync(auditTrail, cancellationToken);

                #endregion --Audit Trail Recording

                await _dbContext.SaveChangesAsync(cancellationToken);
                return RedirectToAction("Index");
            }
            else
            {
                ModelState.AddModelError("", "The information you submitted is not valid!");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
            if (id == null || _dbContext.PurchaseOrders == null)
            {
                return NotFound();
            }

            var purchaseOrder = await _purchaseOrderRepo.FindPurchaseOrder(id, cancellationToken);
            if (purchaseOrder == null)
            {
                return NotFound();
            }

            purchaseOrder.Suppliers = await _dbContext.Suppliers
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Name
                })
                .ToListAsync(cancellationToken);

            purchaseOrder.Products = await _dbContext.Products
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Name
                })
                .ToListAsync(cancellationToken);

            ViewBag.PurchaseOrders = purchaseOrder.Quantity;

            return View(purchaseOrder);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(PurchaseOrder model, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                var existingModel = await _dbContext.PurchaseOrders.FindAsync(model.Id, cancellationToken);

                if (existingModel == null)
                {
                    return NotFound();
                }

                model.Suppliers = await _dbContext.Suppliers
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Name
                })
                .ToListAsync(cancellationToken);

                model.Products = await _dbContext.Products
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Name
                })
                .ToListAsync(cancellationToken);

                existingModel.Date = model.Date;
                existingModel.SupplierId = model.SupplierId;
                existingModel.ProductId = model.ProductId;
                existingModel.Quantity = model.Quantity;
                existingModel.Price = model.Price;
                existingModel.Amount = model.Quantity * model.Price;
                existingModel.Remarks = model.Remarks;
                existingModel.SupplierNo = await _purchaseOrderRepo.GetSupplierNoAsync(model.SupplierId, cancellationToken);
                existingModel.ProductNo = await _purchaseOrderRepo.GetProductNoAsync(model.ProductId, cancellationToken);

                #region --Audit Trail Recording

                AuditTrail auditTrail = new(existingModel.CreatedBy, $"Edit purchase order# {existingModel.PONo}", "Purchase Order");
                await _dbContext.AddAsync(auditTrail, cancellationToken);

                #endregion --Audit Trail Recording

                await _dbContext.SaveChangesAsync(cancellationToken);

                TempData["success"] = "Purchase Order updated successfully";
                return RedirectToAction("Index");
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Print(int? id, CancellationToken cancellationToken)
        {
            if (id == null || _dbContext.ReceivingReports == null)
            {
                return NotFound();
            }

            var purchaseOrder = await _purchaseOrderRepo
                .FindPurchaseOrder(id, cancellationToken);
            if (purchaseOrder == null)
            {
                return NotFound();
            }

            return View(purchaseOrder);
        }

        public async Task<IActionResult> Printed(int id, CancellationToken cancellationToken)
        {
            var po = await _dbContext.PurchaseOrders.FindAsync(id, cancellationToken);
            if (po != null && !po.IsPrinted)
            {
                #region --Audit Trail Recording

                var printedBy = _userManager.GetUserName(this.User);
                AuditTrail auditTrail = new(printedBy, $"Printed original copy of po# {po.PONo}", "Purchase Order");
                await _dbContext.AddAsync(auditTrail, cancellationToken);

                #endregion --Audit Trail Recording

                po.IsPrinted = true;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            return RedirectToAction("Print", new { id = id });
        }

        public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
        {
            var model = await _dbContext.PurchaseOrders.FindAsync(id, cancellationToken);

            if (model != null)
            {
                if (!model.IsPosted)
                {
                    model.IsPosted = true;
                    model.PostedBy = _userManager.GetUserName(this.User);
                    model.PostedDate = DateTime.Now;

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(model.PostedBy, $"Posted purchase order# {model.PONo}", "Purchase Order");
                    await _dbContext.AddAsync(auditTrail, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Purchase Order has been Posted.";
                }
                return RedirectToAction("Index");
            }

            return NotFound();
        }

        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {
            var model = await _dbContext.PurchaseOrders.FindAsync(id, cancellationToken);

            if (model != null)
            {
                if (!model.IsVoided)
                {
                    model.IsVoided = true;
                    model.VoidedBy = _userManager.GetUserName(this.User);
                    model.VoidedDate = DateTime.Now;

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(model.VoidedBy, $"Voided purchase order# {model.PONo}", "Purchase Order");
                    await _dbContext.AddAsync(auditTrail, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Purchase Order has been Voided.";
                }
                return RedirectToAction("Index");
            }

            return NotFound();
        }

        public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
        {
            var model = await _dbContext.PurchaseOrders.FindAsync(id, cancellationToken);

            if (model != null)
            {
                if (!model.IsCanceled)
                {
                    model.IsCanceled = true;
                    model.CanceledBy = _userManager.GetUserName(this.User);
                    model.CanceledDate = DateTime.Now;

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(model.CanceledBy, $"Cancelled purchase order# {model.PONo}", "Purchase Order");
                    await _dbContext.AddAsync(auditTrail, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Purchase Order has been Cancelled.";
                }
                return RedirectToAction("Index");
            }

            return NotFound();
        }

        [HttpGet]
        public async Task<IActionResult> Preview(int? id, CancellationToken cancellationToken)
        {
            var po = await _purchaseOrderRepo.FindPurchaseOrder(id, cancellationToken);
            return PartialView("_PreviewPartialView", po);
        }
    }
}