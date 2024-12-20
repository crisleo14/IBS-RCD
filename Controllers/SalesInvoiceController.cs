using Accounting_System.Data;
using Accounting_System.Models;
using Accounting_System.Models.AccountsReceivable;
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
using Newtonsoft.Json;

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

        public async Task<IActionResult> Index(string? view, CancellationToken cancellationToken)
        {
            var salesInvoices = await _salesInvoiceRepo.GetSalesInvoicesAsync(cancellationToken);

            if (view == nameof(DynamicView.SalesInvoice))
            {
                return View("ImportExportIndex", salesInvoices);
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetSalesInvoices([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var salesInvoices = await _salesInvoiceRepo.GetSalesInvoicesAsync(cancellationToken);
                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search?.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    salesInvoices = salesInvoices
                        .Where(s =>
                            s.SINo.ToLower().Contains(searchValue) ||
                            s.Customer.Name.ToLower().Contains(searchValue) ||
                            s.Customer.Terms.ToLower().Contains(searchValue) ||
                            s.Product.Code.ToLower().Contains(searchValue) ||
                            s.Product.Name.ToLower().Contains(searchValue) ||
                            s.Status.ToLower().Contains(searchValue) ||
                            s.TransactionDate.ToString("MMM dd, yyyy").ToLower().Contains(searchValue) ||
                            s.Quantity.ToString().Contains(searchValue) ||
                            s.UnitPrice.ToString().Contains(searchValue) ||
                            s.Amount.ToString().Contains(searchValue) ||
                            s.Remarks.ToLower().Contains(searchValue) ||
                            s.CreatedBy.ToLower().Contains(searchValue)
                            )
                        .ToList();
                }

                // Sorting
                if (parameters.Order != null && parameters.Order.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Data;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";

                    salesInvoices = salesInvoices
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}")
                        .ToList();
                }

                var totalRecords = salesInvoices.Count();

                var pagedData = salesInvoices
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
        public async Task<IActionResult> GetAllSalesInvoiceIds(CancellationToken cancellationToken)
        {
            var invoiceIds = await _dbContext.SalesInvoices
                                     .Select(invoice => invoice.Id) // Assuming Id is the primary key
                                     .ToListAsync(cancellationToken);
            return Json(invoiceIds);
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
            if (ModelState.IsValid)
            {
                #region -- Validating Series --

                var generateSiNo = await _salesInvoiceRepo.GenerateSINo(cancellationToken);
                var getLastNumber = long.Parse(generateSiNo.Substring(2));

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

                var existingCustomers = await _dbContext.Customers
                                               .FirstOrDefaultAsync(si => si.Id == sales.CustomerId, cancellationToken);

                sales.CreatedBy = _userManager.GetUserName(this.User);
                sales.SINo = generateSiNo;
                sales.Amount = sales.Quantity * sales.UnitPrice;
                sales.DueDate = _salesInvoiceRepo.ComputeDueDateAsync(existingCustomers.Terms, sales.TransactionDate, cancellationToken);
                if (sales.Amount >= sales.Discount)
                {
                    await _dbContext.AddAsync(sales, cancellationToken);
                }
                else
                {
                    TempData["error"] = "Please input below or exact amount based on the Sales Invoice";
                    return View(sales);
                }

                #region --Audit Trail Recording

                if (sales.OriginalSeriesNumber == null && sales.OriginalDocumentId == 0)
                {
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    AuditTrail auditTrailBook = new(sales.CreatedBy, $"Create new invoice# {sales.SINo}", "Sales Invoice", ipAddress);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                }

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

                if (ModelState.IsValid)
                {
                    #region -- Saving Default Enries --

                    existingModel.CustomerId = model.CustomerId;
                    existingModel.TransactionDate = model.TransactionDate;
                    existingModel.OtherRefNo = model.OtherRefNo;
                    existingModel.Quantity = model.Quantity;
                    existingModel.UnitPrice = model.UnitPrice;
                    existingModel.Remarks = model.Remarks;
                    existingModel.Discount = model.Discount;
                    existingModel.Amount = model.Quantity * model.UnitPrice;
                    existingModel.ProductId = model.ProductId;
                    existingModel.DueDate = _salesInvoiceRepo.ComputeDueDateAsync(existingModel.Customer.Terms, existingModel.TransactionDate, cancellationToken);

                    if (existingModel.Amount >= model.Discount)
                    {
                        #region --Audit Trail Recording

                        // if (existingModel.OriginalSeriesNumber == null && existingModel.OriginalDocumentId == 0)
                        // {
                        //     var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        //     var modifiedBy = _userManager.GetUserName(this.User);
                        //     AuditTrail auditTrailBook = new(modifiedBy, $"Edited invoice# {existingModel.SINo}", "Sales Invoice", ipAddress);
                        //     await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                        // }

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
                TempData["success"] = "Sales Invoice updated successfully";
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

                if (sales.OriginalSeriesNumber == null && sales.OriginalDocumentId == 0)
                {
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    var printedBy = _userManager.GetUserName(this.User);
                    AuditTrail auditTrailBook = new(printedBy, $"Printed original copy of invoice# {sales.SINo}", "Sales Invoice", ipAddress);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                }

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
                            sales.Amount = model.Amount - model.Discount;
                            sales.VatableSales = _generalRepo.ComputeNetOfVat(sales.Amount);
                            sales.VatAmount = _generalRepo.ComputeVatAmount(sales.VatableSales);
                            sales.Discount = model.Discount;
                            sales.NetSales = (model.Amount - model.Discount) / 1.12m;
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
                            sales.NetSales = model.Amount - model.Discount;
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
                            sales.NetSales = model.Amount - model.Discount;
                            sales.CreatedBy = model.CreatedBy;
                            sales.CreatedDate = model.CreatedDate;
                            sales.DueDate = model.DueDate;
                            sales.DocumentId = model.Id;
                        }

                        await _dbContext.AddAsync(sales, cancellationToken);

                        #endregion --Sales Book Recording

                        #region --General Ledger Book Recording

                        decimal netDiscount = model.Amount - model.Discount;
                        decimal netOfVatAmount = model.Customer.CustomerType == CS.VatType_Vatable ? _generalRepo.ComputeNetOfVat(netDiscount) : netDiscount;
                        decimal vatAmount = model.Customer.CustomerType == CS.VatType_Vatable ? _generalRepo.ComputeVatAmount(netOfVatAmount) : 0;
                        decimal withHoldingTaxAmount = model.Customer.WithHoldingTax ? _generalRepo.ComputeEwtAmount(netOfVatAmount, 0.01m) : 0;
                        decimal withHoldingVatAmount = model.Customer.WithHoldingVat ? _generalRepo.ComputeEwtAmount(netOfVatAmount, 0.05m) : 0;

                        var ledgers = new List<GeneralLedgerBook>();

                        ledgers.Add(
                            new GeneralLedgerBook
                            {
                                Date = model.TransactionDate,
                                Reference = model.SINo,
                                Description = model.Product.Name,
                                AccountNo = "101020100",
                                AccountTitle = "AR-Trade Receivable",
                                Debit = netDiscount - (withHoldingTaxAmount + withHoldingVatAmount),
                                Credit = 0,
                                CreatedBy = model.CreatedBy,
                                CreatedDate = model.CreatedDate
                            }
                        );

                        if (withHoldingTaxAmount > 0)
                        {
                            ledgers.Add(
                                new GeneralLedgerBook
                                {
                                    Date = model.TransactionDate,
                                    Reference = model.SINo,
                                    Description = model.Product.Name,
                                    AccountNo = "101060500",
                                    AccountTitle = "Deferred Withholding Tax",
                                    Debit = withHoldingTaxAmount,
                                    Credit = 0,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }
                        if (withHoldingVatAmount > 0)
                        {
                            ledgers.Add(
                                new GeneralLedgerBook
                                {
                                    Date = model.TransactionDate,
                                    Reference = model.SINo,
                                    Description = model.Product.Name,
                                    AccountNo = "101060700",
                                    AccountTitle = "Deferred Withholding Vat - Input",
                                    Debit = withHoldingVatAmount,
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
                                    AccountNo = "401010100",
                                    AccountTitle = "Sales - Biodiesel",
                                    Debit = 0,
                                    Credit = netOfVatAmount,
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
                                    AccountNo = "401010200",
                                    AccountTitle = "Sales - Econogas",
                                    Debit = 0,
                                    Credit = netOfVatAmount,
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
                                    AccountNo = "401010300",
                                    AccountTitle = "Sales - Envirogas",
                                    Debit = 0,
                                    Credit = netOfVatAmount,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }

                        if (vatAmount > 0)
                        {
                            ledgers.Add(
                                new GeneralLedgerBook
                                {
                                    Date = model.TransactionDate,
                                    Reference = model.SINo,
                                    Description = model.Product.Name,
                                    AccountNo = "201030100",
                                    AccountTitle = "Vat - Output",
                                    Debit = 0,
                                    Credit = vatAmount,
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

                        if (model.OriginalSeriesNumber == null && model.OriginalDocumentId == 0)
                        {
                            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                            AuditTrail auditTrailBook = new(model.PostedBy, $"Posted invoice# {model.SINo}", "Sales Invoice", ipAddress);
                            await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                        }

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        TempData["success"] = "Sales Invoice has been Posted.";
                        return RedirectToAction(nameof(PrintInvoice), new { id = invoiceId });
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

            var existingInventory = await _dbContext.Inventories
                .Include(i => i.Product)
                .FirstOrDefaultAsync(i => i.Reference == model.SINo);

            if (model != null && existingInventory != null)
            {
                try
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
                        await _inventoryRepo.VoidInventory(existingInventory, cancellationToken);

                        #region --Audit Trail Recording

                        if (model.OriginalSeriesNumber == null && model.OriginalDocumentId == 0)
                        {
                            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                            AuditTrail auditTrailBook = new(model.VoidedBy, $"Voided invoice# {model.SINo}", "Sales Invoice", ipAddress);
                            await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                        }

                        #endregion --Audit Trail Recording

                        //await _dbContext.SaveChangesAsync(cancellationToken);
                        TempData["success"] = "Sales Invoice has been Voided.";
                    }
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(Index));
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

                    if (model.OriginalSeriesNumber == null && model.OriginalDocumentId == 0)
                    {
                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        AuditTrail auditTrailBook = new(model.CanceledBy, $"Cancelled invoice# {model.SINo}", "Sales Invoice", ipAddress);
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                    }

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
                              .Where(rr => rr.POId == purchaseOrderId && rr.ReceivedDate != null && rr.IsPosted)
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
        public async Task<IActionResult> Export(string selectedRecord)
        {
            if (string.IsNullOrEmpty(selectedRecord))
            {
                // Handle the case where no invoices are selected
                return RedirectToAction(nameof(Index));
            }

            var recordIds = selectedRecord.Split(',').Select(int.Parse).ToList();

            // Retrieve the selected invoices from the database
            var selectedList = await _dbContext.SalesInvoices
                .Where(invoice => recordIds.Contains(invoice.Id))
                .OrderBy(invoice => invoice.SINo)
                .ToListAsync();

            // Create the Excel package
            using var package = new ExcelPackage();
            // Add a new worksheet to the Excel package
            var worksheet = package.Workbook.Worksheets.Add("SalesInvoice");

            worksheet.Cells["A1"].Value = "OtherRefNo";
            worksheet.Cells["B1"].Value = "Quantity";
            worksheet.Cells["C1"].Value = "UnitPrice";
            worksheet.Cells["D1"].Value = "Amount";
            worksheet.Cells["E1"].Value = "Remarks";
            worksheet.Cells["F1"].Value = "Status";
            worksheet.Cells["G1"].Value = "TransactionDate";
            worksheet.Cells["H1"].Value = "Discount";
            worksheet.Cells["I1"].Value = "AmountPaid";
            worksheet.Cells["J1"].Value = "Balance";
            worksheet.Cells["K1"].Value = "IsPaid";
            worksheet.Cells["L1"].Value = "IsTaxAndVatPaid";
            worksheet.Cells["M1"].Value = "DueDate";
            worksheet.Cells["N1"].Value = "CreatedBy";
            worksheet.Cells["O1"].Value = "CreatedDate";
            worksheet.Cells["P1"].Value = "CancellationRemarks";
            worksheet.Cells["Q1"].Value = "OriginalReceivingReportId";
            worksheet.Cells["R1"].Value = "OriginalCustomerId";
            worksheet.Cells["S1"].Value = "OriginalPOId";
            worksheet.Cells["T1"].Value = "OriginalProductId";
            worksheet.Cells["U1"].Value = "OriginalSINo";
            worksheet.Cells["V1"].Value = "OriginalDocumentId";

            int row = 2;

            foreach (var item in selectedList)
            {
                worksheet.Cells[row, 1].Value = item.OtherRefNo;
                worksheet.Cells[row, 2].Value = item.Quantity;
                worksheet.Cells[row, 3].Value = item.UnitPrice;
                worksheet.Cells[row, 4].Value = item.Amount;
                worksheet.Cells[row, 5].Value = item.Remarks;
                worksheet.Cells[row, 6].Value = item.Status;
                worksheet.Cells[row, 7].Value = item.TransactionDate.ToString("yyyy-MM-dd");
                worksheet.Cells[row, 8].Value = item.Discount;
                worksheet.Cells[row, 9].Value = item.AmountPaid;
                worksheet.Cells[row, 10].Value = item.Balance;
                worksheet.Cells[row, 11].Value = item.IsPaid;
                worksheet.Cells[row, 12].Value = item.IsTaxAndVatPaid;
                worksheet.Cells[row, 13].Value = item.DueDate.ToString("yyyy-MM-dd");
                worksheet.Cells[row, 14].Value = item.CreatedBy;
                worksheet.Cells[row, 15].Value = item.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                worksheet.Cells[row, 16].Value = item.CancellationRemarks;
                worksheet.Cells[row, 18].Value = item.CustomerId;
                worksheet.Cells[row, 20].Value = item.ProductId;
                worksheet.Cells[row, 21].Value = item.SINo;
                worksheet.Cells[row, 22].Value = item.Id;

                row++;
            }

            // Convert the Excel package to a byte array
            var excelBytes = await package.GetAsByteArrayAsync();

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "SalesInvoiceList.xlsx");
        }

        #endregion -- export xlsx record --

        //Upload as .xlsx file.(Import)
        #region -- import xlsx record --

        [HttpPost]
        public async Task<IActionResult> Import(IFormFile file, CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0)
            {
                return RedirectToAction(nameof(Index));
            }

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                stream.Position = 0;
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                        if (worksheet == null)
                        {
                            TempData["error"] = "The Excel file contains no worksheets.";
                            return RedirectToAction(nameof(Index), new { view = DynamicView.SalesInvoice });
                        }

                        if (worksheet.ToString() != nameof(DynamicView.SalesInvoice))
                        {
                            TempData["error"] = "The Excel file is not related to sales invoice.";
                            return RedirectToAction(nameof(Index), new { view = DynamicView.SalesInvoice });
                        }

                        var rowCount = worksheet.Dimension.Rows;
                        var invoiceList = await _dbContext
                            .SalesInvoices
                            .ToListAsync(cancellationToken);

                        for (int row = 2; row <= rowCount; row++) // Assuming the first row is the header
                        {
                            var invoice = new SalesInvoice
                            {
                                SINo = worksheet.Cells[row, 21].Text,
                                OtherRefNo = worksheet.Cells[row, 1].Text,
                                Quantity = decimal.TryParse(worksheet.Cells[row, 2].Text, out decimal quantity)
                                    ? quantity
                                    : 0,
                                UnitPrice = decimal.TryParse(worksheet.Cells[row, 3].Text, out decimal unitPrice)
                                    ? unitPrice
                                    : 0,
                                Amount =
                                    decimal.TryParse(worksheet.Cells[row, 4].Text, out decimal amount) ? amount : 0,
                                Remarks = worksheet.Cells[row, 5].Text,
                                Status = worksheet.Cells[row, 6].Text,
                                TransactionDate =
                                    DateOnly.TryParse(worksheet.Cells[row, 7].Text, out DateOnly transactionDate)
                                        ? transactionDate
                                        : default,
                                Discount = decimal.TryParse(worksheet.Cells[row, 8].Text, out decimal discount)
                                    ? discount
                                    : 0,
                                // AmountPaid = decimal.TryParse(worksheet.Cells[row, 9].Text, out decimal amountPaid)
                                //     ? amountPaid
                                //     : 0,
                                // Balance = decimal.TryParse(worksheet.Cells[row, 10].Text, out decimal balance)
                                //     ? balance
                                //     : 0,
                                // IsPaid = bool.TryParse(worksheet.Cells[row, 11].Text, out bool isPaid) ? isPaid : false,
                                // IsTaxAndVatPaid = bool.TryParse(worksheet.Cells[row, 12].Text, out bool isTaxAndVatPaid)
                                //     ? isTaxAndVatPaid
                                //     : false,
                                DueDate = DateOnly.TryParse(worksheet.Cells[row, 13].Text, out DateOnly dueDate)
                                    ? dueDate
                                    : default,
                                CreatedBy = worksheet.Cells[row, 14].Text,
                                CreatedDate = DateTime.TryParse(worksheet.Cells[row, 15].Text, out DateTime createdDate)
                                    ? createdDate
                                    : default,
                                CancellationRemarks = worksheet.Cells[row, 16].Text != ""
                                    ? worksheet.Cells[row, 16].Text
                                    : null,
                                OriginalCustomerId = int.TryParse(worksheet.Cells[row, 18].Text, out int customerId)
                                    ? customerId
                                    : 0,
                                OriginalProductId = int.TryParse(worksheet.Cells[row, 20].Text, out int productId)
                                    ? productId
                                    : 0,
                                OriginalSeriesNumber = worksheet.Cells[row, 21].Text,
                                OriginalDocumentId =
                                    int.TryParse(worksheet.Cells[row, 22].Text, out int originalDocumentId)
                                        ? originalDocumentId
                                        : 0,
                            };

                            if (invoiceList.Any(si => si.OriginalDocumentId == invoice.OriginalDocumentId))
                            {
                                continue;
                            }

                            invoice.CustomerId = await _dbContext.Customers
                                                     .Where(c => c.OriginalCustomerId == invoice.OriginalCustomerId)
                                                     .Select(c => (int?)c.Id)
                                                     .FirstOrDefaultAsync(cancellationToken) ?? throw new InvalidOperationException("Please upload the Excel file for the customer master file first.");

                            invoice.ProductId = await _dbContext.Products
                                                    .Where(c => c.OriginalProductId == invoice.OriginalProductId)
                                                    .Select(c => (int?)c.Id)
                                                    .FirstOrDefaultAsync(cancellationToken) ?? throw new InvalidOperationException("Please upload the Excel file for the product master file first.");

                            await _dbContext.SalesInvoices.AddAsync(invoice, cancellationToken);
                        }

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                    }
                }
                catch (OperationCanceledException oce)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = oce.Message;
                    return RedirectToAction(nameof(Index), new { view = DynamicView.SalesInvoice });
                }
                catch (InvalidOperationException ioe)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["warning"] = ioe.Message;
                    return RedirectToAction(nameof(Index), new { view = DynamicView.SalesInvoice });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(Index), new { view = DynamicView.SalesInvoice });
                }
            }
            TempData["success"] = "Uploading Success!";
            return RedirectToAction(nameof(Index), new { view = DynamicView.SalesInvoice });
        }

        #endregion -- import xlsx record --
    }
}
