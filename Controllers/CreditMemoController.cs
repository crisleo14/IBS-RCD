using Accounting_System.Data;
using Accounting_System.Models;
using Accounting_System.Models.ViewModels;
using Accounting_System.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Accounting_System.Controllers
{
    [Authorize]
    public class CreditMemoController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly CreditMemoRepo _creditMemoRepo;

        public CreditMemoController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, CreditMemoRepo creditMemoRepo)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _creditMemoRepo = creditMemoRepo;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var cm = await _dbContext.CreditMemos
                .Include(cm => cm.SalesInvoice)
                .Include(cm => cm.ServiceInvoice)
                .ThenInclude(sv => sv.Customer)
                .Include(cm => cm.ServiceInvoice)
                .ThenInclude(sv => sv.Service)
                .ToListAsync(cancellationToken);

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
        public async Task<IActionResult> Create(CreditMemo model, DateTime[] period, CancellationToken cancellationToken, decimal[]? amount)
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

            if (ModelState.IsValid)
            {

                if (model.SalesInvoiceId != null)
                {
                    var existingSIDMs = _dbContext.DebitMemos
                                  .Where(si => si.SalesInvoiceId == model.SalesInvoiceId && !si.IsPosted && !si.IsCanceled)
                                  .OrderBy(s => s.Id)
                                  .ToList();
                    if (existingSIDMs.Count > 0)
                    {
                        ModelState.AddModelError("", $"Can’t proceed to create you have unposted DM/CM. {existingSIDMs.First().DMNo}");
                        return View(model);
                    }

                    var existingSICMs = _dbContext.CreditMemos
                                      .Where(si => si.SalesInvoiceId == model.SalesInvoiceId && !si.IsPosted && !si.IsCanceled)
                                      .OrderBy(s => s.Id)
                                      .ToList();
                    if (existingSICMs.Count > 0)
                    {
                        ModelState.AddModelError("", $"Can’t proceed to create you have unposted DM/CM. {existingSICMs.First().CMNo}");
                        return View(model);
                    }
                }
                else
                {
                    var existingSOADMs = _dbContext.DebitMemos
                                  .Where(si => si.ServiceInvoiceId == model.ServiceInvoiceId && !si.IsPosted && !si.IsCanceled)
                                  .OrderBy(s => s.Id)
                                  .ToList();
                    if (existingSOADMs.Count > 0)
                    {
                        ModelState.AddModelError("", $"Can’t proceed to create you have unposted DM/CM. {existingSOADMs.First().DMNo}");
                        return View(model);
                    }

                    var existingSOACMs = _dbContext.CreditMemos
                                      .Where(si => si.ServiceInvoiceId == model.ServiceInvoiceId && !si.IsPosted && !si.IsCanceled)
                                      .OrderBy(s => s.Id)
                                      .ToList();
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

                    var existingSalesInvoice = await _dbContext.SalesInvoices
                                               .FirstOrDefaultAsync(si => si.Id == model.SalesInvoiceId, cancellationToken);

                    model.CreditAmount = (decimal)(model.Quantity * -model.AdjustedPrice);

                    if (existingSalesInvoice.CustomerType == "Vatable")
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

                    var existingSv = await _dbContext.ServiceInvoices
                        .Include(sv => sv.Customer)
                        .FirstOrDefaultAsync(sv => sv.Id == model.ServiceInvoiceId, cancellationToken);

                    #region --Retrieval of Services

                    model.ServicesId = existingSv.ServicesId;

                    var services = await _dbContext
                    .Services
                    .FirstOrDefaultAsync(s => s.Id == model.ServicesId, cancellationToken);

                    #endregion --Retrieval of Services

                    model.CreditAmount = -model.Amount;

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

                #region --Audit Trail Recording

                AuditTrail auditTrail = new(model.CreatedBy, $"Create new credit memo# {model.CMNo}", "Credit Memo");
                _dbContext.Add(auditTrail);

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
            if (id == null || _dbContext.CreditMemos == null)
            {
                return NotFound();
            }

            var creditMemo = await _dbContext.CreditMemos.FindAsync(id, cancellationToken);
            if (creditMemo == null)
            {
                return NotFound();
            }

            creditMemo.SalesInvoices = await _dbContext.SalesInvoices
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.SINo
                })
                .ToListAsync(cancellationToken);

            creditMemo.ServiceInvoices = await _dbContext.ServiceInvoices
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.SVNo
                })
                .ToListAsync(cancellationToken);

            return View(creditMemo);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(CreditMemo model, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                var existingModel = await _dbContext.CreditMemos.FindAsync(model.Id, cancellationToken);

                if (existingModel == null)
                {
                    return NotFound();
                }

                existingModel.Date = model.Date;
                existingModel.Source = model.Source;
                existingModel.Description = model.Description;
                existingModel.AdjustedPrice = model.AdjustedPrice;

                if (existingModel.Source == "Sales Invoice")
                {
                    existingModel.ServiceInvoiceId = null;
                    existingModel.SalesInvoiceId = model.SalesInvoiceId;

                    var existingSalesInvoice = await _dbContext.SalesInvoices
                                               .FirstOrDefaultAsync(si => si.Id == existingModel.SalesInvoiceId, cancellationToken);

                    existingModel.CreditAmount = (decimal)(model.Quantity * model.AdjustedPrice);

                    if (existingSalesInvoice.CustomerType == "Vatable")
                    {
                        existingModel.VatableSales = existingModel.CreditAmount / (decimal)1.12;
                        existingModel.VatAmount = existingModel.CreditAmount - existingModel.VatableSales;
                        existingModel.TotalSales = existingModel.VatableSales + existingModel.VatAmount;
                    }

                    existingModel.TotalSales = existingModel.CreditAmount;
                }
                else if (model.Source == "Service Invoice")
                {
                    existingModel.SalesInvoiceId = null;
                    existingModel.ServiceInvoiceId = model.ServiceInvoiceId;

                    var existingSv = await _dbContext.ServiceInvoices
                        .Include(sv => sv.Customer)
                        .FirstOrDefaultAsync(sv => sv.Id == existingModel.ServiceInvoiceId, cancellationToken);

                    existingModel.CreditAmount = (decimal)(model.AdjustedPrice - existingSv.Total);

                    if (existingSv.Customer.CustomerType == "Vatable")
                    {
                        existingModel.VatableSales = existingModel.CreditAmount / (decimal)1.12;
                        existingModel.VatAmount = existingModel.CreditAmount - existingModel.VatableSales;
                        existingModel.TotalSales = existingModel.VatableSales + existingModel.VatAmount;
                    }

                    existingModel.TotalSales = existingModel.CreditAmount;
                }

                await _dbContext.SaveChangesAsync(cancellationToken);

                TempData["success"] = "Credit Memo updated successfully";
                return RedirectToAction("Index");
            }

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
                #region --Audit Trail Recording

                var printedBy = _userManager.GetUserName(this.User);
                AuditTrail auditTrail = new(printedBy, $"Printed original copy of cm# {cm.CMNo}", "Credit Memo");
                await _dbContext.AddAsync(auditTrail, cancellationToken);

                #endregion --Audit Trail Recording

                cm.IsPrinted = true;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            return RedirectToAction("Print", new { id = id });
        }

        public async Task<IActionResult> Post(int id, CancellationToken cancellationToken, ViewModelDMCM viewModelDMCM)
        {
            var model = await _creditMemoRepo.FindCM(id, cancellationToken);

            if (model != null)
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
                                                    .FirstOrDefaultAsync(si => si.Id == model.SalesInvoiceId, cancellationToken);

                        #endregion --Retrieval of SI and SOA--

                        #region --Sales Book Recording(SI)--

                        var sales = new SalesBook();

                        if (model.SalesInvoice.CustomerType == "Vatable")
                        {
                            sales.TransactionDate = model.Date;
                            sales.SerialNo = model.CMNo;
                            sales.SoldTo = model.SalesInvoice.SoldTo;
                            sales.TinNo = model.SalesInvoice.TinNo;
                            sales.Address = model.SalesInvoice.Address;
                            sales.Description = model.SalesInvoice.ProductName;
                            sales.Amount = model.CreditAmount;
                            sales.VatAmount = model.VatAmount;
                            sales.VatableSales = model.VatableSales;
                            //sales.Discount = model.Discount;
                            sales.NetSales = model.VatableSales;
                            sales.CreatedBy = model.CreatedBy;
                            sales.CreatedDate = model.CreatedDate;
                            sales.DueDate = existingSI?.DueDate;
                            sales.DocumentId = model.SalesInvoiceId;
                        }
                        else if (model.SalesInvoice.CustomerType == "Exempt")
                        {
                            sales.TransactionDate = model.Date;
                            sales.SerialNo = model.CMNo;
                            sales.SoldTo = model.SalesInvoice.SoldTo;
                            sales.TinNo = model.SalesInvoice.TinNo;
                            sales.Address = model.SalesInvoice.Address;
                            sales.Description = model.SalesInvoice.ProductName;
                            sales.Amount = model.CreditAmount;
                            sales.VatExemptSales = model.CreditAmount;
                            //sales.Discount = model.Discount;
                            sales.NetSales = model.VatableSales;
                            sales.CreatedBy = model.CreatedBy;
                            sales.CreatedDate = model.CreatedDate;
                            sales.DueDate = existingSI?.DueDate;
                            sales.DocumentId = model.SalesInvoiceId;
                        }
                        else
                        {
                            sales.TransactionDate = model.Date;
                            sales.SerialNo = model.CMNo;
                            sales.SoldTo = model.SalesInvoice.SoldTo;
                            sales.TinNo = model.SalesInvoice.TinNo;
                            sales.Address = model.SalesInvoice.Address;
                            sales.Description = model.SalesInvoice.ProductName;
                            sales.Amount = model.CreditAmount;
                            sales.ZeroRated = model.CreditAmount;
                            //sales.Discount = model.Discount;
                            sales.NetSales = model.VatableSales;
                            sales.CreatedBy = model.CreatedBy;
                            sales.CreatedDate = model.CreatedDate;
                            sales.DueDate = existingSI?.DueDate;
                            sales.DocumentId = model.SalesInvoiceId;
                        }
                        await _dbContext.AddAsync(sales, cancellationToken);

                        #endregion --Sales Book Recording(SI)--

                        #region --General Ledger Book Recording(SI)--

                        var ledgers = new List<GeneralLedgerBook>();

                        ledgers.Add(
                            new GeneralLedgerBook
                            {
                                Date = model.Date,
                                Reference = model.CMNo,
                                Description = model.SalesInvoice.ProductName,
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
                                    Date = model.Date,
                                    Reference = model.CMNo,
                                    Description = model.SalesInvoice.ProductName,
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
                                    Date = model.Date,
                                    Reference = model.CMNo,
                                    Description = model.SalesInvoice.ProductName,
                                    AccountNo = "1010203",
                                    AccountTitle = "Deferred Creditable Withholding Vat",
                                    Debit = 0,
                                    Credit = Math.Abs(model.WithHoldingVatAmount),
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }
                        if (model.SalesInvoice.ProductName == "Biodiesel")
                        {
                            ledgers.Add(
                                new GeneralLedgerBook
                                {
                                    Date = model.Date,
                                    Reference = model.CMNo,
                                    Description = model.SalesInvoice.ProductName,
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
                        else if (model.SalesInvoice.ProductName == "Econogas")
                        {
                            ledgers.Add(
                                new GeneralLedgerBook
                                {
                                    Date = model.Date,
                                    Reference = model.CMNo,
                                    Description = model.SalesInvoice.ProductName,
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
                        else if (model.SalesInvoice.ProductName == "Envirogas")
                        {
                            ledgers.Add(
                                new GeneralLedgerBook
                                {
                                    Date = model.Date,
                                    Reference = model.CMNo,
                                    Description = model.SalesInvoice.ProductName,
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
                                    Date = model.Date,
                                    Reference = model.CMNo,
                                    Description = model.SalesInvoice.ProductName,
                                    AccountNo = "2010301",
                                    AccountTitle = "Vat Output",
                                    Debit = Math.Abs(model.VatAmount),
                                    Credit = 0,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
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

                        var services = await _creditMemoRepo.GetServicesAsync(model.ServicesId, cancellationToken);

                        #endregion --Retrieval of Services

                        #region --SV Computation--

                        viewModelDMCM.Period = DateOnly.FromDateTime(model.CreatedDate) >= model.Period ? DateOnly.FromDateTime(model.CreatedDate) : model.Period.AddMonths(1).AddDays(-1);

                        if (existingSv.Customer.CustomerType == "Vatable")
                        {
                            viewModelDMCM.Total = -model.Amount;
                            viewModelDMCM.NetAmount = (model.Amount - existingSv.Discount) / 1.12m;
                            viewModelDMCM.VatAmount = (model.Amount - existingSv.Discount) - viewModelDMCM.NetAmount;
                            viewModelDMCM.WithholdingTaxAmount = viewModelDMCM.NetAmount * (services.Percent / 100m);
                            if (existingSv.Customer.WithHoldingVat)
                            {
                                viewModelDMCM.WithholdingVatAmount = viewModelDMCM.NetAmount * 0.05m;
                            }
                        }
                        else
                        {
                            viewModelDMCM.NetAmount = model.Amount - existingSv.Discount;
                            viewModelDMCM.WithholdingTaxAmount = viewModelDMCM.NetAmount * (services.Percent / 100m);
                            if (existingSv.Customer.WithHoldingVat)
                            {
                                viewModelDMCM.WithholdingVatAmount = viewModelDMCM.NetAmount * 0.05m;
                            }
                        }

                        if (existingSv.Customer.CustomerType == "Vatable")
                        {
                            var total = Math.Round(model.Amount / 1.12m, 2);

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
                            sales.DueDate = existingSv?.DueDate;
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
                            sales.DueDate = existingSv?.DueDate;
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
                            sales.DueDate = existingSv?.DueDate;
                            sales.DocumentId = model.ServiceInvoiceId;
                        }
                        await _dbContext.AddAsync(sales, cancellationToken);

                        #endregion --Sales Book Recording(SOA)--

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
                            Debit = Math.Abs(viewModelDMCM.Total / 1.12m),
                            Credit = 0,
                            CreatedBy = model.CreatedBy,
                            CreatedDate = model.CreatedDate
                        });

                        if (model.VatAmount < 0)
                        {
                            ledgers.Add(
                                new GeneralLedgerBook
                                {
                                    Date = model.Date,
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

                        await _dbContext.GeneralLedgerBooks.AddRangeAsync(ledgers, cancellationToken);

                        #endregion --General Ledger Book Recording(SOA)--
                    }

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(model.PostedBy, $"Posted credit memo# {model.CMNo}", "Credit Memo");
                    await _dbContext.AddAsync(auditTrail, cancellationToken);

                    #endregion --Audit Trail Recording

                    //await _receiptRepo.UpdateCreditMemo(model.SalesInvoice.Id, model.Total, offsetAmount);

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Credit Memo has been Posted.";
                }
                return RedirectToAction("Index");
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
                    model.IsVoided = true;
                    model.VoidedBy = _userManager.GetUserName(this.User);
                    model.VoidedDate = DateTime.Now;

                    //await _generalRepo.RemoveRecords<CashReceiptBook>(crb => crb.RefNo == model.CRNo);
                    //await _generalRepo.RemoveRecords<GeneralLedgerBook>(gl => gl.Reference == model.CRNo);

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(model.VoidedBy, $"Voided credit memo# {model.CMNo}", "Credit Memo");
                    await _dbContext.AddAsync(auditTrail, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Credit Memo has been Voided.";
                }
                return RedirectToAction("Index");
            }

            return NotFound();
        }

        public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
        {
            var model = await _dbContext.CreditMemos.FindAsync(id, cancellationToken);

            if (model != null)
            {
                if (!model.IsCanceled)
                {
                    model.IsCanceled = true;
                    model.CanceledBy = _userManager.GetUserName(this.User);
                    model.CanceledDate = DateTime.Now;

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(model.CanceledBy, $"Cancelled credit memo# {model.CMNo}", "Credit Memo");
                    await _dbContext.AddAsync(auditTrail, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Credit Memo has been Cancelled.";
                }
                return RedirectToAction("Index");
            }

            return NotFound();
        }

        public async Task<IActionResult> Preview(int id, CancellationToken cancellationToken)
        {
            var cm = await _creditMemoRepo.FindCM(id, cancellationToken);
            return PartialView("_PreviewCredit", cm);
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
    }
}