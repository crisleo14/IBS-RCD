using Accounting_System.Data;
using Accounting_System.Models;
using Accounting_System.Models.AccountsReceivable;
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

namespace Accounting_System.Controllers
{
    [Authorize]
    public class CreditMemoController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly CreditMemoRepo _creditMemoRepo;

        private readonly GeneralRepo _generalRepo;

        public CreditMemoController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, CreditMemoRepo creditMemoRepo, GeneralRepo generalRepo)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _creditMemoRepo = creditMemoRepo;
            _generalRepo = generalRepo;
        }

        public async Task<IActionResult> Index(string? view, CancellationToken cancellationToken)
        {
            var cm = await _dbContext.CreditMemos
                .Include(cm => cm.SalesInvoice)
                .ThenInclude(s => s.Customer)
                .Include(cm => cm.SalesInvoice)
                .ThenInclude(s => s.Product)
                .Include(cm => cm.ServiceInvoice)
                .ThenInclude(sv => sv.Customer)
                .Include(cm => cm.ServiceInvoice)
                .ThenInclude(sv => sv.Service)
                .ToListAsync(cancellationToken);

            if (view == nameof(DynamicView.CreditMemo))
            {
                return View("ImportExportIndex", cm);
            }

            return View(cm);
        }

        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var viewModel = new CreditMemo();
            viewModel.SalesInvoices = await _dbContext.SalesInvoices
                .Where(si => si.IsPosted)
                .Select(si => new SelectListItem
                {
                    Value = si.Id.ToString(),
                    Text = si.SINo
                })
                .ToListAsync(cancellationToken);
            viewModel.ServiceInvoices = await _dbContext.ServiceInvoices
                .Where(sv => sv.IsPosted)
                .Select(sv => new SelectListItem
                {
                    Value = sv.Id.ToString(),
                    Text = sv.SVNo
                })
                .ToListAsync(cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreditMemo model, CancellationToken cancellationToken)
        {
            model.SalesInvoices = await _dbContext.SalesInvoices
                .Where(si => si.IsPosted)
                .Select(si => new SelectListItem
                {
                    Value = si.Id.ToString(),
                    Text = si.SINo
                })
                .ToListAsync(cancellationToken);
            model.ServiceInvoices = await _dbContext.ServiceInvoices
                .Where(sv => sv.IsPosted)
                .Select(sv => new SelectListItem
                {
                    Value = sv.Id.ToString(),
                    Text = sv.SVNo
                })
                .ToListAsync(cancellationToken);

            var existingSalesInvoice = await _dbContext
                        .SalesInvoices
                        .Include(c => c.Customer)
                        .Include(s => s.Product)
                        .FirstOrDefaultAsync(invoice => invoice.Id == model.SalesInvoiceId, cancellationToken);

            var existingSv = await _dbContext.ServiceInvoices
                        .Include(sv => sv.Customer)
                        .FirstOrDefaultAsync(sv => sv.Id == model.ServiceInvoiceId, cancellationToken);


            if (model.SalesInvoiceId != null)
            {
                if (model.AdjustedPrice > existingSalesInvoice.UnitPrice)
                {
                    ModelState.AddModelError("AdjustedPrice", "Cannot input more than the existing SI unit price!");
                }
                if (model.Quantity > existingSalesInvoice.Quantity)
                {
                    ModelState.AddModelError("Quantity", "Cannot input more than the existing SI quantity!");
                }
            }
            else
            {
                if (model.Amount > existingSv.Amount)
                {
                    ModelState.AddModelError("Amount", "Cannot input more than the existing SV amount!");
                }
            }

            if (ModelState.IsValid)
            {
                if (model.SalesInvoiceId != null)
                {
                    var existingSIDMs = await _dbContext.DebitMemos
                                  .Where(si => si.SalesInvoiceId == model.SalesInvoiceId && !si.IsPosted && !si.IsCanceled && !si.IsVoided)
                                  .OrderBy(s => s.Id)
                                  .ToListAsync(cancellationToken);
                    if (existingSIDMs.Count > 0)
                    {
                        ModelState.AddModelError("", $"Can’t proceed to create you have unposted DM/CM. {existingSIDMs.First().DMNo}");
                        return View(model);
                    }

                    var existingSICMs = await _dbContext.CreditMemos
                                      .Where(si => si.SalesInvoiceId == model.SalesInvoiceId && !si.IsPosted && !si.IsCanceled && !si.IsVoided)
                                      .OrderBy(s => s.Id)
                                      .ToListAsync(cancellationToken);
                    if (existingSICMs.Count > 0)
                    {
                        ModelState.AddModelError("", $"Can’t proceed to create you have unposted DM/CM. {existingSICMs.First().CMNo}");
                        return View(model);
                    }
                }
                else
                {
                    var existingSOADMs = await _dbContext.DebitMemos
                                  .Where(si => si.ServiceInvoiceId == model.ServiceInvoiceId && !si.IsPosted && !si.IsCanceled && !si.IsVoided)
                                  .OrderBy(s => s.Id)
                                  .ToListAsync(cancellationToken);
                    if (existingSOADMs.Count > 0)
                    {
                        ModelState.AddModelError("", $"Can’t proceed to create you have unposted DM/CM. {existingSOADMs.First().DMNo}");
                        return View(model);
                    }

                    var existingSOACMs = await _dbContext.CreditMemos
                                      .Where(si => si.ServiceInvoiceId == model.ServiceInvoiceId && !si.IsPosted && !si.IsCanceled && !si.IsVoided)
                                      .OrderBy(s => s.Id)
                                      .ToListAsync(cancellationToken);
                    if (existingSOACMs.Count > 0)
                    {
                        ModelState.AddModelError("", $"Can’t proceed to create you have unposted DM/CM. {existingSOACMs.First().CMNo}");
                        return View(model);
                    }
                }

                #region --Validating the series--

                var getLastNumber = await _creditMemoRepo.GetLastSeriesNumber(cancellationToken);

                if (getLastNumber > 9999999999)
                {
                    TempData["error"] = "You reach the maximum Series Number";
                    return View(model);
                }
                var totalRemainingSeries = 9999999999 - getLastNumber;
                if (getLastNumber >= 9999999899)
                {
                    TempData["warning"] = $"Credit Memo created successfully, Warning {totalRemainingSeries} series number remaining";
                }
                else
                {
                    TempData["success"] = "Credit Memo created successfully";
                }

                #endregion --Validating the series--

                var generatedCM = await _creditMemoRepo.GenerateCMNo(cancellationToken);

                model.SeriesNumber = getLastNumber;
                model.CMNo = generatedCM;
                model.CreatedBy = _userManager.GetUserName(this.User);

                if (model.Source == "Sales Invoice")
                {
                    model.ServiceInvoiceId = null;

                    model.CreditAmount = (decimal)(model.Quantity * -model.AdjustedPrice);

                    if (existingSalesInvoice.Customer.CustomerType == "Vatable")
                    {
                        model.VatableSales = model.CreditAmount / 1.12m;
                        model.VatAmount = model.CreditAmount - model.VatableSales;

                        if (existingSalesInvoice.WithHoldingTaxAmount != 0)
                        {
                            model.WithHoldingTaxAmount = model.VatableSales * 0.01m;
                        }
                        if (existingSalesInvoice.WithHoldingVatAmount != 0)
                        {
                            model.WithHoldingVatAmount = model.VatableSales * 0.05m;
                        }
                        model.TotalSales = model.VatableSales + model.VatAmount;
                    }
                    else
                    {
                        model.TotalSales = model.CreditAmount;
                    }
                }
                else if (model.Source == "Service Invoice")
                {
                    model.SalesInvoiceId = null;

                    #region --Retrieval of Services

                    model.ServicesId = existingSv.ServicesId;

                    var services = await _dbContext
                    .Services
                    .FirstOrDefaultAsync(s => s.Id == model.ServicesId, cancellationToken);

                    #endregion --Retrieval of Services

                    model.CreditAmount = -model.Amount ?? 0;

                    if (existingSv.Customer.CustomerType == "Vatable")
                    {
                        model.VatableSales = model.CreditAmount / 1.12m;
                        model.VatAmount = model.CreditAmount - model.VatableSales;
                        model.TotalSales = model.VatableSales + model.VatAmount;
                        model.WithHoldingTaxAmount = model.VatableSales * (services.Percent / 100m);

                        if (existingSv.WithholdingVatAmount != 0)
                        {
                            model.WithHoldingVatAmount = model.VatableSales * 0.05m;
                        }
                    }
                    else
                    {
                        model.TotalSales = model.CreditAmount;
                    }
                }

                //#region --Audit Trail Recording

                //AuditTrail auditTrail = new(model.CreatedBy, $"Create new credit memo# {model.CMNo}", "Credit Memo");
                //await _dbContext.AddAsync(auditTrail, cancellationToken);

                //#endregion --Audit Trail Recording

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
            if (id == null || _dbContext.CreditMemos == null)
            {
                return NotFound();
            }

            var creditMemo = await _dbContext.CreditMemos
                .Include(cm => cm.SalesInvoice)
                .Include(cm => cm.ServiceInvoice)
                .ThenInclude(sv => sv.Customer)
                .Include(cm => cm.ServiceInvoice)
                .ThenInclude(sv => sv.Service)
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

            if (creditMemo == null)
            {
                return NotFound();
            }

            creditMemo.SalesInvoices = await _dbContext.SalesInvoices
                .Where(si => si.IsPosted)
                .Select(si => new SelectListItem
                {
                    Value = si.Id.ToString(),
                    Text = si.SINo
                })
                .ToListAsync(cancellationToken);
            creditMemo.ServiceInvoices = await _dbContext.ServiceInvoices
                .Where(sv => sv.IsPosted)
                .Select(sv => new SelectListItem
                {
                    Value = sv.Id.ToString(),
                    Text = sv.SVNo
                })
                .ToListAsync(cancellationToken);


            return View(creditMemo);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(CreditMemo model, CancellationToken cancellationToken)
        {
            var existingSalesInvoice = await _dbContext
                        .SalesInvoices
                        .Include(c => c.Customer)
                        .Include(s => s.Product)
                        .FirstOrDefaultAsync(invoice => invoice.Id == model.SalesInvoiceId, cancellationToken);
            var existingSv = await _dbContext.ServiceInvoices
                        .Include(sv => sv.Customer)
                        .FirstOrDefaultAsync(sv => sv.Id == model.ServiceInvoiceId, cancellationToken);

            if (model.SalesInvoiceId != null)
            {
                if (model.AdjustedPrice > existingSalesInvoice.UnitPrice)
                {
                    ModelState.AddModelError("AdjustedPrice", "Cannot input more than the existing SI unit price!");
                }
                if (model.Quantity > existingSalesInvoice.Quantity)
                {
                    ModelState.AddModelError("Quantity", "Cannot input more than the existing SI quantity!");
                }
            }
            else
            {
                if (model.Amount > existingSv.Amount)
                {
                    ModelState.AddModelError("Amount", "Cannot input more than the existing SV amount!");
                }
            }

            if (ModelState.IsValid)
            {
                var existingCM = await _dbContext
                        .CreditMemos
                        .FirstOrDefaultAsync(cm => cm.Id == model.Id, cancellationToken);

                model.CreatedBy = _userManager.GetUserName(this.User);

                if (model.Source == "Sales Invoice")
                {
                    model.ServiceInvoiceId = null;

                    #region -- Saving Default Enries --

                    existingCM.TransactionDate = model.TransactionDate;
                    existingCM.SalesInvoiceId = model.SalesInvoiceId;
                    existingCM.Quantity = model.Quantity;
                    existingCM.AdjustedPrice = model.AdjustedPrice;
                    existingCM.Description = model.Description;
                    existingCM.Remarks = model.Remarks;

                    #endregion -- Saving Default Enries --

                    existingCM.CreditAmount = (decimal)(model.Quantity * -model.AdjustedPrice);

                    if (existingSalesInvoice.Customer.CustomerType == "Vatable")
                    {
                        existingCM.VatableSales = existingCM.CreditAmount / 1.12m;
                        existingCM.VatAmount = existingCM.CreditAmount - existingCM.VatableSales;

                        if (existingSalesInvoice.WithHoldingTaxAmount != 0)
                        {
                            existingCM.WithHoldingTaxAmount = existingCM.VatableSales * 0.01m;
                        }
                        if (existingSalesInvoice.WithHoldingVatAmount != 0)
                        {
                            existingCM.WithHoldingVatAmount = existingCM.VatableSales * 0.05m;
                        }
                        existingCM.TotalSales = existingCM.VatableSales + existingCM.VatAmount;
                    }
                    else
                    {
                        existingCM.TotalSales = existingCM.CreditAmount;
                    }

                }
                else if (model.Source == "Service Invoice")
                {
                    model.SalesInvoiceId = null;

                    #region --Retrieval of Services

                    existingCM.ServicesId = existingSv.ServicesId;

                    var services = await _dbContext
                    .Services
                    .FirstOrDefaultAsync(s => s.Id == existingCM.ServicesId, cancellationToken);

                    #endregion --Retrieval of Services

                    #region -- Saving Default Enries --

                    existingCM.TransactionDate = model.TransactionDate;
                    existingCM.ServiceInvoiceId = model.ServiceInvoiceId;
                    existingCM.Period = model.Period;
                    existingCM.Amount = model.Amount;
                    existingCM.Description = model.Description;
                    existingCM.Remarks = model.Remarks;

                    #endregion -- Saving Default Enries --

                    existingCM.CreditAmount = -model.Amount ?? 0;

                    if (existingSv.Customer.CustomerType == "Vatable")
                    {
                        existingCM.VatableSales = existingCM.CreditAmount / 1.12m;
                        existingCM.VatAmount = existingCM.CreditAmount - existingCM.VatableSales;
                        existingCM.TotalSales = existingCM.VatableSales + existingCM.VatAmount;
                        existingCM.WithHoldingTaxAmount = existingCM.VatableSales * (services.Percent / 100m);

                        if (existingSv.WithholdingVatAmount != 0)
                        {
                            existingCM.WithHoldingVatAmount = existingCM.VatableSales * 0.05m;
                        }
                    }
                    else
                    {
                        existingCM.TotalSales = existingCM.CreditAmount;
                    }

                }

                //#region --Audit Trail Recording

                //AuditTrail auditTrail = new(_userManager.GetUserName(this.User), $"Edit credit memo# {existingCM.CMNo}", "Credit Memo");
                //await _dbContext.AddAsync(auditTrail, cancellationToken);

                //#endregion --Audit Trail Recording

                await _dbContext.SaveChangesAsync(cancellationToken);
                TempData["success"] = "Credit Memo edited successfully";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError("", "The information you submitted is not valid!");
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Print(int? id, CancellationToken cancellationToken)
        {
            if (id == null || _dbContext.CreditMemos == null)
            {
                return NotFound();
            }

            var creditMemo = await _dbContext.CreditMemos
                .Include(cm => cm.SalesInvoice)
                .ThenInclude(s => s.Customer)
                .Include(cm => cm.SalesInvoice)
                .ThenInclude(s => s.Product)
                .Include(cm => cm.ServiceInvoice)
                .ThenInclude(sv => sv.Customer)
                .Include(cm => cm.ServiceInvoice)
                .ThenInclude(sv => sv.Service)
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
            if (creditMemo == null)
            {
                return NotFound();
            }

            return View(creditMemo);
        }

        public async Task<IActionResult> Printed(int id, CancellationToken cancellationToken)
        {
            var cm = await _dbContext.CreditMemos.FindAsync(id, cancellationToken);
            if (cm != null && !cm.IsPrinted)
            {
                //#region --Audit Trail Recording

                //var printedBy = _userManager.GetUserName(this.User);
                //AuditTrail auditTrail = new(printedBy, $"Printed original copy of cm# {cm.CMNo}", "Credit Memo");
                //await _dbContext.AddAsync(auditTrail, cancellationToken);

                //#endregion --Audit Trail Recording

                cm.IsPrinted = true;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            return RedirectToAction(nameof(Print), new { id });
        }

        public async Task<IActionResult> Post(int id, CancellationToken cancellationToken, ViewModelDMCM viewModelDMCM)
        {
            var model = await _creditMemoRepo.FindCM(id, cancellationToken);

            if (model != null)
            {
                try
                {
                    if (!model.IsPosted)
                    {
                        model.IsPosted = true;
                        model.PostedBy = _userManager.GetUserName(this.User);
                        model.PostedDate = DateTime.Now;

                        if (model.SalesInvoiceId != null)
                        {
                            #region --Retrieval of SI and SOA--

                            var existingSI = await _dbContext.SalesInvoices
                                                        .Include(s => s.Customer)
                                                        .Include(s => s.Product)
                                                        .FirstOrDefaultAsync(si => si.Id == model.SalesInvoiceId, cancellationToken);

                            #endregion --Retrieval of SI and SOA--

                            #region --Sales Book Recording(SI)--

                            var sales = new SalesBook();

                            if (model.SalesInvoice.Customer.CustomerType == "Vatable")
                            {
                                sales.TransactionDate = model.TransactionDate;
                                sales.SerialNo = model.CMNo;
                                sales.SoldTo = model.SalesInvoice.Customer.Name;
                                sales.TinNo = model.SalesInvoice.Customer.TinNo;
                                sales.Address = model.SalesInvoice.Customer.Address;
                                sales.Description = model.SalesInvoice.Product.Name;
                                sales.Amount = model.CreditAmount;
                                sales.VatAmount = model.VatAmount;
                                sales.VatableSales = model.VatableSales;
                                //sales.Discount = model.Discount;
                                sales.NetSales = model.VatableSales;
                                sales.CreatedBy = model.CreatedBy;
                                sales.CreatedDate = model.CreatedDate;
                                sales.DueDate = existingSI.DueDate;
                                sales.DocumentId = model.SalesInvoiceId;
                            }
                            else if (model.SalesInvoice.Customer.CustomerType == "Exempt")
                            {
                                sales.TransactionDate = model.TransactionDate;
                                sales.SerialNo = model.CMNo;
                                sales.SoldTo = model.SalesInvoice.Customer.Name;
                                sales.TinNo = model.SalesInvoice.Customer.TinNo;
                                sales.Address = model.SalesInvoice.Customer.Address;
                                sales.Description = model.SalesInvoice.Product.Name;
                                sales.Amount = model.CreditAmount;
                                sales.VatExemptSales = model.CreditAmount;
                                //sales.Discount = model.Discount;
                                sales.NetSales = model.VatableSales;
                                sales.CreatedBy = model.CreatedBy;
                                sales.CreatedDate = model.CreatedDate;
                                sales.DueDate = existingSI.DueDate;
                                sales.DocumentId = model.SalesInvoiceId;
                            }
                            else
                            {
                                sales.TransactionDate = model.TransactionDate;
                                sales.SerialNo = model.CMNo;
                                sales.SoldTo = model.SalesInvoice.Customer.Name;
                                sales.TinNo = model.SalesInvoice.Customer.TinNo;
                                sales.Address = model.SalesInvoice.Customer.Address;
                                sales.Description = model.SalesInvoice.Product.Name;
                                sales.Amount = model.CreditAmount;
                                sales.ZeroRated = model.CreditAmount;
                                //sales.Discount = model.Discount;
                                sales.NetSales = model.VatableSales;
                                sales.CreatedBy = model.CreatedBy;
                                sales.CreatedDate = model.CreatedDate;
                                sales.DueDate = existingSI.DueDate;
                                sales.DocumentId = model.SalesInvoiceId;
                            }
                            await _dbContext.AddAsync(sales, cancellationToken);

                            #endregion --Sales Book Recording(SI)--

                            #region --General Ledger Book Recording(SI)--

                            var ledgers = new List<GeneralLedgerBook>();

                            ledgers.Add(
                                new GeneralLedgerBook
                                {
                                    Date = model.TransactionDate,
                                    Reference = model.CMNo,
                                    Description = model.SalesInvoice.Product.Name,
                                    AccountNo = "1010201",
                                    AccountTitle = "AR-Trade Receivable",
                                    Debit = 0,
                                    Credit = Math.Abs(model.CreditAmount - (model.WithHoldingTaxAmount + model.WithHoldingVatAmount)),
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );

                            if (model.WithHoldingTaxAmount < 0)
                            {
                                ledgers.Add(
                                    new GeneralLedgerBook
                                    {
                                        Date = model.TransactionDate,
                                        Reference = model.CMNo,
                                        Description = model.SalesInvoice.Product.Name,
                                        AccountNo = "1010202",
                                        AccountTitle = "Deferred Creditable Withholding Tax",
                                        Debit = 0,
                                        Credit = Math.Abs(model.WithHoldingTaxAmount),
                                        CreatedBy = model.CreatedBy,
                                        CreatedDate = model.CreatedDate
                                    }
                                );
                            }
                            if (model.WithHoldingVatAmount < 0)
                            {
                                ledgers.Add(
                                    new GeneralLedgerBook
                                    {
                                        Date = model.TransactionDate,
                                        Reference = model.CMNo,
                                        Description = model.SalesInvoice.Product.Name,
                                        AccountNo = "1010203",
                                        AccountTitle = "Deferred Creditable Withholding Vat",
                                        Debit = 0,
                                        Credit = Math.Abs(model.WithHoldingVatAmount),
                                        CreatedBy = model.CreatedBy,
                                        CreatedDate = model.CreatedDate
                                    }
                                );
                            }
                            if (model.SalesInvoice.Product.Name == "Biodiesel")
                            {
                                ledgers.Add(
                                    new GeneralLedgerBook
                                    {
                                        Date = model.TransactionDate,
                                        Reference = model.CMNo,
                                        Description = model.SalesInvoice.Product.Name,
                                        AccountNo = "4010101",
                                        AccountTitle = "Sales - Biodiesel",
                                        Debit = model.VatableSales < 0
                                                    ? Math.Abs(model.VatableSales)
                                                    : Math.Abs(model.CreditAmount),
                                        CreatedBy = model.CreatedBy,
                                        Credit = 0,
                                        CreatedDate = model.CreatedDate
                                    }
                                );
                            }
                            else if (model.SalesInvoice.Product.Name == "Econogas")
                            {
                                ledgers.Add(
                                    new GeneralLedgerBook
                                    {
                                        Date = model.TransactionDate,
                                        Reference = model.CMNo,
                                        Description = model.SalesInvoice.Product.Name,
                                        AccountNo = "4010102",
                                        AccountTitle = "Sales - Econogas",
                                        Debit = model.VatableSales < 0
                                                    ? Math.Abs(model.VatableSales)
                                                    : Math.Abs(model.CreditAmount),
                                        Credit = 0,
                                        CreatedBy = model.CreatedBy,
                                        CreatedDate = model.CreatedDate
                                    }
                                );
                            }
                            else if (model.SalesInvoice.Product.Name == "Envirogas")
                            {
                                ledgers.Add(
                                    new GeneralLedgerBook
                                    {
                                        Date = model.TransactionDate,
                                        Reference = model.CMNo,
                                        Description = model.SalesInvoice.Product.Name,
                                        AccountNo = "4010103",
                                        AccountTitle = "Sales - Envirogas",
                                        Debit = model.VatableSales < 0
                                                    ? Math.Abs(model.VatableSales)
                                                    : Math.Abs(model.CreditAmount),
                                        Credit = 0,
                                        CreatedBy = model.CreatedBy,
                                        CreatedDate = model.CreatedDate
                                    }
                                );
                            }

                            if (model.VatAmount < 0)
                            {
                                ledgers.Add(
                                    new GeneralLedgerBook
                                    {
                                        Date = model.TransactionDate,
                                        Reference = model.CMNo,
                                        Description = model.SalesInvoice.Product.Name,
                                        AccountNo = "2010301",
                                        AccountTitle = "Vat Output",
                                        Debit = Math.Abs(model.VatAmount),
                                        Credit = 0,
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

                            #endregion --General Ledger Book Recording(SI)--
                        }

                        if (model.ServiceInvoiceId != null)
                        {
                            var existingSv = await _dbContext.ServiceInvoices
                                                    .Include(sv => sv.Customer)
                                                    .FirstOrDefaultAsync(si => si.Id == model.ServiceInvoiceId, cancellationToken);

                            #region --Retrieval of Services

                            var services = await _creditMemoRepo.GetServicesAsync(model?.ServicesId, cancellationToken);

                            #endregion --Retrieval of Services

                            #region --SV Computation--

                            viewModelDMCM.Period = DateOnly.FromDateTime(model.CreatedDate) >= model.Period ? DateOnly.FromDateTime(model.CreatedDate) : model.Period.AddMonths(1).AddDays(-1);

                            if (existingSv.Customer.CustomerType == "Vatable")
                            {
                                viewModelDMCM.Total = -model.Amount ?? 0;
                                viewModelDMCM.NetAmount = (model.Amount ?? 0 - existingSv.Discount) / 1.12m;
                                viewModelDMCM.VatAmount = (model.Amount ?? 0 - existingSv.Discount) - viewModelDMCM.NetAmount;
                                viewModelDMCM.WithholdingTaxAmount = viewModelDMCM.NetAmount * (services.Percent / 100m);
                                if (existingSv.Customer.WithHoldingVat)
                                {
                                    viewModelDMCM.WithholdingVatAmount = viewModelDMCM.NetAmount * 0.05m;
                                }
                            }
                            else
                            {
                                viewModelDMCM.NetAmount = model.Amount ?? 0 - existingSv.Discount;
                                viewModelDMCM.WithholdingTaxAmount = viewModelDMCM.NetAmount * (services.Percent / 100m);
                                if (existingSv.Customer.WithHoldingVat)
                                {
                                    viewModelDMCM.WithholdingVatAmount = viewModelDMCM.NetAmount * 0.05m;
                                }
                            }

                            if (existingSv.Customer.CustomerType == "Vatable")
                            {
                                var total = Math.Round(model.Amount ?? 0 / 1.12m, 2);

                                var roundedNetAmount = Math.Round(viewModelDMCM.NetAmount, 2);

                                if (roundedNetAmount > total)
                                {
                                    var shortAmount = viewModelDMCM.NetAmount - total;

                                    viewModelDMCM.Amount += shortAmount;
                                }
                            }

                            #endregion --SV Computation--

                            #region --Sales Book Recording(SV)--

                            var sales = new SalesBook();

                            if (model.ServiceInvoice.Customer.CustomerType == "Vatable")
                            {
                                sales.TransactionDate = viewModelDMCM.Period;
                                sales.SerialNo = model.CMNo;
                                sales.SoldTo = model.ServiceInvoice.Customer.Name;
                                sales.TinNo = model.ServiceInvoice.Customer.TinNo;
                                sales.Address = model.ServiceInvoice.Customer.Address;
                                sales.Description = model.ServiceInvoice.Service.Name;
                                sales.Amount = viewModelDMCM.Total;
                                sales.VatAmount = viewModelDMCM.VatAmount;
                                sales.VatableSales = viewModelDMCM.Total / 1.12m;
                                //sales.Discount = model.Discount;
                                sales.NetSales = viewModelDMCM.NetAmount;
                                sales.CreatedBy = model.CreatedBy;
                                sales.CreatedDate = model.CreatedDate;
                                sales.DueDate = existingSv.DueDate;
                                sales.DocumentId = model.ServiceInvoiceId;
                            }
                            else if (model.ServiceInvoice.Customer.CustomerType == "Exempt")
                            {
                                sales.TransactionDate = viewModelDMCM.Period;
                                sales.SerialNo = model.CMNo;
                                sales.SoldTo = model.ServiceInvoice.Customer.Name;
                                sales.TinNo = model.ServiceInvoice.Customer.TinNo;
                                sales.Address = model.ServiceInvoice.Customer.Address;
                                sales.Description = model.ServiceInvoice.Service.Name;
                                sales.Amount = viewModelDMCM.Total;
                                sales.VatExemptSales = viewModelDMCM.Total;
                                //sales.Discount = model.Discount;
                                sales.NetSales = viewModelDMCM.NetAmount;
                                sales.CreatedBy = model.CreatedBy;
                                sales.CreatedDate = model.CreatedDate;
                                sales.DueDate = existingSv.DueDate;
                                sales.DocumentId = model.ServiceInvoiceId;
                            }
                            else
                            {
                                sales.TransactionDate = viewModelDMCM.Period;
                                sales.SerialNo = model.CMNo;
                                sales.SoldTo = model.ServiceInvoice.Customer.Name;
                                sales.TinNo = model.ServiceInvoice.Customer.TinNo;
                                sales.Address = model.ServiceInvoice.Customer.Address;
                                sales.Description = model.ServiceInvoice.Service.Name;
                                sales.Amount = viewModelDMCM.Total;
                                sales.ZeroRated = viewModelDMCM.Total;
                                //sales.Discount = model.Discount;
                                sales.NetSales = viewModelDMCM.NetAmount;
                                sales.CreatedBy = model.CreatedBy;
                                sales.CreatedDate = model.CreatedDate;
                                sales.DueDate = existingSv.DueDate;
                                sales.DocumentId = model.ServiceInvoiceId;
                            }
                            await _dbContext.AddAsync(sales, cancellationToken);

                            #endregion --Sales Book Recording(SV)--

                            #region --General Ledger Book Recording(SOA)--

                            var ledgers = new List<GeneralLedgerBook>();

                            ledgers.Add(
                                    new GeneralLedgerBook
                                    {
                                        Date = viewModelDMCM.Period,
                                        Reference = model.CMNo,
                                        Description = model.ServiceInvoice.Service.Name,
                                        AccountNo = "1010204",
                                        AccountTitle = "AR-Non Trade Receivable",
                                        Debit = 0,
                                        Credit = Math.Abs(model.CreditAmount - (model.WithHoldingTaxAmount + model.WithHoldingVatAmount)),
                                        CreatedBy = model.CreatedBy,
                                        CreatedDate = model.CreatedDate
                                    }
                                );
                            if (model.WithHoldingTaxAmount < 0)
                            {
                                ledgers.Add(
                                    new GeneralLedgerBook
                                    {
                                        Date = viewModelDMCM.Period,
                                        Reference = model.CMNo,
                                        Description = model.ServiceInvoice.Service.Name,
                                        AccountNo = "1010202",
                                        AccountTitle = "Deferred Creditable Withholding Tax",
                                        Debit = 0,
                                        Credit = Math.Abs(model.WithHoldingTaxAmount),
                                        CreatedBy = model.CreatedBy,
                                        CreatedDate = model.CreatedDate
                                    }
                                );
                            }
                            if (model.WithHoldingVatAmount < 0)
                            {
                                ledgers.Add(
                                    new GeneralLedgerBook
                                    {
                                        Date = viewModelDMCM.Period,
                                        Reference = model.CMNo,
                                        Description = model.ServiceInvoice.Service.Name,
                                        AccountNo = "1010203",
                                        AccountTitle = "Deferred Creditable Withholding Vat",
                                        Debit = 0,
                                        Credit = Math.Abs(model.WithHoldingVatAmount),
                                        CreatedBy = model.CreatedBy,
                                        CreatedDate = model.CreatedDate
                                    }
                                );
                            }

                            ledgers.Add(new GeneralLedgerBook
                            {
                                Date = viewModelDMCM.Period,
                                Reference = model.CMNo,
                                Description = model.ServiceInvoice.Service.Name,
                                AccountNo = model.ServiceInvoice.Service.CurrentAndPreviousNo,
                                AccountTitle = model.ServiceInvoice.Service.CurrentAndPreviousTitle,
                                Debit = Math.Round(Math.Abs(viewModelDMCM.Total / 1.12m), 2),
                                Credit = 0,
                                CreatedBy = model.CreatedBy,
                                CreatedDate = model.CreatedDate
                            });

                            if (model.VatAmount < 0)
                            {
                                ledgers.Add(
                                    new GeneralLedgerBook
                                    {
                                        Date = model.TransactionDate,
                                        Reference = model.CMNo,
                                        Description = model.ServiceInvoice.Service.Name,
                                        AccountNo = "2010304",
                                        AccountTitle = "Deferred Vat Output",
                                        Debit = Math.Abs(model.VatAmount),
                                        Credit = 0,
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

                            #endregion --General Ledger Book Recording(SOA)--
                        }

                        //#region --Audit Trail Recording

                        //AuditTrail auditTrail = new(model.PostedBy, $"Posted credit memo# {model.CMNo}", "Credit Memo");
                        //await _dbContext.AddAsync(auditTrail, cancellationToken);

                        //#endregion --Audit Trail Recording

                        //await _receiptRepo.UpdateCreditMemo(model.SalesInvoice.Id, model.Total, offsetAmount);

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        TempData["success"] = "Credit Memo has been Posted.";
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

        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {
            var model = await _dbContext.CreditMemos.FindAsync(id, cancellationToken);

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

                    await _generalRepo.RemoveRecords<SalesBook>(crb => crb.SerialNo == model.CMNo, cancellationToken);
                    await _generalRepo.RemoveRecords<GeneralLedgerBook>(gl => gl.Reference == model.CMNo, cancellationToken);

                    //#region --Audit Trail Recording

                    //AuditTrail auditTrail = new(model.VoidedBy, $"Voided credit memo# {model.CMNo}", "Credit Memo");
                    //await _dbContext.AddAsync(auditTrail, cancellationToken);

                    //#endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Credit Memo has been Voided.";
                }
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        public async Task<IActionResult> Cancel(int id, string cancellationRemarks, CancellationToken cancellationToken)
        {
            var model = await _dbContext.CreditMemos.FindAsync(id, cancellationToken);

            if (model != null)
            {
                if (!model.IsCanceled)
                {
                    model.IsCanceled = true;
                    model.CanceledBy = _userManager.GetUserName(this.User);
                    model.CanceledDate = DateTime.Now;
                    model.CancellationRemarks = cancellationRemarks;

                    //#region --Audit Trail Recording

                    //AuditTrail auditTrail = new(model.CanceledBy, $"Cancelled credit memo# {model.CMNo}", "Credit Memo");
                    //await _dbContext.AddAsync(auditTrail, cancellationToken);

                    //#endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Credit Memo has been Cancelled.";
                }
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        [HttpGet]
        public async Task<JsonResult> GetSVDetails(int svId, CancellationToken cancellationToken)
        {
            var model = await _dbContext.ServiceInvoices.FirstOrDefaultAsync(sv => sv.Id == svId, cancellationToken);
            if (model != null)
            {
                return Json(new
                {
                    model.Period,
                    model.Amount
                });
            }

            return Json(null);
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
            var selectedList = await _dbContext.CreditMemos
                .Where(cm => recordIds.Contains(cm.Id))
                .OrderBy(cm => cm.CMNo)
                .ToListAsync();

            // Create the Excel package
            using var package = new ExcelPackage();
            // Add a new worksheet to the Excel package
            var worksheet = package.Workbook.Worksheets.Add("CreditMemo");

            worksheet.Cells["A1"].Value = "TransactionDate";
            worksheet.Cells["B1"].Value = "DebitAmount";
            worksheet.Cells["C1"].Value = "Description";
            worksheet.Cells["D1"].Value = "VatableSales";
            worksheet.Cells["E1"].Value = "VatAmount";
            worksheet.Cells["F1"].Value = "TotalSales";
            worksheet.Cells["G1"].Value = "AdjustedPrice";
            worksheet.Cells["H1"].Value = "Quantity";
            worksheet.Cells["I1"].Value = "WithholdingVatAmount";
            worksheet.Cells["J1"].Value = "WithholdingTaxAmount";
            worksheet.Cells["K1"].Value = "Source";
            worksheet.Cells["L1"].Value = "Remarks";
            worksheet.Cells["M1"].Value = "Period";
            worksheet.Cells["N1"].Value = "Amount";
            worksheet.Cells["O1"].Value = "CurrentAndPreviousAmount";
            worksheet.Cells["P1"].Value = "UnearnedAmount";
            worksheet.Cells["Q1"].Value = "ServicesId";
            worksheet.Cells["R1"].Value = "CreatedBy";
            worksheet.Cells["S1"].Value = "CreatedDate";
            worksheet.Cells["T1"].Value = "CancellationRemarks";
            worksheet.Cells["U1"].Value = "OriginalSalesInvoiceId";
            worksheet.Cells["V1"].Value = "OriginalSeriesNumber";
            worksheet.Cells["W1"].Value = "OriginalServiceInvoiceId";
            worksheet.Cells["X1"].Value = "OriginalDocumentId";

            int row = 2;

            foreach (var item in selectedList)
            {
                worksheet.Cells[row, 1].Value = item.TransactionDate.ToString("yyyy-MM-dd");
                worksheet.Cells[row, 2].Value = item.CreditAmount;
                worksheet.Cells[row, 3].Value = item.Description;
                worksheet.Cells[row, 4].Value = item.VatableSales;
                worksheet.Cells[row, 5].Value = item.VatAmount;
                worksheet.Cells[row, 6].Value = item.TotalSales;
                worksheet.Cells[row, 7].Value = item.AdjustedPrice;
                worksheet.Cells[row, 8].Value = item.Quantity;
                worksheet.Cells[row, 9].Value = item.WithHoldingVatAmount;
                worksheet.Cells[row, 10].Value = item.WithHoldingTaxAmount;
                worksheet.Cells[row, 11].Value = item.Source;
                worksheet.Cells[row, 12].Value = item.Remarks;
                worksheet.Cells[row, 13].Value = item.Period;
                worksheet.Cells[row, 14].Value = item.Amount;
                worksheet.Cells[row, 15].Value = item.CurrentAndPreviousAmount;
                worksheet.Cells[row, 16].Value = item.UnearnedAmount;
                worksheet.Cells[row, 17].Value = item.ServicesId;
                worksheet.Cells[row, 18].Value = item.CreatedBy;
                worksheet.Cells[row, 19].Value = item.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                worksheet.Cells[row, 20].Value = item.CancellationRemarks;
                worksheet.Cells[row, 21].Value = item.SalesInvoiceId;
                worksheet.Cells[row, 22].Value = item.CMNo;
                worksheet.Cells[row, 23].Value = item.ServiceInvoiceId;
                worksheet.Cells[row, 24].Value = item.Id;

                row++;
            }

            // Convert the Excel package to a byte array
            var excelBytes = await package.GetAsByteArrayAsync();

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "CreditMemoList.xlsx");
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
                            return RedirectToAction(nameof(Index), new { view = DynamicView.CreditMemo });
                        }
                        if (worksheet.ToString() != nameof(DynamicView.CreditMemo))
                        {
                            TempData["error"] = "The Excel file is not related to credit memo.";
                            return RedirectToAction(nameof(Index), new { view = DynamicView.CreditMemo });
                        }

                        var rowCount = worksheet.Dimension.Rows;

                        for (int row = 2; row <= rowCount; row++)  // Assuming the first row is the header
                        {
                            var creditMemo = new CreditMemo
                            {
                                CMNo = await _creditMemoRepo.GenerateCMNo(),
                                SeriesNumber = await _creditMemoRepo.GetLastSeriesNumber(),
                                TransactionDate = DateOnly.TryParse(worksheet.Cells[row, 1].Text, out DateOnly transactionDate) ? transactionDate : default,
                                CreditAmount = decimal.TryParse(worksheet.Cells[row, 2].Text, out decimal debitAmount) ? debitAmount : 0,
                                Description = worksheet.Cells[row, 3].Text,
                                VatableSales = decimal.TryParse(worksheet.Cells[row, 4].Text, out decimal vatableSales) ? vatableSales : 0,
                                VatAmount = decimal.TryParse(worksheet.Cells[row, 5].Text, out decimal vatAmount) ? vatAmount : 0,
                                TotalSales = decimal.TryParse(worksheet.Cells[row, 6].Text, out decimal totalSales) ? totalSales : 0,
                                AdjustedPrice = decimal.TryParse(worksheet.Cells[row, 7].Text, out decimal adjustedPrice) ? adjustedPrice : 0,
                                Quantity = decimal.TryParse(worksheet.Cells[row, 8].Text, out decimal quantity) ? quantity : 0,
                                WithHoldingVatAmount = decimal.TryParse(worksheet.Cells[row, 9].Text, out decimal withHoldingVatAmount) ? withHoldingVatAmount : 0,
                                WithHoldingTaxAmount = decimal.TryParse(worksheet.Cells[row, 10].Text, out decimal withHoldingTaxAmount) ? withHoldingTaxAmount : 0,
                                Source = worksheet.Cells[row, 11].Text,
                                Remarks = worksheet.Cells[row, 12].Text,
                                Period = DateOnly.TryParse(worksheet.Cells[row, 13].Text, out DateOnly period) ? period : default,
                                Amount = decimal.TryParse(worksheet.Cells[row, 14].Text, out decimal amount) ? amount : 0,
                                CurrentAndPreviousAmount = decimal.TryParse(worksheet.Cells[row, 15].Text, out decimal currentAndPreviousAmount) ? currentAndPreviousAmount : 0,
                                UnearnedAmount = decimal.TryParse(worksheet.Cells[row, 16].Text, out decimal unearnedAmount) ? unearnedAmount : 0,
                                ServicesId = int.TryParse(worksheet.Cells[row, 17].Text, out int servicesId) ? servicesId : 0,
                                CreatedBy = worksheet.Cells[row, 18].Text,
                                CreatedDate = DateTime.TryParse(worksheet.Cells[row, 19].Text, out DateTime createdDate) ? createdDate : default,
                                CancellationRemarks = worksheet.Cells[row, 20].Text,
                                OriginalSalesInvoiceId = int.TryParse(worksheet.Cells[row, 21].Text, out int originalSalesInvoiceId) ? originalSalesInvoiceId : 0,
                                OriginalSeriesNumber = worksheet.Cells[row, 22].Text,
                                OriginalServiceInvoiceId = int.TryParse(worksheet.Cells[row, 23].Text, out int originalServiceInvoiceId) ? originalServiceInvoiceId : 0,
                                OriginalDocumentId = int.TryParse(worksheet.Cells[row, 24].Text, out int originalDocumentId) ? originalDocumentId : 0,
                            };

                            creditMemo.SalesInvoiceId = await _dbContext.SalesInvoices
                                .Where(c => c.OriginalDocumentId == creditMemo.OriginalSalesInvoiceId)
                                .Select(c => (int?)c.Id)
                                .FirstOrDefaultAsync();

                            creditMemo.ServiceInvoiceId = await _dbContext.ServiceInvoices
                                .Where(c => c.OriginalDocumentId == creditMemo.OriginalServiceInvoiceId)
                                .Select(c => (int?)c.Id)
                                .FirstOrDefaultAsync();

                            await _dbContext.CreditMemos.AddAsync(creditMemo);
                            await _dbContext.SaveChangesAsync();
                        }

                    }
                }
                catch (OperationCanceledException oce)
                {
                    TempData["error"] = oce.Message;
                    return RedirectToAction(nameof(Index), new { view = DynamicView.CreditMemo });
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(Index), new { view = DynamicView.CreditMemo });
                }
            }
            TempData["success"] = "Uploading Success!";
            return RedirectToAction(nameof(Index), new { view = DynamicView.CreditMemo });
        }

        #endregion -- import xlsx record --
    }
}