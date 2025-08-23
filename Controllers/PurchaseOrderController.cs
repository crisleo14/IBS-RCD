﻿using System.Globalization;
using Accounting_System.Data;
using Accounting_System.Models;
using Accounting_System.Models.AccountsPayable;
using Accounting_System.Models.Reports;
using Accounting_System.Models.ViewModels;
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
    public class PurchaseOrderController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly PurchaseOrderRepo _purchaseOrderRepo;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly InventoryRepo _inventoryRepo;

        public PurchaseOrderController(ApplicationDbContext dbContext, PurchaseOrderRepo purchaseOrderRepo, UserManager<IdentityUser> userManager, InventoryRepo inventoryRepo)
        {
            _dbContext = dbContext;
            _purchaseOrderRepo = purchaseOrderRepo;
            _userManager = userManager;
            _inventoryRepo = inventoryRepo;
        }

        public async Task<IActionResult> Index(string? view, CancellationToken cancellationToken)
        {
            var purchaseOrders = await _purchaseOrderRepo.GetPurchaseOrderAsync(cancellationToken);

            if (view == nameof(DynamicView.PurchaseOrder))
            {
                return View("ImportExportIndex", purchaseOrders);
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetPurchaseOrders([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var purchaseOrders = await _purchaseOrderRepo.GetPurchaseOrderAsync(cancellationToken);
                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();
                    purchaseOrders = purchaseOrders
                        .Where(po =>
                            po.PurchaseOrderNo!.ToLower().Contains(searchValue) ||
                            po.Date.ToString("MMM dd, yyyy").ToLower().Contains(searchValue) ||
                            po.Supplier!.SupplierName.ToLower().Contains(searchValue) ||
                            po.Product!.ProductName.ToLower().Contains(searchValue) ||
                            po.Amount.ToString(CultureInfo.InvariantCulture).Contains(searchValue) ||
                            po.Quantity.ToString(CultureInfo.InvariantCulture).Contains(searchValue) ||
                            po.Remarks.ToLower().Contains(searchValue) ||
                            po.CreatedBy!.ToLower().Contains(searchValue)
                            )
                        .ToList();
                }
                // Sorting
                if (parameters.Order != null && parameters.Order.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Data;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";
                    purchaseOrders = purchaseOrders
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}")
                        .ToList();
                }
                var totalRecords = purchaseOrders.Count();
                var pagedData = purchaseOrders
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
        public async Task<IActionResult> GetAllPurchaseOrderIds(CancellationToken cancellationToken)
        {
            var purchaseOrderIds = await _dbContext.PurchaseOrders
                                     .Select(po => po.PurchaseOrderId) // Assuming Id is the primary key
                                     .ToListAsync(cancellationToken);
            return Json(purchaseOrderIds);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var viewModel = new PurchaseOrder
            {
                Suppliers = await _dbContext.Suppliers
                    .Select(s => new SelectListItem
                    {
                        Value = s.SupplierId.ToString(),
                        Text = s.SupplierName
                    })
                    .ToListAsync(cancellationToken),
                Products = await _dbContext.Products
                    .Select(s => new SelectListItem
                    {
                        Value = s.ProductId.ToString(),
                        Text = s.ProductName
                    })
                    .ToListAsync(cancellationToken)
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PurchaseOrder model, CancellationToken cancellationToken)
        {
            model.Suppliers = await _dbContext.Suppliers
                .Select(s => new SelectListItem
                {
                    Value = s.SupplierId.ToString(),
                    Text = s.SupplierName
                })
                .ToListAsync(cancellationToken);

            model.Products = await _dbContext.Products
                .Select(s => new SelectListItem
                {
                    Value = s.ProductId.ToString(),
                    Text = s.ProductName
                })
                .ToListAsync(cancellationToken);

            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    var generatedPo = await _purchaseOrderRepo.GeneratePONo(cancellationToken);
                    var getLastNumber = long.Parse(generatedPo.Substring(2));

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

                    model.PurchaseOrderNo = generatedPo;
                    model.CreatedBy = User.Identity!.Name;
                    model.Amount = model.Quantity * model.Price;
                    model.SupplierNo = await _purchaseOrderRepo.GetSupplierNoAsync(model.SupplierId, cancellationToken);
                    model.ProductNo = await _purchaseOrderRepo.GetProductNoAsync(model.ProductId, cancellationToken);

                    await _dbContext.AddAsync(model, cancellationToken);

                    #region --Audit Trail Recording

                    if (model.OriginalSeriesNumber.IsNullOrEmpty() && model.OriginalDocumentId == 0)
                    {
                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        AuditTrail auditTrailBook = new(model.CreatedBy!, $"Create new purchase order# {model.PurchaseOrderNo}", "Purchase Order", ipAddress!);
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                    }

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                 await transaction.RollbackAsync(cancellationToken);
                 TempData["error"] = ex.Message;
                 return RedirectToAction(nameof(Index));
                }
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
            if (id == null || !_dbContext.PurchaseOrders.Any())
            {
                return NotFound();
            }

            var purchaseOrder = await _purchaseOrderRepo.FindPurchaseOrder(id, cancellationToken);

            purchaseOrder.Suppliers = await _dbContext.Suppliers
                .Select(s => new SelectListItem
                {
                    Value = s.SupplierId.ToString(),
                    Text = s.SupplierName
                })
                .ToListAsync(cancellationToken);

            purchaseOrder.Products = await _dbContext.Products
                .Select(s => new SelectListItem
                {
                    Value = s.ProductId.ToString(),
                    Text = s.ProductName
                })
                .ToListAsync(cancellationToken);

            ViewBag.PurchaseOrders = purchaseOrder.Quantity;

            return View(purchaseOrder);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(PurchaseOrder model, CancellationToken cancellationToken)
        {
            var existingModel = await _dbContext.PurchaseOrders.FirstOrDefaultAsync(x => x.PurchaseOrderId == model.PurchaseOrderId, cancellationToken);
            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    if (existingModel == null)
                    {
                        return NotFound();
                    }

                    existingModel.Date = model.Date;
                    existingModel.SupplierId = model.SupplierId;
                    existingModel.ProductId = model.ProductId;
                    existingModel.Quantity = model.Quantity;
                    existingModel.Price = model.Price;
                    existingModel.Amount = model.Quantity * model.Price;
                    existingModel.Remarks = model.Remarks;
                    existingModel.Terms = model.Terms;
                    existingModel.SupplierNo = await _purchaseOrderRepo.GetSupplierNoAsync(model.SupplierId, cancellationToken);
                    existingModel.ProductNo = await _purchaseOrderRepo.GetProductNoAsync(model.ProductId, cancellationToken);

                    if (_dbContext.ChangeTracker.HasChanges())
                    {
                        #region --Audit Trail Recording

                        if (existingModel.OriginalSeriesNumber.IsNullOrEmpty() && existingModel.OriginalDocumentId == 0)
                        {
                            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                            AuditTrail auditTrailBook = new(existingModel.CreatedBy!, $"Edit purchase order# {existingModel.PurchaseOrderNo}", "Purchase Order", ipAddress!);
                            await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                        }

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Purchase Order updated successfully";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        throw new InvalidOperationException("No data changes!");
                    }

                }
                catch (Exception ex)
                {
                 await transaction.RollbackAsync(cancellationToken);
                 existingModel!.Suppliers = await _dbContext.Suppliers
                     .Select(s => new SelectListItem
                     {
                         Value = s.SupplierId.ToString(),
                         Text = s.SupplierName
                     })
                     .ToListAsync(cancellationToken);

                 existingModel.Products = await _dbContext.Products
                     .Select(s => new SelectListItem
                     {
                         Value = s.ProductId.ToString(),
                         Text = s.ProductName
                     })
                     .ToListAsync(cancellationToken);
                 TempData["error"] = ex.Message;
                 return View(existingModel);
                }
            }

            existingModel!.Suppliers = await _dbContext.Suppliers
                .Select(s => new SelectListItem
                {
                    Value = s.SupplierId.ToString(),
                    Text = s.SupplierName
                })
                .ToListAsync(cancellationToken);

            existingModel.Products = await _dbContext.Products
                .Select(s => new SelectListItem
                {
                    Value = s.ProductId.ToString(),
                    Text = s.ProductName
                })
                .ToListAsync(cancellationToken);
            return View(existingModel);
        }

        [HttpGet]
        public async Task<IActionResult> Print(int? id, CancellationToken cancellationToken)
        {
            if (id == null || !_dbContext.ReceivingReports.Any())
            {
                return NotFound();
            }

            var purchaseOrder = await _purchaseOrderRepo
                .FindPurchaseOrder(id, cancellationToken);

            return View(purchaseOrder);
        }

        public async Task<IActionResult> Printed(int id, CancellationToken cancellationToken)
        {
            var po = await _dbContext.PurchaseOrders.FirstOrDefaultAsync(x => x.PurchaseOrderId == id, cancellationToken);
            if (po != null && !po.IsPrinted)
            {

                #region --Audit Trail Recording

                if (po.OriginalSeriesNumber.IsNullOrEmpty() && po.OriginalDocumentId == 0)
                {
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    var printedBy = User.Identity!.Name;
                    AuditTrail auditTrailBook = new(printedBy!, $"Printed original copy of po# {po.PurchaseOrderNo}", "Purchase Order", ipAddress!);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                }

                #endregion --Audit Trail Recording

                po.IsPrinted = true;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            return RedirectToAction(nameof(Print), new { id });
        }

        public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
        {
            var model = await _dbContext.PurchaseOrders.FirstOrDefaultAsync(x => x.PurchaseOrderId == id, cancellationToken);
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                if (model != null)
                {
                    if (!model.IsPosted)
                    {
                        model.IsPosted = true;
                        model.PostedBy = _userManager.GetUserName(this.User);
                        model.PostedDate = DateTime.Now;

                        #region --Audit Trail Recording

                        if (model.OriginalSeriesNumber.IsNullOrEmpty() && model.OriginalDocumentId == 0)
                        {
                            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                            AuditTrail auditTrailBook = new(model.PostedBy!, $"Posted purchase order# {model.PurchaseOrderNo}", "Purchase Order", ipAddress!);
                            await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                        }

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Purchase Order has been Posted.";
                    }
                    return RedirectToAction(nameof(Print), new { id });
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {
            var model = await _dbContext.PurchaseOrders.FirstOrDefaultAsync(x => x.PurchaseOrderId == id, cancellationToken);
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
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

                        #region --Audit Trail Recording

                        if (model.OriginalSeriesNumber.IsNullOrEmpty() && model.OriginalDocumentId == 0)
                        {
                            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                            AuditTrail auditTrailBook = new(model.VoidedBy!, $"Voided purchase order# {model.PurchaseOrderNo}", "Purchase Order", ipAddress!);
                            await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                        }

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Purchase Order has been Voided.";
                    }
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        public async Task<IActionResult> Cancel(int id, string cancellationRemarks, CancellationToken cancellationToken)
        {
            var model = await _dbContext.PurchaseOrders.FirstOrDefaultAsync(x => x.PurchaseOrderId == id, cancellationToken);
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                if (model != null)
                {
                    if (!model.IsCanceled)
                    {
                        model.IsCanceled = true;
                        model.CanceledBy = _userManager.GetUserName(this.User);
                        model.CanceledDate = DateTime.Now;
                        model.CancellationRemarks = cancellationRemarks;

                        #region --Audit Trail Recording

                        if (model.OriginalSeriesNumber.IsNullOrEmpty() && model.OriginalDocumentId == 0)
                        {
                            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                            AuditTrail auditTrailBook = new(model.CanceledBy!, $"Cancelled purchase order# {model.PurchaseOrderNo}", "Purchase Order", ipAddress!);
                            await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                        }

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Purchase Order has been Cancelled.";
                    }
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        [HttpGet]
        public async Task<IActionResult> ChangePrice(CancellationToken cancellationToken)
        {
            PurchaseChangePriceViewModel po = new()
            {
                PO = await _dbContext.PurchaseOrders
                    .Where(po => po.FinalPrice == 0 || po.FinalPrice == null && po.IsPosted && po.QuantityReceived != 0)
                    .Select(s => new SelectListItem
                    {
                        Value = s.PurchaseOrderId.ToString(),
                        Text = s.PurchaseOrderNo
                    })
                    .ToListAsync(cancellationToken)
            };

            return View(po);
        }

        [HttpPost]
        public async Task<IActionResult> ChangePrice(PurchaseChangePriceViewModel model, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    var existingModel = await _dbContext.PurchaseOrders.FirstOrDefaultAsync(x => x.PurchaseOrderId == model.POId, cancellationToken);

                    existingModel!.FinalPrice = model.FinalPrice;

                    #region--Inventory Recording

                    await _inventoryRepo.ChangePriceToInventoryAsync(model, User, cancellationToken);

                    #endregion

                    #region --Audit Trail Recording

                    if (existingModel.OriginalSeriesNumber.IsNullOrEmpty() && existingModel.OriginalDocumentId == 0)
                    {
                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        AuditTrail auditTrailBook = new(existingModel.CreatedBy!, $"Change price, purchase order# {existingModel.PurchaseOrderNo}", "Purchase Order", ipAddress!);
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                    }

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    TempData["success"] = "Change Price updated successfully";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    model.PO = await _dbContext.PurchaseOrders
                        .Where(po => po.FinalPrice == 0 || po.FinalPrice == null && po.IsPosted && po.QuantityReceived != 0)
                        .Select(s => new SelectListItem
                        {
                            Value = s.PurchaseOrderId.ToString(),
                            Text = s.PurchaseOrderNo
                        })
                        .ToListAsync(cancellationToken);

                    TempData["error"] = ex.Message;
                    return View(model);
                }

            }
            model.PO = await _dbContext.PurchaseOrders
                .Where(po => po.FinalPrice == 0 || po.FinalPrice == null && po.IsPosted)
                .Select(s => new SelectListItem
                {
                    Value = s.PurchaseOrderId.ToString(),
                    Text = s.PurchaseOrderNo
                })
                .ToListAsync(cancellationToken);

            TempData["error"] = "The information provided was invalid.";
            return View(nameof(ChangePrice));
        }

        [HttpGet]
        public async Task<IActionResult> ClosePO(int id, CancellationToken cancellationToken)
        {
            var purchaseOrder = await _dbContext.PurchaseOrders.FirstOrDefaultAsync(x => x.PurchaseOrderId == id, cancellationToken);

            if (purchaseOrder != null)
            {
                var rrList = await _dbContext.ReceivingReports
                    .Where(rr => rr.PONo == purchaseOrder.PurchaseOrderNo)
                    .ToListAsync(cancellationToken);

                purchaseOrder.RrList = rrList;

                return View(purchaseOrder);
            }

            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> ClosePO(PurchaseOrder model, CancellationToken cancellationToken)
        {
            var purchaseOrder = await _dbContext.PurchaseOrders.FirstOrDefaultAsync(x => x.PurchaseOrderId == model.PurchaseOrderId, cancellationToken);
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                if (purchaseOrder != null)
                {
                    if (!purchaseOrder.IsClosed)
                    {
                        purchaseOrder.IsClosed = true;

                        #region --Audit Trail Recording

                        if (model.OriginalSeriesNumber.IsNullOrEmpty() && model.OriginalDocumentId == 0)
                        {
                            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                            AuditTrail auditTrailBook = new(User.Identity!.Name!, $"Closed purchase order# {model.PurchaseOrderNo}", "Purchase Order", ipAddress!);
                            await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                        }

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Purchase Order has been Closed.";
                    }
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
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

                // Retrieve the selected invoices from the database
                var selectedList = await _dbContext.PurchaseOrders
                    .Where(po => recordIds.Contains(po.PurchaseOrderId))
                    .OrderBy(po => po.PurchaseOrderNo)
                    .ToListAsync(cancellationToken: cancellationToken);

                // Create the Excel package
                using var package = new ExcelPackage();
                // Add a new worksheet to the Excel package
                var worksheet = package.Workbook.Worksheets.Add("PurchaseOrder");

                worksheet.Cells["A1"].Value = "Date";
                worksheet.Cells["B1"].Value = "Terms";
                worksheet.Cells["C1"].Value = "Quantity";
                worksheet.Cells["D1"].Value = "Price";
                worksheet.Cells["E1"].Value = "Amount";
                worksheet.Cells["F1"].Value = "FinalPrice";
                worksheet.Cells["G1"].Value = "QuantityReceived";
                worksheet.Cells["H1"].Value = "IsReceived";
                worksheet.Cells["I1"].Value = "ReceivedDate";
                worksheet.Cells["J1"].Value = "Remarks";
                worksheet.Cells["K1"].Value = "CreatedBy";
                worksheet.Cells["L1"].Value = "CreatedDate";
                worksheet.Cells["M1"].Value = "IsClosed";
                worksheet.Cells["N1"].Value = "CancellationRemarks";
                worksheet.Cells["O1"].Value = "OriginalProductId";
                worksheet.Cells["P1"].Value = "OriginalPONo";
                worksheet.Cells["Q1"].Value = "OriginalSupplierId";
                worksheet.Cells["R1"].Value = "OriginalDocumentId";

                int row = 2;

                foreach (var item in selectedList)
                {
                    worksheet.Cells[row, 1].Value = item.Date.ToString("yyyy-MM-dd");
                    worksheet.Cells[row, 2].Value = item.Terms;
                    worksheet.Cells[row, 3].Value = item.Quantity;
                    worksheet.Cells[row, 4].Value = item.Price;
                    worksheet.Cells[row, 5].Value = item.Amount;
                    worksheet.Cells[row, 6].Value = item.FinalPrice;
                    worksheet.Cells[row, 7].Value = item.QuantityReceived;
                    worksheet.Cells[row, 8].Value = item.IsReceived;
                    worksheet.Cells[row, 9].Value = item.ReceivedDate != default ? item.ReceivedDate.ToString("yyyy-MM-dd HH:mm:ss.ffffff zzz") : null;
                    worksheet.Cells[row, 10].Value = item.Remarks;
                    worksheet.Cells[row, 11].Value = item.CreatedBy;
                    worksheet.Cells[row, 12].Value = item.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                    worksheet.Cells[row, 13].Value = item.IsClosed;
                    worksheet.Cells[row, 14].Value = item.CancellationRemarks;
                    worksheet.Cells[row, 15].Value = item.ProductId;
                    worksheet.Cells[row, 16].Value = item.PurchaseOrderNo;
                    worksheet.Cells[row, 17].Value = item.SupplierId;
                    worksheet.Cells[row, 18].Value = item.PurchaseOrderId;

                    row++;
                }

                // Convert the Excel package to a byte array
                var excelBytes = await package.GetAsByteArrayAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "PurchaseOrderList.xlsx");
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
        #region -- import xlsx record --

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

                try
                {
                    using var package = new ExcelPackage(stream);
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                    if (worksheet == null)
                    {
                        TempData["error"] = "The Excel file contains no worksheets.";
                        return RedirectToAction(nameof(Index), new { view = DynamicView.PurchaseOrder });
                    }
                    if (worksheet.ToString() != nameof(DynamicView.PurchaseOrder))
                    {
                        TempData["error"] = "The Excel file is not related to purchase order.";
                        return RedirectToAction(nameof(Index), new { view = DynamicView.PurchaseOrder });
                    }

                    var rowCount = worksheet.Dimension.Rows;
                    var poDictionary = new Dictionary<string, bool>();
                    var purchaseOrderList = await _dbContext
                        .PurchaseOrders
                        .ToListAsync(cancellationToken);

                    for (int row = 2; row <= rowCount; row++)  // Assuming the first row is the header
                    {
                        var purchaseOrder = new PurchaseOrder
                        {
                            PurchaseOrderNo = worksheet.Cells[row, 16].Text,
                            Date = DateOnly.TryParse(worksheet.Cells[row, 1].Text, out DateOnly dueDate) ? dueDate : default,
                            Terms = worksheet.Cells[row, 2].Text,
                            Quantity = decimal.TryParse(worksheet.Cells[row, 3].Text, out decimal quantity) ? quantity : 0,
                            Price = decimal.TryParse(worksheet.Cells[row, 4].Text, out decimal price) ? price : 0,
                            Amount = decimal.TryParse(worksheet.Cells[row, 5].Text, out decimal amount) ? amount : 0,
                            FinalPrice = decimal.TryParse(worksheet.Cells[row, 6].Text, out decimal finalPrice) ? finalPrice : 0,
                            // QuantityReceived = decimal.TryParse(worksheet.Cells[row, 7].Text, out decimal quantityReceived) ? quantityReceived : 0,
                            // IsReceived = bool.TryParse(worksheet.Cells[row, 8].Text, out bool isReceived) ? isReceived : default,
                            // ReceivedDate = DateTime.TryParse(worksheet.Cells[row, 9].Text, out DateTime receivedDate) ? receivedDate : default,
                            Remarks = worksheet.Cells[row, 10].Text,
                            CreatedBy = worksheet.Cells[row, 11].Text,
                            CreatedDate = DateTime.TryParse(worksheet.Cells[row, 12].Text, out DateTime createdDate) ? createdDate : default,
                            PostedBy = worksheet.Cells[row, 19].Text,
                            PostedDate = DateTime.TryParse(worksheet.Cells[row, 20].Text, out DateTime postedDate) ? postedDate : default,
                            IsClosed = bool.TryParse(worksheet.Cells[row, 13].Text, out bool isClosed) && isClosed,
                            CancellationRemarks = worksheet.Cells[row, 14].Text != "" ? worksheet.Cells[row, 14].Text : null,
                            OriginalProductId = int.TryParse(worksheet.Cells[row, 15].Text, out int originalProductId) ? originalProductId : 0,
                            OriginalSeriesNumber = worksheet.Cells[row, 16].Text,
                            OriginalSupplierId = int.TryParse(worksheet.Cells[row, 17].Text, out int originalSupplierId) ? originalSupplierId : 0,
                            OriginalDocumentId = int.TryParse(worksheet.Cells[row, 18].Text, out int originalDocumentId) ? originalDocumentId : 0,
                        };

                        if (!poDictionary.TryAdd(purchaseOrder.OriginalSeriesNumber, true))
                        {
                            continue;
                        }

                        if (purchaseOrderList.Any(po => po.OriginalDocumentId == purchaseOrder.OriginalDocumentId))
                        {
                            var poChanges = new Dictionary<string, (string OriginalValue, string NewValue)>();
                            var existingPo = await _dbContext.PurchaseOrders.FirstOrDefaultAsync(si => si.OriginalDocumentId == purchaseOrder.OriginalDocumentId, cancellationToken);

                            if (existingPo!.PurchaseOrderNo!.TrimStart().TrimEnd() != worksheet.Cells[row, 16].Text.TrimStart().TrimEnd())
                            {
                                poChanges["PONo"] = (existingPo.PurchaseOrderNo.TrimStart().TrimEnd(), worksheet.Cells[row, 16].Text.TrimStart().TrimEnd());
                            }

                            if (existingPo.Date.ToString("yyyy-MM-dd").TrimStart().TrimEnd() != worksheet.Cells[row, 1].Text.TrimStart().TrimEnd())
                            {
                                poChanges["Date"] = (existingPo.Date.ToString("yyyy-MM-dd").TrimStart().TrimEnd(), worksheet.Cells[row, 1].Text.TrimStart().TrimEnd());
                            }

                            if (existingPo.Terms.TrimStart().TrimEnd() != worksheet.Cells[row, 2].Text.TrimStart().TrimEnd())
                            {
                                poChanges["Terms"] = (existingPo.Terms.TrimStart().TrimEnd(), worksheet.Cells[row, 2].Text.TrimStart().TrimEnd());
                            }

                            if (existingPo.Quantity.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet.Cells[row, 3].Text).ToString("F2").TrimStart().TrimEnd())
                            {
                                poChanges["Quantity"] = (existingPo.Quantity.ToString("F2").TrimStart().TrimEnd(), decimal.Parse(worksheet.Cells[row, 3].Text).ToString("F2").TrimStart().TrimEnd());
                            }

                            if (existingPo.Price.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet.Cells[row, 4].Text).ToString("F2").TrimStart().TrimEnd())
                            {
                                poChanges["Price"] = (existingPo.Price.ToString("F2").TrimStart().TrimEnd(), decimal.Parse(worksheet.Cells[row, 4].Text).ToString("F2").TrimStart().TrimEnd());
                            }

                            if (existingPo.Amount.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet.Cells[row, 5].Text).ToString("F2").TrimStart().TrimEnd())
                            {
                                poChanges["Amount"] = (existingPo.Amount.ToString("F2").TrimStart().TrimEnd(), decimal.Parse(worksheet.Cells[row, 5].Text).ToString("F2").TrimStart().TrimEnd());
                            }

                            if (existingPo.FinalPrice?.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet.Cells[row, 6].Text).ToString("F2").TrimStart().TrimEnd())
                            {
                                poChanges["FinalPrice"] = (existingPo.FinalPrice?.ToString("F2").TrimStart().TrimEnd(), decimal.Parse(worksheet.Cells[row, 6].Text).ToString("F2").TrimStart().TrimEnd())!;
                            }

                            if (existingPo.Remarks.TrimStart().TrimEnd() != worksheet.Cells[row, 10].Text.TrimStart().TrimEnd())
                            {
                                poChanges["Remarks"] = (existingPo.Remarks.TrimStart().TrimEnd(), worksheet.Cells[row, 10].Text.TrimStart().TrimEnd());
                            }

                            if (existingPo.CreatedBy!.TrimStart().TrimEnd() != worksheet.Cells[row, 11].Text.TrimStart().TrimEnd())
                            {
                                poChanges["CreatedBy"] = (existingPo.CreatedBy.TrimStart().TrimEnd(), worksheet.Cells[row, 11].Text.TrimStart().TrimEnd());
                            }

                            if (existingPo.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff").TrimStart().TrimEnd() != worksheet.Cells[row, 12].Text.TrimStart().TrimEnd())
                            {
                                poChanges["CreatedDate"] = (existingPo.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff").TrimStart().TrimEnd(), worksheet.Cells[row, 12].Text.TrimStart().TrimEnd());
                            }

                            if (existingPo.IsClosed.ToString().ToUpper().TrimStart().TrimEnd() != worksheet.Cells[row, 13].Text.TrimStart().TrimEnd())
                            {
                                poChanges["IsClosed"] = (existingPo.IsClosed.ToString().ToUpper().TrimStart().TrimEnd(), worksheet.Cells[row, 13].Text.TrimStart().TrimEnd());
                            }

                            if ((string.IsNullOrWhiteSpace(existingPo.CancellationRemarks?.TrimStart().TrimEnd()) ? "" : existingPo.CancellationRemarks.TrimStart().TrimEnd()) != worksheet.Cells[row, 14].Text.TrimStart().TrimEnd())
                            {
                                poChanges["CancellationRemarks"] = (existingPo.CancellationRemarks?.TrimStart().TrimEnd(), worksheet.Cells[row, 14].Text.TrimStart().TrimEnd())!;
                            }

                            if (existingPo.OriginalProductId.ToString()!.TrimStart().TrimEnd() != (worksheet.Cells[row, 15].Text.TrimStart().TrimEnd() == "" ? 0.ToString() : worksheet.Cells[row, 15].Text.TrimStart().TrimEnd()))
                            {
                                poChanges["OriginalProductId"] = (existingPo.OriginalProductId.ToString()!.TrimStart().TrimEnd(), worksheet.Cells[row, 15].Text.TrimStart().TrimEnd() == "" ? 0.ToString() : worksheet.Cells[row, 15].Text.TrimStart().TrimEnd());
                            }

                            if (existingPo.OriginalSeriesNumber!.TrimStart().TrimEnd() != worksheet.Cells[row, 16].Text.TrimStart().TrimEnd())
                            {
                                poChanges["OriginalSeriesNumber"] = (existingPo.OriginalSeriesNumber.TrimStart().TrimEnd(), worksheet.Cells[row, 16].Text.TrimStart().TrimEnd());
                            }

                            if (existingPo.OriginalSupplierId.ToString()!.TrimStart().TrimEnd() != (worksheet.Cells[row, 17].Text.TrimStart().TrimEnd() == "" ? 0.ToString() : worksheet.Cells[row, 17].Text.TrimStart().TrimEnd()))
                            {
                                poChanges["SupplierId"] = (existingPo.SupplierId.ToString()!.TrimStart().TrimEnd(), worksheet.Cells[row, 17].Text.TrimStart().TrimEnd() == "" ? 0.ToString() : worksheet.Cells[row, 17].Text.TrimStart().TrimEnd());
                            }

                            if (existingPo.OriginalDocumentId.ToString().TrimStart().TrimEnd() != (worksheet.Cells[row, 18].Text.TrimStart().TrimEnd() == "" ? 0.ToString() : worksheet.Cells[row, 18].Text.TrimStart().TrimEnd()))
                            {
                                poChanges["OriginalDocumentId"] = (existingPo.OriginalDocumentId.ToString().TrimStart().TrimEnd(), worksheet.Cells[row, 18].Text.TrimStart().TrimEnd() == "" ? 0.ToString() : worksheet.Cells[row, 18].Text.TrimStart().TrimEnd());
                            }

                            if (poChanges.Any())
                            {
                                await _purchaseOrderRepo.LogChangesAsync(existingPo.OriginalDocumentId, poChanges, _userManager.GetUserName(this.User), existingPo.PurchaseOrderNo);
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
                    await transaction.CommitAsync(cancellationToken);

                    var checkChangesOfRecord = await _dbContext.ImportExportLogs
                        .Where(iel => iel.Action == string.Empty).ToListAsync(cancellationToken);
                    if (checkChangesOfRecord.Any())
                    {
                        TempData["importChanges"] = "";
                    }
                }
                catch (OperationCanceledException oce)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = oce.Message;
                    return RedirectToAction(nameof(Index), new { view = DynamicView.PurchaseOrder });
                }
                catch (InvalidOperationException ioe)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["warning"] = ioe.Message;
                    return RedirectToAction(nameof(Index), new { view = DynamicView.PurchaseOrder });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(Index), new { view = DynamicView.PurchaseOrder });
                }
            }
            TempData["success"] = "Uploading Success!";
            return RedirectToAction(nameof(Index), new { view = DynamicView.PurchaseOrder });
        }

        #endregion -- import xlsx record --
    }
}
