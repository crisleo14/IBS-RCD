using Accounting_System.Data;
using Accounting_System.Models;
using Accounting_System.Repository;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Accounting_System.Controllers
{
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
                .Include(cm => cm.StatementOfAccount)
                .ThenInclude(soa => soa.Customer)
                .Include(cm => cm.StatementOfAccount)
                .ThenInclude(soa => soa.Service)
                .OrderBy(cm => cm.Id) 
                .ToListAsync(cancellationToken);

            return View(cm);
        }

        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var viewModel = new CreditMemo();
            viewModel.Invoices = await _dbContext.SalesInvoices
                .Where(si => si.IsPosted)
                .Select(si => new SelectListItem
                {
                    Value = si.Id.ToString(),
                    Text = si.SINo
                })
                .ToListAsync(cancellationToken);
            viewModel.Soa = await _dbContext.StatementOfAccounts
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreditMemo model, DateTime[] period, CancellationToken cancellationToken, decimal[]? amount)
        {
            model.Invoices = await _dbContext.SalesInvoices
                .Where(si => si.IsPosted)
                .Select(si => new SelectListItem
                {
                    Value = si.Id.ToString(),
                    Text = si.SINo
                })
                .ToListAsync(cancellationToken);
            model.Soa = await _dbContext.StatementOfAccounts
                .Where(soa => soa.IsPosted)
                .Select(soa => new SelectListItem
                {
                    Value = soa.Id.ToString(),
                    Text = soa.SOANo
                })
                .ToListAsync(cancellationToken);

            if (ModelState.IsValid)
            {

                if (model.SIId != null)
                {
                    var existingSIDMs = _dbContext.DebitMemos
                                  .Where(si => si.SalesInvoiceId == model.SIId && !si.IsPosted)
                                  .OrderBy(s => s.Id)
                                  .ToList();
                    if (existingSIDMs.Count > 0)
                    {
                        ModelState.AddModelError("", $"You have a unposted DM for SI. Post that one first before starting a new one. DM#{existingSIDMs.First().DMNo}");
                        return View(model);
                    }

                    var existingSICMs = _dbContext.CreditMemos
                                      .Where(si => si.SIId == model.SIId && !si.IsPosted)
                                      .OrderBy(s => s.Id)
                                      .ToList();
                    if (existingSICMs.Count > 0)
                    {
                        ModelState.AddModelError("", $"You have a unposted CM for SI. Post that one first before starting a new one. CM#{existingSICMs.First().CMNo}");
                        return View(model);
                    }
                }
                else
                {
                    var existingSOADMs = _dbContext.DebitMemos
                                  .Where(si => si.SOAId == model.SOAId && !si.IsPosted)
                                  .OrderBy(s => s.Id)
                                  .ToList();
                    if (existingSOADMs.Count > 0)
                    {
                        ModelState.AddModelError("", $"You have a unposted DM for SOA. Post that one first before starting a new one. DM#{existingSOADMs.First().DMNo}");
                        return View(model);
                    }

                    var existingSOACMs = _dbContext.CreditMemos
                                      .Where(si => si.SOAId == model.SOAId && !si.IsPosted)
                                      .OrderBy(s => s.Id)
                                      .ToList();
                    if (existingSOACMs.Count > 0)
                    {
                        ModelState.AddModelError("", $"You have a unposted CM for SOA. Post that one first before starting a new one. CM#{existingSOACMs.First().CMNo}");
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
                    model.SOAId = null;
                    model.SINo = await _creditMemoRepo.GetSINoAsync(model.SIId, cancellationToken);

                    var existingSalesInvoice = await _dbContext.SalesInvoices
                                               .FirstOrDefaultAsync(si => si.Id == model.SIId, cancellationToken);

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
                else if (model.Source == "Statement Of Account")
                {
                    model.SIId = null;
                    model.SOANo = await _creditMemoRepo.GetSOANoAsync(model.SOAId, cancellationToken);

                    var existingSoa = await _dbContext.StatementOfAccounts
                        .Include(soa => soa.Customer)
                        .FirstOrDefaultAsync(soa => soa.Id == model.SOAId, cancellationToken);

                    #region --Retrieval of Services

                    model.ServicesId = existingSoa.ServicesId;

                    var services = await _dbContext
                    .Services
                    .FirstOrDefaultAsync(s => s.Id == model.ServicesId, cancellationToken);

                    #endregion --Retrieval of Services

                    #region --CM Entries function

                    for (int i = 0; i < amount.Length; i++)
                    {
                        model.Amount[i] = -amount[i];
                    }

                    for (int i = 0; i < period.Length; i++)
                    {
                        if (model.CreatedDate >= period[i])
                        {
                            model.CurrentAndPreviousAmount += model.Amount[i];
                        }
                    }

                    #endregion ----CM Entries function

                    model.CreditAmount = model.CurrentAndPreviousAmount;
                    

                    if (existingSoa.Customer.CustomerType == "Vatable")
                    {
                        model.VatableSales = model.CreditAmount / 1.12m;
                        model.VatAmount = model.CreditAmount - model.VatableSales;
                        model.TotalSales = model.VatableSales + model.VatAmount;
                        model.WithHoldingTaxAmount = model.VatableSales * (services.Percent / 100m);

                        if (existingSoa.WithholdingVatAmount != 0)
                        {
                            model.WithHoldingVatAmount = model.VatableSales * 0.05m;
                        }
                    }
                    else
                    {
                        model.TotalSales = model.CreditAmount;
                    }
                }

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

            creditMemo.Invoices = await _dbContext.SalesInvoices
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.SINo
                })
                .ToListAsync(cancellationToken);

            creditMemo.Soa = await _dbContext.StatementOfAccounts
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.SOANo
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
                    existingModel.SOAId = null;
                    existingModel.SIId = model.SIId;

                    var existingSalesInvoice = await _dbContext.SalesInvoices
                                               .FirstOrDefaultAsync(si => si.Id == existingModel.SIId, cancellationToken);

                    existingModel.CreditAmount = (decimal)(model.Quantity * model.AdjustedPrice);

                    if (existingSalesInvoice.CustomerType == "Vatable")
                    {
                        existingModel.VatableSales = existingModel.CreditAmount / (decimal)1.12;
                        existingModel.VatAmount = existingModel.CreditAmount - existingModel.VatableSales;
                        existingModel.TotalSales = existingModel.VatableSales + existingModel.VatAmount;
                    }

                    existingModel.TotalSales = existingModel.CreditAmount;
                }
                else if (model.Source == "Statement Of Account")
                {
                    existingModel.SIId = null;
                    existingModel.SOAId = model.SOAId;

                    var existingSoa = await _dbContext.StatementOfAccounts
                        .Include(soa => soa.Customer)
                        .FirstOrDefaultAsync(soa => soa.Id == existingModel.SOAId, cancellationToken);

                    existingModel.CreditAmount = (decimal)(model.AdjustedPrice - existingSoa.Total);

                    if (existingSoa.Customer.CustomerType == "Vatable")
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
                .Include(cm => cm.StatementOfAccount)
                .ThenInclude(soa => soa.Customer)
                .Include(cm => cm.StatementOfAccount)
                .ThenInclude(soa => soa.Service)
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

                    if (model.SIId != null)
                    {
                        #region --Retrieval of SI and SOA--

                        var existingSI = await _dbContext.SalesInvoices
                                                    .FirstOrDefaultAsync(si => si.Id == model.SIId, cancellationToken);

                        #endregion --Retrieval of SI and SOA--

                        #region --Sales Book Recording(SI)--

                        var sales = new SalesBook();

                        if (model.SalesInvoice.CustomerType == "Vatable")
                        {
                            sales.TransactionDate = model.Date.ToShortDateString();
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
                        }
                        else if (model.SalesInvoice.CustomerType == "Exempt")
                        {
                            sales.TransactionDate = model.Date.ToShortDateString();
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
                        }
                        else
                        {
                            sales.TransactionDate = model.Date.ToShortDateString();
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
                        }
                        await _dbContext.AddAsync(sales, cancellationToken);

                        #endregion --Sales Book Recording(SI)--

                        #region --General Ledger Book Recording(SI)--

                        var ledgers = new List<GeneralLedgerBook>();

                        ledgers.Add(
                            new GeneralLedgerBook
                            {
                                Date = model.Date.ToShortDateString(),
                                Reference = model.CMNo,
                                Description = model.SalesInvoice.ProductName,
                                AccountTitle = "1010201 AR-Trade Receivable",
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
                                    Date = model.Date.ToShortDateString(),
                                    Reference = model.CMNo,
                                    Description = model.SalesInvoice.ProductName,
                                    AccountTitle = "1010202 Deferred Creditable Withholding Tax",
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
                                    Date = model.Date.ToShortDateString(),
                                    Reference = model.CMNo,
                                    Description = model.SalesInvoice.ProductName,
                                    AccountTitle = "1010203 Deferred Creditable Withholding Vat",
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
                                    Date = model.Date.ToShortDateString(),
                                    Reference = model.CMNo,
                                    Description = model.SalesInvoice.ProductName,
                                    AccountTitle = "4010101 Sales - Biodiesel",
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
                                    Date = model.Date.ToShortDateString(),
                                    Reference = model.CMNo,
                                    Description = model.SalesInvoice.ProductName,
                                    AccountTitle = "4010102 Sales - Econogas",
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
                                    Date = model.Date.ToShortDateString(),
                                    Reference = model.CMNo,
                                    Description = model.SalesInvoice.ProductName,
                                    AccountTitle = "4010103 Sales - Envirogas",
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
                                    Date = model.Date.ToShortDateString(),
                                    Reference = model.CMNo,
                                    Description = model.SalesInvoice.ProductName,
                                    AccountTitle = "2010301 Vat Output",
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

                    if (model.SOAId != null)
                    {
                        var existingSOA = await _dbContext.StatementOfAccounts
                                                .Include(soa => soa.Customer)
                                                .FirstOrDefaultAsync(si => si.Id == model.SOAId, cancellationToken);

                        #region --Retrieval of Services

                        var services = await _creditMemoRepo.GetServicesAsync(model.ServicesId, cancellationToken);

                        #endregion --Retrieval of Services

                        for (int i = 0; i < model.Amount.Length; i++)
                        {
                                model.Amount[i] = model.Amount[i];
                        }
                        for (int i = 0; i < model.Period.Length; i++)
                        {
                            #region --SOA Computation--

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

                                    viewModelDMCM.Amount[i] += shortAmount;
                                }
                            }

                            #endregion --SOA Computation--

                            if (model.CreatedDate >= model.Period[i])
                            {
                                if (model.Amount[i] < 0)
                                {
                                    #region --Sales Book Recording

                                    var sales = new SalesBook();

                                    if (model.StatementOfAccount.Customer.CustomerType == "Vatable")
                                    {
                                        sales.TransactionDate = model.Date.ToShortDateString();
                                        sales.SerialNo = model.CMNo;
                                        sales.SoldTo = model.StatementOfAccount.Customer.Name;
                                        sales.TinNo = model.StatementOfAccount.Customer.TinNo;
                                        sales.Address = model.StatementOfAccount.Customer.Address;
                                        sales.Description = model.StatementOfAccount.Service.Name;
                                        sales.Amount = model.CreditAmount;
                                        sales.VatAmount = model.VatAmount;
                                        sales.VatableSales = model.VatableSales;
                                        //sales.Discount = model.Discount;
                                        sales.NetSales = model.VatableSales;
                                        sales.CreatedBy = model.CreatedBy;
                                        sales.CreatedDate = model.CreatedDate;
                                        sales.DueDate = existingSOA?.DueDate;
                                        sales.DocumentId = model.SOAId;
                                    }
                                    else if (model.StatementOfAccount.Customer.CustomerType == "Exempt")
                                    {
                                        sales.TransactionDate = model.Date.ToShortDateString();
                                        sales.SerialNo = model.CMNo;
                                        sales.SoldTo = model.StatementOfAccount.Customer.Name;
                                        sales.TinNo = model.StatementOfAccount.Customer.TinNo;
                                        sales.Address = model.StatementOfAccount.Customer.Address;
                                        sales.Description = model.StatementOfAccount.Service.Name;
                                        sales.Amount = model.CreditAmount;
                                        sales.VatExemptSales = model.CreditAmount;
                                        //sales.Discount = model.Discount;
                                        sales.NetSales = model.VatableSales;
                                        sales.CreatedBy = model.CreatedBy;
                                        sales.CreatedDate = model.CreatedDate;
                                        sales.DueDate = existingSOA?.DueDate;
                                        sales.DocumentId = model.SOAId;
                                    }
                                    else
                                    {
                                        sales.TransactionDate = model.Date.ToShortDateString();
                                        sales.SerialNo = model.CMNo;
                                        sales.SoldTo = model.StatementOfAccount.Customer.Name;
                                        sales.TinNo = model.StatementOfAccount.Customer.TinNo;
                                        sales.Address = model.StatementOfAccount.Customer.Address;
                                        sales.Description = model.StatementOfAccount.Service.Name;
                                        sales.Amount = model.CreditAmount;
                                        sales.ZeroRated = model.CreditAmount;
                                        //sales.Discount = model.Discount;
                                        sales.NetSales = model.VatableSales;
                                        sales.CreatedBy = model.CreatedBy;
                                        sales.CreatedDate = model.CreatedDate;
                                        sales.DueDate = existingSOA?.DueDate;
                                        sales.DocumentId = model.SOAId;
                                    }
                                    await _dbContext.AddAsync(sales, cancellationToken);

                                    #endregion --Sales Book Recording

                                    #region --General Ledger Book Recording

                                    var ledgers = new List<GeneralLedgerBook>();

                                    ledgers.Add(
                                            new GeneralLedgerBook
                                            {
                                                Date = model.Date.ToShortDateString(),
                                                Reference = model.CMNo,
                                                Description = model.StatementOfAccount.Service.Name,
                                                AccountTitle = "1010204 AR-Non Trade Receivable",
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
                                                Date = model.Date.ToShortDateString(),
                                                Reference = model.CMNo,
                                                Description = model.StatementOfAccount.Service.Name,
                                                AccountTitle = "1010202 Deferred Creditable Withholding Tax",
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
                                                Date = model.Date.ToShortDateString(),
                                                Reference = model.CMNo,
                                                Description = model.StatementOfAccount.Service.Name,
                                                AccountTitle = "1010203 Deferred Creditable Withholding Vat",
                                                Debit = 0,
                                                Credit = Math.Abs(model.WithHoldingVatAmount),
                                                CreatedBy = model.CreatedBy,
                                                CreatedDate = model.CreatedDate
                                            }
                                        );
                                    }

                                    if (model.CurrentAndPreviousAmount < 0)
                                    {
                                        ledgers.Add(new GeneralLedgerBook
                                        {
                                            Date = model.Date.ToShortDateString(),
                                            Reference = model.CMNo,
                                            Description = model.StatementOfAccount.Service.Name,
                                            AccountTitle = model.StatementOfAccount.Service.CurrentAndPrevious,
                                            Debit = Math.Abs(model.CurrentAndPreviousAmount / 1.12m),
                                            Credit = 0,
                                            CreatedBy = model.CreatedBy,
                                            CreatedDate = model.CreatedDate
                                        });
                                    }

                                    if (model.UnearnedAmount < 0)
                                    {
                                        ledgers.Add(
                                            new GeneralLedgerBook
                                            {
                                                Date = model.Date.ToShortDateString(),
                                                Reference = model.CMNo,
                                                Description = model.StatementOfAccount.Service.Name,
                                                AccountTitle = model.StatementOfAccount.Service.Unearned,
                                                Debit = Math.Abs(model.UnearnedAmount / 1.12m),
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
                                                Date = model.Date.ToShortDateString(),
                                                Reference = model.CMNo,
                                                Description = model.StatementOfAccount.Service.Name,
                                                AccountTitle = "2010304 Deferred Vat Output",
                                                Debit = Math.Abs(model.VatAmount),
                                                Credit = 0,
                                                CreatedBy = model.CreatedBy,
                                                CreatedDate = model.CreatedDate
                                            }
                                        );
                                    }

                                    await _dbContext.GeneralLedgerBooks.AddRangeAsync(ledgers, cancellationToken);

                                    #endregion --General Ledger Book Recording
                                }
                            }
                            else if (model.CreatedDate < model.Period[i])
                            {
                                if (model.Amount[i] < 0)
                                {
                                    #region --Sales Book Recording(SOA)--

                                    var sales = new SalesBook();

                                    if (model.StatementOfAccount.Customer.CustomerType == "Vatable")
                                    {
                                        sales.TransactionDate = model.Period[i].AddMonths(1).AddDays(-1).ToShortDateString();
                                        sales.SerialNo = model.CMNo;
                                        sales.SoldTo = model.StatementOfAccount.Customer.Name;
                                        sales.TinNo = model.StatementOfAccount.Customer.TinNo;
                                        sales.Address = model.StatementOfAccount.Customer.Address;
                                        sales.Description = model.StatementOfAccount.Service.Name;
                                        sales.Amount = viewModelDMCM.Total;
                                        sales.VatAmount = viewModelDMCM.VatAmount;
                                        sales.VatableSales = viewModelDMCM.Total / 1.12m;
                                        //sales.Discount = model.Discount;
                                        sales.NetSales = viewModelDMCM.NetAmount;
                                        sales.CreatedBy = model.CreatedBy;
                                        sales.CreatedDate = model.CreatedDate;
                                        sales.DueDate = existingSOA?.DueDate;
                                        sales.DocumentId = model.SOAId;
                                    }
                                    else if (model.StatementOfAccount.Customer.CustomerType == "Exempt")
                                    {
                                        sales.TransactionDate = model.Period[i].AddMonths(1).AddDays(-1).ToShortDateString();
                                        sales.SerialNo = model.CMNo;
                                        sales.SoldTo = model.StatementOfAccount.Customer.Name;
                                        sales.TinNo = model.StatementOfAccount.Customer.TinNo;
                                        sales.Address = model.StatementOfAccount.Customer.Address;
                                        sales.Description = model.StatementOfAccount.Service.Name;
                                        sales.Amount = viewModelDMCM.Total;
                                        sales.VatExemptSales = viewModelDMCM.Total;
                                        //sales.Discount = model.Discount;
                                        sales.NetSales = viewModelDMCM.NetAmount;
                                        sales.CreatedBy = model.CreatedBy;
                                        sales.CreatedDate = model.CreatedDate;
                                        sales.DueDate = existingSOA?.DueDate;
                                        sales.DocumentId = model.SOAId;
                                    }
                                    else
                                    {
                                        sales.TransactionDate = model.Period[i].AddMonths(1).AddDays(-1).ToShortDateString();
                                        sales.SerialNo = model.CMNo;
                                        sales.SoldTo = model.StatementOfAccount.Customer.Name;
                                        sales.TinNo = model.StatementOfAccount.Customer.TinNo;
                                        sales.Address = model.StatementOfAccount.Customer.Address;
                                        sales.Description = model.StatementOfAccount.Service.Name;
                                        sales.Amount = viewModelDMCM.Total;
                                        sales.ZeroRated = viewModelDMCM.Total;
                                        //sales.Discount = model.Discount;
                                        sales.NetSales = viewModelDMCM.NetAmount;
                                        sales.CreatedBy = model.CreatedBy;
                                        sales.CreatedDate = model.CreatedDate;
                                        sales.DueDate = existingSOA?.DueDate;
                                        sales.DocumentId = model.SOAId;
                                    }
                                    await _dbContext.AddAsync(sales, cancellationToken);

                                    #endregion --Sales Book Recording(SOA)--

                                    #region --General Ledger Book Recording(SOA)--

                                    var ledgers = new List<GeneralLedgerBook>();

                                    ledgers.Add(
                                            new GeneralLedgerBook
                                            {
                                                Date = model.Period[i].AddMonths(1).AddDays(-1).ToShortDateString(),
                                                Reference = model.CMNo,
                                                Description = model.StatementOfAccount.Service.Name,
                                                AccountTitle = "1010204 AR-Non Trade Receivable",
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
                                                Date = model.Period[i].AddMonths(1).AddDays(-1).ToShortDateString(),
                                                Reference = model.CMNo,
                                                Description = model.StatementOfAccount.Service.Name,
                                                AccountTitle = "1010202 Deferred Creditable Withholding Tax",
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
                                                Date = model.Period[i].AddMonths(1).AddDays(-1).ToShortDateString(),
                                                Reference = model.CMNo,
                                                Description = model.StatementOfAccount.Service.Name,
                                                AccountTitle = "1010203 Deferred Creditable Withholding Vat",
                                                Debit = viewModelDMCM.WithholdingVatAmount,
                                                Credit = 0,
                                                CreatedBy = model.CreatedBy,
                                                CreatedDate = model.CreatedDate
                                            }
                                        );
                                    }

                                    if (viewModelDMCM.Total > 0)
                                    {
                                        ledgers.Add(new GeneralLedgerBook
                                        {
                                            Date = model.Period[i].AddMonths(1).AddDays(-1).ToShortDateString(),
                                            Reference = model.CMNo,
                                            Description = model.StatementOfAccount.Service.Name,
                                            AccountTitle = model.StatementOfAccount.Service.CurrentAndPrevious,
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
                                                Date = model.Period[i].AddMonths(1).AddDays(-1).ToShortDateString(),
                                                Reference = model.CMNo,
                                                Description = model.StatementOfAccount.Service.Name,
                                                AccountTitle = "2010304 Deferred Vat Output",
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