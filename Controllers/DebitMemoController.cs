using Accounting_System.Data;
using Accounting_System.Models;
using Accounting_System.Repository;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Accounting_System.Controllers
{
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
                .OrderBy(dm => dm.Id)
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
            viewModel.StatementOfAccounts = await _dbContext.StatementOfAccounts
                .Where(soa => soa.IsPosted)
                .Select(soa => new SelectListItem
                {
                    Value = soa.Id.ToString(),
                    Text = soa.SOANo
                })
                .ToListAsync(cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(DebitMemo model, DateTime[] period, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
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

                if (model.Source == "Sales Invoice")
                {
                    model.SOAId = null;

                    var existingSalesInvoice = await _dbContext.SalesInvoices
                                               .FirstOrDefaultAsync(si => si.Id == model.SalesInvoiceId, cancellationToken);

                    model.DebitAmount = model.Quantity * model.AdjustedPrice;

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

                    var existingSoa = await _dbContext.StatementOfAccounts
                        .Include(soa => soa.Customer)
                        .FirstOrDefaultAsync(soa => soa.Id == model.SOAId, cancellationToken);


                    #region --Retrieval of Services

                    model.ServicesId = existingSoa.ServicesId;

                    var services = await _dbContext
                    .Services
                    .FirstOrDefaultAsync(s => s.Id == model.ServicesId, cancellationToken);

                    #endregion --Retrieval of Services

                    #region --DM Entries function

                    for (int i = 0; i < period.Length; i++)
                    {
                        if (model.CreatedDate >= period[i])
                        {
                            model.CurrentAndPreviousAmount += model.Amount[i];
                        }
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

                model.CreatedBy = _userManager.GetUserName(this.User);
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
                findIdOfDM.IsPrinted = true;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            return RedirectToAction("Print", new { id = id });
        }

        public async Task<IActionResult> Post(int id, CancellationToken cancellationToken, ViewModelSOA viewModelSOA)
        {
            var model = await _debitMemoRepo.FindDM(id, cancellationToken);

            if (model != null)
            {
                if (!model.IsPosted)
                {
                    model.IsPosted = true;
                    model.PostedBy = _userManager.GetUserName(this.User);
                    model.PostedDate = DateTime.Now;

                    #region --Retrieval of Services

                    var services = await _debitMemoRepo.GetServicesAsync(model.ServicesId, cancellationToken);

                    #endregion --Retrieval of Services

                    #region --Retrieval of SI and SOA

                    var existingSI = await _dbContext.SalesInvoices
                                                .FirstOrDefaultAsync(si => si.Id == model.SalesInvoiceId, cancellationToken);
                    var existingSOA = await _dbContext.StatementOfAccounts
                                                .Include(soa => soa.Customer)
                                                .FirstOrDefaultAsync(si => si.Id == model.SOAId, cancellationToken);

                    #endregion --Retrieval of SI and SOA

                    if (model.SalesInvoiceId != null)
                    {
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
                                AccountTitle = "1010201 AR-Trade Receivable",
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
                                    AccountTitle = "1010202 Deferred Creditable Withholding Tax",
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
                                    AccountTitle = "1010203 Deferred Creditable Withholding Vat",
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
                                    AccountTitle = "4010101 Sales - Biodiesel",
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
                                    AccountTitle = "4010102 Sales - Econogas",
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
                                    AccountTitle = "4010103 Sales - Envirogas",
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
                                    AccountTitle = "2010301 Vat Output",
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
                        for (int i = 0; i < model.Period.Length; i++)
                        {
                            #region --SOA Computation--

                            if (existingSOA.Customer.CustomerType == "Vatable")
                            {
                                viewModelSOA.Total = model.Amount[i];
                                viewModelSOA.NetAmount = (model.Amount[i] - existingSOA.Discount) / 1.12m;
                                viewModelSOA.VatAmount = (model.Amount[i] - existingSOA.Discount) - viewModelSOA.NetAmount;
                                viewModelSOA.WithholdingTaxAmount = viewModelSOA.NetAmount * (services.Percent / 100m);
                                if (existingSOA.Customer.WithHoldingVat)
                                {
                                    viewModelSOA.WithholdingVatAmount = viewModelSOA.NetAmount * 0.05m;
                                }
                            }
                            else
                            {
                                viewModelSOA.NetAmount = model.Amount[i] - existingSOA.Discount;
                                viewModelSOA.WithholdingTaxAmount = viewModelSOA.NetAmount * (services.Percent / 100m);
                                if (existingSOA.Customer.WithHoldingVat)
                                {
                                    viewModelSOA.WithholdingVatAmount = viewModelSOA.NetAmount * 0.05m;
                                }
                            }

                            if (existingSOA.Customer.CustomerType == "Vatable")
                            {
                                var total = Math.Round(model.Amount[i] / 1.12m, 2);

                                var roundedNetAmount = Math.Round(viewModelSOA.NetAmount, 2);

                                if (roundedNetAmount > total)
                                {
                                    var shortAmount = viewModelSOA.NetAmount - total;

                                    viewModelSOA.Amount[i] += shortAmount;
                                }
                            }

                            #endregion --SOA Computation--

                            if (model.CreatedDate >= model.Period[i])
                            {
                                #region --Sales Book Recording(SOA)--

                                var sales = new SalesBook();

                                if (model.SOA.Customer.CustomerType == "Vatable")
                                {
                                    sales.TransactionDate = model.Date.ToShortDateString();
                                    sales.SerialNo = model.DMNo;
                                    sales.SoldTo = model.SOA.Customer.Name;
                                    sales.TinNo = model.SOA.Customer.TinNo;
                                    sales.Address = model.SOA.Customer.Address;
                                    sales.Description = model.SOA.Service.Name;
                                    sales.Amount = model.DebitAmount;
                                    sales.VatAmount = model.VatAmount;
                                    sales.VatableSales = model.VatableSales;
                                    //sales.Discount = model.Discount;
                                    sales.NetSales = model.VatableSales;
                                    sales.CreatedBy = model.CreatedBy;
                                    sales.CreatedDate = model.CreatedDate;
                                    sales.DueDate = existingSOA?.DueDate;
                                }
                                else if (model.SOA.Customer.CustomerType == "Exempt")
                                {
                                    sales.TransactionDate = model.Date.ToShortDateString();
                                    sales.SerialNo = model.DMNo;
                                    sales.SoldTo = model.SOA.Customer.Name;
                                    sales.TinNo = model.SOA.Customer.TinNo;
                                    sales.Address = model.SOA.Customer.Address;
                                    sales.Description = model.SOA.Service.Name;
                                    sales.Amount = model.DebitAmount;
                                    sales.VatExemptSales = model.DebitAmount;
                                    //sales.Discount = model.Discount;
                                    sales.NetSales = model.VatableSales;
                                    sales.CreatedBy = model.CreatedBy;
                                    sales.CreatedDate = model.CreatedDate;
                                    sales.DueDate = existingSOA?.DueDate;
                                }
                                else
                                {
                                    sales.TransactionDate = model.Date.ToShortDateString();
                                    sales.SerialNo = model.DMNo;
                                    sales.SoldTo = model.SOA.Customer.Name;
                                    sales.TinNo = model.SOA.Customer.TinNo;
                                    sales.Address = model.SOA.Customer.Address;
                                    sales.Description = model.SOA.Service.Name;
                                    sales.Amount = model.DebitAmount;
                                    sales.ZeroRated = model.DebitAmount;
                                    //sales.Discount = model.Discount;
                                    sales.NetSales = model.VatableSales;
                                    sales.CreatedBy = model.CreatedBy;
                                    sales.CreatedDate = model.CreatedDate;
                                    sales.DueDate = existingSOA?.DueDate;
                                }
                                await _dbContext.AddAsync(sales, cancellationToken);

                                #endregion --Sales Book Recording(SOA)--

                                #region --General Ledger Book Recording(SOA)--

                                var ledgers = new List<GeneralLedgerBook>();

                                ledgers.Add(
                                        new GeneralLedgerBook
                                        {
                                            Date = model.Date.ToShortDateString(),
                                            Reference = model.DMNo,
                                            Description = model.SOA.Service.Name,
                                            AccountTitle = "1010204 AR-Non Trade Receivable",
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
                                            Description = model.SOA.Service.Name,
                                            AccountTitle = "1010202 Deferred Creditable Withholding Tax",
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
                                            Description = model.SOA.Service.Name,
                                            AccountTitle = "1010203 Deferred Creditable Withholding Vat",
                                            Debit = model.WithHoldingVatAmount,
                                            Credit = 0,
                                            CreatedBy = model.CreatedBy,
                                            CreatedDate = model.CreatedDate
                                        }
                                    );
                                }

                                if (model.CurrentAndPreviousAmount > 0)
                                {
                                    ledgers.Add(new GeneralLedgerBook
                                    {
                                        Date = model.Date.ToShortDateString(),
                                        Reference = model.DMNo,
                                        Description = model.SOA.Service.Name,
                                        AccountTitle = model.SOA.Service.CurrentAndPrevious,
                                        Debit = 0,
                                        Credit = model.CurrentAndPreviousAmount / 1.12m,
                                        CreatedBy = model.CreatedBy,
                                        CreatedDate = model.CreatedDate
                                    });
                                }

                                if (model.UnearnedAmount > 0)
                                {
                                    ledgers.Add(
                                        new GeneralLedgerBook
                                        {
                                            Date = model.Date.ToShortDateString(),
                                            Reference = model.DMNo,
                                            Description = model.SOA.Service.Name,
                                            AccountTitle = model.SOA.Service.Unearned,
                                            Debit = 0,
                                            Credit = model.UnearnedAmount / 1.12m,
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
                                            Date = model.Date.ToShortDateString(),
                                            Reference = model.DMNo,
                                            Description = model.SOA.Service.Name,
                                            AccountTitle = "2010304 Deferred Vat Output",
                                            Debit = 0,
                                            Credit = model.VatAmount,
                                            CreatedBy = model.CreatedBy,
                                            CreatedDate = model.CreatedDate
                                        }
                                    );
                                }

                                await _dbContext.GeneralLedgerBooks.AddRangeAsync(ledgers, cancellationToken);

                                #endregion --General Ledger Book Recording(SOA)--
                            }
                            else
                            {
                                #region --Sales Book Recording(SOA)--

                                var sales = new SalesBook();

                                if (model.SOA.Customer.CustomerType == "Vatable")
                                {
                                    sales.TransactionDate = model.Period[i].AddMonths(1).AddDays(-1).ToShortDateString();
                                    sales.SerialNo = model.DMNo;
                                    sales.SoldTo = model.SOA.Customer.Name;
                                    sales.TinNo = model.SOA.Customer.TinNo;
                                    sales.Address = model.SOA.Customer.Address;
                                    sales.Description = model.SOA.Service.Name;
                                    sales.Amount = viewModelSOA.Total;
                                    sales.VatAmount = viewModelSOA.VatAmount;
                                    sales.VatableSales = viewModelSOA.Total / 1.12m;
                                    //sales.Discount = model.Discount;
                                    sales.NetSales = viewModelSOA.NetAmount;
                                    sales.CreatedBy = model.CreatedBy;
                                    sales.CreatedDate = model.CreatedDate;
                                    sales.DueDate = existingSOA?.DueDate;
                                }
                                else if (model.SOA.Customer.CustomerType == "Exempt")
                                {
                                    sales.TransactionDate = model.Period[i].AddMonths(1).AddDays(-1).ToShortDateString();
                                    sales.SerialNo = model.DMNo;
                                    sales.SoldTo = model.SOA.Customer.Name;
                                    sales.TinNo = model.SOA.Customer.TinNo;
                                    sales.Address = model.SOA.Customer.Address;
                                    sales.Description = model.SOA.Service.Name;
                                    sales.Amount = viewModelSOA.Total;
                                    sales.VatExemptSales = viewModelSOA.Total;
                                    //sales.Discount = model.Discount;
                                    sales.NetSales = viewModelSOA.NetAmount;
                                    sales.CreatedBy = model.CreatedBy;
                                    sales.CreatedDate = model.CreatedDate;
                                    sales.DueDate = existingSOA?.DueDate;
                                }
                                else
                                {
                                    sales.TransactionDate = model.Period[i].AddMonths(1).AddDays(-1).ToShortDateString();
                                    sales.SerialNo = model.DMNo;
                                    sales.SoldTo = model.SOA.Customer.Name;
                                    sales.TinNo = model.SOA.Customer.TinNo;
                                    sales.Address = model.SOA.Customer.Address;
                                    sales.Description = model.SOA.Service.Name;
                                    sales.Amount = viewModelSOA.Total;
                                    sales.ZeroRated = viewModelSOA.Total;
                                    //sales.Discount = model.Discount;
                                    sales.NetSales = viewModelSOA.NetAmount;
                                    sales.CreatedBy = model.CreatedBy;
                                    sales.CreatedDate = model.CreatedDate;
                                    sales.DueDate = existingSOA?.DueDate;
                                }
                                await _dbContext.AddAsync(sales, cancellationToken);

                                #endregion --Sales Book Recording(SOA)--

                                #region --General Ledger Book Recording(SOA)--

                                var ledgers = new List<GeneralLedgerBook>();

                                ledgers.Add(
                                        new GeneralLedgerBook
                                        {
                                            Date = model.Period[i].AddMonths(1).AddDays(-1).ToShortDateString(),
                                            Reference = model.DMNo,
                                            Description = model.SOA.Service.Name,
                                            AccountTitle = "1010204 AR-Non Trade Receivable",
                                            Debit = viewModelSOA.Total - (viewModelSOA.WithholdingTaxAmount + viewModelSOA.WithholdingVatAmount),
                                            Credit = 0,
                                            CreatedBy = model.CreatedBy,
                                            CreatedDate = model.CreatedDate
                                        }
                                    );
                                if (viewModelSOA.WithholdingTaxAmount > 0)
                                {
                                    ledgers.Add(
                                        new GeneralLedgerBook
                                        {
                                            Date = model.Period[i].AddMonths(1).AddDays(-1).ToShortDateString(),
                                            Reference = model.DMNo,
                                            Description = model.SOA.Service.Name,
                                            AccountTitle = "1010202 Deferred Creditable Withholding Tax",
                                            Debit = viewModelSOA.WithholdingTaxAmount,
                                            Credit = 0,
                                            CreatedBy = model.CreatedBy,
                                            CreatedDate = model.CreatedDate
                                        }
                                    );
                                }
                                if (viewModelSOA.WithholdingVatAmount > 0)
                                {
                                    ledgers.Add(
                                        new GeneralLedgerBook
                                        {
                                            Date = model.Period[i].AddMonths(1).AddDays(-1).ToShortDateString(),
                                            Reference = model.DMNo,
                                            Description = model.SOA.Service.Name,
                                            AccountTitle = "1010203 Deferred Creditable Withholding Vat",
                                            Debit = viewModelSOA.WithholdingVatAmount,
                                            Credit = 0,
                                            CreatedBy = model.CreatedBy,
                                            CreatedDate = model.CreatedDate
                                        }
                                    );
                                }

                                if (viewModelSOA.Total > 0)
                                {
                                    ledgers.Add(new GeneralLedgerBook
                                    {
                                        Date = model.Period[i].AddMonths(1).AddDays(-1).ToShortDateString(),
                                        Reference = model.DMNo,
                                        Description = model.SOA.Service.Name,
                                        AccountTitle = model.SOA.Service.CurrentAndPrevious,
                                        Debit = 0,
                                        Credit = viewModelSOA.Total / 1.12m,
                                        CreatedBy = model.CreatedBy,
                                        CreatedDate = model.CreatedDate
                                    });
                                }

                                if (viewModelSOA.VatAmount > 0)
                                {
                                    ledgers.Add(
                                        new GeneralLedgerBook
                                        {
                                            Date = model.Period[i].AddMonths(1).AddDays(-1).ToShortDateString(),
                                            Reference = model.DMNo,
                                            Description = model.SOA.Service.Name,
                                            AccountTitle = "2010304 Deferred Vat Output",
                                            Debit = 0,
                                            Credit = viewModelSOA.VatAmount,
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
            var model = await _dbContext.StatementOfAccounts.FirstOrDefaultAsync(soa => soa.Id == soaId, cancellationToken);
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