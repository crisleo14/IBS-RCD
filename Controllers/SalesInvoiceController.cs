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
            viewModel.PO = await _dbContext.PurchaseOrders
                .OrderBy(c => c.Id)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.PONo
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
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.PONo
                })
                .ToListAsync(cancellationToken);
            if (ModelState.IsValid)
            {
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

                var generateCRNo = await _salesInvoiceRepo.GenerateSINo(cancellationToken);
                var existingCustomers = await _dbContext.Customers
                                               .FirstOrDefaultAsync(si => si.Id == sales.CustomerId, cancellationToken);

                sales.SeriesNumber = getLastNumber;
                sales.CreatedBy = _userManager.GetUserName(this.User);
                sales.SINo = generateCRNo;
                sales.Amount = sales.Quantity * sales.UnitPrice;
                sales.DueDate = await _salesInvoiceRepo.ComputeDueDateAsync(existingCustomers.Terms, sales.TransactionDate);
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
                return RedirectToAction("Index");
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
                var existingModel = await _salesInvoiceRepo.FindSalesInvoice(model.Id, cancellationToken);

                if (existingModel == null)
                {
                    return NotFound(); // Return a "Not Found" response when the entity is not found.
                }

                if (ModelState.IsValid)
                {
                    existingModel.TransactionDate = model.TransactionDate;
                    existingModel.OtherRefNo = model.OtherRefNo;
                    existingModel.POId = model.POId;
                    existingModel.Quantity = model.Quantity;
                    existingModel.UnitPrice = model.UnitPrice;
                    existingModel.Remarks = model.Remarks;
                    existingModel.Discount = model.Discount;
                    existingModel.Amount = model.Quantity * model.UnitPrice;

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
                        AuditTrail auditTrail = new(modifiedBy, $"Edited invoice# {model.SINo}", "Sales Invoice");
                        await _dbContext.AddAsync(auditTrail, cancellationToken);

                        #endregion --Audit Trail Recording
                    }
                    else
                    {
                        TempData["error"] = "Please input below or exact amount based unit price multiply quantity";
                        return View(model);
                    }
                }
                else
                {
                    ModelState.AddModelError("", "The information you submitted is not valid!");
                    return View(model);
                }

                // Save the changes to the database
                await _dbContext.SaveChangesAsync(cancellationToken);

                return RedirectToAction("Index"); // Redirect to a success page or the index page
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
            return RedirectToAction("PrintInvoice", new { id = id });
        }

        [HttpGet]
        public async Task<IActionResult> Preview(int? id, CancellationToken cancellationToken)
        {
            var invoice = await _salesInvoiceRepo.FindSalesInvoice(id, cancellationToken);
            return PartialView("_PreviewPartialView", invoice);
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
                        return RedirectToAction("Index");
                    }
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction("Index");
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

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(model.VoidedBy, $"Voided invoice# {model.SINo}", "Sales Invoice");
                    await _dbContext.AddAsync(auditTrail, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Sales Invoice has been Voided.";
                }
                return RedirectToAction("Index");
            }

            return NotFound();
        }

        public async Task<IActionResult> Cancel(int invoiceId, CancellationToken cancellationToken)
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

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(model.CanceledBy, $"Cancelled invoice# {model.SINo}", "Sales Invoice");
                    await _dbContext.AddAsync(auditTrail, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Sales Invoice has been Cancelled.";
                }
                return RedirectToAction("Index");
            }

            return NotFound();
        }
    }
}