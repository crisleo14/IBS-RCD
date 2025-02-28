using Accounting_System.Data;
using Accounting_System.Models;
using Accounting_System.Models.AccountsReceivable;
using Accounting_System.Models.MasterFile;
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

namespace Accounting_System.Controllers
{
    [Authorize]
    public class DebitMemoController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly DebitMemoRepo _debitMemoRepo;

        private readonly SalesInvoiceRepo _salesInvoiceRepo;

        private readonly ServiceInvoiceRepo _serviceInvoiceRepo;

        private readonly GeneralRepo _generalRepo;

        public DebitMemoController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, DebitMemoRepo dmcmRepo, GeneralRepo generalRepo, SalesInvoiceRepo salesInvoiceRepo, ServiceInvoiceRepo serviceInvoiceRepo)
        {
            _dbContext = dbContext;
            this._userManager = userManager;
            _debitMemoRepo = dmcmRepo;
            _generalRepo = generalRepo;
            _salesInvoiceRepo = salesInvoiceRepo;
            _serviceInvoiceRepo = serviceInvoiceRepo;
        }

        public async Task<IActionResult> Index(string? view, CancellationToken cancellationToken)
        {

            if (view == nameof(DynamicView.DebitMemo))
            {
                return View("ImportExportIndex", await _debitMemoRepo.GetDebitMemosAsync(cancellationToken));
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetDebitMemos([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var debitMemos = await _debitMemoRepo.GetDebitMemosAsync(cancellationToken);
                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search?.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();
                    debitMemos = debitMemos
                        .Where(dm =>
                            dm.DMNo.ToLower().Contains(searchValue) ||
                            dm.TransactionDate.ToString("MMM dd, yyyy").ToLower().Contains(searchValue) ||
                            dm.SalesInvoice?.SINo?.ToLower().Contains(searchValue) == true ||
                            dm.ServiceInvoice?.SVNo?.ToLower().Contains(searchValue) == true ||
                            dm.Source.ToLower().Contains(searchValue) ||
                            dm.DebitAmount.ToString().Contains(searchValue) ||
                            dm.Remarks?.ToLower().Contains(searchValue) == true ||
                            dm.Description.ToLower().Contains(searchValue) ||
                            dm.CreatedBy.ToLower().Contains(searchValue)
                            )
                        .ToList();
                }
                // Sorting
                if (parameters.Order != null && parameters.Order.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Data;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";
                    debitMemos = debitMemos
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}")
                        .ToList();
                }
                var totalRecords = debitMemos.Count();
                var pagedData = debitMemos
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
        public async Task<IActionResult> GetAllDebitMemoIds(CancellationToken cancellationToken)
        {
            var debitMemoIds = await _dbContext.DebitMemos
                                     .Select(dm => dm.Id) // Assuming Id is the primary key
                                     .ToListAsync(cancellationToken);
            return Json(debitMemoIds);
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

            if (ModelState.IsValid)
            {
                #region -- checking for unposted DM or CM --

                    var existingSiDms = await _dbContext.DebitMemos
                                  .Where(dm => !dm.IsPosted && !dm.IsCanceled && !dm.IsVoided)
                                  .OrderBy(dm => dm.Id)
                                  .ToListAsync(cancellationToken);
                    if (existingSiDms.Count > 0)
                    {
                        var dmNo = new List<string>();
                        foreach (var item in existingSiDms)
                        {
                            dmNo.Add(item.DMNo);
                        }
                        ModelState.AddModelError("", $"Can’t proceed to create you have unposted DM. {string.Join(" , ", dmNo)}");
                        return View(model);
                    }

                    var existingSiCms = await _dbContext.CreditMemos
                                      .Where(cm => !cm.IsPosted && !cm.IsCanceled && !cm.IsVoided)
                                      .OrderBy(cm => cm.Id)
                                      .ToListAsync(cancellationToken);

                    if (existingSiCms.Count > 0)
                    {
                        var cmNo = new List<string>();
                        foreach (var item in existingSiCms)
                        {
                            cmNo.Add(item.CMNo);
                        }
                        ModelState.AddModelError("", $"Can’t proceed to create you have unposted CM. {string.Join(" , ", cmNo)}");
                        return View(model);
                    }

                #endregion

                #region --Validating the series--

                var generateDmNo = await _debitMemoRepo.GenerateDMNo(cancellationToken);
                var getLastNumber = long.Parse(generateDmNo.Substring(2));

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

                model.DMNo = generateDmNo;
                model.CreatedBy = _userManager.GetUserName(this.User);

                if (model.Source == "Sales Invoice")
                {
                    model.ServiceInvoiceId = null;

                    model.DebitAmount = (decimal)(model.Quantity * model.AdjustedPrice);
                }
                else if (model.Source == "Service Invoice")
                {
                    model.SalesInvoiceId = null;

                    #region --Retrieval of Services

                    model.ServicesId = existingSv?.ServicesId;

                    var services = await _dbContext
                    .Services
                    .FirstOrDefaultAsync(s => s.Id == model.ServicesId, cancellationToken);

                    #endregion --Retrieval of Services

                    model.DebitAmount = model.Amount ?? 0;
                }

                #region --Audit Trail Recording

                if (model.OriginalSeriesNumber == null && model.OriginalDocumentId == 0)
                {
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    AuditTrail auditTrailBook = new(model.CreatedBy, $"Create new debit memo# {model.DMNo}", "Debit Memo", ipAddress);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                }

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

                if (findIdOfDM.OriginalSeriesNumber == null && findIdOfDM.OriginalDocumentId == 0)
                {
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    var printedBy = _userManager.GetUserName(this.User);
                    AuditTrail auditTrailBook = new(printedBy, $"Printed original copy of dm# {findIdOfDM.DMNo}", "Debit Memo", ipAddress);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                }

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

                        var accountTitlesDto = await _generalRepo.GetListOfAccountTitleDto(cancellationToken);
                        var arTradeReceivableTitle = accountTitlesDto.Find(c => c.AccountNumber == "101020100") ?? throw new ArgumentException("Account number: '101020100', Account title: 'AR-Trade Receivable' not found.");
                        var arNonTradeReceivableTitle = accountTitlesDto.Find(c => c.AccountNumber == "101020500") ?? throw new ArgumentException("Account number: '101020500', Account title: 'AR-Non Trade Receivable' not found.");
                        var arTradeCwt = accountTitlesDto.Find(c => c.AccountNumber == "101020200") ?? throw new ArgumentException("Account number: '101020200', Account title: 'AR-Trade Receivable - Creditable Withholding Tax' not found.");
                        var arTradeCwv = accountTitlesDto.Find(c => c.AccountNumber == "101020300") ?? throw new ArgumentException("Account number: '101020300', Account title: 'AR-Trade Receivable - Creditable Withholding Vat' not found.");
                        var biodieselTitle = accountTitlesDto.Find(c => c.AccountNumber == "401010100") ?? throw new ArgumentException("Account number: '401010100', Account title: 'Sales - Biodiesel' not found.");
                        var econogasTitle = accountTitlesDto.Find(c => c.AccountNumber == "401010200") ?? throw new ArgumentException("Account number: '401010200', Account title: 'Sales - Econogas' not found.");
                        var envirogasTitle = accountTitlesDto.Find(c => c.AccountNumber == "401010300") ?? throw new ArgumentException("Account number: '401010300', Account title: 'Sales - Envirogas' not found.");
                        var vatOutputTitle = accountTitlesDto.Find(c => c.AccountNumber == "201030100") ?? throw new ArgumentException("Account number: '201030100', Account title: 'Vat - Output' not found.");


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
                                sales.VatableSales = _generalRepo.ComputeNetOfVat(sales.Amount);
                                sales.VatAmount = _generalRepo.ComputeVatAmount(sales.VatableSales);
                                //sales.Discount = model.Discount;
                                sales.NetSales = sales.VatableSales;
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
                                sales.NetSales = sales.Amount;
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
                                sales.NetSales = sales.Amount;
                                sales.CreatedBy = model.CreatedBy;
                                sales.CreatedDate = model.CreatedDate;
                                sales.DueDate = existingSI.DueDate;
                                sales.DocumentId = model.SalesInvoiceId;
                            }
                            await _dbContext.AddAsync(sales, cancellationToken);

                            #endregion --Sales Book Recording(SI)--

                            #region --General Ledger Book Recording(SI)--

                            decimal withHoldingTaxAmount = 0;
                            decimal withHoldingVatAmount = 0;
                            decimal netOfVatAmount = 0;
                            decimal vatAmount = 0;
                            if (model.SalesInvoice.Customer.CustomerType == CS.VatType_Vatable)
                            {
                                netOfVatAmount = _generalRepo.ComputeNetOfVat(model.DebitAmount);
                                vatAmount = _generalRepo.ComputeVatAmount(netOfVatAmount);
                            }
                            else
                            {
                                netOfVatAmount = model.DebitAmount;
                            }
                            if (model.SalesInvoice.Customer.WithHoldingTax)
                            {
                                withHoldingTaxAmount = _generalRepo.ComputeEwtAmount(netOfVatAmount, 0.01m);
                            }
                            if (model.SalesInvoice.Customer.WithHoldingVat)
                            {
                                withHoldingVatAmount = _generalRepo.ComputeEwtAmount(netOfVatAmount, 0.05m);
                            }

                            var ledgers = new List<GeneralLedgerBook>();

                            ledgers.Add(
                                new GeneralLedgerBook
                                {
                                    Date = model.TransactionDate,
                                    Reference = model.DMNo,
                                    Description = model.SalesInvoice.Product.Name,
                                    AccountNo = arTradeReceivableTitle.AccountNumber,
                                    AccountTitle = arTradeReceivableTitle.AccountName,
                                    Debit = model.DebitAmount - (withHoldingTaxAmount + withHoldingVatAmount),
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
                                        Reference = model.DMNo,
                                        Description = model.SalesInvoice.Product.Name,
                                        AccountNo = arTradeCwt.AccountNumber,
                                        AccountTitle = arTradeCwt.AccountNumber,
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
                                        Reference = model.DMNo,
                                        Description = model.SalesInvoice.Product.Name,
                                        AccountNo = arTradeCwv.AccountNumber,
                                        AccountTitle = arTradeCwv.AccountName,
                                        Debit = withHoldingVatAmount,
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
                                        AccountNo = biodieselTitle.AccountNumber,
                                        AccountTitle = biodieselTitle.AccountNumber,
                                        Debit = 0,
                                        CreatedBy = model.CreatedBy,
                                        Credit = netOfVatAmount,
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
                                        AccountNo = econogasTitle.AccountNumber,
                                        AccountTitle = econogasTitle.AccountName,
                                        Debit = 0,
                                        CreatedBy = model.CreatedBy,
                                        Credit = netOfVatAmount,
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
                                        AccountNo = envirogasTitle.AccountNumber,
                                        AccountTitle = envirogasTitle.AccountName,
                                        Debit = 0,
                                        CreatedBy = model.CreatedBy,
                                        Credit = netOfVatAmount,
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
                                        Reference = model.DMNo,
                                        Description = model.SalesInvoice.Product.Name,
                                        AccountNo = vatOutputTitle.AccountNumber,
                                        AccountTitle = vatOutputTitle.AccountName,
                                        Debit = 0,
                                        Credit = vatAmount,
                                        CreatedBy = model.CreatedBy,
                                        CreatedDate = model.CreatedDate
                                    }
                                );
                            }

                            if (!_generalRepo.IsJournalEntriesBalanced(ledgers))
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
                                .Include(sv => sv.Service)
                                .FirstOrDefaultAsync(sv => sv.Id == model.ServiceInvoiceId, cancellationToken);

                            #region --SV Computation--

                            viewModelDMCM.Period = DateOnly.FromDateTime(model.CreatedDate) >= model.Period ? DateOnly.FromDateTime(model.CreatedDate) : model.Period.AddMonths(1).AddDays(-1);

                            if (existingSv.Customer.CustomerType == "Vatable")
                            {
                                viewModelDMCM.Total = model.Amount ?? 0  - existingSv.Discount;
                                viewModelDMCM.NetAmount = _generalRepo.ComputeNetOfVat(viewModelDMCM.Total);
                                viewModelDMCM.VatAmount = _generalRepo.ComputeVatAmount(viewModelDMCM.NetAmount);
                                viewModelDMCM.WithholdingTaxAmount = viewModelDMCM.NetAmount * (existingSv.Customer.WithHoldingTax ? existingSv.Service.Percent / 100m : 0);
                                if (existingSv.Customer.WithHoldingVat)
                                {
                                    viewModelDMCM.WithholdingVatAmount = viewModelDMCM.NetAmount * 0.05m;
                                }
                            }
                            else
                            {
                                viewModelDMCM.NetAmount = model.Amount ?? 0 - existingSv.Discount;
                                viewModelDMCM.WithholdingTaxAmount = viewModelDMCM.NetAmount * (existingSv.Customer.WithHoldingTax ? existingSv.Service.Percent / 100m : 0);
                                if (existingSv.Customer.WithHoldingVat)
                                {
                                    viewModelDMCM.WithholdingVatAmount = viewModelDMCM.NetAmount * 0.05m;
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
                                        AccountNo = arNonTradeReceivableTitle.AccountNumber,
                                        AccountTitle = arNonTradeReceivableTitle.AccountName,
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
                                        AccountNo = arTradeCwt.AccountNumber,
                                        AccountTitle = arTradeCwt.AccountName,
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
                                        AccountNo = arTradeCwv.AccountNumber,
                                        AccountTitle = arTradeCwv.AccountName,
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
                                        AccountNo = vatOutputTitle.AccountNumber,
                                        AccountTitle = vatOutputTitle.AccountName,
                                        Debit = 0,
                                        Credit = viewModelDMCM.VatAmount,
                                        CreatedBy = model.CreatedBy,
                                        CreatedDate = model.CreatedDate
                                    }
                                );
                            }

                            if (!_generalRepo.IsJournalEntriesBalanced(ledgers))
                            {
                                throw new ArgumentException("Debit and Credit is not equal, check your entries.");
                            }

                            await _dbContext.GeneralLedgerBooks.AddRangeAsync(ledgers, cancellationToken);

                            #endregion --General Ledger Book Recording(SV)--
                        }

                        #region --Audit Trail Recording

                        if (model.OriginalSeriesNumber == null && model.OriginalDocumentId == 0)
                        {
                            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                            AuditTrail auditTrailBook = new(model.PostedBy, $"Posted debit memo# {model.DMNo}", "Debit Memo", ipAddress);
                            await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                        }

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        TempData["success"] = "Debit Memo has been Posted.";
                    }
                    return RedirectToAction(nameof(Print), new { id = id });
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(Print), new { id = id });
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

                    if (model.OriginalSeriesNumber == null && model.OriginalDocumentId == 0)
                    {
                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        AuditTrail auditTrailBook = new(model.VoidedBy, $"Voided debit memo# {model.DMNo}", "Debit Memo", ipAddress);
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                    }

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

                    if (model.OriginalSeriesNumber == null && model.OriginalDocumentId == 0)
                    {
                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        AuditTrail auditTrailBook = new(model.CanceledBy, $"Cancelled debit memo# {model.DMNo}", "Debit Memo", ipAddress);
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                    }

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
                }

                #region --Audit Trail Recording

                // if (model.OriginalSeriesNumber == null && model.OriginalDocumentId == 0)
                // {
                //     var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                //     AuditTrail auditTrailBook = new(_userManager.GetUserName(this.User), $"Edit debit memo# {existingDM.DMNo}", "Debit Memo", ipAddress);
                //     await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                // }

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
        public async Task<IActionResult> Export(string selectedRecord)
        {
            if (string.IsNullOrEmpty(selectedRecord))
            {
                // Handle the case where no invoices are selected
                return RedirectToAction(nameof(Index));
            }

            var recordIds = selectedRecord.Split(',').Select(int.Parse).ToList();

            // Retrieve the selected invoices from the database
            var selectedList = await _dbContext.DebitMemos
                .Where(dm => recordIds.Contains(dm.Id))
                .Include(dm => dm.SalesInvoice)
                .Include(dm => dm.ServiceInvoice)
                .OrderBy(dm => dm.DMNo)
                .ToListAsync();

            // Create the Excel package
            using (var package = new ExcelPackage())
            {
                // Add a new worksheet to the Excel package
                #region -- Sales Invoice Table Header --

                var worksheet2 = package.Workbook.Worksheets.Add("SalesInvoice");

                worksheet2.Cells["A1"].Value = "OtherRefNo";
                worksheet2.Cells["B1"].Value = "Quantity";
                worksheet2.Cells["C1"].Value = "UnitPrice";
                worksheet2.Cells["D1"].Value = "Amount";
                worksheet2.Cells["E1"].Value = "Remarks";
                worksheet2.Cells["F1"].Value = "Status";
                worksheet2.Cells["G1"].Value = "TransactionDate";
                worksheet2.Cells["H1"].Value = "Discount";
                worksheet2.Cells["I1"].Value = "AmountPaid";
                worksheet2.Cells["J1"].Value = "Balance";
                worksheet2.Cells["K1"].Value = "IsPaid";
                worksheet2.Cells["L1"].Value = "IsTaxAndVatPaid";
                worksheet2.Cells["M1"].Value = "DueDate";
                worksheet2.Cells["N1"].Value = "CreatedBy";
                worksheet2.Cells["O1"].Value = "CreatedDate";
                worksheet2.Cells["P1"].Value = "CancellationRemarks";
                worksheet2.Cells["Q1"].Value = "OriginalReceivingReportId";
                worksheet2.Cells["R1"].Value = "OriginalCustomerId";
                worksheet2.Cells["S1"].Value = "OriginalPOId";
                worksheet2.Cells["T1"].Value = "OriginalProductId";
                worksheet2.Cells["U1"].Value = "OriginalSINo";
                worksheet2.Cells["V1"].Value = "OriginalDocumentId";

                #endregion -- Sales Invoice Table Header --

                #region -- Service Invoice Table Header --

                var worksheet3 = package.Workbook.Worksheets.Add("ServiceInvoice");

                worksheet3.Cells["A1"].Value = "DueDate";
                worksheet3.Cells["B1"].Value = "Period";
                worksheet3.Cells["C1"].Value = "Amount";
                worksheet3.Cells["D1"].Value = "Total";
                worksheet3.Cells["E1"].Value = "Discount";
                worksheet3.Cells["F1"].Value = "CurrentAndPreviousMonth";
                worksheet3.Cells["G1"].Value = "UnearnedAmount";
                worksheet3.Cells["H1"].Value = "Status";
                worksheet3.Cells["I1"].Value = "AmountPaid";
                worksheet3.Cells["J1"].Value = "Balance";
                worksheet3.Cells["K1"].Value = "Instructions";
                worksheet3.Cells["L1"].Value = "IsPaid";
                worksheet3.Cells["M1"].Value = "CreatedBy";
                worksheet3.Cells["N1"].Value = "CreatedDate";
                worksheet3.Cells["O1"].Value = "CancellationRemarks";
                worksheet3.Cells["P1"].Value = "OriginalCustomerId";
                worksheet3.Cells["Q1"].Value = "OriginalSVNo";
                worksheet3.Cells["R1"].Value = "OriginalServicesId";
                worksheet3.Cells["S1"].Value = "OriginalDocumentId";

                #endregion -- Service Invoice Table Header --

                #region -- Debit Memo Table Header --

                var worksheet = package.Workbook.Worksheets.Add("DebitMemo");

                worksheet.Cells["A1"].Value = "TransactionDate";
                worksheet.Cells["B1"].Value = "DebitAmount";
                worksheet.Cells["C1"].Value = "Description";
                worksheet.Cells["D1"].Value = "AdjustedPrice";
                worksheet.Cells["E1"].Value = "Quantity";
                worksheet.Cells["F1"].Value = "Source";
                worksheet.Cells["G1"].Value = "Remarks";
                worksheet.Cells["H1"].Value = "Period";
                worksheet.Cells["I1"].Value = "Amount";
                worksheet.Cells["J1"].Value = "CurrentAndPreviousAmount";
                worksheet.Cells["K1"].Value = "UnearnedAmount";
                worksheet.Cells["L1"].Value = "ServicesId";
                worksheet.Cells["M1"].Value = "CreatedBy";
                worksheet.Cells["N1"].Value = "CreatedDate";
                worksheet.Cells["O1"].Value = "CancellationRemarks";
                worksheet.Cells["P1"].Value = "OriginalSalesInvoiceId";
                worksheet.Cells["Q1"].Value = "OriginalDMNo";
                worksheet.Cells["R1"].Value = "OriginalServiceInvoiceId";
                worksheet.Cells["S1"].Value = "OriginalDocumentId";

                #endregion -- Debit Memo Table Header --

                #region -- Debit Memo Export --

                int row = 2;

                foreach (var item in selectedList)
                {
                    worksheet.Cells[row, 1].Value = item.TransactionDate.ToString("yyyy-MM-dd");
                    worksheet.Cells[row, 2].Value = item.DebitAmount;
                    worksheet.Cells[row, 3].Value = item.Description;
                    worksheet.Cells[row, 4].Value = item.AdjustedPrice;
                    worksheet.Cells[row, 5].Value = item.Quantity;
                    worksheet.Cells[row, 6].Value = item.Source;
                    worksheet.Cells[row, 7].Value = item.Remarks;
                    worksheet.Cells[row, 8].Value = item.Period.ToString("yyyy-MM-dd");
                    worksheet.Cells[row, 9].Value = item.Amount;
                    worksheet.Cells[row, 10].Value = item.CurrentAndPreviousAmount;
                    worksheet.Cells[row, 11].Value = item.UnearnedAmount;
                    worksheet.Cells[row, 12].Value = item.ServicesId;
                    worksheet.Cells[row, 13].Value = item.CreatedBy;
                    worksheet.Cells[row, 14].Value = item.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                    worksheet.Cells[row, 15].Value = item.CancellationRemarks;
                    worksheet.Cells[row, 16].Value = item.SalesInvoiceId;
                    worksheet.Cells[row, 17].Value = item.DMNo;
                    worksheet.Cells[row, 18].Value = item.ServiceInvoiceId;
                    worksheet.Cells[row, 19].Value = item.Id;

                    row++;
                }

                #endregion -- Debit Memo Export --

                #region -- Sales Invoice Export --

                int siRow = 2;

                foreach (var item in selectedList)
                {
                    if (item.SalesInvoice == null)
                    {
                        continue;
                    }
                    worksheet2.Cells[siRow, 1].Value = item.SalesInvoice.OtherRefNo;
                    worksheet2.Cells[siRow, 2].Value = item.SalesInvoice.Quantity;
                    worksheet2.Cells[siRow, 3].Value = item.SalesInvoice.UnitPrice;
                    worksheet2.Cells[siRow, 4].Value = item.SalesInvoice.Amount;
                    worksheet2.Cells[siRow, 5].Value = item.SalesInvoice.Remarks;
                    worksheet2.Cells[siRow, 6].Value = item.SalesInvoice.Status;
                    worksheet2.Cells[siRow, 7].Value = item.SalesInvoice.TransactionDate.ToString("yyyy-MM-dd");
                    worksheet2.Cells[siRow, 8].Value = item.SalesInvoice.Discount;
                    worksheet2.Cells[siRow, 9].Value = item.SalesInvoice.AmountPaid;
                    worksheet2.Cells[siRow, 10].Value = item.SalesInvoice.Balance;
                    worksheet2.Cells[siRow, 11].Value = item.SalesInvoice.IsPaid;
                    worksheet2.Cells[siRow, 12].Value = item.SalesInvoice.IsTaxAndVatPaid;
                    worksheet2.Cells[siRow, 13].Value = item.SalesInvoice.DueDate.ToString("yyyy-MM-dd");
                    worksheet2.Cells[siRow, 14].Value = item.SalesInvoice.CreatedBy;
                    worksheet2.Cells[siRow, 15].Value = item.SalesInvoice.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                    worksheet2.Cells[siRow, 16].Value = item.SalesInvoice.CancellationRemarks;
                    worksheet2.Cells[siRow, 18].Value = item.SalesInvoice.CustomerId;
                    worksheet2.Cells[siRow, 20].Value = item.SalesInvoice.ProductId;
                    worksheet2.Cells[siRow, 21].Value = item.SalesInvoice.SINo;
                    worksheet2.Cells[siRow, 22].Value = item.SalesInvoice.Id;

                    siRow++;
                }

                #endregion -- Sales Invoice Export --

                #region -- Service Invoice Export --

                int svRow = 2;

                foreach (var item in selectedList)
                {
                    if (item.ServiceInvoice == null)
                    {
                        continue;
                    }
                    worksheet3.Cells[svRow, 1].Value = item.ServiceInvoice.DueDate.ToString("yyyy-MM-dd");
                    worksheet3.Cells[svRow, 2].Value = item.ServiceInvoice.Period.ToString("yyyy-MM-dd");
                    worksheet3.Cells[svRow, 3].Value = item.ServiceInvoice.Amount;
                    worksheet3.Cells[svRow, 4].Value = item.ServiceInvoice.Total;
                    worksheet3.Cells[svRow, 5].Value = item.ServiceInvoice.Discount;
                    worksheet3.Cells[svRow, 6].Value = item.ServiceInvoice.CurrentAndPreviousAmount;
                    worksheet3.Cells[svRow, 7].Value = item.ServiceInvoice.UnearnedAmount;
                    worksheet3.Cells[svRow, 8].Value = item.ServiceInvoice.Status;
                    worksheet3.Cells[svRow, 9].Value = item.ServiceInvoice.AmountPaid;
                    worksheet3.Cells[svRow, 10].Value = item.ServiceInvoice.Balance;
                    worksheet3.Cells[svRow, 11].Value = item.ServiceInvoice.Instructions;
                    worksheet3.Cells[svRow, 12].Value = item.ServiceInvoice.IsPaid;
                    worksheet3.Cells[svRow, 13].Value = item.ServiceInvoice.CreatedBy;
                    worksheet3.Cells[svRow, 14].Value = item.ServiceInvoice.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                    worksheet3.Cells[svRow, 15].Value = item.ServiceInvoice.CancellationRemarks;
                    worksheet3.Cells[svRow, 16].Value = item.ServiceInvoice.CustomerId;
                    worksheet3.Cells[svRow, 17].Value = item.ServiceInvoice.SVNo;
                    worksheet3.Cells[svRow, 18].Value = item.ServiceInvoice.ServicesId;
                    worksheet3.Cells[svRow, 19].Value = item.ServiceInvoice.Id;

                    svRow++;
                }

                #endregion -- Service Invoice Export --

                // Convert the Excel package to a byte array
                var excelBytes = await package.GetAsByteArrayAsync();

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "DebitMemoList.xlsx");
            }
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
                        var worksheet = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "DebitMemo");

                        var worksheet2 = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "SalesInvoice");

                        var worksheet3 = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "ServiceInvoice");

                        if (worksheet == null)
                        {
                            TempData["error"] = "The Excel file contains no worksheets.";
                            return RedirectToAction(nameof(Index), new { view = DynamicView.DebitMemo });
                        }

                        if (worksheet.ToString() != nameof(DynamicView.DebitMemo))
                        {
                            TempData["error"] = "The Excel file is not related to debit memo.";
                            return RedirectToAction(nameof(Index), new { view = DynamicView.DebitMemo });
                        }

                        #region -- Sales Invoice Import --

                        var siRowCount = worksheet2?.Dimension?.Rows ?? 0;
                        var siDictionary = new Dictionary<string, bool>();
                        var invoiceList = await _dbContext
                            .SalesInvoices
                            .ToListAsync(cancellationToken);

                        for (int row = 2; row <= siRowCount; row++) // Assuming the first row is the header
                        {
                            if (worksheet2 == null || siRowCount == 0)
                            {
                                continue;
                            }
                            var invoice = new SalesInvoice
                            {
                                SINo = worksheet2.Cells[row, 21].Text,
                                OtherRefNo = worksheet2.Cells[row, 1].Text,
                                Quantity = decimal.TryParse(worksheet2.Cells[row, 2].Text, out decimal quantity)
                                    ? quantity
                                    : 0,
                                UnitPrice = decimal.TryParse(worksheet2.Cells[row, 3].Text, out decimal unitPrice)
                                    ? unitPrice
                                    : 0,
                                Amount =
                                    decimal.TryParse(worksheet2.Cells[row, 4].Text, out decimal amount) ? amount : 0,
                                Remarks = worksheet2.Cells[row, 5].Text,
                                Status = worksheet2.Cells[row, 6].Text,
                                TransactionDate =
                                    DateOnly.TryParse(worksheet2.Cells[row, 7].Text, out DateOnly transactionDate)
                                        ? transactionDate
                                        : default,
                                Discount = decimal.TryParse(worksheet2.Cells[row, 8].Text, out decimal discount)
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
                                DueDate = DateOnly.TryParse(worksheet2.Cells[row, 13].Text, out DateOnly dueDate)
                                    ? dueDate
                                    : default,
                                CreatedBy = worksheet2.Cells[row, 14].Text,
                                CreatedDate = DateTime.TryParse(worksheet2.Cells[row, 15].Text, out DateTime createdDate)
                                    ? createdDate
                                    : default,
                                CancellationRemarks = worksheet2.Cells[row, 16].Text != ""
                                    ? worksheet2.Cells[row, 16].Text
                                    : null,
                                OriginalCustomerId = int.TryParse(worksheet2.Cells[row, 18].Text, out int customerId)
                                    ? customerId
                                    : 0,
                                OriginalProductId = int.TryParse(worksheet2.Cells[row, 20].Text, out int productId)
                                    ? productId
                                    : 0,
                                OriginalSeriesNumber = worksheet2.Cells[row, 21].Text,
                                OriginalDocumentId =
                                    int.TryParse(worksheet2.Cells[row, 22].Text, out int originalDocumentId)
                                        ? originalDocumentId
                                        : 0,
                            };

                            if (!siDictionary.TryAdd(invoice.OriginalSeriesNumber, true))
                            {
                                continue;
                            }

                            if (invoiceList.Any(si => si.OriginalDocumentId == invoice.OriginalDocumentId))
                            {
                                var siChanges = new Dictionary<string, (string OriginalValue, string NewValue)>();
                                var existingSI = await _dbContext.SalesInvoices.FirstOrDefaultAsync(si => si.OriginalDocumentId == invoice.OriginalDocumentId, cancellationToken);

                                if (existingSI.SINo.TrimStart().TrimEnd() != worksheet2.Cells[row, 21].Text.TrimStart().TrimEnd())
                                {
                                    siChanges["SiNo"] = (existingSI.SINo.TrimStart().TrimEnd(), worksheet2.Cells[row, 21].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingSI.OriginalCustomerId.ToString().TrimStart().TrimEnd() != worksheet2.Cells[row, 18].Text.TrimStart().TrimEnd())
                                {
                                    siChanges["OriginalCustomerId"] = (existingSI.OriginalCustomerId.ToString().TrimStart().TrimEnd(), worksheet2.Cells[row, 18].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingSI.OriginalProductId.ToString().TrimStart().TrimEnd() != worksheet2.Cells[row, 20].Text.TrimStart().TrimEnd())
                                {
                                    siChanges["OriginalProductId"] = (existingSI.OriginalProductId.ToString().TrimStart().TrimEnd(), worksheet2.Cells[row, 20].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingSI.OtherRefNo.TrimStart().TrimEnd() != worksheet2.Cells[row, 1].Text.TrimStart().TrimEnd())
                                {
                                    siChanges["OtherRefNo"] = (existingSI.OtherRefNo.TrimStart().TrimEnd(), worksheet2.Cells[row, 1].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingSI.Quantity.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet2.Cells[row, 2].Text).ToString("F2").TrimStart().TrimEnd())
                                {
                                    siChanges["Quantity"] = (existingSI.Quantity.ToString("F2").TrimStart().TrimEnd(), decimal.Parse(worksheet2.Cells[row, 2].Text.TrimStart().TrimEnd()).ToString("F2"));
                                }

                                if (existingSI.UnitPrice.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet2.Cells[row, 3].Text).ToString("F2").TrimStart().TrimEnd())
                                {
                                    siChanges["UnitPrice"] = (existingSI.UnitPrice.ToString("F2").TrimStart().TrimEnd(), decimal.Parse(worksheet2.Cells[row, 3].Text).ToString("F2").TrimStart().TrimEnd());
                                }

                                if (existingSI.Amount.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet2.Cells[row, 4].Text).ToString("F2").TrimStart().TrimEnd())
                                {
                                    siChanges["Amount"] = (existingSI.Amount.ToString("F2").TrimStart().TrimEnd(), decimal.Parse(worksheet2.Cells[row, 4].Text).ToString("F2").TrimStart().TrimEnd());
                                }

                                if (existingSI.Remarks.TrimStart().TrimEnd() != worksheet2.Cells[row, 5].Text.TrimStart().TrimEnd())
                                {
                                    siChanges["Remarks"] = (existingSI.Remarks.TrimStart().TrimEnd(), worksheet2.Cells[row, 5].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingSI.Status.TrimStart().TrimEnd() != worksheet2.Cells[row, 6].Text.TrimStart().TrimEnd())
                                {
                                    siChanges["Status"] = (existingSI.Status.TrimStart().TrimEnd(), worksheet2.Cells[row, 6].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingSI.TransactionDate.ToString("yyyy-MM-dd").TrimStart().TrimEnd() != worksheet2.Cells[row, 7].Text.TrimStart().TrimEnd())
                                {
                                    siChanges["TransactionDate"] = (existingSI.TransactionDate.ToString("yyyy-MM-dd").TrimStart().TrimEnd(), worksheet2.Cells[row, 7].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingSI.Discount.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet2.Cells[row, 8].Text).ToString("F2").TrimStart().TrimEnd())
                                {
                                    siChanges["Discount"] = (existingSI.Discount.ToString("F2").TrimStart().TrimEnd(), decimal.Parse(worksheet2.Cells[row, 8].Text).ToString("F2").TrimStart().TrimEnd());
                                }

                                if (existingSI.DueDate.ToString("yyyy-MM-dd").TrimStart().TrimEnd() != worksheet2.Cells[row, 13].Text.TrimStart().TrimEnd())
                                {
                                    siChanges["DueDate"] = (existingSI.DueDate.ToString("yyyy-MM-dd").TrimStart().TrimEnd(), worksheet2.Cells[row, 13].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingSI.CreatedBy.TrimStart().TrimEnd() != worksheet2.Cells[row, 14].Text.TrimStart().TrimEnd())
                                {
                                    siChanges["CreatedBy"] = (existingSI.CreatedBy.TrimStart().TrimEnd(), worksheet2.Cells[row, 14].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingSI.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff").TrimStart().TrimEnd() != worksheet2.Cells[row, 15].Text.TrimStart().TrimEnd())
                                {
                                    siChanges["CreatedDate"] = (existingSI.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff").TrimStart().TrimEnd(), worksheet2.Cells[row, 15].Text.TrimStart().TrimEnd())!;
                                }

                                if ((string.IsNullOrWhiteSpace(existingSI.CancellationRemarks?.TrimStart().TrimEnd()) ? "" : existingSI.CancellationRemarks.TrimStart().TrimEnd()) != worksheet2.Cells[row, 16].Text.TrimStart().TrimEnd())
                                {
                                    siChanges["CancellationRemarks"] = (existingSI.CancellationRemarks?.TrimStart().TrimEnd(), worksheet2.Cells[row, 16].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingSI.OriginalSeriesNumber.TrimStart().TrimEnd() != worksheet2.Cells[row, 21].Text.TrimStart().TrimEnd())
                                {
                                    siChanges["OriginalSeriesNumber"] = (existingSI.OriginalSeriesNumber.TrimStart().TrimEnd(), worksheet2.Cells[row, 21].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingSI.OriginalDocumentId.ToString().TrimStart().TrimEnd() != worksheet2.Cells[row, 22].Text.TrimStart().TrimEnd())
                                {
                                    siChanges["OriginalDocumentId"] = (existingSI.OriginalDocumentId.ToString().TrimStart().TrimEnd(), worksheet2.Cells[row, 22].Text.TrimStart().TrimEnd())!;
                                }

                                if (siChanges.Any())
                                {
                                    await _salesInvoiceRepo.LogChangesAsync(existingSI.OriginalDocumentId, siChanges, _userManager.GetUserName(this.User));
                                }

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

                        #endregion -- Sales Invoice Import --

                        #region -- Service Invoice Import --

                        var svRowCount = worksheet3?.Dimension?.Rows ?? 0;
                        var svDictionary = new Dictionary<string, bool>();
                        var serviceInvoiceList = await _dbContext
                            .ServiceInvoices
                            .ToListAsync(cancellationToken);

                        for (int row = 2; row <= svRowCount; row++)  // Assuming the first row is the header
                        {
                            if (worksheet3 == null || svRowCount == 0)
                            {
                                continue;
                            }
                            var serviceInvoice = new ServiceInvoice
                            {
                                SVNo = worksheet3.Cells[row, 17].Text,
                                DueDate = DateOnly.TryParse(worksheet3.Cells[row, 1].Text, out DateOnly dueDate) ? dueDate : default,
                                Period = DateOnly.TryParse(worksheet3.Cells[row, 2].Text, out DateOnly period) ? period : default,
                                Amount = decimal.TryParse(worksheet3.Cells[row, 3].Text, out decimal amount) ? amount : 0,
                                Total = decimal.TryParse(worksheet3.Cells[row, 4].Text, out decimal total) ? total : 0,
                                Discount = decimal.TryParse(worksheet3.Cells[row, 5].Text, out decimal discount) ? discount : 0,
                                CurrentAndPreviousAmount = decimal.TryParse(worksheet3.Cells[row, 6].Text, out decimal currentAndPreviousAmount) ? currentAndPreviousAmount : 0,
                                UnearnedAmount = decimal.TryParse(worksheet3.Cells[row, 7].Text, out decimal unearnedAmount) ? unearnedAmount : 0,
                                Status = worksheet3.Cells[row, 8].Text,
                                // AmountPaid = decimal.TryParse(worksheet.Cells[row, 9].Text, out decimal amountPaid) ? amountPaid : 0,
                                // Balance = decimal.TryParse(worksheet.Cells[row, 10].Text, out decimal balance) ? balance : 0,
                                Instructions = worksheet3.Cells[row, 11].Text,
                                // IsPaid = bool.TryParse(worksheet.Cells[row, 12].Text, out bool isPaid) ? isPaid : false,
                                CreatedBy = worksheet3.Cells[row, 13].Text,
                                CreatedDate = DateTime.TryParse(worksheet3.Cells[row, 14].Text, out DateTime createdDate) ? createdDate : default,
                                CancellationRemarks = worksheet3.Cells[row, 15].Text,
                                OriginalCustomerId = int.TryParse(worksheet3.Cells[row, 16].Text, out int originalCustomerId) ? originalCustomerId : 0,
                                OriginalSeriesNumber = worksheet3.Cells[row, 17].Text,
                                OriginalServicesId = int.TryParse(worksheet3.Cells[row, 18].Text, out int originalServicesId) ? originalServicesId : 0,
                                OriginalDocumentId = int.TryParse(worksheet3.Cells[row, 19].Text, out int originalDocumentId) ? originalDocumentId : 0,
                            };

                            if (!svDictionary.TryAdd(serviceInvoice.OriginalSeriesNumber, true))
                            {
                                continue;
                            }

                            if (serviceInvoiceList.Any(sv => sv.OriginalDocumentId == serviceInvoice.OriginalDocumentId))
                            {
                                var svChanges = new Dictionary<string, (string OriginalValue, string NewValue)>();
                                var existingSV = await _dbContext.ServiceInvoices.FirstOrDefaultAsync(si => si.OriginalDocumentId == serviceInvoice.OriginalDocumentId, cancellationToken);

                                if (existingSV.SVNo.TrimStart().TrimEnd() != worksheet3.Cells[row, 17].Text.TrimStart().TrimEnd())
                                {
                                    svChanges["SvNo"] = (existingSV.SVNo.TrimStart().TrimEnd(), worksheet3.Cells[row, 17].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingSV.DueDate.ToString("yyyy-MM-dd").TrimStart().TrimEnd() != worksheet3.Cells[row, 1].Text.TrimStart().TrimEnd())
                                {
                                    svChanges["DueDate"] = (existingSV.DueDate.ToString("yyyy-MM-dd").TrimStart().TrimEnd(), worksheet3.Cells[row, 1].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingSV.Period.ToString("yyyy-MM-dd").TrimStart().TrimEnd() != worksheet3.Cells[row, 2].Text.TrimStart().TrimEnd())
                                {
                                    svChanges["Period"] = (existingSV.Period.ToString("yyyy-MM-dd").TrimStart().TrimEnd(), worksheet3.Cells[row, 2].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingSV.Amount.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet3.Cells[row, 3].Text).ToString("F2").TrimStart().TrimEnd())
                                {
                                    svChanges["Amount"] = (existingSV.Amount.ToString("F2").TrimStart().TrimEnd(), decimal.Parse(worksheet3.Cells[row, 3].Text).ToString("F2").TrimStart().TrimEnd());
                                }

                                if (existingSV.Total.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet3.Cells[row, 4].Text).ToString("F2").TrimStart().TrimEnd())
                                {
                                    svChanges["Total"] = (existingSV.Total.ToString("F2").TrimStart().TrimEnd(), decimal.Parse(worksheet3.Cells[row, 4].Text).ToString("F2").TrimStart().TrimEnd());
                                }

                                if (existingSV.Discount.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet3.Cells[row, 5].Text).ToString("F2").TrimStart().TrimEnd())
                                {
                                    svChanges["Discount"] = (existingSV.Discount.ToString("F2").TrimStart().TrimEnd(), decimal.Parse(worksheet3.Cells[row, 5].Text).ToString("F2").TrimStart().TrimEnd());
                                }

                                if (existingSV.CurrentAndPreviousAmount.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet3.Cells[row, 6].Text).ToString("F2").TrimStart().TrimEnd())
                                {
                                    svChanges["CurrentAndPreviousAmount"] = (existingSV.CurrentAndPreviousAmount.ToString("F2").TrimStart().TrimEnd(), decimal.Parse(worksheet3.Cells[row, 6].Text).ToString("F2").TrimStart().TrimEnd());
                                }

                                if (existingSV.UnearnedAmount.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet3.Cells[row, 7].Text).ToString("F2").TrimStart().TrimEnd())
                                {
                                    svChanges["UnearnedAmount"] = (existingSV.UnearnedAmount.ToString("F2").TrimStart().TrimEnd(), decimal.Parse(worksheet3.Cells[row, 7].Text).ToString("F2").TrimStart().TrimEnd());
                                }

                                if (existingSV.Status.TrimStart().TrimEnd() != worksheet3.Cells[row, 8].Text.TrimStart().TrimEnd())
                                {
                                    svChanges["Status"] = (existingSV.Status.TrimStart().TrimEnd(), worksheet3.Cells[row, 8].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingSV.Instructions.TrimStart().TrimEnd() != worksheet3.Cells[row, 11].Text.TrimStart().TrimEnd())
                                {
                                    svChanges["Instructions"] = (existingSV.Instructions.TrimStart().TrimEnd(), worksheet3.Cells[row, 11].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingSV.CreatedBy.TrimStart().TrimEnd() != worksheet3.Cells[row, 13].Text.TrimStart().TrimEnd())
                                {
                                    svChanges["CreatedBy"] = (existingSV.CreatedBy.TrimStart().TrimEnd(), worksheet3.Cells[row, 13].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingSV.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff").TrimStart().TrimEnd() != worksheet3.Cells[row, 14].Text.TrimStart().TrimEnd())
                                {
                                    svChanges["CreatedDate"] = (existingSV.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff").TrimStart().TrimEnd(), worksheet3.Cells[row, 14].Text.TrimStart().TrimEnd())!;
                                }

                                if ((string.IsNullOrWhiteSpace(existingSV.CancellationRemarks?.TrimStart().TrimEnd()) ? "" : existingSV.CancellationRemarks.TrimStart().TrimEnd()) != worksheet3.Cells[row, 15].Text.TrimStart().TrimEnd())
                                {
                                    svChanges["CancellationRemarks"] = (existingSV.CancellationRemarks?.TrimStart().TrimEnd(), worksheet3.Cells[row, 15].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingSV.OriginalCustomerId.ToString().TrimStart().TrimEnd() != worksheet3.Cells[row, 16].Text.TrimStart().TrimEnd())
                                {
                                    svChanges["OriginalCustomerId"] = (existingSV.OriginalCustomerId.ToString().TrimStart().TrimEnd(), worksheet3.Cells[row, 16].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingSV.OriginalSeriesNumber.TrimStart().TrimEnd() != worksheet3.Cells[row, 17].Text.TrimStart().TrimEnd())
                                {
                                    svChanges["OriginalSeriesNumber"] = (existingSV.OriginalSeriesNumber.TrimStart().TrimEnd(), worksheet3.Cells[row, 17].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingSV.OriginalServicesId.ToString().TrimStart().TrimEnd() != worksheet3.Cells[row, 18].Text.TrimStart().TrimEnd())
                                {
                                    svChanges["OriginalServicesId"] = (existingSV.OriginalServicesId.ToString().TrimStart().TrimEnd(), worksheet3.Cells[row, 18].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingSV.OriginalDocumentId.ToString().TrimStart().TrimEnd() != worksheet3.Cells[row, 19].Text.TrimStart().TrimEnd())
                                {
                                    svChanges["OriginalDocumentId"] = (existingSV.OriginalDocumentId.ToString().TrimStart().TrimEnd(), worksheet3.Cells[row, 19].Text.TrimStart().TrimEnd())!;
                                }

                                if (svChanges.Any())
                                {
                                    await _serviceInvoiceRepo.LogChangesAsync(existingSV.OriginalDocumentId, svChanges, _userManager.GetUserName(this.User));
                                }

                                continue;
                            }

                            serviceInvoice.CustomerId = await _dbContext.Customers
                                .Where(sv => sv.OriginalCustomerId == serviceInvoice.OriginalCustomerId)
                                .Select(sv => (int?)sv.Id)
                                .FirstOrDefaultAsync(cancellationToken) ?? throw new InvalidOperationException("Please upload the Excel file for the customer master file first.");

                            serviceInvoice.ServicesId = await _dbContext.Services
                                .Where(sv => sv.OriginalServiceId == serviceInvoice.OriginalServicesId)
                                .Select(sv => (int?)sv.Id)
                                .FirstOrDefaultAsync(cancellationToken) ?? throw new InvalidOperationException("Please upload the Excel file for the service master file first.");

                            await _dbContext.ServiceInvoices.AddAsync(serviceInvoice, cancellationToken);
                        }
                        await _dbContext.SaveChangesAsync(cancellationToken);

                        #endregion -- Service Invoice Import --

                        #region -- Debit Memo Import --

                        var rowCount = worksheet.Dimension.Rows;
                        var dmDictionary = new Dictionary<string, bool>();
                        var debitMemoList = await _dbContext
                            .DebitMemos
                            .ToListAsync(cancellationToken);

                        for (int row = 2; row <= rowCount; row++) // Assuming the first row is the header
                        {
                            var debitMemo = new DebitMemo
                            {
                                DMNo = worksheet.Cells[row, 17].Text,
                                TransactionDate =
                                    DateOnly.TryParse(worksheet.Cells[row, 1].Text, out DateOnly transactionDate)
                                        ? transactionDate
                                        : default,
                                DebitAmount = decimal.TryParse(worksheet.Cells[row, 2].Text, out decimal debitAmount)
                                    ? debitAmount
                                    : 0,
                                Description = worksheet.Cells[row, 3].Text,
                                AdjustedPrice =
                                    decimal.TryParse(worksheet.Cells[row, 4].Text, out decimal adjustedPrice)
                                        ? adjustedPrice
                                        : 0,
                                Quantity = decimal.TryParse(worksheet.Cells[row, 5].Text, out decimal quantity)
                                    ? quantity
                                    : 0,
                                Source = worksheet.Cells[row, 6].Text,
                                Remarks = worksheet.Cells[row, 7].Text,
                                Period = DateOnly.TryParse(worksheet.Cells[row, 8].Text, out DateOnly period)
                                    ? period
                                    : default,
                                Amount =
                                    decimal.TryParse(worksheet.Cells[row, 9].Text, out decimal amount) ? amount : 0,
                                CurrentAndPreviousAmount =
                                    decimal.TryParse(worksheet.Cells[row, 10].Text,
                                        out decimal currentAndPreviousAmount)
                                        ? currentAndPreviousAmount
                                        : 0,
                                UnearnedAmount =
                                    decimal.TryParse(worksheet.Cells[row, 11].Text, out decimal unearnedAmount)
                                        ? unearnedAmount
                                        : 0,
                                ServicesId = int.TryParse(worksheet.Cells[row, 12].Text, out int servicesId)
                                    ? servicesId
                                    : 0,
                                CreatedBy = worksheet.Cells[row, 13].Text,
                                CreatedDate = DateTime.TryParse(worksheet.Cells[row, 14].Text, out DateTime createdDate)
                                    ? createdDate
                                    : default,
                                CancellationRemarks = worksheet.Cells[row, 15].Text,
                                OriginalSalesInvoiceId =
                                    int.TryParse(worksheet.Cells[row, 16].Text, out int originalSalesInvoiceId)
                                        ? originalSalesInvoiceId
                                        : 0,
                                OriginalSeriesNumber = worksheet.Cells[row, 17].Text,
                                OriginalServiceInvoiceId =
                                    int.TryParse(worksheet.Cells[row, 18].Text, out int originalServiceInvoiceId)
                                        ? originalServiceInvoiceId
                                        : 0,
                                OriginalDocumentId =
                                    int.TryParse(worksheet.Cells[row, 19].Text, out int originalDocumentId)
                                        ? originalDocumentId
                                        : 0,
                            };

                            if (!dmDictionary.TryAdd(debitMemo.OriginalSeriesNumber, true))
                            {
                                continue;
                            }

                            if (debitMemoList.Any(dm => dm.OriginalDocumentId == debitMemo.OriginalDocumentId))
                            {
                                var dmChanges = new Dictionary<string, (string OriginalValue, string NewValue)>();
                                var existingDM = await _dbContext.DebitMemos.FirstOrDefaultAsync(si => si.OriginalDocumentId == debitMemo.OriginalDocumentId, cancellationToken);

                                if (existingDM.DMNo.TrimStart().TrimEnd() != worksheet.Cells[row, 17].Text.TrimStart().TrimEnd())
                                {
                                    dmChanges["DMNo"] = (existingDM.DMNo.TrimStart().TrimEnd(), worksheet.Cells[row, 17].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingDM.TransactionDate.ToString("yyyy-MM-dd").TrimStart().TrimEnd() != worksheet.Cells[row, 1].Text.TrimStart().TrimEnd())
                                {
                                    dmChanges["TransactionDate"] = (existingDM.TransactionDate.ToString("yyyy-MM-dd").TrimStart().TrimEnd(), worksheet.Cells[row, 1].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingDM.DebitAmount.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet.Cells[row, 2].Text).ToString("F2").TrimStart().TrimEnd())
                                {
                                    dmChanges["DebitAmount"] = (existingDM.DebitAmount.ToString("F2").TrimStart().TrimEnd(), decimal.Parse(worksheet.Cells[row, 2].Text).ToString("F2").TrimStart().TrimEnd())!;
                                }

                                if (existingDM.Description.TrimStart().TrimEnd() != worksheet.Cells[row, 3].Text.TrimStart().TrimEnd())
                                {
                                    dmChanges["Description"] = (existingDM.Description.TrimStart().TrimEnd(), worksheet.Cells[row, 3].Text.TrimStart().TrimEnd())!;
                                }

                                if ((existingDM.AdjustedPrice != null ? existingDM.AdjustedPrice?.ToString("F2").TrimStart().TrimEnd() : 0.ToString("F2")) != decimal.Parse(worksheet.Cells[row, 4].Text.TrimStart().TrimEnd() != "" ? worksheet.Cells[row, 4].Text.TrimStart().TrimEnd() : 0.ToString("F2")).ToString("F2").TrimStart().TrimEnd())
                                {
                                    dmChanges["AdjustedPrice"] = (existingDM.AdjustedPrice?.ToString("F2").TrimStart().TrimEnd(), decimal.Parse(worksheet.Cells[row, 4].Text.TrimStart().TrimEnd() != "" ? worksheet.Cells[row, 4].Text.TrimStart().TrimEnd() : 0.ToString("F2")).ToString("F2").TrimStart().TrimEnd())!;
                                }

                                if ((existingDM.Quantity != null ? existingDM.Quantity?.ToString("F0").TrimStart().TrimEnd() : 0.ToString("F0")) != decimal.Parse(worksheet.Cells[row, 5].Text.TrimStart().TrimEnd() != "" ? worksheet.Cells[row, 5].Text.TrimStart().TrimEnd() : 0.ToString("F0")).ToString("F0").TrimStart().TrimEnd())
                                {
                                    dmChanges["Quantity"] = (existingDM.Quantity?.ToString("F0").TrimStart().TrimEnd(), decimal.Parse(worksheet.Cells[row, 5].Text.TrimStart().TrimEnd() != "" ? worksheet.Cells[row, 5].Text.TrimStart().TrimEnd() : 0.ToString("F0")).ToString("F0").TrimStart().TrimEnd())!;
                                }

                                if (existingDM.Source.TrimStart().TrimEnd() != worksheet.Cells[row, 6].Text.TrimStart().TrimEnd())
                                {
                                    dmChanges["Source"] = (existingDM.Source.TrimStart().TrimEnd(), worksheet.Cells[row, 6].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingDM.Remarks.TrimStart().TrimEnd() != worksheet.Cells[row, 7].Text.TrimStart().TrimEnd())
                                {
                                    dmChanges["Remarks"] = (existingDM.Remarks.TrimStart().TrimEnd(), worksheet.Cells[row, 7].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingDM.Period.ToString("yyyy-MM-dd").TrimStart().TrimEnd() != DateOnly.Parse(worksheet.Cells[row, 8].Text).ToString("yyyy-MM-dd").TrimStart().TrimEnd())
                                {
                                    dmChanges["Period"] = (existingDM.Period.ToString("yyyy-MM-dd").TrimStart().TrimEnd(), worksheet.Cells[row, 8].Text.TrimStart().TrimEnd())!;
                                }

                                if ((existingDM.Amount != null ? existingDM.Amount?.ToString("F2").TrimStart().TrimEnd() : 0.ToString("F2")) != decimal.Parse(worksheet.Cells[row, 9].Text.TrimStart().TrimEnd() != "" ? worksheet.Cells[row, 9].Text.TrimStart().TrimEnd() : 0.ToString("F2")).ToString("F2").TrimStart().TrimEnd())
                                {
                                    dmChanges["Amount"] = (existingDM.Amount?.ToString("F2").TrimStart().TrimEnd(), decimal.Parse(worksheet.Cells[row, 9].Text).ToString("F2").TrimStart().TrimEnd())!;
                                }

                                if (existingDM.CurrentAndPreviousAmount.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet.Cells[row, 10].Text).ToString("F2").TrimStart().TrimEnd())
                                {
                                    dmChanges["CurrentAndPreviousAmount"] = (existingDM.CurrentAndPreviousAmount.ToString("F2").TrimStart().TrimEnd(), decimal.Parse(worksheet.Cells[row, 10].Text).ToString("F2").TrimStart().TrimEnd())!;
                                }

                                if (existingDM.UnearnedAmount.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet.Cells[row, 11].Text).ToString("F2").TrimStart().TrimEnd())
                                {
                                    dmChanges["UnearnedAmount"] = (existingDM.UnearnedAmount.ToString("F2").TrimStart().TrimEnd(), decimal.Parse(worksheet.Cells[row, 11].Text).ToString("F2").TrimStart().TrimEnd())!;
                                }

                                if (existingDM.ServicesId.ToString().TrimStart().TrimEnd() != (worksheet.Cells[row, 12].Text.TrimStart().TrimEnd() == "" ? 0.ToString() : worksheet.Cells[row, 12].Text.TrimStart().TrimEnd()))
                                {
                                    dmChanges["ServicesId"] = (existingDM.ServicesId.ToString().TrimStart().TrimEnd(), worksheet.Cells[row, 12].Text.TrimStart().TrimEnd() == "" ? 0.ToString() : worksheet.Cells[row, 12].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingDM.CreatedBy.TrimStart().TrimEnd() != worksheet.Cells[row, 13].Text.TrimStart().TrimEnd())
                                {
                                    dmChanges["CreatedBy"] = (existingDM.CreatedBy.TrimStart().TrimEnd(), worksheet.Cells[row, 13].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingDM.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff").TrimStart().TrimEnd() != worksheet.Cells[row, 14].Text.TrimStart().TrimEnd())
                                {
                                    dmChanges["CreatedDate"] = (existingDM.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff").TrimStart().TrimEnd(), worksheet.Cells[row, 14].Text.TrimStart().TrimEnd())!;
                                }

                                if ((string.IsNullOrWhiteSpace(existingDM.CancellationRemarks?.TrimStart().TrimEnd()) ? "" : existingDM.CancellationRemarks.TrimStart().TrimEnd()) != worksheet.Cells[row, 15].Text.TrimStart().TrimEnd())
                                {
                                    dmChanges["CancellationRemarks"] = (existingDM.CancellationRemarks?.TrimStart().TrimEnd(), worksheet.Cells[row, 15].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingDM.OriginalSalesInvoiceId.ToString().TrimStart().TrimEnd() != (worksheet.Cells[row, 16].Text.TrimStart().TrimEnd() == "" ? 0.ToString() : worksheet.Cells[row, 16].Text.TrimStart().TrimEnd()))
                                {
                                    dmChanges["OriginalSalesInvoiceId"] = (existingDM.OriginalSalesInvoiceId.ToString().TrimStart().TrimEnd(), worksheet.Cells[row, 16].Text.TrimStart().TrimEnd() == "" ? 0.ToString() : worksheet.Cells[row, 16].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingDM.OriginalSeriesNumber.TrimStart().TrimEnd() != worksheet.Cells[row, 17].Text.TrimStart().TrimEnd())
                                {
                                    dmChanges["OriginalSeriesNumber"] = (existingDM.OriginalSeriesNumber.TrimStart().TrimEnd(), worksheet.Cells[row, 17].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingDM.OriginalServiceInvoiceId.ToString().TrimStart().TrimEnd() != (worksheet.Cells[row, 18].Text.TrimStart().TrimEnd() == "" ? 0.ToString() : worksheet.Cells[row, 18].Text.TrimStart().TrimEnd()))
                                {
                                    dmChanges["OriginalServiceInvoiceId"] = (existingDM.OriginalServiceInvoiceId.ToString().TrimStart().TrimEnd(), worksheet.Cells[row, 18].Text.TrimStart().TrimEnd() == "" ? 0.ToString() : worksheet.Cells[row, 18].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingDM.OriginalDocumentId.ToString().TrimStart().TrimEnd() != (worksheet.Cells[row, 19].Text.TrimStart().TrimEnd() == "" ? 0.ToString() : worksheet.Cells[row, 19].Text.TrimStart().TrimEnd()))
                                {
                                    dmChanges["OriginalDocumentId"] = (existingDM.OriginalDocumentId.ToString().TrimStart().TrimEnd(), worksheet.Cells[row, 19].Text.TrimStart().TrimEnd() == "" ? 0.ToString() : worksheet.Cells[row, 19].Text.TrimStart().TrimEnd())!;
                                }

                                if (dmChanges.Any())
                                {
                                    await _debitMemoRepo.LogChangesAsync(existingDM.OriginalDocumentId, dmChanges, _userManager.GetUserName(this.User));
                                }

                                continue;
                            }

                            debitMemo.SalesInvoiceId = await _dbContext.SalesInvoices
                                .Where(c => c.OriginalDocumentId == debitMemo.OriginalSalesInvoiceId)
                                .Select(c => (int?)c.Id)
                                .FirstOrDefaultAsync(cancellationToken);

                            debitMemo.ServiceInvoiceId = await _dbContext.ServiceInvoices
                                .Where(c => c.OriginalDocumentId == debitMemo.OriginalServiceInvoiceId)
                                .Select(c => (int?)c.Id)
                                .FirstOrDefaultAsync(cancellationToken);

                            if (debitMemo.SalesInvoiceId == null && debitMemo.ServiceInvoiceId == null)
                            {
                                throw new InvalidOperationException(
                                    "Please upload the Excel file for the sales invoice or service invoice first.");
                            }

                            await _dbContext.DebitMemos.AddAsync(debitMemo, cancellationToken);
                        }
                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);

                        var checkChangesOfRecord = await _dbContext.ImportExportLogs
                            .Where(iel => iel.Action == string.Empty).ToListAsync(cancellationToken);
                        if (checkChangesOfRecord.Any())
                        {
                            TempData["importChanges"] = "";
                        }
                        #endregion -- Debit Memo Import --
                    }
                }
                catch (OperationCanceledException oce)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = oce.Message;
                    return RedirectToAction(nameof(Index), new { view = DynamicView.DebitMemo });
                }
                catch (InvalidOperationException ioe)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["warning"] = ioe.Message;
                    return RedirectToAction(nameof(Index), new { view = DynamicView.DebitMemo });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(Index), new { view = DynamicView.DebitMemo });
                }
            }
            TempData["success"] = "Uploading Success!";
            return RedirectToAction(nameof(Index), new { view = DynamicView.DebitMemo });
        }

        #endregion -- import xlsx record --
    }
}
