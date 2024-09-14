using Accounting_System.Data;
using Accounting_System.Models;
using Accounting_System.Models.AccountsReceivable;
using Accounting_System.Models.Reports;
using Accounting_System.Models.ViewModels;
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
    public class DebitMemoController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly DebitMemoRepo _debitMemoRepo;

        private readonly GeneralRepo _generalRepo;

        public DebitMemoController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, DebitMemoRepo dmcmRepo, GeneralRepo generalRepo)
        {
            _dbContext = dbContext;
            this._userManager = userManager;
            _debitMemoRepo = dmcmRepo;
            _generalRepo = generalRepo;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var dm = await _dbContext.DebitMemos
                .Include(dm => dm.SalesInvoice)
                .ThenInclude(s => s.Customer)
                .Include(dm => dm.SalesInvoice)
                .ThenInclude(s => s.Product)
                .Include(dm => dm.ServiceInvoice)
                .ThenInclude(sv => sv.Customer)
                .Include(dm => dm.ServiceInvoice)
                .ThenInclude(sv => sv.Service)
                .ToListAsync(cancellationToken);
            return View(dm);
        }
        public async Task<IActionResult> ImportExportIndex(CancellationToken cancellationToken)
        {
            var dm = await _dbContext.DebitMemos
                .Include(dm => dm.SalesInvoice)
                .ThenInclude(s => s.Customer)
                .Include(dm => dm.SalesInvoice)
                .ThenInclude(s => s.Product)
                .Include(dm => dm.ServiceInvoice)
                .ThenInclude(sv => sv.Customer)
                .Include(dm => dm.ServiceInvoice)
                .ThenInclude(sv => sv.Service)
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
        public async Task<IActionResult> Create(DebitMemo model, CancellationToken cancellationToken)
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
                if (model.AdjustedPrice < existingSalesInvoice.UnitPrice)
                {
                    ModelState.AddModelError("AdjustedPrice", "Cannot input less than the existing SI unit price!");
                }
                if (model.Quantity < existingSalesInvoice.Quantity)
                {
                    ModelState.AddModelError("Quantity", "Cannot input more than the existing SI quantity!");
                }
            }
            else
            {
                if (model.Amount < existingSv.Amount)
                {
                    ModelState.AddModelError("Amount", "Cannot input less than the existing SV amount!");
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
                    var existingSVDMs = await _dbContext.DebitMemos
                                  .Where(si => si.ServiceInvoiceId == model.ServiceInvoiceId && !si.IsPosted && !si.IsCanceled && !si.IsVoided)
                                  .OrderBy(s => s.Id)
                                  .ToListAsync(cancellationToken);
                    if (existingSVDMs.Count > 0)
                    {
                        ModelState.AddModelError("", $"Can’t proceed to create you have unposted DM/CM. {existingSVDMs.First().DMNo}");
                        return View(model);
                    }

                    var existingSVCMs = await _dbContext.CreditMemos
                                      .Where(si => si.ServiceInvoiceId == model.ServiceInvoiceId && !si.IsPosted && !si.IsCanceled && !si.IsVoided)
                                      .OrderBy(s => s.Id)
                                      .ToListAsync(cancellationToken);
                    if (existingSVCMs.Count > 0)
                    {
                        ModelState.AddModelError("", $"Can’t proceed to create you have unposted DM/CM. {existingSVCMs.First().CMNo}");
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
                    model.ServiceInvoiceId = null;

                    model.DebitAmount = (decimal)(model.Quantity * model.AdjustedPrice);

                    if (existingSalesInvoice.Customer.CustomerType == "Vatable")
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
                else if (model.Source == "Service Invoice")
                {
                    model.SalesInvoiceId = null;

                    #region --Retrieval of Services

                    model.ServicesId = existingSv.ServicesId;

                    var services = await _dbContext
                    .Services
                    .FirstOrDefaultAsync(s => s.Id == model.ServicesId, cancellationToken);

                    #endregion --Retrieval of Services

                    model.DebitAmount = model.Amount ?? 0;

                    if (existingSv.Customer.CustomerType == "Vatable")
                    {
                        model.VatableSales = model.DebitAmount / 1.12m;
                        model.VatAmount = model.DebitAmount - model.VatableSales;
                        model.TotalSales = model.VatableSales + model.VatAmount;
                        model.WithHoldingTaxAmount = model.VatableSales * (services.Percent / 100m);

                        if (existingSv.WithholdingVatAmount != 0)
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
                await _dbContext.AddAsync(auditTrail, cancellationToken);

                #endregion --Audit Trail Recording

                await _dbContext.AddAsync(model, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                return RedirectToAction(nameof(Index));
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
                .ThenInclude(s => s.Customer)
                .Include(dm => dm.SalesInvoice)
                .ThenInclude(s => s.Product)
                .Include(dm => dm.ServiceInvoice)
                .ThenInclude(sv => sv.Customer)
                .Include(dm => dm.ServiceInvoice)
                .ThenInclude(sv => sv.Service)
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
            return RedirectToAction(nameof(Print), new { id });
        }

        public async Task<IActionResult> Post(int id, ViewModelDMCM viewModelDMCM, CancellationToken cancellationToken)
        {
            var model = await _debitMemoRepo.FindDM(id, cancellationToken);

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
                            #region --Retrieval of SI

                            var existingSI = await _dbContext
                                .SalesInvoices
                                .Include(c => c.Customer)
                                .Include(s => s.Product)
                                .FirstOrDefaultAsync(invoice => invoice.Id == model.SalesInvoiceId, cancellationToken);

                            #endregion --Retrieval of SI

                            #region --Sales Book Recording(SI)--

                            var sales = new SalesBook();

                            if (model.SalesInvoice.Customer.CustomerType == "Vatable")
                            {
                                sales.TransactionDate = model.TransactionDate;
                                sales.SerialNo = model.DMNo;
                                sales.SoldTo = model.SalesInvoice.Customer.Name;
                                sales.TinNo = model.SalesInvoice.Customer.TinNo;
                                sales.Address = model.SalesInvoice.Customer.Address;
                                sales.Description = model.SalesInvoice.Product.Name;
                                sales.Amount = model.DebitAmount;
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
                                sales.SerialNo = model.DMNo;
                                sales.SoldTo = model.SalesInvoice.Customer.Name;
                                sales.TinNo = model.SalesInvoice.Customer.TinNo;
                                sales.Address = model.SalesInvoice.Customer.Address;
                                sales.Description = model.SalesInvoice.Product.Name;
                                sales.Amount = model.DebitAmount;
                                sales.VatExemptSales = model.DebitAmount;
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
                                sales.SerialNo = model.DMNo;
                                sales.SoldTo = model.SalesInvoice.Customer.Name;
                                sales.TinNo = model.SalesInvoice.Customer.TinNo;
                                sales.Address = model.SalesInvoice.Customer.Address;
                                sales.Description = model.SalesInvoice.Product.Name;
                                sales.Amount = model.DebitAmount;
                                sales.ZeroRated = model.DebitAmount;
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
                                    Reference = model.DMNo,
                                    Description = model.SalesInvoice.Product.Name,
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
                                        Date = model.TransactionDate,
                                        Reference = model.DMNo,
                                        Description = model.SalesInvoice.Product.Name,
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
                                        Reference = model.DMNo,
                                        Description = model.SalesInvoice.Product.Name,
                                        AccountNo = "1010203",
                                        AccountTitle = "Deferred Creditable Withholding Vat",
                                        Debit = model.WithHoldingVatAmount,
                                        Credit = 0,
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
                                        Reference = model.DMNo,
                                        Description = model.SalesInvoice.Product.Name,
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
                            else if (model.SalesInvoice.Product.Name == "Econogas")
                            {
                                ledgers.Add(
                                    new GeneralLedgerBook
                                    {
                                        Date = model.TransactionDate,
                                        Reference = model.DMNo,
                                        Description = model.SalesInvoice.Product.Name,
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
                            else if (model.SalesInvoice.Product.Name == "Envirogas")
                            {
                                ledgers.Add(
                                    new GeneralLedgerBook
                                    {
                                        Date = model.TransactionDate,
                                        Reference = model.DMNo,
                                        Description = model.SalesInvoice.Product.Name,
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
                                        Date = model.TransactionDate,
                                        Reference = model.DMNo,
                                        Description = model.SalesInvoice.Product.Name,
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

                            #endregion --General Ledger Book Recording(SI)--
                        }

                        if (model.ServiceInvoiceId != null)
                        {
                            var existingSv = await _dbContext.ServiceInvoices
                                .Include(sv => sv.Customer)
                                .FirstOrDefaultAsync(sv => sv.Id == model.ServiceInvoiceId, cancellationToken);

                            #region --Retrieval of Services

                            var services = await _debitMemoRepo.GetServicesAsync(model?.ServicesId, cancellationToken);

                            #endregion --Retrieval of Services

                            #region --SV Computation--

                            viewModelDMCM.Period = DateOnly.FromDateTime(model.CreatedDate) >= model.Period ? DateOnly.FromDateTime(model.CreatedDate) : model.Period.AddMonths(1).AddDays(-1);

                            if (existingSv.Customer.CustomerType == "Vatable")
                            {
                                viewModelDMCM.Total = model.Amount ?? 0;
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
                                sales.SerialNo = model.DMNo;
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
                                sales.DocumentId = existingSv.Id;
                            }
                            else if (model.ServiceInvoice.Customer.CustomerType == "Exempt")
                            {
                                sales.TransactionDate = viewModelDMCM.Period;
                                sales.SerialNo = model.DMNo;
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
                                sales.DocumentId = existingSv.Id;
                            }
                            else
                            {
                                sales.TransactionDate = viewModelDMCM.Period;
                                sales.SerialNo = model.DMNo;
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
                                sales.DocumentId = existingSv.Id;
                            }
                            await _dbContext.AddAsync(sales, cancellationToken);

                            #endregion --Sales Book Recording(SV)--

                            #region --General Ledger Book Recording(SV)--

                            var ledgers = new List<GeneralLedgerBook>();

                            ledgers.Add(
                                    new GeneralLedgerBook
                                    {
                                        Date = viewModelDMCM.Period,
                                        Reference = model.DMNo,
                                        Description = model.ServiceInvoice.Service.Name,
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
                                        Date = viewModelDMCM.Period,
                                        Reference = model.DMNo,
                                        Description = model.ServiceInvoice.Service.Name,
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
                                        Date = viewModelDMCM.Period,
                                        Reference = model.DMNo,
                                        Description = model.ServiceInvoice.Service.Name,
                                        AccountNo = "1010203",
                                        AccountTitle = "Deferred Creditable Withholding Vat",
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
                                    Date = viewModelDMCM.Period,
                                    Reference = model.DMNo,
                                    Description = model.ServiceInvoice.Service.Name,
                                    AccountNo = model.ServiceInvoice.Service.CurrentAndPreviousNo,
                                    AccountTitle = model.ServiceInvoice.Service.CurrentAndPreviousTitle,
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
                                        Date = viewModelDMCM.Period,
                                        Reference = model.DMNo,
                                        Description = model.ServiceInvoice.Service.Name,
                                        AccountNo = "2010304",
                                        AccountTitle = "Deferred Vat Output",
                                        Debit = 0,
                                        Credit = viewModelDMCM.VatAmount,
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

                            #endregion --General Ledger Book Recording(SV)--
                        }

                        #region --Audit Trail Recording

                        AuditTrail auditTrail = new(model.PostedBy, $"Posted debit memo# {model.DMNo}", "Debit Memo");
                        await _dbContext.AddAsync(auditTrail, cancellationToken);

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        TempData["success"] = "Debit Memo has been Posted.";
                    }
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(Index));
                }
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
                    if (model.IsPosted)
                    {
                        model.IsPosted = false;
                    }

                    model.IsVoided = true;
                    model.VoidedBy = _userManager.GetUserName(this.User);
                    model.VoidedDate = DateTime.Now;

                    await _generalRepo.RemoveRecords<SalesBook>(crb => crb.SerialNo == model.DMNo, cancellationToken);
                    await _generalRepo.RemoveRecords<GeneralLedgerBook>(gl => gl.Reference == model.DMNo, cancellationToken);

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(model.VoidedBy, $"Voided debit memo# {model.DMNo}", "Debit Memo");
                    await _dbContext.AddAsync(auditTrail, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Debit Memo has been Voided.";
                }
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        public async Task<IActionResult> Cancel(int id, string cancellationRemarks, CancellationToken cancellationToken)
        {
            var model = await _dbContext.DebitMemos.FindAsync(id, cancellationToken);

            if (model != null)
            {
                if (!model.IsCanceled)
                {
                    model.IsCanceled = true;
                    model.CanceledBy = _userManager.GetUserName(this.User);
                    model.CanceledDate = DateTime.Now;
                    model.CancellationRemarks = cancellationRemarks;

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(model.CanceledBy, $"Cancelled debit memo# {model.DMNo}", "Debit Memo");
                    await _dbContext.AddAsync(auditTrail, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Debit Memo has been Cancelled.";
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

        [HttpGet]
        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
            if (id == null || _dbContext.DebitMemos == null)
            {
                return NotFound();
            }

            var debitMemo = await _dbContext.DebitMemos
                .Include(cm => cm.SalesInvoice)
                .Include(cm => cm.ServiceInvoice)
                .ThenInclude(sv => sv.Customer)
                .Include(cm => cm.ServiceInvoice)
                .ThenInclude(sv => sv.Service)
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

            if (debitMemo == null)
            {
                return NotFound();
            }

            debitMemo.SalesInvoices = await _dbContext.SalesInvoices
                .Where(si => si.IsPosted)
                .Select(si => new SelectListItem
                {
                    Value = si.Id.ToString(),
                    Text = si.SINo
                })
                .ToListAsync(cancellationToken);
            debitMemo.ServiceInvoices = await _dbContext.ServiceInvoices
                .Where(sv => sv.IsPosted)
                .Select(sv => new SelectListItem
                {
                    Value = sv.Id.ToString(),
                    Text = sv.SVNo
                })
                .ToListAsync(cancellationToken);


            return View(debitMemo);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(DebitMemo model, CancellationToken cancellationToken)
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
                if (model.AdjustedPrice < existingSalesInvoice.UnitPrice)
                {
                    ModelState.AddModelError("AdjustedPrice", "Cannot input less than the existing SI unit price!");
                }
                if (model.Quantity < existingSalesInvoice.Quantity)
                {
                    ModelState.AddModelError("Quantity", "Cannot input less than the existing SI quantity!");
                }
            }
            else
            {
                if (model.Amount < existingSv.Amount)
                {
                    ModelState.AddModelError("Amount", "Cannot input less than the existing SV amount!");
                }
            }
            if (ModelState.IsValid)
            {
                var existingDM = await _dbContext
                        .DebitMemos
                        .FirstOrDefaultAsync(dm => dm.Id == model.Id, cancellationToken);

                model.CreatedBy = _userManager.GetUserName(this.User);

                if (model.Source == "Sales Invoice")
                {
                    model.ServiceInvoiceId = null;

                    #region -- Saving Default Enries --

                    existingDM.TransactionDate = model.TransactionDate;
                    existingDM.SalesInvoiceId = model.SalesInvoiceId;
                    existingDM.Quantity = model.Quantity;
                    existingDM.AdjustedPrice = model.AdjustedPrice;
                    existingDM.Description = model.Description;
                    existingDM.Remarks = model.Remarks;

                    #endregion -- Saving Default Enries --

                    existingDM.DebitAmount = (decimal)(model.Quantity * model.AdjustedPrice);

                    if (existingSalesInvoice.Customer.CustomerType == "Vatable")
                    {
                        existingDM.VatableSales = existingDM.DebitAmount / 1.12m;
                        existingDM.VatAmount = existingDM.DebitAmount - existingDM.VatableSales;

                        if (existingSalesInvoice.WithHoldingTaxAmount != 0)
                        {
                            existingDM.WithHoldingTaxAmount = existingDM.VatableSales * 0.01m;
                        }
                        if (existingSalesInvoice.WithHoldingVatAmount != 0)
                        {
                            existingDM.WithHoldingVatAmount = existingDM.VatableSales * 0.05m;
                        }
                        existingDM.TotalSales = existingDM.VatableSales + existingDM.VatAmount;
                    }
                    else
                    {
                        existingDM.TotalSales = existingDM.DebitAmount;
                    }

                }
                else if (model.Source == "Service Invoice")
                {
                    model.SalesInvoiceId = null;

                    #region --Retrieval of Services

                    existingDM.ServicesId = existingSv.ServicesId;

                    var services = await _dbContext
                    .Services
                    .FirstOrDefaultAsync(s => s.Id == existingDM.ServicesId, cancellationToken);

                    #endregion --Retrieval of Services

                    #region -- Saving Default Enries --

                    existingDM.TransactionDate = model.TransactionDate;
                    existingDM.ServiceInvoiceId = model.ServiceInvoiceId;
                    existingDM.Period = model.Period;
                    existingDM.Amount = model.Amount;
                    existingDM.Description = model.Description;
                    existingDM.Remarks = model.Remarks;

                    #endregion -- Saving Default Enries --

                    existingDM.DebitAmount = model.Amount ?? 0;

                    if (existingSv.Customer.CustomerType == "Vatable")
                    {
                        existingDM.VatableSales = existingDM.DebitAmount / 1.12m;
                        existingDM.VatAmount = existingDM.DebitAmount - existingDM.VatableSales;
                        existingDM.TotalSales = existingDM.VatableSales + existingDM.VatAmount;
                        existingDM.WithHoldingTaxAmount = existingDM.VatableSales * (services.Percent / 100m);

                        if (existingSv.WithholdingVatAmount != 0)
                        {
                            existingDM.WithHoldingVatAmount = existingDM.VatableSales * 0.05m;
                        }
                    }
                    else
                    {
                        existingDM.TotalSales = existingDM.DebitAmount;
                    }

                }

                #region --Audit Trail Recording

                AuditTrail auditTrail = new(_userManager.GetUserName(this.User), $"Edit credit memo# {existingDM.DMNo}", "Credit Memo");
                await _dbContext.AddAsync(auditTrail, cancellationToken);

                #endregion --Audit Trail Recording

                await _dbContext.SaveChangesAsync(cancellationToken);
                TempData["success"] = "Debit Memo edited successfully";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError("", "The information you submitted is not valid!");
            return View(model);
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
            var selectedList = _dbContext.DebitMemos
                .Where(dm => recordIds.Contains(dm.Id))
                .OrderBy(dm => dm.DMNo)
                .ToList();

            // Create the Excel package
            using var package = new ExcelPackage();
            // Add a new worksheet to the Excel package
            var worksheet = package.Workbook.Worksheets.Add("DebitMemo");

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
                worksheet.Cells[row, 2].Value = item.DebitAmount;
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
                worksheet.Cells[row, 22].Value = item.DMNo;
                worksheet.Cells[row, 23].Value = item.ServiceInvoiceId;
                worksheet.Cells[row, 24].Value = item.Id;

                row++;
            }

            // Convert the Excel package to a byte array
            var excelBytes = package.GetAsByteArray();

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "DebitMemoList.xlsx");
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
                            var debitMemo = new DebitMemo
                            {
                                DMNo = await _debitMemoRepo.GenerateDMNo(),
                                SeriesNumber = await _debitMemoRepo.GetLastSeriesNumber(),
                                TransactionDate = DateOnly.TryParse(worksheet.Cells[row, 1].Text, out DateOnly transactionDate) ? transactionDate : default,
                                DebitAmount = decimal.TryParse(worksheet.Cells[row, 2].Text, out decimal debitAmount) ? debitAmount : 0,
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

                            debitMemo.SalesInvoiceId = await _dbContext.SalesInvoices
                                .Where(c => c.OriginalDocumentId == debitMemo.OriginalSalesInvoiceId)
                                .Select(c => (int?)c.Id)
                                .FirstOrDefaultAsync();

                            debitMemo.ServiceInvoiceId = await _dbContext.ServiceInvoices
                                .Where(c => c.OriginalDocumentId == debitMemo.OriginalServiceInvoiceId)
                                .Select(c => (int?)c.Id)
                                .FirstOrDefaultAsync();

                            await _dbContext.DebitMemos.AddAsync(debitMemo);
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