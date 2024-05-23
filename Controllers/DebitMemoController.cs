using Accounting_System.Data;
using Accounting_System.Models;
using Accounting_System.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Accounting_System.Controllers
{
    [Authorize]
    public class DebitMemoController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly DebitMemoRepo _debitMemoRepo;

        public DebitMemoController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, DebitMemoRepo dmcmRepo)
        {
            _dbContext = dbContext;
            this._userManager = userManager;
            _debitMemoRepo = dmcmRepo;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var dm = await _dbContext.DebitMemos
                .Include(dm => dm.SalesInvoice)
                .Include(dm => dm.SOA)
                .ThenInclude(soa => soa.Customer)
                .Include(dm => dm.SOA)
                .ThenInclude(soa => soa.Service)
                .ToListAsync(cancellationToken);
            return View(dm);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var viewModel = new DebitMemo();
            viewModel.SalesInvoices = await _dbContext.SalesInvoices
                .Where(si => si.IsPosted)
                .Select(si => new SelectListItem
                {
                    Value = si.Id.ToString(),
                    Text = si.SINo
                })
                .ToListAsync(cancellationToken);
            viewModel.StatementOfAccounts = await _dbContext.ServiceInvoices
                .Where(soa => soa.IsPosted)
                .Select(soa => new SelectListItem
                {
                    Value = soa.Id.ToString(),
                    Text = soa.SVNo
                })
                .ToListAsync(cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(DebitMemo model, DateTime[] period, CancellationToken cancellationToken)
        {
            model.SalesInvoices = await _dbContext.SalesInvoices
                .Where(si => si.IsPosted)
                .Select(si => new SelectListItem
                {
                    Value = si.Id.ToString(),
                    Text = si.SINo
                })
                .ToListAsync(cancellationToken);
            model.StatementOfAccounts = await _dbContext.ServiceInvoices
                .Where(soa => soa.IsPosted)
                .Select(soa => new SelectListItem
                {
                    Value = soa.Id.ToString(),
                    Text = soa.SVNo
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
                                      .Where(si => si.SIId == model.SalesInvoiceId && !si.IsPosted && !si.IsCanceled)
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
                                  .Where(si => si.SOAId == model.SOAId && !si.IsPosted && !si.IsCanceled)
                                  .OrderBy(s => s.Id)
                                  .ToList();
                    if (existingSOADMs.Count > 0)
                    {
                        ModelState.AddModelError("", $"Can’t proceed to create you have unposted DM/CM. {existingSOADMs.First().DMNo}");
                        return View(model);
                    }

                    var existingSOACMs = _dbContext.CreditMemos
                                      .Where(si => si.SOAId == model.SOAId && !si.IsPosted && !si.IsCanceled)
                                      .OrderBy(s => s.Id)
                                      .ToList();
                    if (existingSOACMs.Count > 0)
                    {
                        ModelState.AddModelError("", $"Can’t proceed to create you have unposted DM/CM. {existingSOACMs.First().CMNo}");
                        return View(model);
                    }
                }

                #region --Validating the series--

                var getLastNumber = await _debitMemoRepo.GetLastSeriesNumber(cancellationToken);

                if (getLastNumber > 9999999999)
                {
                    TempData["error"] = "You reach the maximum Series Number";
                    return View(model);
                }
                var totalRemainingSeries = 9999999999 - getLastNumber;
                if (getLastNumber >= 9999999899)
                {
                    TempData["warning"] = $"Debit Memo created successfully, Warning {totalRemainingSeries} series number remaining";
                }
                else
                {
                    TempData["success"] = "Debit Memo created successfully";
                }

                #endregion --Validating the series--

                var generateDMNo = await _debitMemoRepo.GenerateDMNo(cancellationToken);

                model.SeriesNumber = getLastNumber;
                model.DMNo = generateDMNo;
                model.CreatedBy = _userManager.GetUserName(this.User);

                if (model.Source == "Sales Invoice")
                {
                    model.SOAId = null;

                    var existingSalesInvoice = await _dbContext.SalesInvoices
                                               .FirstOrDefaultAsync(si => si.Id == model.SalesInvoiceId, cancellationToken);

                    model.DebitAmount = (decimal)(model.Quantity * model.AdjustedPrice);

                    if (existingSalesInvoice.CustomerType == "Vatable")
                    {
                        model.VatableSales = model.DebitAmount / 1.12m;
                        model.VatAmount = model.DebitAmount - model.VatableSales;

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
                        model.TotalSales = model.DebitAmount;
                    }

                }
                else if (model.Source == "Statement Of Account")
                {
                    model.SalesInvoiceId = null;

                    var existingSoa = await _dbContext.ServiceInvoices
                        .Include(soa => soa.Customer)
                        .FirstOrDefaultAsync(soa => soa.Id == model.SOAId, cancellationToken);


                    #region --Retrieval of Services

                    model.ServicesId = existingSoa.ServicesId;

                    var services = await _dbContext
                    .Services
                    .FirstOrDefaultAsync(s => s.Id == model.ServicesId, cancellationToken);

                    #endregion --Retrieval of Services

                    #region --DM Entries function

                    foreach (var amount in model.Amount)
                    {
                        model.CurrentAndPreviousAmount += amount;
                    }

                    #endregion ----DM Entries function

                    model.DebitAmount = model.CurrentAndPreviousAmount;


                    if (existingSoa.Customer.CustomerType == "Vatable")
                    {
                        model.VatableSales = model.DebitAmount / 1.12m;
                        model.VatAmount = model.DebitAmount - model.VatableSales;
                        model.TotalSales = model.VatableSales + model.VatAmount;
                        model.WithHoldingTaxAmount = model.VatableSales * (services.Percent / 100m);

                        if (existingSoa.WithholdingVatAmount != 0)
                        {
                            model.WithHoldingVatAmount = model.VatableSales * 0.05m;
                        }
                    }
                    else
                    {
                        model.TotalSales = model.DebitAmount;
                    }
                }

                #region --Audit Trail Recording

                AuditTrail auditTrail = new(model.CreatedBy, $"Create new debit memo# {model.DMNo}", "Debit Memo");
                _dbContext.Add(auditTrail);

                #endregion --Audit Trail Recording

                await _dbContext.AddAsync(model, cancellationToken);
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
        public async Task<IActionResult> Print(int? id, CancellationToken cancellationToken)
        {
            if (id == null || _dbContext.DebitMemos == null)
            {
                return NotFound();
            }

            var debitMemo = await _dbContext.DebitMemos
                .Include(dm => dm.SalesInvoice)
                .Include(dm => dm.SOA)
                .ThenInclude(soa => soa.Customer)
                .Include(dm => dm.SOA)
                .ThenInclude(soa => soa.Service)
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
            if (debitMemo == null)
            {
                return NotFound();
            }
            return View(debitMemo);
        }

        public async Task<IActionResult> PrintedDM(int id, CancellationToken cancellationToken)
        {
            var findIdOfDM = await _debitMemoRepo.FindDM(id, cancellationToken);
            if (findIdOfDM != null && !findIdOfDM.IsPrinted)
            {
                #region --Audit Trail Recording

                var printedBy = _userManager.GetUserName(this.User);
                AuditTrail auditTrail = new(printedBy, $"Printed original copy of dm# {findIdOfDM.DMNo}", "Debit Memo");
                await _dbContext.AddAsync(auditTrail, cancellationToken);

                #endregion --Audit Trail Recording

                findIdOfDM.IsPrinted = true;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            return RedirectToAction("Print", new { id = id });
        }

        public async Task<IActionResult> Post(int id, CancellationToken cancellationToken, ViewModelDMCM viewModelDMCM)
        {
            var model = await _debitMemoRepo.FindDM(id, cancellationToken);

            if (model != null)
            {
                if (!model.IsPosted)
                {
                    model.IsPosted = true;
                    model.PostedBy = _userManager.GetUserName(this.User);
                    model.PostedDate = DateTime.Now;

                    if (model.SalesInvoiceId != null)
                    {
                        #region --Retrieval of SI

                        var existingSI = await _dbContext.SalesInvoices
                                                    .FirstOrDefaultAsync(si => si.Id == model.SalesInvoiceId, cancellationToken);

                        #endregion --Retrieval of SI

                        #region --Sales Book Recording(SI)--

                        var sales = new SalesBook();

                        if (model.SalesInvoice.CustomerType == "Vatable")
                        {
                            sales.TransactionDate = model.Date.ToShortDateString();
                            sales.SerialNo = model.DMNo;
                            sales.SoldTo = model.SalesInvoice.SoldTo;
                            sales.TinNo = model.SalesInvoice.TinNo;
                            sales.Address = model.SalesInvoice.Address;
                            sales.Description = model.SalesInvoice.ProductName;
                            sales.Amount = model.DebitAmount;
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
                            sales.TransactionDate = model.Date.ToShortDateString();
                            sales.SerialNo = model.DMNo;
                            sales.SoldTo = model.SalesInvoice.SoldTo;
                            sales.TinNo = model.SalesInvoice.TinNo;
                            sales.Address = model.SalesInvoice.Address;
                            sales.Description = model.SalesInvoice.ProductName;
                            sales.Amount = model.DebitAmount;
                            sales.VatExemptSales = model.DebitAmount;
                            //sales.Discount = model.Discount;
                            sales.NetSales = model.VatableSales;
                            sales.CreatedBy = model.CreatedBy;
                            sales.CreatedDate = model.CreatedDate;
                            sales.DueDate = existingSI?.DueDate;
                            sales.DocumentId = model.SalesInvoiceId;
                        }
                        else
                        {
                            sales.TransactionDate = model.Date.ToShortDateString();
                            sales.SerialNo = model.DMNo;
                            sales.SoldTo = model.SalesInvoice.SoldTo;
                            sales.TinNo = model.SalesInvoice.TinNo;
                            sales.Address = model.SalesInvoice.Address;
                            sales.Description = model.SalesInvoice.ProductName;
                            sales.Amount = model.DebitAmount;
                            sales.ZeroRated = model.DebitAmount;
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
                                Date = model.Date.ToShortDateString(),
                                Reference = model.DMNo,
                                Description = model.SalesInvoice.ProductName,
                                AccountNo = "1010201",
                                AccountTitle = "AR-Trade Receivable",
                                Debit = model.DebitAmount - (model.WithHoldingTaxAmount + model.WithHoldingVatAmount),
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
                                    Date = model.Date.ToShortDateString(),
                                    Reference = model.DMNo,
                                    Description = model.SalesInvoice.ProductName,
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
                                    Date = model.Date.ToShortDateString(),
                                    Reference = model.DMNo,
                                    Description = model.SalesInvoice.ProductName,
                                    AccountNo = "1010203",
                                    AccountTitle = "Deferred Creditable Withholding Vat",
                                    Debit = model.WithHoldingVatAmount,
                                    Credit = 0,
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
                                    Date = model.Date.ToShortDateString(),
                                    Reference = model.DMNo,
                                    Description = model.SalesInvoice.ProductName,
                                    AccountNo = "4010101",
                                    AccountTitle = "Sales - Biodiesel",
                                    Debit = 0,
                                    CreatedBy = model.CreatedBy,
                                    Credit = model.VatableSales > 0
                                                ? model.VatableSales
                                                : model.DebitAmount,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }
                        else if (model.SalesInvoice.ProductName == "Econogas")
                        {
                            ledgers.Add(
                                new GeneralLedgerBook
                                {
                                    Date = model.Date.ToShortDateString(),
                                    Reference = model.DMNo,
                                    Description = model.SalesInvoice.ProductName,
                                    AccountNo = "4010102",
                                    AccountTitle = "Sales - Econogas",
                                    Debit = 0,
                                    CreatedBy = model.CreatedBy,
                                    Credit = model.VatableSales > 0
                                                ? model.VatableSales
                                                : model.DebitAmount,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }
                        else if (model.SalesInvoice.ProductName == "Envirogas")
                        {
                            ledgers.Add(
                                new GeneralLedgerBook
                                {
                                    Date = model.Date.ToShortDateString(),
                                    Reference = model.DMNo,
                                    Description = model.SalesInvoice.ProductName,
                                    AccountNo = "4010103",
                                    AccountTitle = "Sales - Envirogas",
                                    Debit = 0,
                                    CreatedBy = model.CreatedBy,
                                    Credit = model.VatableSales > 0
                                                ? model.VatableSales
                                                : model.DebitAmount,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }

                        if (model.VatAmount > 0)
                        {
                            ledgers.Add(
                                new GeneralLedgerBook
                                {
                                    Date = model.Date.ToShortDateString(),
                                    Reference = model.DMNo,
                                    Description = model.SalesInvoice.ProductName,
                                    AccountNo = "2010301",
                                    AccountTitle = "Vat Output",
                                    Debit = 0,
                                    Credit = model.VatAmount,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }

                        await _dbContext.GeneralLedgerBooks.AddRangeAsync(ledgers, cancellationToken);

                        #endregion --General Ledger Book Recording(SI)--
                    }

                    if (model.SOAId != null)
                    {
                        var existingSOA = await _dbContext.ServiceInvoices
                            .Include(soa => soa.Customer)
                            .FirstOrDefaultAsync(si => si.Id == model.SOAId, cancellationToken);

                        #region --Retrieval of Services

                        var services = await _debitMemoRepo.GetServicesAsync(model.ServicesId, cancellationToken);

                        #endregion --Retrieval of Services

                        for (int i = 0; i < model.Amount.Length; i++)
                        {
                            model.Amount[i] = model.Amount[i];
                        }
                        for (int i = 0; i < model.Period.Length; i++)
                        {
                            if (model.Amount[i] > 0)
                            {
                                #region --SOA Computation--

                                viewModelDMCM.Period = model.CreatedDate >= model.Period[i] ? model.CreatedDate : model.Period[i].AddMonths(1).AddDays(-1);

                                if (existingSOA.Customer.CustomerType == "Vatable")
                                {
                                    viewModelDMCM.Total = model.Amount[i];
                                    viewModelDMCM.NetAmount = (model.Amount[i] - existingSOA.Discount) / 1.12m;
                                    viewModelDMCM.VatAmount = (model.Amount[i] - existingSOA.Discount) - viewModelDMCM.NetAmount;
                                    viewModelDMCM.WithholdingTaxAmount = viewModelDMCM.NetAmount * (services.Percent / 100m);
                                    if (existingSOA.Customer.WithHoldingVat)
                                    {
                                        viewModelDMCM.WithholdingVatAmount = viewModelDMCM.NetAmount * 0.05m;
                                    }
                                }
                                else
                                {
                                    viewModelDMCM.NetAmount = model.Amount[i] - existingSOA.Discount;
                                    viewModelDMCM.WithholdingTaxAmount = viewModelDMCM.NetAmount * (services.Percent / 100m);
                                    if (existingSOA.Customer.WithHoldingVat)
                                    {
                                        viewModelDMCM.WithholdingVatAmount = viewModelDMCM.NetAmount * 0.05m;
                                    }
                                }

                                if (existingSOA.Customer.CustomerType == "Vatable")
                                {
                                    var total = Math.Round(model.Amount[i] / 1.12m, 2);

                                    var roundedNetAmount = Math.Round(viewModelDMCM.NetAmount, 2);

                                    if (roundedNetAmount > total)
                                    {
                                        var shortAmount = viewModelDMCM.NetAmount - total;

                                        viewModelDMCM.Amount += shortAmount;
                                    }
                                }

                                #endregion --SOA Computation--

                                #region --Sales Book Recording(SOA)--

                                var sales = new SalesBook();

                                if (model.SOA.Customer.CustomerType == "Vatable")
                                {
                                    sales.TransactionDate = viewModelDMCM.Period.ToShortDateString();
                                    sales.SerialNo = model.DMNo;
                                    sales.SoldTo = model.SOA.Customer.Name;
                                    sales.TinNo = model.SOA.Customer.TinNo;
                                    sales.Address = model.SOA.Customer.Address;
                                    sales.Description = model.SOA.Service.Name;
                                    sales.Amount = viewModelDMCM.Total;
                                    sales.VatAmount = viewModelDMCM.VatAmount;
                                    sales.VatableSales = viewModelDMCM.Total / 1.12m;
                                    //sales.Discount = model.Discount;
                                    sales.NetSales = viewModelDMCM.NetAmount;
                                    sales.CreatedBy = model.CreatedBy;
                                    sales.CreatedDate = model.CreatedDate;
                                    sales.DueDate = existingSOA?.DueDate;
                                    sales.DocumentId = existingSOA.Id;
                                }
                                else if (model.SOA.Customer.CustomerType == "Exempt")
                                {
                                    sales.TransactionDate = viewModelDMCM.Period.ToShortDateString();
                                    sales.SerialNo = model.DMNo;
                                    sales.SoldTo = model.SOA.Customer.Name;
                                    sales.TinNo = model.SOA.Customer.TinNo;
                                    sales.Address = model.SOA.Customer.Address;
                                    sales.Description = model.SOA.Service.Name;
                                    sales.Amount = viewModelDMCM.Total;
                                    sales.VatExemptSales = viewModelDMCM.Total;
                                    //sales.Discount = model.Discount;
                                    sales.NetSales = viewModelDMCM.NetAmount;
                                    sales.CreatedBy = model.CreatedBy;
                                    sales.CreatedDate = model.CreatedDate;
                                    sales.DueDate = existingSOA?.DueDate;
                                    sales.DocumentId = existingSOA.Id;
                                }
                                else
                                {
                                    sales.TransactionDate = viewModelDMCM.Period.ToShortDateString();
                                    sales.SerialNo = model.DMNo;
                                    sales.SoldTo = model.SOA.Customer.Name;
                                    sales.TinNo = model.SOA.Customer.TinNo;
                                    sales.Address = model.SOA.Customer.Address;
                                    sales.Description = model.SOA.Service.Name;
                                    sales.Amount = viewModelDMCM.Total;
                                    sales.ZeroRated = viewModelDMCM.Total;
                                    //sales.Discount = model.Discount;
                                    sales.NetSales = viewModelDMCM.NetAmount;
                                    sales.CreatedBy = model.CreatedBy;
                                    sales.CreatedDate = model.CreatedDate;
                                    sales.DueDate = existingSOA?.DueDate;
                                    sales.DocumentId = existingSOA.Id;
                                }
                                await _dbContext.AddAsync(sales, cancellationToken);

                                #endregion --Sales Book Recording(SOA)--

                                #region --General Ledger Book Recording(SOA)--

                                var ledgers = new List<GeneralLedgerBook>();

                                ledgers.Add(
                                        new GeneralLedgerBook
                                        {
                                            Date = viewModelDMCM.Period.ToShortDateString(),
                                            Reference = model.DMNo,
                                            Description = model.SOA.Service.Name,
                                            AccountNo = "1010204",
                                            AccountTitle = "AR-Non Trade Receivable",
                                            Debit = viewModelDMCM.Total - (viewModelDMCM.WithholdingTaxAmount + viewModelDMCM.WithholdingVatAmount),
                                            Credit = 0,
                                            CreatedBy = model.CreatedBy,
                                            CreatedDate = model.CreatedDate
                                        }
                                    );
                                if (viewModelDMCM.WithholdingTaxAmount > 0)
                                {
                                    ledgers.Add(
                                        new GeneralLedgerBook
                                        {
                                            Date = viewModelDMCM.Period.ToShortDateString(),
                                            Reference = model.DMNo,
                                            Description = model.SOA.Service.Name,
                                            AccountNo = "1010202",
                                            AccountTitle = "Deferred Creditable Withholding Tax",
                                            Debit = viewModelDMCM.WithholdingTaxAmount,
                                            Credit = 0,
                                            CreatedBy = model.CreatedBy,
                                            CreatedDate = model.CreatedDate
                                        }
                                    );
                                }
                                if (viewModelDMCM.WithholdingVatAmount > 0)
                                {
                                    ledgers.Add(
                                        new GeneralLedgerBook
                                        {
                                            Date = viewModelDMCM.Period.ToShortDateString(),
                                            Reference = model.DMNo,
                                            Description = model.SOA.Service.Name,
                                            AccountNo = "1010203",
                                            AccountTitle = "Deferred Creditable Withholding Vat",
                                            Debit = viewModelDMCM.WithholdingVatAmount,
                                            Credit = 0,
                                            CreatedBy = model.CreatedBy,
                                            CreatedDate = model.CreatedDate
                                        }
                                    );
                                }

                                var split = model.SOA.Service.CurrentAndPreviousTitle.Split(" ");
                                var serviceNo = split.First();
                                var serviceName = split.Last();

                                if (viewModelDMCM.Total > 0)
                                {
                                    ledgers.Add(new GeneralLedgerBook
                                    {
                                        Date = viewModelDMCM.Period.ToShortDateString(),
                                        Reference = model.DMNo,
                                        Description = model.SOA.Service.Name,
                                        AccountNo = serviceNo,
                                        AccountTitle = serviceName,
                                        Debit = 0,
                                        Credit = viewModelDMCM.Total / 1.12m,
                                        CreatedBy = model.CreatedBy,
                                        CreatedDate = model.CreatedDate
                                    });
                                }

                                if (viewModelDMCM.VatAmount > 0)
                                {
                                    ledgers.Add(
                                        new GeneralLedgerBook
                                        {
                                            Date = viewModelDMCM.Period.ToShortDateString(),
                                            Reference = model.DMNo,
                                            Description = model.SOA.Service.Name,
                                            AccountNo = "2010304",
                                            AccountTitle = "Deferred Vat Output",
                                            Debit = 0,
                                            Credit = viewModelDMCM.VatAmount,
                                            CreatedBy = model.CreatedBy,
                                            CreatedDate = model.CreatedDate
                                        }
                                    );
                                }

                                await _dbContext.GeneralLedgerBooks.AddRangeAsync(ledgers, cancellationToken);

                                #endregion --General Ledger Book Recording(SOA)--
                            }
                        }
                    }


                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(model.PostedBy, $"Posted debit memo# {model.DMNo}", "Debit Memo");
                    await _dbContext.AddAsync(auditTrail, cancellationToken);

                    #endregion --Audit Trail Recording

                    //await _receiptRepo.UpdateCreditMemo(model.SalesInvoice.Id, model.Total, offsetAmount);

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Debit Memo has been Posted.";
                }
                return RedirectToAction("Index");
            }

            return NotFound();
        }

        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {
            var model = await _dbContext.DebitMemos.FindAsync(id, cancellationToken);

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

                    AuditTrail auditTrail = new(model.VoidedBy, $"Voided debit memo# {model.DMNo}", "Debit Memo");
                    await _dbContext.AddAsync(auditTrail, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Debit Memo has been Voided.";
                }
                return RedirectToAction("Index");
            }

            return NotFound();
        }

        public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
        {
            var model = await _dbContext.DebitMemos.FindAsync(id, cancellationToken);

            if (model != null)
            {
                if (!model.IsCanceled)
                {
                    model.IsCanceled = true;
                    model.CanceledBy = _userManager.GetUserName(this.User);
                    model.CanceledDate = DateTime.Now;

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(model.CanceledBy, $"Cancelled debit memo# {model.DMNo}", "Debit Memo");
                    await _dbContext.AddAsync(auditTrail, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Debit Memo has been Cancelled.";
                }
                return RedirectToAction("Index");
            }

            return NotFound();
        }

        public async Task<IActionResult> Preview(int id, CancellationToken cancellationToken)
        {
            var dm = await _debitMemoRepo.FindDM(id, cancellationToken);
            return PartialView("_PreviewDebit", dm);
        }

        [HttpGet]
        public async Task<JsonResult> GetSOADetails(int soaId, CancellationToken cancellationToken)
        {
            var model = await _dbContext.ServiceInvoices.FirstOrDefaultAsync(soa => soa.Id == soaId, cancellationToken);
            if (model != null)
            {
                return Json(new
                {
                    Period = model?.Period,
                    Amount = model?.Amount
                });
            }

            return Json(null);
        }
    }
}