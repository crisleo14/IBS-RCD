using Accounting_System.Data;
using Accounting_System.Models;
using Accounting_System.Models.AccountsReceivable;
using Accounting_System.Models.Reports;
using Accounting_System.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace Accounting_System.Controllers
{
    [Authorize]
    public class SalesInvoiceController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly SalesInvoiceRepo _salesInvoiceRepo;

        private readonly ILogger<HomeController> _logger;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly InventoryRepo _inventoryRepo;

        private readonly GeneralRepo _generalRepo;

        public SalesInvoiceController(ILogger<HomeController> logger, ApplicationDbContext dbContext, SalesInvoiceRepo salesInvoiceRepo, UserManager<IdentityUser> userManager, InventoryRepo inventoryRepo, GeneralRepo generalRepo)
        {
            _dbContext = dbContext;
            _salesInvoiceRepo = salesInvoiceRepo;
            _logger = logger;
            this._userManager = userManager;
            _inventoryRepo = inventoryRepo;
            _generalRepo = generalRepo;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var salesInvoice = await _salesInvoiceRepo.GetSalesInvoicesAsync(cancellationToken);

            return View(salesInvoice);
        }

        public async Task<IActionResult> ImportExportIndex(CancellationToken cancellationToken)
        {
            var salesInvoice = await _salesInvoiceRepo.GetSalesInvoicesAsync(cancellationToken);

            return View(salesInvoice);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var viewModel = new SalesInvoice();
            viewModel.Customers = await _dbContext.Customers
                .OrderBy(c => c.Id)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                })
                .ToListAsync(cancellationToken);
            viewModel.Products = await _dbContext.Products
                .OrderBy(p => p.Id)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.Name
                })
                .ToListAsync(cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SalesInvoice sales, CancellationToken cancellationToken)
        {
            sales.Customers = await _dbContext.Customers
                .OrderBy(c => c.Id)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                })
                .ToListAsync(cancellationToken);
            sales.Products = await _dbContext.Products
                .OrderBy(p => p.Code)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.Name
                })
                .ToListAsync(cancellationToken);
            sales.PO = await _dbContext.PurchaseOrders
                .OrderBy(c => c.Id)
                .Where(p => !p.IsReceived && p.QuantityReceived != 0 && p.IsPosted)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.PONo
                })
                .ToListAsync(cancellationToken);
            if (ModelState.IsValid)
            {
                #region -- Validating Series --

                var getLastNumber = await _salesInvoiceRepo.GetLastSeriesNumber(cancellationToken);

                if (getLastNumber > 9999999999)
                {
                    TempData["error"] = "You reach the maximum Series Number";
                    return View(sales);
                }
                var totalRemainingSeries = 9999999999 - getLastNumber;
                if (getLastNumber >= 9999999899)
                {
                    TempData["warning"] = $"Sales Invoice created successfully, Warning {totalRemainingSeries} series number remaining";
                }
                else
                {
                    TempData["success"] = "Sales Invoice created successfully";
                }

                #endregion -- Validating Series --

                #region -- Saving Default Entries --

                var generateCRNo = await _salesInvoiceRepo.GenerateSINo(cancellationToken);
                var existingCustomers = await _dbContext.Customers
                                               .FirstOrDefaultAsync(si => si.Id == sales.CustomerId, cancellationToken);

                sales.SeriesNumber = getLastNumber;
                sales.CreatedBy = _userManager.GetUserName(this.User);
                sales.SINo = generateCRNo;
                sales.Amount = sales.Quantity * sales.UnitPrice;
                sales.DueDate = _salesInvoiceRepo.ComputeDueDateAsync(existingCustomers.Terms, sales.TransactionDate, cancellationToken);
                if (sales.Amount >= sales.Discount)
                {
                    if (existingCustomers.CustomerType == "Vatable")
                    {
                        sales.NetDiscount = sales.Amount - sales.Discount;
                        sales.VatableSales = sales.NetDiscount / 1.12m;
                        sales.VatAmount = sales.NetDiscount - sales.VatableSales;
                        if (existingCustomers.WithHoldingTax)
                        {
                            sales.WithHoldingTaxAmount = sales.VatableSales * 0.01m;
                        }
                        if (existingCustomers.WithHoldingVat)
                        {
                            sales.WithHoldingVatAmount = sales.VatableSales * 0.05m;
                        }
                    }
                    else if (existingCustomers.CustomerType == "Zero Rated")
                    {
                        sales.NetDiscount = sales.Amount - sales.Discount;
                        sales.ZeroRated = sales.Amount;

                        if (existingCustomers.WithHoldingTax)
                        {
                            sales.WithHoldingTaxAmount = sales.ZeroRated * 0.01m;
                        }
                        if (existingCustomers.WithHoldingVat)
                        {
                            sales.WithHoldingVatAmount = sales.ZeroRated * 0.05m;
                        }
                    }
                    else
                    {
                        sales.NetDiscount = sales.Amount - sales.Discount;
                        sales.VatExempt = sales.Amount;
                        if (existingCustomers.WithHoldingTax)
                        {
                            sales.WithHoldingTaxAmount = sales.VatExempt * 0.01m;
                        }
                        if (existingCustomers.WithHoldingVat)
                        {
                            sales.WithHoldingVatAmount = sales.VatExempt * 0.05m;
                        }
                    }
                    await _dbContext.AddAsync(sales, cancellationToken);
                }
                else
                {
                    TempData["error"] = "Please input below or exact amount based on the Sales Invoice";
                    return View(sales);
                }

                #region --Audit Trail Recording

                AuditTrail auditTrail = new(sales.CreatedBy, $"Create new invoice# {sales.SINo}", "Sales Invoice");
                await _dbContext.AddAsync(auditTrail, cancellationToken);

                #endregion --Audit Trail Recording

                await _dbContext.SaveChangesAsync(cancellationToken);
                return RedirectToAction(nameof(Index));

                #endregion -- Saving Default Entries --
            }
            else
            {
                ModelState.AddModelError("", "The information you submitted is not valid!");
                return View(sales);
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetCustomerDetails(int customerId, CancellationToken cancellationToken)
        {
            var customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken);
            if (customer != null)
            {
                return Json(new
                {
                    SoldTo = customer.Name,
                    customer.Address,
                    customer.TinNo,
                    customer.BusinessStyle,
                    customer.Terms,
                    customer.CustomerType,
                    customer.WithHoldingTax
                });
            }
            return Json(null); // Return null if no matching customer is found
        }

        [HttpGet]
        public async Task<JsonResult> GetProductDetails(int productId, CancellationToken cancellationToken)
        {
            var product = await _dbContext.Products.FirstOrDefaultAsync(c => c.Id == productId, cancellationToken);
            if (product != null)
            {
                return Json(new
                {
                    ProductName = product.Name,
                    ProductUnit = product.Unit
                });
            }
            return Json(null); // Return null if no matching product is found
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            try
            {
                var salesInvoice = await _salesInvoiceRepo.FindSalesInvoice(id, cancellationToken);
                salesInvoice.Customers = await _dbContext.Customers
                .OrderBy(c => c.Id)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                })
                .ToListAsync(cancellationToken);
                salesInvoice.Products = await _dbContext.Products
                .OrderBy(p => p.Id)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.Name
                })
                .ToListAsync(cancellationToken);

                salesInvoice.PO = await _dbContext.PurchaseOrders
               .OrderBy(p => p.PONo)
               .Where(po => po.ProductId == salesInvoice.ProductId && po.QuantityReceived != 0 && po.PostedBy != null)
               .Select(p => new SelectListItem
               {
                   Value = p.Id.ToString(),
                   Text = p.PONo
               })
               .ToListAsync(cancellationToken);

                var receivingReports = await _dbContext.ReceivingReports
                    .Where(rr => rr.POId == salesInvoice.POId && rr.ReceivedDate != null)
                    .Select(rr => new
                    {
                        rr.Id,
                        rr.RRNo,
                        rr.ReceivedDate
                    })
                    .ToListAsync();

                salesInvoice.RR = receivingReports.Select(rr => new SelectListItem
                {
                    Value = rr.Id.ToString(),
                    Text = rr.RRNo
                }).ToList();

                return View(salesInvoice);
            }
            catch (Exception ex)
            {
                // Handle other exceptions, log them, and return an error response.
                _logger.LogError(ex, "An error occurred.");
                return StatusCode(500, "An error occurred. Please try again later.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SalesInvoice model, CancellationToken cancellationToken)
        {
            try
            {
                #region -- Checking existing record --

                var existingModel = await _salesInvoiceRepo.FindSalesInvoice(model.Id, cancellationToken);

                if (existingModel == null)
                {
                    return NotFound(); // Return a "Not Found" response when the entity is not found.
                }

                #endregion -- Checking existing record --

                #region -- Validating Series --

                var getLastNumber = await _salesInvoiceRepo.GetLastSeriesNumber(cancellationToken);

                if (getLastNumber > 9999999999)
                {
                    TempData["error"] = "You reach the maximum Series Number";
                    return View(model);
                }
                var totalRemainingSeries = 9999999999 - getLastNumber;
                if (getLastNumber >= 9999999899)
                {
                    TempData["warning"] = $"Sales Invoice created successfully, Warning {totalRemainingSeries} series number remaining";
                }
                else
                {
                    TempData["success"] = "Sales Invoice created successfully";
                }

                #endregion -- Validating Series --

                if (ModelState.IsValid)
                {
                    #region -- Saving Default Enries --

                    existingModel.CustomerId = model.CustomerId;
                    existingModel.TransactionDate = model.TransactionDate;
                    existingModel.OtherRefNo = model.OtherRefNo;
                    existingModel.POId = model.POId;
                    existingModel.Quantity = model.Quantity;
                    existingModel.UnitPrice = model.UnitPrice;
                    existingModel.Remarks = model.Remarks;
                    existingModel.Discount = model.Discount;
                    existingModel.Amount = model.Quantity * model.UnitPrice;
                    existingModel.ProductId = model.ProductId;
                    existingModel.ReceivingReportId = model.ReceivingReportId;
                    existingModel.DueDate = _salesInvoiceRepo.ComputeDueDateAsync(existingModel.Customer.Terms, existingModel.TransactionDate, cancellationToken);

                    if (existingModel.Amount >= model.Discount)
                    {
                        if (existingModel.Customer.CustomerType == "Vatable")
                        {
                            existingModel.NetDiscount = existingModel.Amount - model.Discount;
                            existingModel.VatableSales = existingModel.NetDiscount / 1.12m;
                            existingModel.VatAmount = existingModel.NetDiscount - existingModel.VatableSales;
                            if (existingModel.Customer.WithHoldingTax)
                            {
                                existingModel.WithHoldingTaxAmount = existingModel.VatableSales * (decimal)0.01;
                            }
                            if (existingModel.Customer.WithHoldingVat)
                            {
                                existingModel.WithHoldingVatAmount = existingModel.VatableSales * (decimal)0.05;
                            }
                        }
                        else if (existingModel.Customer.CustomerType == "Zero Rated")
                        {
                            existingModel.NetDiscount = existingModel.Amount - model.Discount;
                            existingModel.ZeroRated = existingModel.Amount;

                            if (existingModel.Customer.WithHoldingTax)
                            {
                                existingModel.WithHoldingTaxAmount = existingModel.ZeroRated * 0.01m;
                            }
                            if (existingModel.Customer.WithHoldingVat)
                            {
                                existingModel.WithHoldingVatAmount = existingModel.ZeroRated * 0.05m;
                            }
                        }
                        else
                        {
                            existingModel.NetDiscount = existingModel.Amount - model.Discount;
                            existingModel.VatExempt = existingModel.Amount;
                            if (existingModel.Customer.WithHoldingTax)
                            {
                                existingModel.WithHoldingTaxAmount = existingModel.VatExempt * 0.01m;
                            }
                            if (existingModel.Customer.WithHoldingVat)
                            {
                                existingModel.WithHoldingVatAmount = existingModel.VatExempt * 0.05m;
                            }
                        }

                        #region --Audit Trail Recording

                        var modifiedBy = _userManager.GetUserName(this.User);
                        AuditTrail auditTrail = new(modifiedBy, $"Edited invoice# {existingModel.SINo}", "Sales Invoice");
                        await _dbContext.AddAsync(auditTrail, cancellationToken);

                        #endregion --Audit Trail Recording
                    }
                    else
                    {
                        TempData["error"] = "Please input below or exact amount based unit price multiply quantity";
                        return View(model);
                    }

                    #endregion -- Saving Default Enries --
                }
                else
                {
                    ModelState.AddModelError("", "The information you submitted is not valid!");
                    return View(model);
                }

                // Save the changes to the database
                await _dbContext.SaveChangesAsync(cancellationToken);

                return RedirectToAction(nameof(Index)); // Redirect to a success page or the index page
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred.");
                return StatusCode(500, "An error occurred. Please try again later.");
            }
        }

        public async Task<IActionResult> PrintInvoice(int id, CancellationToken cancellationToken)
        {
            var sales = await _salesInvoiceRepo.FindSalesInvoice(id, cancellationToken);
            return View(sales);
        }

        public async Task<IActionResult> PrintedInvoice(int id, CancellationToken cancellationToken)
        {
            var sales = await _salesInvoiceRepo.FindSalesInvoice(id, cancellationToken);
            if (sales != null && !sales.IsPrinted)
            {
                sales.IsPrinted = true;

                #region --Audit Trail Recording

                var printedBy = _userManager.GetUserName(this.User);
                AuditTrail auditTrail = new(printedBy, $"Printed original copy of invoice# {sales.SINo}", "Sales Invoice");
                await _dbContext.AddAsync(auditTrail, cancellationToken);

                #endregion --Audit Trail Recording

                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            return RedirectToAction(nameof(PrintInvoice), new { id });
        }

        public async Task<IActionResult> Post(int invoiceId, CancellationToken cancellationToken)
        {
            var model = await _salesInvoiceRepo.FindSalesInvoice(invoiceId, cancellationToken);

            if (model != null)
            {
                try
                {
                    if (!model.IsPosted)
                    {
                        model.IsPosted = true;
                        model.PostedBy = _userManager.GetUserName(this.User);
                        model.PostedDate = DateTime.Now;

                        #region --Sales Book Recording

                        var sales = new SalesBook();

                        if (model.Customer.CustomerType == "Vatable")
                        {
                            sales.TransactionDate = model.TransactionDate;
                            sales.SerialNo = model.SINo;
                            sales.SoldTo = model.Customer.Name;
                            sales.TinNo = model.Customer.TinNo;
                            sales.Address = model.Customer.Address;
                            sales.Description = model.Product.Name;
                            sales.Amount = model.Amount;
                            sales.VatAmount = model.VatAmount;
                            sales.VatableSales = model.VatableSales;
                            sales.Discount = model.Discount;
                            sales.NetSales = model.NetDiscount / 1.12m;
                            sales.CreatedBy = model.CreatedBy;
                            sales.CreatedDate = model.CreatedDate;
                            sales.DueDate = model.DueDate;
                            sales.DocumentId = model.Id;
                        }
                        else if (model.Customer.CustomerType == "Exempt")
                        {
                            sales.TransactionDate = model.TransactionDate;
                            sales.SerialNo = model.SINo;
                            sales.SoldTo = model.Customer.Name;
                            sales.TinNo = model.Customer.TinNo;
                            sales.Address = model.Customer.Address;
                            sales.Description = model.Product.Name;
                            sales.Amount = model.Amount;
                            sales.VatExemptSales = model.Amount;
                            sales.Discount = model.Discount;
                            sales.NetSales = model.NetDiscount;
                            sales.CreatedBy = model.CreatedBy;
                            sales.CreatedDate = model.CreatedDate;
                            sales.DueDate = model.DueDate;
                            sales.DocumentId = model.Id;
                        }
                        else
                        {
                            sales.TransactionDate = model.TransactionDate;
                            sales.SerialNo = model.SINo;
                            sales.SoldTo = model.Customer.Name;
                            sales.TinNo = model.Customer.TinNo;
                            sales.Address = model.Customer.Address;
                            sales.Description = model.Product.Name;
                            sales.Amount = model.Amount;
                            sales.ZeroRated = model.Amount;
                            sales.Discount = model.Discount;
                            sales.NetSales = model.NetDiscount;
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
                                Date = model.TransactionDate,
                                Reference = model.SINo,
                                Description = model.Product.Name,
                                AccountNo = "1010201",
                                AccountTitle = "AR-Trade Receivable",
                                Debit = model.NetDiscount - (model.WithHoldingTaxAmount + model.WithHoldingVatAmount),
                                Credit = 0,
                                CreatedBy = model.CreatedBy,
                                CreatedDate = model.CreatedDate
                            }
                        );

                        if (model.WithHoldingTaxAmount > 0)
                        {
                            ledgers.Add(
                                new GeneralLedgerBook
                                {
                                    Date = model.TransactionDate,
                                    Reference = model.SINo,
                                    Description = model.Product.Name,
                                    AccountNo = "1010202",
                                    AccountTitle = "Deferred Creditable Withholding Tax",
                                    Debit = model.WithHoldingTaxAmount,
                                    Credit = 0,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }
                        if (model.WithHoldingVatAmount > 0)
                        {
                            ledgers.Add(
                                new GeneralLedgerBook
                                {
                                    Date = model.TransactionDate,
                                    Reference = model.SINo,
                                    Description = model.Product.Name,
                                    AccountNo = "1010203",
                                    AccountTitle = "Deferred Creditable Withholding Vat",
                                    Debit = model.WithHoldingVatAmount,
                                    Credit = 0,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }
                        if (model.Product.Name == "Biodiesel")
                        {
                            ledgers.Add(
                                new GeneralLedgerBook
                                {
                                    Date = model.TransactionDate,
                                    Reference = model.SINo,
                                    Description = model.Product.Name,
                                    AccountNo = "4010101",
                                    AccountTitle = "Sales - Biodiesel",
                                    Debit = 0,
                                    Credit = model.VatableSales > 0
                                                ? model.VatableSales
                                                : (model.ZeroRated + model.VatExempt) - model.Discount,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }
                        else if (model.Product.Name == "Econogas")
                        {
                            ledgers.Add(
                                new GeneralLedgerBook
                                {
                                    Date = model.TransactionDate,
                                    Reference = model.SINo,
                                    Description = model.Product.Name,
                                    AccountNo = "4010102",
                                    AccountTitle = "Sales - Econogas",
                                    Debit = 0,
                                    Credit = model.VatableSales > 0
                                                ? model.VatableSales
                                                : (model.ZeroRated + model.VatExempt) - model.Discount,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }
                        else if (model.Product.Name == "Envirogas")
                        {
                            ledgers.Add(
                                new GeneralLedgerBook
                                {
                                    Date = model.TransactionDate,
                                    Reference = model.SINo,
                                    Description = model.Product.Name,
                                    AccountNo = "4010103",
                                    AccountTitle = "Sales - Envirogas",
                                    Debit = 0,
                                    Credit = model.VatableSales > 0
                                                ? model.VatableSales
                                                : (model.ZeroRated + model.VatExempt) - model.Discount,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }

                        if (model.VatAmount > 0)
                        {
                            ledgers.Add(
                                new GeneralLedgerBook
                                {
                                    Date = model.TransactionDate,
                                    Reference = model.SINo,
                                    Description = model.Product.Name,
                                    AccountNo = "2010301",
                                    AccountTitle = "Vat Output",
                                    Debit = 0,
                                    Credit = model.VatAmount,
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

                        #region--Inventory Recording

                        await _inventoryRepo.AddSalesToInventoryAsync(model, User, cancellationToken);

                        #endregion

                        #region --Audit Trail Recording

                        AuditTrail auditTrail = new(model.PostedBy, $"Posted invoice# {model.SINo}", "Sales Invoice");
                        await _dbContext.AddAsync(auditTrail, cancellationToken);

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        TempData["success"] = "Sales Invoice has been Posted.";
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

        public async Task<IActionResult> Void(int invoiceId, CancellationToken cancellationToken)
        {
            var model = await _dbContext.SalesInvoices.FindAsync(invoiceId, cancellationToken);

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

                    await _generalRepo.RemoveRecords<SalesBook>(sb => sb.SerialNo == model.SINo, cancellationToken);
                    await _generalRepo.RemoveRecords<GeneralLedgerBook>(gl => gl.Reference == model.SINo, cancellationToken);
                    await _generalRepo.RemoveRecords<Inventory>(i => i.Reference == model.SINo, cancellationToken);

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(model.VoidedBy, $"Voided invoice# {model.SINo}", "Sales Invoice");
                    await _dbContext.AddAsync(auditTrail, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Sales Invoice has been Voided.";
                }
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        public async Task<IActionResult> Cancel(int invoiceId, string cancellationRemarks, CancellationToken cancellationToken)
        {
            var model = await _dbContext.SalesInvoices.FindAsync(invoiceId, cancellationToken);

            if (model != null)
            {
                if (!model.IsCanceled)
                {
                    model.IsCanceled = true;
                    model.CanceledBy = _userManager.GetUserName(this.User);
                    model.CanceledDate = DateTime.Now;
                    model.Status = "Cancelled";
                    model.CancellationRemarks = cancellationRemarks;

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(model.CanceledBy, $"Cancelled invoice# {model.SINo}", "Sales Invoice");
                    await _dbContext.AddAsync(auditTrail, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Sales Invoice has been Cancelled.";
                }
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        public async Task<IActionResult> GetPOs(int productId, CancellationToken cancellationToken)
        {
            var purchaseOrders = await _dbContext.PurchaseOrders
                .Where(po => po.ProductId == productId && po.QuantityReceived != 0 && po.IsPosted)
                .ToListAsync(cancellationToken);

            if (purchaseOrders.Count > 0)
            {
                var poList = purchaseOrders.Select(po => new { Id = po.Id, PONumber = po.PONo }).ToList();
                return Json(poList);
            }

            return Json(null);
        }

        public IActionResult GetRRs(int purchaseOrderId)
        {
            var rrs = _dbContext.ReceivingReports
                              .Where(rr => rr.POId == purchaseOrderId && rr.ReceivedDate != null)
                              .Select(rr => new
                              {
                                  rr.Id,
                                  rr.RRNo,
                                  rr.ReceivedDate
                              })
                              .ToList();

            return Json(rrs);
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
            var selectedList = _dbContext.SalesInvoices
                .Where(invoice => recordIds.Contains(invoice.Id))
                .OrderBy(invoice => invoice.SINo)
                .ToList();

            // Create the Excel package
            using var package = new ExcelPackage();
            // Add a new worksheet to the Excel package
            var worksheet = package.Workbook.Worksheets.Add("SalesInvoice");

            worksheet.Cells["A1"].Value = "OtherRefNo";
            worksheet.Cells["B1"].Value = "Quantity";
            worksheet.Cells["C1"].Value = "UnitPrice";
            worksheet.Cells["D1"].Value = "Amount";
            worksheet.Cells["E1"].Value = "Remarks";
            worksheet.Cells["F1"].Value = "VatableSales";
            worksheet.Cells["G1"].Value = "VatAmount";
            worksheet.Cells["H1"].Value = "Status";
            worksheet.Cells["I1"].Value = "TransactionDate";
            worksheet.Cells["J1"].Value = "Discount";
            worksheet.Cells["K1"].Value = "NetDiscount";
            worksheet.Cells["L1"].Value = "VatExempt";
            worksheet.Cells["M1"].Value = "ZeroRated";
            worksheet.Cells["N1"].Value = "WithholdingVatAmount";
            worksheet.Cells["O1"].Value = "WithholdingTaxAmount";
            worksheet.Cells["P1"].Value = "AmountPaid";
            worksheet.Cells["Q1"].Value = "Balance";
            worksheet.Cells["R1"].Value = "IsPaid";
            worksheet.Cells["S1"].Value = "IsTaxAndVatPaid";
            worksheet.Cells["T1"].Value = "DueDate";
            worksheet.Cells["U1"].Value = "CreatedBy";
            worksheet.Cells["V1"].Value = "CreatedDate";
            worksheet.Cells["W1"].Value = "POId";
            worksheet.Cells["X1"].Value = "CancellationRemarks";
            worksheet.Cells["Y1"].Value = "OriginalReceivingReportId";
            worksheet.Cells["Z1"].Value = "OriginalCustomerId";
            worksheet.Cells["AA1"].Value = "OriginalPOId";
            worksheet.Cells["AB1"].Value = "OriginalProductId";
            worksheet.Cells["AC1"].Value = "OriginalSeriesNumber";
            worksheet.Cells["AD1"].Value = "OriginalDocumentId";

            int row = 2;

            foreach (var item in selectedList)
            {
                worksheet.Cells[row, 1].Value = item.OtherRefNo;
                worksheet.Cells[row, 2].Value = item.Quantity;
                worksheet.Cells[row, 3].Value = item.UnitPrice;
                worksheet.Cells[row, 4].Value = item.Amount;
                worksheet.Cells[row, 5].Value = item.Remarks;
                worksheet.Cells[row, 6].Value = item.VatableSales;
                worksheet.Cells[row, 7].Value = item.VatAmount;
                worksheet.Cells[row, 8].Value = item.Status;
                worksheet.Cells[row, 9].Value = item.TransactionDate.ToString("yyyy-MM-dd");
                worksheet.Cells[row, 10].Value = item.Discount;
                worksheet.Cells[row, 11].Value = item.NetDiscount;
                worksheet.Cells[row, 12].Value = item.VatExempt;
                worksheet.Cells[row, 13].Value = item.ZeroRated;
                worksheet.Cells[row, 14].Value = item.WithHoldingVatAmount;
                worksheet.Cells[row, 15].Value = item.WithHoldingTaxAmount;
                worksheet.Cells[row, 16].Value = item.AmountPaid;
                worksheet.Cells[row, 17].Value = item.Balance;
                worksheet.Cells[row, 18].Value = item.IsPaid;
                worksheet.Cells[row, 19].Value = item.IsTaxAndVatPaid;
                worksheet.Cells[row, 20].Value = item.DueDate.ToString("yyyy-MM-dd");
                worksheet.Cells[row, 21].Value = item.CreatedBy;
                worksheet.Cells[row, 22].Value = item.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                worksheet.Cells[row, 23].Value = item.CancellationRemarks;
                worksheet.Cells[row, 24].Value = item.ReceivingReportId;
                worksheet.Cells[row, 25].Value = item.CustomerId;
                worksheet.Cells[row, 26].Value = item.POId;
                worksheet.Cells[row, 27].Value = item.ProductId;
                worksheet.Cells[row, 28].Value = item.SINo;
                worksheet.Cells[row, 29].Value = item.Id;

                row++;
            }

            // Convert the Excel package to a byte array
            var excelBytes = package.GetAsByteArray();

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "SalesInvoiceList.xlsx");
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
                            var invoice = new SalesInvoice
                            {
                                SINo = await _salesInvoiceRepo.GenerateSINo(),
                                SeriesNumber = await _salesInvoiceRepo.GetLastSeriesNumber(),
                                OtherRefNo = worksheet.Cells[row, 1].Text,
                                Quantity = decimal.TryParse(worksheet.Cells[row, 2].Text, out decimal quantity) ? quantity : 0,
                                UnitPrice = decimal.TryParse(worksheet.Cells[row, 3].Text, out decimal unitPrice) ? unitPrice : 0,
                                Amount = decimal.TryParse(worksheet.Cells[row, 4].Text, out decimal amount) ? amount : 0,
                                Remarks = worksheet.Cells[row, 5].Text,
                                VatableSales = decimal.TryParse(worksheet.Cells[row, 6].Text, out decimal vatableSales) ? vatableSales : 0,
                                VatAmount = decimal.TryParse(worksheet.Cells[row, 7].Text, out decimal vatAmount) ? vatAmount : 0,
                                Status = worksheet.Cells[row, 8].Text,
                                TransactionDate = DateOnly.TryParse(worksheet.Cells[row, 9].Text, out DateOnly transactionDate) ? transactionDate : default,
                                Discount = decimal.TryParse(worksheet.Cells[row, 10].Text, out decimal discount) ? discount : 0,
                                NetDiscount = decimal.TryParse(worksheet.Cells[row, 11].Text, out decimal netDiscount) ? netDiscount : 0,
                                VatExempt = decimal.TryParse(worksheet.Cells[row, 12].Text, out decimal vatExempt) ? vatExempt : 0,
                                ZeroRated = decimal.TryParse(worksheet.Cells[row, 13].Text, out decimal zeroRated) ? zeroRated : 0,
                                WithHoldingVatAmount = decimal.TryParse(worksheet.Cells[row, 14].Text, out decimal withHoldingVatAmount) ? withHoldingVatAmount : 0,
                                WithHoldingTaxAmount = decimal.TryParse(worksheet.Cells[row, 15].Text, out decimal withHoldingTaxAmount) ? withHoldingTaxAmount : 0,
                                AmountPaid = decimal.TryParse(worksheet.Cells[row, 16].Text, out decimal amountPaid) ? amountPaid : 0,
                                Balance = decimal.TryParse(worksheet.Cells[row, 17].Text, out decimal balance) ? balance : 0,
                                IsPaid = bool.TryParse(worksheet.Cells[row, 18].Text, out bool isPaid) ? isPaid : false,
                                IsTaxAndVatPaid = bool.TryParse(worksheet.Cells[row, 19].Text, out bool isTaxAndVatPaid) ? isTaxAndVatPaid : false,
                                DueDate = DateOnly.TryParse(worksheet.Cells[row, 20].Text, out DateOnly dueDate) ? dueDate : default,
                                CreatedBy = worksheet.Cells[row, 21].Text,
                                CreatedDate = DateTime.TryParse(worksheet.Cells[row, 22].Text, out DateTime createdDate) ? createdDate : default,
                                CancellationRemarks = worksheet.Cells[row, 23].Text != "" ? worksheet.Cells[row, 23].Text : null,
                                OriginalReceivingReportId = int.TryParse(worksheet.Cells[row, 24].Text, out int receivingReportId) ? receivingReportId : 0,
                                OriginalCustomerId = int.TryParse(worksheet.Cells[row, 25].Text, out int customerId) ? customerId : 0,
                                OriginalPOId = int.TryParse(worksheet.Cells[row, 26].Text, out int poId) ? poId : 0,
                                OriginalProductId = int.TryParse(worksheet.Cells[row, 27].Text, out int productId) ? productId : 0,
                                OriginalSeriesNumber = worksheet.Cells[row, 28].Text,
                                OriginalDocumentId = int.TryParse(worksheet.Cells[row, 29].Text, out int originalDocumentId) ? originalDocumentId : 0,
                            };
                            await _dbContext.SalesInvoices.AddAsync(invoice);
                            await _dbContext.SaveChangesAsync();

                            var si = await _dbContext
                                .SalesInvoices
                                .FirstOrDefaultAsync(s => s.Id == invoice.Id);

                            si.CustomerId = await _dbContext.Customers
                                .Where(c => c.OriginalCustomerId == invoice.OriginalCustomerId)
                                .Select(c => c.Id)
                                .FirstOrDefaultAsync();

                            si.ProductId = await _dbContext.Products
                                .Where(c => c.OriginalProductId == invoice.OriginalProductId)
                                .Select(c => c.Id)
                                .FirstOrDefaultAsync();

                            si.ReceivingReportId = await _dbContext.ReceivingReports
                                .Where(c => c.OriginalDocumentId == invoice.OriginalReceivingReportId)
                                .Select(c => c.Id)
                                .FirstOrDefaultAsync();

                            si.POId = await _dbContext.PurchaseOrders
                                .Where(c => c.OriginalDocumentId == invoice.OriginalPOId)
                                .Select(c => c.Id)
                                .FirstOrDefaultAsync();

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