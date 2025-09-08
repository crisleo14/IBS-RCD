using System.Globalization;
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
using System.Linq.Dynamic.Core;
using Microsoft.IdentityModel.Tokens;

namespace Accounting_System.Controllers
{
    [Authorize]
    public class CreditMemoController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly AasDbContext _aasDbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly CreditMemoRepo _creditMemoRepo;

        private readonly SalesInvoiceRepo _salesInvoiceRepo;

        private readonly ServiceInvoiceRepo _serviceInvoiceRepo;

        private readonly GeneralRepo _generalRepo;

        public CreditMemoController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, CreditMemoRepo creditMemoRepo, GeneralRepo generalRepo, SalesInvoiceRepo salesInvoiceRepo, ServiceInvoiceRepo serviceInvoiceRepo, AasDbContext aasDbContext)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _creditMemoRepo = creditMemoRepo;
            _generalRepo = generalRepo;
            _salesInvoiceRepo = salesInvoiceRepo;
            _serviceInvoiceRepo = serviceInvoiceRepo;
            _aasDbContext = aasDbContext;
        }

        public async Task<IActionResult> Index(string? view, CancellationToken cancellationToken)
        {
            var creditMemos = await _creditMemoRepo.GetCreditMemosAsync(cancellationToken);

            if (view == nameof(DynamicView.CreditMemo))
            {
                return View("ImportExportIndex", creditMemos);
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetCreditMemos([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var creditMemos = await _creditMemoRepo.GetCreditMemosAsync(cancellationToken);
                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();
                    creditMemos = creditMemos
                        .Where(cm =>
                            cm.CreditMemoNo!.ToLower().Contains(searchValue) ||
                            cm.TransactionDate.ToString("MMM dd, yyyy").ToLower().Contains(searchValue) ||
                            cm.SalesInvoice?.SalesInvoiceNo?.ToLower().Contains(searchValue) == true ||
                            cm.ServiceInvoice?.ServiceInvoiceNo?.ToLower().Contains(searchValue) == true ||
                            cm.Source.ToLower().Contains(searchValue) ||
                            cm.CreditAmount.ToString(CultureInfo.InvariantCulture).Contains(searchValue) ||
                            cm.Remarks?.ToLower().Contains(searchValue) == true ||
                            cm.Description.ToLower().Contains(searchValue) ||
                            cm.CreatedBy!.ToLower().Contains(searchValue)
                            )
                        .ToList();
                }
                // Sorting
                if (parameters.Order != null && parameters.Order.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Data;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";
                    creditMemos = creditMemos
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}")
                        .ToList();
                }
                var totalRecords = creditMemos.Count();
                var pagedData = creditMemos
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
        public async Task<IActionResult> GetAllCreditMemoIds(CancellationToken cancellationToken)
        {
            var creditMemoIds = await _dbContext.CreditMemos
                                     .Select(cm => cm.CreditMemoId) // Assuming Id is the primary key
                                     .ToListAsync(cancellationToken);
            return Json(creditMemoIds);
        }

        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var viewModel = new CreditMemo
            {
                SalesInvoices = await _dbContext.SalesInvoices
                    .Where(si => si.IsPosted)
                    .Select(si => new SelectListItem
                    {
                        Value = si.SalesInvoiceId.ToString(),
                        Text = si.SalesInvoiceNo
                    })
                    .ToListAsync(cancellationToken),
                ServiceInvoices = await _dbContext.ServiceInvoices
                    .Where(sv => sv.IsPosted)
                    .Select(sv => new SelectListItem
                    {
                        Value = sv.ServiceInvoiceId.ToString(),
                        Text = sv.ServiceInvoiceNo
                    })
                    .ToListAsync(cancellationToken)
            };

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
                    Value = si.SalesInvoiceId.ToString(),
                    Text = si.SalesInvoiceNo
                })
                .ToListAsync(cancellationToken);
            model.ServiceInvoices = await _dbContext.ServiceInvoices
                .Where(sv => sv.IsPosted)
                .Select(sv => new SelectListItem
                {
                    Value = sv.ServiceInvoiceId.ToString(),
                    Text = sv.ServiceInvoiceNo
                })
                .ToListAsync(cancellationToken);

            var existingSv = await _dbContext.ServiceInvoices
                        .Include(sv => sv.Customer)
                        .FirstOrDefaultAsync(sv => sv.ServiceInvoiceId == model.ServiceInvoiceId, cancellationToken);

            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    #region -- checking for unposted DM or CM --

                        var existingSiDms = await _dbContext.DebitMemos
                                      .Where(dm => !dm.IsPosted && !dm.IsCanceled && !dm.IsVoided)
                                      .OrderBy(cm => cm.DebitMemoId)
                                      .ToListAsync(cancellationToken);
                        if (existingSiDms.Count > 0)
                        {
                            var dmNo = new List<string>();
                            foreach (var item in existingSiDms)
                            {
                                dmNo.Add(item.DebitMemoNo!);
                            }
                            ModelState.AddModelError("", $"Can’t proceed to create you have unposted DM/CM. {string.Join(" , ", dmNo)}");
                            return View(model);
                        }

                        var existingSiCms = await _dbContext.CreditMemos
                                          .Where(cm => !cm.IsPosted && !cm.IsCanceled && !cm.IsVoided)
                                          .OrderBy(cm => cm.CreditMemoId)
                                          .ToListAsync(cancellationToken);
                        if (existingSiCms.Count > 0)
                        {
                            var cmNo = new List<string>();
                            foreach (var item in existingSiCms)
                            {
                                cmNo.Add(item.CreditMemoNo!);
                            }
                            ModelState.AddModelError("", $"Can’t proceed to create you have unposted DM/CM. {string.Join(" , ", cmNo)}");
                            return View(model);
                        }

                    #endregion

                    #region --Validating the series--

                    var generatedCm = await _creditMemoRepo.GenerateCMNo(cancellationToken);
                    var getLastNumber = long.Parse(generatedCm.Substring(2));

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

                    model.CreditMemoNo = generatedCm;
                    model.CreatedBy = _userManager.GetUserName(this.User);

                    if (model.Source == "Sales Invoice")
                    {
                        model.ServiceInvoiceId = null;

                        model.CreditAmount = (decimal)(model.Quantity * -model.AdjustedPrice)!;
                    }
                    else if (model.Source == "Service Invoice")
                    {
                        model.SalesInvoiceId = null;

                        #region --Retrieval of Services

                        model.ServicesId = existingSv?.ServicesId;

                        await _dbContext
                            .Services
                            .FirstOrDefaultAsync(s => s.ServiceId == model.ServicesId, cancellationToken);

                        #endregion --Retrieval of Services

                        model.CreditAmount = -model.Amount ?? 0;
                    }

                    #region --Audit Trail Recording

                    if (model.OriginalSeriesNumber.IsNullOrEmpty() && model.OriginalDocumentId == 0)
                    {
                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        AuditTrail auditTrailBook = new(model.CreatedBy!, $"Create new credit memo# {model.CreditMemoNo}", "Credit Memo", ipAddress!);
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                    }

                    #endregion --Audit Trail Recording

                    await _dbContext.AddAsync(model, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                 await transaction.RollbackAsync(cancellationToken);
                 TempData["error"] = ex.Message;
                 return RedirectToAction(nameof(Index));
                }
            }

            ModelState.AddModelError("", "The information you submitted is not valid!");
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
            if (id == null || !_dbContext.CreditMemos.Any())
            {
                return NotFound();
            }

            var creditMemo = await _dbContext.CreditMemos
                .Include(cm => cm.SalesInvoice)
                .Include(cm => cm.ServiceInvoice)
                .ThenInclude(sv => sv!.Customer)
                .Include(cm => cm.ServiceInvoice)
                .ThenInclude(sv => sv!.Service)
                .FirstOrDefaultAsync(r => r.CreditMemoId == id, cancellationToken);

            if (creditMemo == null)
            {
                return NotFound();
            }

            creditMemo.SalesInvoices = await _dbContext.SalesInvoices
                .Where(si => si.IsPosted)
                .Select(si => new SelectListItem
                {
                    Value = si.SalesInvoiceId.ToString(),
                    Text = si.SalesInvoiceNo
                })
                .ToListAsync(cancellationToken);
            creditMemo.ServiceInvoices = await _dbContext.ServiceInvoices
                .Where(sv => sv.IsPosted)
                .Select(sv => new SelectListItem
                {
                    Value = sv.ServiceInvoiceId.ToString(),
                    Text = sv.ServiceInvoiceNo
                })
                .ToListAsync(cancellationToken);


            return View(creditMemo);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(CreditMemo model, CancellationToken cancellationToken)
        {
            var existingSv = await _dbContext.ServiceInvoices
                        .Include(sv => sv.Customer)
                        .FirstOrDefaultAsync(sv => sv.ServiceInvoiceId == model.ServiceInvoiceId, cancellationToken);
            var existingCm = await _dbContext
                .CreditMemos
                .FirstOrDefaultAsync(cm => cm.CreditMemoId == model.CreditMemoId, cancellationToken);

            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    if (model.Source == "Sales Invoice")
                    {
                        model.ServiceInvoiceId = null;

                        #region -- Saving Default Enries --

                        existingCm!.TransactionDate = model.TransactionDate;
                        existingCm.SalesInvoiceId = model.SalesInvoiceId;
                        existingCm.Quantity = model.Quantity;
                        existingCm.AdjustedPrice = model.AdjustedPrice;
                        existingCm.Description = model.Description;
                        existingCm.Remarks = model.Remarks;
                        existingCm.CreditAmount = (decimal)(model.Quantity * -model.AdjustedPrice)!;

                        #endregion -- Saving Default Enries --

                    }
                    else if (model.Source == "Service Invoice")
                    {
                        model.SalesInvoiceId = null;

                        #region --Retrieval of Services

                        await _dbContext
                            .Services
                            .FirstOrDefaultAsync(s => s.ServiceId == existingCm!.ServicesId, cancellationToken);

                        #endregion --Retrieval of Services

                        #region -- Saving Default Enries --

                        existingCm!.TransactionDate = model.TransactionDate;
                        existingCm.ServiceInvoiceId = model.ServiceInvoiceId;
                        existingCm.ServicesId = existingSv!.ServicesId;
                        existingCm.Period = model.Period;
                        existingCm.Amount = model.Amount;
                        existingCm.Description = model.Description;
                        existingCm.Remarks = model.Remarks;
                        existingCm.CreditAmount = -model.Amount ?? 0;

                        #endregion -- Saving Default Enries --

                    }

                    if (_dbContext.ChangeTracker.HasChanges())
                    {
                        #region --Audit Trail Recording

                        if (existingCm!.OriginalSeriesNumber.IsNullOrEmpty() && existingCm.OriginalDocumentId == 0)
                        {
                            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                            AuditTrail auditTrailBook = new(User.Identity!.Name!, $"Edit credit memo# {existingCm.CreditMemoNo}", "Credit Memo", ipAddress!);
                            await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                        }

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Credit Memo edited successfully";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        throw new InvalidOperationException("No data changes!");
                    }
                }
                catch (Exception ex)
                {
                 await transaction.RollbackAsync(cancellationToken);
                 existingCm!.SalesInvoices = await _dbContext.SalesInvoices
                     .Where(si => si.IsPosted)
                     .Select(si => new SelectListItem
                     {
                         Value = si.SalesInvoiceId.ToString(),
                         Text = si.SalesInvoiceNo
                     })
                     .ToListAsync(cancellationToken);
                 existingCm.ServiceInvoices = await _dbContext.ServiceInvoices
                     .Where(sv => sv.IsPosted)
                     .Select(sv => new SelectListItem
                     {
                         Value = sv.ServiceInvoiceId.ToString(),
                         Text = sv.ServiceInvoiceNo
                     })
                     .ToListAsync(cancellationToken);
                 TempData["error"] = ex.Message;
                 return View(existingCm);
                }
            }

            ModelState.AddModelError("", "The information you submitted is not valid!");
            existingCm!.SalesInvoices = await _dbContext.SalesInvoices
                .Where(si => si.IsPosted)
                .Select(si => new SelectListItem
                {
                    Value = si.SalesInvoiceId.ToString(),
                    Text = si.SalesInvoiceNo
                })
                .ToListAsync(cancellationToken);
            existingCm.ServiceInvoices = await _dbContext.ServiceInvoices
                .Where(sv => sv.IsPosted)
                .Select(sv => new SelectListItem
                {
                    Value = sv.ServiceInvoiceId.ToString(),
                    Text = sv.ServiceInvoiceNo
                })
                .ToListAsync(cancellationToken);
            return View(existingCm);
        }

        [HttpGet]
        public async Task<IActionResult> Print(int? id, CancellationToken cancellationToken)
        {
            if (id == null || !_dbContext.CreditMemos.Any())
            {
                return NotFound();
            }

            var creditMemo = await _dbContext.CreditMemos
                .Include(cm => cm.SalesInvoice)
                .ThenInclude(s => s!.Customer)
                .Include(cm => cm.SalesInvoice)
                .ThenInclude(s => s!.Product)
                .Include(cm => cm.ServiceInvoice)
                .ThenInclude(sv => sv!.Customer)
                .Include(cm => cm.ServiceInvoice)
                .ThenInclude(sv => sv!.Service)
                .FirstOrDefaultAsync(r => r.CreditMemoId == id, cancellationToken);
            if (creditMemo == null)
            {
                return NotFound();
            }

            return View(creditMemo);
        }

        public async Task<IActionResult> Printed(int id, CancellationToken cancellationToken)
        {
            var cm = await _dbContext.CreditMemos.FirstOrDefaultAsync(x => x.CreditMemoId == id, cancellationToken);
            if (cm != null && !cm.IsPrinted)
            {

                #region --Audit Trail Recording

                if (cm.OriginalSeriesNumber.IsNullOrEmpty() && cm.OriginalDocumentId == 0)
                {
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    var printedBy = _userManager.GetUserName(this.User);
                    AuditTrail auditTrailBook = new(printedBy!, $"Printed original copy of cm# {cm.CreditMemoNo}", "Credit Memo", ipAddress!);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                }

                #endregion --Audit Trail Recording

                cm.IsPrinted = true;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            return RedirectToAction(nameof(Print), new { id });
        }

        public async Task<IActionResult> Post(int id, CancellationToken cancellationToken, ViewModelDMCM viewModelDmcm)
        {
            var model = await _creditMemoRepo.FindCM(id, cancellationToken);

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
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
                    var (salesAcctNo, _) = _generalRepo.GetSalesAccountTitle(model.SalesInvoice!.Product!.ProductCode!);
                    var salesTitle = accountTitlesDto.Find(c => c.AccountNumber == salesAcctNo) ?? throw new ArgumentException($"Account title '{salesAcctNo}' not found.");
                    var vatOutputTitle = accountTitlesDto.Find(c => c.AccountNumber == "201030100") ?? throw new ArgumentException("Account number: '201030100', Account title: 'Vat - Output' not found.");


                    if (model.SalesInvoiceId != null)
                    {
                        #region --Retrieval of SI and SOA--

                        var existingSi = await _dbContext.SalesInvoices
                                                    .Include(s => s.Customer)
                                                    .Include(s => s.Product)
                                                    .FirstOrDefaultAsync(si => si.SalesInvoiceId == model.SalesInvoiceId, cancellationToken);

                        #endregion --Retrieval of SI and SOA--

                        #region --Sales Book Recording(SI)--

                        var sales = new SalesBook();

                        if (model.SalesInvoice.Customer!.CustomerType == "Vatable")
                        {
                            sales.TransactionDate = model.TransactionDate;
                            sales.SerialNo = model.CreditMemoNo!;
                            sales.SoldTo = model.SalesInvoice.Customer.CustomerName;
                            sales.TinNo = model.SalesInvoice.Customer.CustomerTin;
                            sales.Address = model.SalesInvoice.Customer.CustomerAddress;
                            sales.Description = model.SalesInvoice.Product.ProductName;
                            sales.Amount = model.CreditAmount;
                            sales.VatableSales = (_generalRepo.ComputeNetOfVat(Math.Abs(sales.Amount))) * -1;
                            sales.VatAmount = (_generalRepo.ComputeVatAmount(Math.Abs(sales.VatableSales))) * -1;
                            //sales.Discount = model.Discount;
                            sales.NetSales = sales.VatableSales;
                            sales.CreatedBy = model.CreatedBy;
                            sales.CreatedDate = model.CreatedDate;
                            sales.DueDate = existingSi!.DueDate;
                            sales.DocumentId = model.SalesInvoiceId;
                        }
                        else if (model.SalesInvoice.Customer.CustomerType == "Exempt")
                        {
                            sales.TransactionDate = model.TransactionDate;
                            sales.SerialNo = model.CreditMemoNo!;
                            sales.SoldTo = model.SalesInvoice.Customer.CustomerName;
                            sales.TinNo = model.SalesInvoice.Customer.CustomerTin;
                            sales.Address = model.SalesInvoice.Customer.CustomerAddress;
                            sales.Description = model.SalesInvoice.Product.ProductName;
                            sales.Amount = model.CreditAmount;
                            sales.VatExemptSales = model.CreditAmount;
                            //sales.Discount = model.Discount;
                            sales.NetSales = sales.Amount;
                            sales.CreatedBy = model.CreatedBy;
                            sales.CreatedDate = model.CreatedDate;
                            sales.DueDate = existingSi!.DueDate;
                            sales.DocumentId = model.SalesInvoiceId;
                        }
                        else
                        {
                            sales.TransactionDate = model.TransactionDate;
                            sales.SerialNo = model.CreditMemoNo!;
                            sales.SoldTo = model.SalesInvoice.Customer.CustomerName;
                            sales.TinNo = model.SalesInvoice.Customer.CustomerTin;
                            sales.Address = model.SalesInvoice.Customer.CustomerAddress;
                            sales.Description = model.SalesInvoice.Product.ProductName;
                            sales.Amount = model.CreditAmount;
                            sales.ZeroRated = model.CreditAmount;
                            //sales.Discount = model.Discount;
                            sales.NetSales = sales.Amount;
                            sales.CreatedBy = model.CreatedBy;
                            sales.CreatedDate = model.CreatedDate;
                            sales.DueDate = existingSi!.DueDate;
                            sales.DocumentId = model.SalesInvoiceId;
                        }
                        await _dbContext.AddAsync(sales, cancellationToken);

                        #endregion --Sales Book Recording(SI)--

                        #region --General Ledger Book Recording(SI)--

                        decimal withHoldingTaxAmount = 0;
                        decimal withHoldingVatAmount = 0;
                        decimal netOfVatAmount;
                        decimal vatAmount = 0;
                        if (model.SalesInvoice.Customer.CustomerType == CS.VatType_Vatable)
                        {
                            netOfVatAmount = (_generalRepo.ComputeNetOfVat(Math.Abs(model.CreditAmount))) * -1;
                            vatAmount = (_generalRepo.ComputeVatAmount(Math.Abs(netOfVatAmount))) * -1;
                        }
                        else
                        {
                            netOfVatAmount = model.CreditAmount;
                        }
                        if (model.SalesInvoice.Customer.WithHoldingTax)
                        {
                            withHoldingTaxAmount = (_generalRepo.ComputeEwtAmount(Math.Abs(netOfVatAmount), 0.01m)) * -1;

                        }
                        if (model.SalesInvoice.Customer.WithHoldingVat)
                        {
                            withHoldingVatAmount = (_generalRepo.ComputeEwtAmount(Math.Abs(netOfVatAmount), 0.05m)) * -1;
                        }

                        var ledgers = new List<GeneralLedgerBook>
                        {
                            new GeneralLedgerBook
                            {
                                Date = model.TransactionDate,
                                Reference = model.CreditMemoNo!,
                                Description = model.SalesInvoice.Product.ProductName,
                                AccountNo = arTradeReceivableTitle.AccountNumber,
                                AccountTitle = arTradeReceivableTitle.AccountName,
                                Debit = 0,
                                Credit = Math.Abs(model.CreditAmount - (withHoldingTaxAmount + withHoldingVatAmount)),
                                CreatedBy = model.CreatedBy,
                                CreatedDate = model.CreatedDate
                            }
                        };

                        if (withHoldingTaxAmount < 0)
                        {
                            ledgers.Add(
                                new GeneralLedgerBook
                                {
                                    Date = model.TransactionDate,
                                    Reference = model.CreditMemoNo!,
                                    Description = model.SalesInvoice.Product.ProductName,
                                    AccountNo = arTradeCwt.AccountNumber,
                                    AccountTitle = arTradeCwt.AccountName,
                                    Debit = 0,
                                    Credit = Math.Abs(withHoldingTaxAmount),
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }
                        if (withHoldingVatAmount < 0)
                        {
                            ledgers.Add(
                                new GeneralLedgerBook
                                {
                                    Date = model.TransactionDate,
                                    Reference = model.CreditMemoNo!,
                                    Description = model.SalesInvoice.Product.ProductName,
                                    AccountNo = arTradeCwv.AccountNumber,
                                    AccountTitle = arTradeCwv.AccountName,
                                    Debit = 0,
                                    Credit = Math.Abs(withHoldingVatAmount),
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }
                        ledgers.Add(
                            new GeneralLedgerBook
                            {
                                Date = model.TransactionDate,
                                Reference = model.CreditMemoNo!,
                                Description = model.SalesInvoice.Product.ProductName,
                                AccountNo = salesTitle.AccountNumber,
                                AccountTitle = salesTitle.AccountName,
                                Debit = Math.Abs(netOfVatAmount),
                                CreatedBy = model.CreatedBy,
                                Credit = 0,
                                CreatedDate = model.CreatedDate
                            }
                        );
                        if (vatAmount < 0)
                        {
                            ledgers.Add(
                                new GeneralLedgerBook
                                {
                                    Date = model.TransactionDate,
                                    Reference = model.CreditMemoNo!,
                                    Description = model.SalesInvoice.Product.ProductName,
                                    AccountNo = vatOutputTitle.AccountNumber,
                                    AccountTitle = vatOutputTitle.AccountName,
                                    Debit = Math.Abs(vatAmount),
                                    Credit = 0,
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
                                                .FirstOrDefaultAsync(si => si.ServiceInvoiceId == model.ServiceInvoiceId, cancellationToken);

                        #region --SV Computation--

                        viewModelDmcm.Period = DateOnly.FromDateTime(model.CreatedDate) >= model.Period ? DateOnly.FromDateTime(model.CreatedDate) : model.Period.AddMonths(1).AddDays(-1);

                        if (existingSv!.Customer!.CustomerType == "Vatable")
                        {
                            viewModelDmcm.Total = -model.Amount ?? 0;
                            viewModelDmcm.NetAmount = (model.Amount ?? 0 - existingSv.Discount) / 1.12m;
                            viewModelDmcm.VatAmount = (model.Amount ?? 0 - existingSv.Discount) - viewModelDmcm.NetAmount;
                            viewModelDmcm.WithholdingTaxAmount = viewModelDmcm.NetAmount * (existingSv.Service!.Percent / 100m);
                            if (existingSv.Customer.WithHoldingVat)
                            {
                                viewModelDmcm.WithholdingVatAmount = viewModelDmcm.NetAmount * 0.05m;
                            }
                        }
                        else
                        {
                            viewModelDmcm.NetAmount = model.Amount ?? 0 - existingSv.Discount;
                            viewModelDmcm.WithholdingTaxAmount = viewModelDmcm.NetAmount * (existingSv.Service!.Percent / 100m);
                            if (existingSv.Customer.WithHoldingVat)
                            {
                                viewModelDmcm.WithholdingVatAmount = viewModelDmcm.NetAmount * 0.05m;
                            }
                        }

                        if (existingSv.Customer.CustomerType == "Vatable")
                        {
                            var total = Math.Round(model.Amount ?? 0 / 1.12m, 2);

                            var roundedNetAmount = Math.Round(viewModelDmcm.NetAmount, 2);

                            if (roundedNetAmount > total)
                            {
                                var shortAmount = viewModelDmcm.NetAmount - total;

                                viewModelDmcm.Amount += shortAmount;
                            }
                        }

                        #endregion --SV Computation--

                        #region --Sales Book Recording(SV)--

                        var sales = new SalesBook();

                        if (model.ServiceInvoice!.Customer!.CustomerType == "Vatable")
                        {
                            sales.TransactionDate = viewModelDmcm.Period;
                            sales.SerialNo = model.CreditMemoNo!;
                            sales.SoldTo = model.ServiceInvoice.Customer.CustomerName;
                            sales.TinNo = model.ServiceInvoice.Customer.CustomerTin;
                            sales.Address = model.ServiceInvoice.Customer.CustomerAddress;
                            sales.Description = model.ServiceInvoice.Service!.Name;
                            sales.Amount = viewModelDmcm.Total;
                            sales.VatableSales = (_generalRepo.ComputeNetOfVat(Math.Abs(sales.Amount))) * -1;
                            sales.VatAmount = (_generalRepo.ComputeVatAmount(Math.Abs(sales.VatableSales))) * -1;
                            //sales.Discount = model.Discount;
                            sales.NetSales = viewModelDmcm.NetAmount;
                            sales.CreatedBy = model.CreatedBy;
                            sales.CreatedDate = model.CreatedDate;
                            sales.DueDate = existingSv.DueDate;
                            sales.DocumentId = model.ServiceInvoiceId;
                        }
                        else if (model.ServiceInvoice.Customer.CustomerType == "Exempt")
                        {
                            sales.TransactionDate = viewModelDmcm.Period;
                            sales.SerialNo = model.CreditMemoNo!;
                            sales.SoldTo = model.ServiceInvoice.Customer.CustomerName;
                            sales.TinNo = model.ServiceInvoice.Customer.CustomerTin;
                            sales.Address = model.ServiceInvoice.Customer.CustomerAddress;
                            sales.Description = model.ServiceInvoice.Service!.Name;
                            sales.Amount = viewModelDmcm.Total;
                            sales.VatExemptSales = viewModelDmcm.Total;
                            //sales.Discount = model.Discount;
                            sales.NetSales = viewModelDmcm.NetAmount;
                            sales.CreatedBy = model.CreatedBy;
                            sales.CreatedDate = model.CreatedDate;
                            sales.DueDate = existingSv.DueDate;
                            sales.DocumentId = model.ServiceInvoiceId;
                        }
                        else
                        {
                            sales.TransactionDate = viewModelDmcm.Period;
                            sales.SerialNo = model.CreditMemoNo!;
                            sales.SoldTo = model.ServiceInvoice.Customer.CustomerName;
                            sales.TinNo = model.ServiceInvoice.Customer.CustomerTin;
                            sales.Address = model.ServiceInvoice.Customer.CustomerAddress;
                            sales.Description = model.ServiceInvoice.Service!.Name;
                            sales.Amount = viewModelDmcm.Total;
                            sales.ZeroRated = viewModelDmcm.Total;
                            //sales.Discount = model.Discount;
                            sales.NetSales = viewModelDmcm.NetAmount;
                            sales.CreatedBy = model.CreatedBy;
                            sales.CreatedDate = model.CreatedDate;
                            sales.DueDate = existingSv.DueDate;
                            sales.DocumentId = model.ServiceInvoiceId;
                        }
                        await _dbContext.AddAsync(sales, cancellationToken);

                        #endregion --Sales Book Recording(SV)--

                        #region --General Ledger Book Recording(SV)--

                        decimal withHoldingTaxAmount = 0;
                        decimal withHoldingVatAmount = 0;
                        decimal netOfVatAmount;
                        decimal vatAmount = 0;
                        if (model.ServiceInvoice.Customer.CustomerType == CS.VatType_Vatable)
                        {
                            netOfVatAmount = (_generalRepo.ComputeNetOfVat(Math.Abs(model.CreditAmount))) * -1;
                            vatAmount = (_generalRepo.ComputeVatAmount(Math.Abs(netOfVatAmount))) * -1;
                        }
                        else
                        {
                            netOfVatAmount = model.CreditAmount;
                        }
                        if (model.ServiceInvoice.Customer.WithHoldingTax)
                        {
                            withHoldingTaxAmount = (_generalRepo.ComputeEwtAmount(Math.Abs(netOfVatAmount), 0.01m)) * -1;
                        }
                        if (model.ServiceInvoice.Customer.WithHoldingVat)
                        {
                            withHoldingVatAmount = _generalRepo.ComputeEwtAmount(Math.Abs(netOfVatAmount), 0.05m) * -1;
                        }

                        var ledgers = new List<GeneralLedgerBook>
                        {
                            new GeneralLedgerBook
                            {
                                Date = model.TransactionDate,
                                Reference = model.CreditMemoNo!,
                                Description = model.ServiceInvoice.Service.Name,
                                AccountNo = arNonTradeReceivableTitle.AccountNumber,
                                AccountTitle = arNonTradeReceivableTitle.AccountName,
                                Debit = 0,
                                Credit = Math.Abs(model.CreditAmount - (withHoldingTaxAmount + withHoldingVatAmount)),
                                CreatedBy = model.CreatedBy,
                                CreatedDate = model.CreatedDate
                            }
                        };

                        if (withHoldingTaxAmount < 0)
                        {
                            ledgers.Add(
                                new GeneralLedgerBook
                                {
                                    Date = model.TransactionDate,
                                    Reference = model.CreditMemoNo!,
                                    Description = model.ServiceInvoice.Service.Name,
                                    AccountNo = arTradeCwt.AccountNumber,
                                    AccountTitle = arTradeCwt.AccountName,
                                    Debit = 0,
                                    Credit = Math.Abs(withHoldingTaxAmount),
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }
                        if (withHoldingVatAmount < 0)
                        {
                            ledgers.Add(
                                new GeneralLedgerBook
                                {
                                    Date = model.TransactionDate,
                                    Reference = model.CreditMemoNo!,
                                    Description = model.ServiceInvoice.Service.Name,
                                    AccountNo = arTradeCwv.AccountNumber,
                                    AccountTitle = arTradeCwv.AccountName,
                                    Debit = 0,
                                    Credit = Math.Abs(withHoldingVatAmount),
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }

                        ledgers.Add(new GeneralLedgerBook
                        {
                            Date = model.TransactionDate,
                            Reference = model.CreditMemoNo!,
                            Description = model.ServiceInvoice.Service.Name,
                            AccountNo = model.ServiceInvoice.Service.CurrentAndPreviousNo!,
                            AccountTitle = model.ServiceInvoice.Service.CurrentAndPreviousTitle!,
                            Debit = viewModelDmcm.NetAmount,
                            Credit = 0,
                            CreatedBy = model.CreatedBy,
                            CreatedDate = model.CreatedDate
                        });

                        if (vatAmount < 0)
                        {
                            ledgers.Add(
                                new GeneralLedgerBook
                                {
                                    Date = model.TransactionDate,
                                    Reference = model.CreditMemoNo!,
                                    Description = model.ServiceInvoice.Service.Name,
                                    AccountNo = vatOutputTitle.AccountNumber,
                                    AccountTitle = vatOutputTitle.AccountName,
                                    Debit = Math.Abs(vatAmount),
                                    Credit = 0,
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

                    if (model.OriginalSeriesNumber.IsNullOrEmpty() && model.OriginalDocumentId == 0)
                    {
                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        AuditTrail auditTrailBook = new(model.PostedBy!, $"Posted credit memo# {model.CreditMemoNo}", "Credit Memo", ipAddress!);
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                    }

                    #endregion --Audit Trail Recording

                    //await _receiptRepo.UpdateCreditMemo(model.SalesInvoice.Id, model.Total, offsetAmount);

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    TempData["success"] = "Credit Memo has been Posted.";
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {
            var model = await _dbContext.CreditMemos.FirstOrDefaultAsync(x => x.CreditMemoId == id, cancellationToken);
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
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

                        await _generalRepo.RemoveRecords<SalesBook>(crb => crb.SerialNo == model.CreditMemoNo, cancellationToken);
                        await _generalRepo.RemoveRecords<GeneralLedgerBook>(gl => gl.Reference == model.CreditMemoNo, cancellationToken);

                        #region --Audit Trail Recording

                        if (model.OriginalSeriesNumber.IsNullOrEmpty() && model.OriginalDocumentId == 0)
                        {
                            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                            AuditTrail auditTrailBook = new(model.VoidedBy!, $"Voided credit memo# {model.CreditMemoNo}", "Credit Memo", ipAddress!);
                            await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                        }

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Credit Memo has been Voided.";
                    }
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        public async Task<IActionResult> Cancel(int id, string cancellationRemarks, CancellationToken cancellationToken)
        {
            var model = await _dbContext.CreditMemos.FirstOrDefaultAsync(x => x.CreditMemoId == id, cancellationToken);
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                if (model != null)
                {
                    if (!model.IsCanceled)
                    {
                        model.IsCanceled = true;
                        model.CanceledBy = _userManager.GetUserName(this.User);
                        model.CanceledDate = DateTime.Now;
                        model.CancellationRemarks = cancellationRemarks;

                        #region --Audit Trail Recording

                        if (model.OriginalSeriesNumber.IsNullOrEmpty() && model.OriginalDocumentId == 0)
                        {
                            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                            AuditTrail auditTrailBook = new(model.CanceledBy!, $"Cancelled credit memo# {model.CreditMemoNo}", "Credit Memo", ipAddress!);
                            await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                        }

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Credit Memo has been Cancelled.";
                    }
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        [HttpGet]
        public async Task<JsonResult> GetSVDetails(int svId, CancellationToken cancellationToken)
        {
            var model = await _dbContext.ServiceInvoices.FirstOrDefaultAsync(sv => sv.ServiceInvoiceId == svId, cancellationToken);
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
        public async Task<IActionResult> Export(string selectedRecord, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(selectedRecord))
            {
                // Handle the case where no invoices are selected
                return RedirectToAction(nameof(Index));
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
		    {
                var recordIds = selectedRecord.Split(',').Select(int.Parse).ToList();

                // Retrieve the selected invoices from the database
                var selectedList = await _dbContext.CreditMemos
                    .Where(cm => recordIds.Contains(cm.CreditMemoId))
                    .Include(cm => cm.SalesInvoice)
                    .Include(cm => cm.ServiceInvoice)
                    .OrderBy(cm => cm.CreditMemoNo)
                    .ToListAsync(cancellationToken: cancellationToken);

                // Create the Excel package
                using var package = new ExcelPackage();
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

                #region -- Credit Memo Table Header --

                var worksheet = package.Workbook.Worksheets.Add("CreditMemo");

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
                worksheet.Cells["Q1"].Value = "OriginalCMNo";
                worksheet.Cells["R1"].Value = "OriginalServiceInvoiceId";
                worksheet.Cells["S1"].Value = "OriginalDocumentId";

                #endregion -- Credit Memo Table Header --

                #region -- Credit Memo Export --

                int row = 2;

                foreach (var item in selectedList)
                {
                    worksheet.Cells[row, 1].Value = item.TransactionDate.ToString("yyyy-MM-dd");
                    worksheet.Cells[row, 2].Value = item.CreditAmount;
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
                    worksheet.Cells[row, 17].Value = item.CreditMemoNo;
                    worksheet.Cells[row, 18].Value = item.ServiceInvoiceId;
                    worksheet.Cells[row, 19].Value = item.CreditMemoId;

                    row++;
                }

                #endregion -- Credit Memo Export --

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
                    worksheet2.Cells[siRow, 21].Value = item.SalesInvoice.SalesInvoiceNo;
                    worksheet2.Cells[siRow, 22].Value = item.SalesInvoice.SalesInvoiceId;

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
                    worksheet3.Cells[svRow, 17].Value = item.ServiceInvoice.ServiceInvoiceNo;
                    worksheet3.Cells[svRow, 18].Value = item.ServiceInvoice.ServicesId;
                    worksheet3.Cells[svRow, 19].Value = item.ServiceInvoice.ServiceInvoiceId;

                    svRow++;
                }

                #endregion -- Service Invoice Export --

                // Convert the Excel package to a byte array
                var excelBytes = await package.GetAsByteArrayAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"CreditMemoList_IBS-RCD_{DateTime.Now:yyyyddMMHHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index), new { view = DynamicView.BankAccount });
            }

        }

        #endregion -- export xlsx record --

        //Upload as .xlsx file.(Import)
        #region -- import xlsx record from IBS --

        [HttpPost]
        public async Task<IActionResult> Import(IFormFile file, CancellationToken cancellationToken)
        {
            if (file.Length == 0)
            {
                return RedirectToAction(nameof(Index));
            }

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream, cancellationToken);
                stream.Position = 0;
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    using var package = new ExcelPackage(stream);
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "CreditMemo");

                    var worksheet2 = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "SalesInvoice");

                    var worksheet3 = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "ServiceInvoice");

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
                            SalesInvoiceNo = worksheet2.Cells[row, 21].Text,
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
                            PostedBy = worksheet2.Cells[row, 23].Text,
                            PostedDate = DateTime.TryParse(worksheet2.Cells[row, 24].Text, out DateTime postedDate)
                                ? postedDate
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
                            var existingSi = await _dbContext.SalesInvoices.FirstOrDefaultAsync(si => si.OriginalDocumentId == invoice.OriginalDocumentId, cancellationToken);
                            var existingSiInLogs = await _dbContext.ImportExportLogs
                                .Where(x => x.DocumentNo == existingSi.SalesInvoiceNo)
                                .ToListAsync(cancellationToken);

                            if (existingSi!.SalesInvoiceNo!.TrimStart().TrimEnd() != worksheet2.Cells[row, 21].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingSi.SalesInvoiceNo.TrimStart().TrimEnd();
                                var adjustedValue = worksheet2.Cells[row, 21].Text.TrimStart().TrimEnd();
                                var find  = existingSiInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    siChanges["SiNo"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSi.OriginalCustomerId.ToString()!.TrimStart().TrimEnd() != worksheet2.Cells[row, 18].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingSi.OriginalCustomerId.ToString()!.TrimStart().TrimEnd();
                                var adjustedValue = worksheet2.Cells[row, 18].Text.TrimStart().TrimEnd();
                                var find  = existingSiInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    siChanges["OriginalCustomerId"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSi.OriginalProductId.ToString()!.TrimStart().TrimEnd() != worksheet2.Cells[row, 20].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingSi.OriginalProductId.ToString()!.TrimStart().TrimEnd();
                                var adjustedValue = worksheet2.Cells[row, 20].Text.TrimStart().TrimEnd();
                                var find  = existingSiInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    siChanges["OriginalProductId"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSi.OtherRefNo.TrimStart().TrimEnd() != worksheet2.Cells[row, 1].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingSi.OtherRefNo.TrimStart().TrimEnd();
                                var adjustedValue = worksheet2.Cells[row, 1].Text.TrimStart().TrimEnd();
                                var find  = existingSiInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    siChanges["OtherRefNo"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSi.Quantity.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet2.Cells[row, 2].Text).ToString("F2").TrimStart().TrimEnd())
                            {
                                var originalValue = existingSi.Quantity.ToString("F2").TrimStart().TrimEnd();
                                var adjustedValue = decimal.Parse(worksheet2.Cells[row, 2].Text).ToString("F2").TrimStart().TrimEnd();
                                var find  = existingSiInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    siChanges["Quantity"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSi.UnitPrice.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet2.Cells[row, 3].Text).ToString("F2").TrimStart().TrimEnd())
                            {
                                var originalValue = existingSi.UnitPrice.ToString("F2").TrimStart().TrimEnd();
                                var adjustedValue = decimal.Parse(worksheet2.Cells[row, 3].Text).ToString("F2").TrimStart().TrimEnd();
                                var find  = existingSiInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    siChanges["UnitPrice"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSi.Amount.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet2.Cells[row, 4].Text).ToString("F2").TrimStart().TrimEnd())
                            {
                                var originalValue = existingSi.Amount.ToString("F2").TrimStart().TrimEnd();
                                var adjustedValue = decimal.Parse(worksheet2.Cells[row, 4].Text).ToString("F2").TrimStart().TrimEnd();
                                var find  = existingSiInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    siChanges["Amount"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSi.Remarks.TrimStart().TrimEnd() != worksheet2.Cells[row, 5].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingSi.Remarks.TrimStart().TrimEnd();
                                var adjustedValue = worksheet2.Cells[row, 5].Text.TrimStart().TrimEnd();
                                var find  = existingSiInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    siChanges["Remarks"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSi.Status.TrimStart().TrimEnd() != worksheet2.Cells[row, 6].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingSi.Status.TrimStart().TrimEnd();
                                var adjustedValue = worksheet2.Cells[row, 6].Text.TrimStart().TrimEnd();
                                var find  = existingSiInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    siChanges["Status"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSi.TransactionDate.ToString("yyyy-MM-dd").TrimStart().TrimEnd() != worksheet2.Cells[row, 7].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingSi.TransactionDate.ToString("yyyy-MM-dd").TrimStart().TrimEnd();
                                var adjustedValue = worksheet2.Cells[row, 7].Text.TrimStart().TrimEnd();
                                var find  = existingSiInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    siChanges["TransactionDate"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSi.Discount.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet2.Cells[row, 8].Text).ToString("F2").TrimStart().TrimEnd())
                            {
                                var originalValue = existingSi.Discount.ToString("F2").TrimStart().TrimEnd();
                                var adjustedValue = decimal.Parse(worksheet2.Cells[row, 8].Text).ToString("F2").TrimStart().TrimEnd();
                                var find  = existingSiInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    siChanges["Discount"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSi.DueDate.ToString("yyyy-MM-dd").TrimStart().TrimEnd() != worksheet2.Cells[row, 13].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingSi.DueDate.ToString("yyyy-MM-dd").TrimStart().TrimEnd();
                                var adjustedValue = worksheet2.Cells[row, 13].Text.TrimStart().TrimEnd();
                                var find  = existingSiInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    siChanges["DueDate"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSi.CreatedBy!.TrimStart().TrimEnd() != worksheet2.Cells[row, 14].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingSi.CreatedBy.TrimStart().TrimEnd();
                                var adjustedValue = worksheet2.Cells[row, 14].Text.TrimStart().TrimEnd();
                                var find  = existingSiInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    siChanges["CreatedBy"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSi.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff").TrimStart().TrimEnd() != worksheet2.Cells[row, 15].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingSi.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff").TrimStart().TrimEnd();
                                var adjustedValue = worksheet2.Cells[row, 15].Text.TrimStart().TrimEnd();
                                var find  = existingSiInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    siChanges["CreatedDate"] = (originalValue, adjustedValue);
                                }
                            }

                            if ((string.IsNullOrWhiteSpace(existingSi.CancellationRemarks?.TrimStart().TrimEnd()) ? "" : existingSi.CancellationRemarks.TrimStart().TrimEnd()) != worksheet2.Cells[row, 16].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingSi.CancellationRemarks?.TrimStart().TrimEnd() ?? String.Empty;
                                var adjustedValue = worksheet2.Cells[row, 16].Text.TrimStart().TrimEnd();
                                var find  = existingSiInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    siChanges["CancellationRemarks"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSi.OriginalSeriesNumber!.TrimStart().TrimEnd() != worksheet2.Cells[row, 21].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingSi.OriginalSeriesNumber.TrimStart().TrimEnd();
                                var adjustedValue = worksheet2.Cells[row, 21].Text.TrimStart().TrimEnd();
                                var find  = existingSiInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    siChanges["OriginalSeriesNumber"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSi.OriginalDocumentId.ToString().TrimStart().TrimEnd() != worksheet2.Cells[row, 22].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingSi.OriginalDocumentId.ToString().TrimStart().TrimEnd();
                                var adjustedValue = worksheet2.Cells[row, 22].Text.TrimStart().TrimEnd();
                                var find  = existingSiInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    siChanges["OriginalDocumentId"] = (originalValue, adjustedValue);
                                }
                            }

                            if (siChanges.Any())
                            {
                                await _salesInvoiceRepo.LogChangesAsync(existingSi.OriginalDocumentId, siChanges, _userManager.GetUserName(this.User), existingSi.SalesInvoiceNo);
                            }

                            continue;
                        }
                        else
                        {
                            #region --Audit Trail Recording

                            if (!invoice.CreatedBy.IsNullOrEmpty())
                            {
                                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                                AuditTrail auditTrailBook = new(invoice.CreatedBy, $"Create new invoice# {invoice.SalesInvoiceNo}", "Sales Invoice", ipAddress!, invoice.CreatedDate);
                                await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                            }
                            if (!invoice.PostedBy.IsNullOrEmpty())
                            {
                                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                                AuditTrail auditTrailBook = new(invoice.PostedBy, $"Posted invoice# {invoice.SalesInvoiceNo}", "Sales Invoice", ipAddress!, invoice.PostedDate);
                                await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                            }

                            #endregion --Audit Trail Recording
                        }

                        invoice.CustomerId = await _dbContext.Customers
                            .Where(c => c.OriginalCustomerId == invoice.OriginalCustomerId)
                            .Select(c => (int?)c.CustomerId)
                            .FirstOrDefaultAsync(cancellationToken) ?? throw new InvalidOperationException("Please upload the Excel file for the customer master file first.");

                        invoice.ProductId = await _dbContext.Products
                            .Where(c => c.OriginalProductId == invoice.OriginalProductId)
                            .Select(c => (int?)c.ProductId)
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
                            ServiceInvoiceNo = worksheet3.Cells[row, 17].Text,
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
                            PostedBy = worksheet3.Cells[row, 20].Text,
                            PostedDate = DateTime.TryParse(worksheet3.Cells[row, 21].Text, out DateTime postedDate) ? postedDate : default,
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
                            var existingSv = await _dbContext.ServiceInvoices.FirstOrDefaultAsync(si => si.OriginalDocumentId == serviceInvoice.OriginalDocumentId, cancellationToken);
                            var existingSvInLogs = await _dbContext.ImportExportLogs
                                .Where(x => x.DocumentNo == existingSv.ServiceInvoiceNo)
                                .ToListAsync(cancellationToken);

                            if (existingSv!.ServiceInvoiceNo!.TrimStart().TrimEnd() != worksheet3.Cells[row, 17].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingSv.ServiceInvoiceNo.TrimStart().TrimEnd();
                                var adjustedValue = worksheet3.Cells[row, 17].Text.TrimStart().TrimEnd();
                                var find  = existingSvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    svChanges["SvNo"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSv.DueDate.ToString("yyyy-MM-dd").TrimStart().TrimEnd() != worksheet3.Cells[row, 1].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingSv.DueDate.ToString("yyyy-MM-dd").TrimStart().TrimEnd();
                                var adjustedValue = worksheet3.Cells[row, 1].Text.TrimStart().TrimEnd();
                                var find  = existingSvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    svChanges["DueDate"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSv.Period.ToString("yyyy-MM-dd").TrimStart().TrimEnd() != worksheet3.Cells[row, 2].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingSv.Period.ToString("yyyy-MM-dd").TrimStart().TrimEnd();
                                var adjustedValue = worksheet3.Cells[row, 2].Text.TrimStart().TrimEnd();
                                var find  = existingSvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    svChanges["Period"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSv.Amount.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet3.Cells[row, 3].Text).ToString("F2").TrimStart().TrimEnd())
                            {
                                var originalValue = existingSv.Amount.ToString("F2").TrimStart().TrimEnd();
                                var adjustedValue = decimal.Parse(worksheet3.Cells[row, 3].Text).ToString("F2").TrimStart().TrimEnd();
                                var find  = existingSvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    svChanges["Amount"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSv.Total.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet3.Cells[row, 4].Text).ToString("F2").TrimStart().TrimEnd())
                            {
                                var originalValue = existingSv.Total.ToString("F2").TrimStart().TrimEnd();
                                var adjustedValue = decimal.Parse(worksheet3.Cells[row, 4].Text).ToString("F2").TrimStart().TrimEnd();
                                var find  = existingSvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    svChanges["Total"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSv.Discount.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet3.Cells[row, 5].Text).ToString("F2").TrimStart().TrimEnd())
                            {
                                var originalValue = existingSv.Discount.ToString("F2").TrimStart().TrimEnd();
                                var adjustedValue = decimal.Parse(worksheet3.Cells[row, 5].Text).ToString("F2").TrimStart().TrimEnd();
                                var find  = existingSvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    svChanges["Discount"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSv.CurrentAndPreviousAmount.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet3.Cells[row, 6].Text).ToString("F2").TrimStart().TrimEnd())
                            {
                                var originalValue = existingSv.CurrentAndPreviousAmount.ToString("F2").TrimStart().TrimEnd();
                                var adjustedValue = decimal.Parse(worksheet3.Cells[row, 6].Text).ToString("F2").TrimStart().TrimEnd();
                                var find  = existingSvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    svChanges["CurrentAndPreviousAmount"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSv.UnearnedAmount.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet3.Cells[row, 7].Text).ToString("F2").TrimStart().TrimEnd())
                            {
                                var originalValue = existingSv.UnearnedAmount.ToString("F2").TrimStart().TrimEnd();
                                var adjustedValue = decimal.Parse(worksheet3.Cells[row, 7].Text).ToString("F2").TrimStart().TrimEnd();
                                var find  = existingSvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    svChanges["UnearnedAmount"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSv.Status.TrimStart().TrimEnd() != worksheet3.Cells[row, 8].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingSv.Status.TrimStart().TrimEnd();
                                var adjustedValue = worksheet3.Cells[row, 8].Text.TrimStart().TrimEnd();
                                var find  = existingSvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    svChanges["Status"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSv.Instructions!.TrimStart().TrimEnd() != worksheet3.Cells[row, 11].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingSv.Instructions.TrimStart().TrimEnd();
                                var adjustedValue = worksheet3.Cells[row, 11].Text.TrimStart().TrimEnd();
                                var find  = existingSvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    svChanges["Instructions"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSv.CreatedBy!.TrimStart().TrimEnd() != worksheet3.Cells[row, 13].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingSv.CreatedBy.TrimStart().TrimEnd();
                                var adjustedValue = worksheet3.Cells[row, 13].Text.TrimStart().TrimEnd();
                                var find  = existingSvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    svChanges["CreatedBy"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSv.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff").TrimStart().TrimEnd() != worksheet3.Cells[row, 14].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingSv.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff").TrimStart().TrimEnd();
                                var adjustedValue = worksheet3.Cells[row, 14].Text.TrimStart().TrimEnd();
                                var find  = existingSvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    svChanges["CreatedDate"] = (originalValue, adjustedValue);
                                }
                            }

                            if ((string.IsNullOrWhiteSpace(existingSv.CancellationRemarks?.TrimStart().TrimEnd()) ? "" : existingSv.CancellationRemarks.TrimStart().TrimEnd()) != worksheet3.Cells[row, 15].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingSv.CancellationRemarks?.TrimStart().TrimEnd();
                                var adjustedValue = worksheet3.Cells[row, 15].Text.TrimStart().TrimEnd();
                                var find  = existingSvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    svChanges["CancellationRemarks"] = (originalValue, adjustedValue)!;
                                }
                            }

                            if (existingSv.OriginalCustomerId.ToString()!.TrimStart().TrimEnd() != worksheet3.Cells[row, 16].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingSv.OriginalCustomerId.ToString()!.TrimStart().TrimEnd();
                                var adjustedValue = worksheet3.Cells[row, 16].Text.TrimStart().TrimEnd();
                                var find  = existingSvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    svChanges["OriginalCustomerId"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSv.OriginalSeriesNumber!.TrimStart().TrimEnd() != worksheet3.Cells[row, 17].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingSv.OriginalSeriesNumber.TrimStart().TrimEnd();
                                var adjustedValue = worksheet3.Cells[row, 17].Text.TrimStart().TrimEnd();
                                var find  = existingSvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    svChanges["OriginalSeriesNumber"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSv.OriginalServicesId.ToString()!.TrimStart().TrimEnd() != worksheet3.Cells[row, 18].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingSv.OriginalServicesId.ToString()!.TrimStart().TrimEnd();
                                var adjustedValue = worksheet3.Cells[row, 18].Text.TrimStart().TrimEnd();
                                var find  = existingSvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    svChanges["OriginalServicesId"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSv.OriginalDocumentId.ToString().TrimStart().TrimEnd() != worksheet3.Cells[row, 19].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingSv.OriginalDocumentId.ToString().TrimStart().TrimEnd();
                                var adjustedValue = worksheet3.Cells[row, 19].Text.TrimStart().TrimEnd();
                                var find  = existingSvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    svChanges["OriginalDocumentId"] = (originalValue, adjustedValue);
                                }
                            }

                            if (svChanges.Any())
                            {
                                await _serviceInvoiceRepo.LogChangesAsync(existingSv.OriginalDocumentId, svChanges, _userManager.GetUserName(this.User), existingSv.ServiceInvoiceNo);
                            }

                            continue;
                        }
                        else
                        {
                            #region --Audit Trail Recording

                            if (!serviceInvoice.CreatedBy.IsNullOrEmpty())
                            {
                                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                                AuditTrail auditTrailBook = new(serviceInvoice.CreatedBy, $"Create new service invoice# {serviceInvoice.ServiceInvoiceNo}", "Service Invoice", ipAddress!, serviceInvoice.CreatedDate);
                                await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                            }
                            if (!serviceInvoice.PostedBy.IsNullOrEmpty())
                            {
                                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                                AuditTrail auditTrailBook = new(serviceInvoice.PostedBy, $"Posted service invoice# {serviceInvoice.ServiceInvoiceNo}", "Service Invoice", ipAddress!, serviceInvoice.PostedDate);
                                await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                            }

                            #endregion --Audit Trail Recording
                        }

                        serviceInvoice.CustomerId = await _dbContext.Customers
                            .Where(sv => sv.OriginalCustomerId == serviceInvoice.OriginalCustomerId)
                            .Select(sv => (int?)sv.CustomerId)
                            .FirstOrDefaultAsync(cancellationToken) ?? throw new InvalidOperationException("Please upload the Excel file for the customer master file first.");

                        serviceInvoice.ServicesId = await _dbContext.Services
                            .Where(sv => sv.OriginalServiceId == serviceInvoice.OriginalServicesId)
                            .Select(sv => (int?)sv.ServiceId)
                            .FirstOrDefaultAsync(cancellationToken) ?? throw new InvalidOperationException("Please upload the Excel file for the service master file first.");

                        await _dbContext.ServiceInvoices.AddAsync(serviceInvoice, cancellationToken);
                    }
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    #endregion -- Service Invoice Import --

                    #region -- Credit Memo Import --

                    var rowCount = worksheet.Dimension.Rows;
                    var cmDictionary = new Dictionary<string, bool>();
                    var creditMemoList = await _dbContext
                        .CreditMemos
                        .ToListAsync(cancellationToken);

                    for (int row = 2; row <= rowCount; row++)  // Assuming the first row is the header
                    {
                        var creditMemo = new CreditMemo
                        {
                            CreditMemoNo = worksheet.Cells[row, 17].Text,
                            TransactionDate = DateOnly.TryParse(worksheet.Cells[row, 1].Text, out DateOnly transactionDate) ? transactionDate : default,
                            CreditAmount = decimal.TryParse(worksheet.Cells[row, 2].Text, out decimal debitAmount) ? debitAmount : 0,
                            Description = worksheet.Cells[row, 3].Text,
                            AdjustedPrice = decimal.TryParse(worksheet.Cells[row, 4].Text, out decimal adjustedPrice) ? adjustedPrice : 0,
                            Quantity = decimal.TryParse(worksheet.Cells[row, 5].Text, out decimal quantity) ? quantity : 0,
                            Source = worksheet.Cells[row, 6].Text,
                            Remarks = worksheet.Cells[row, 7].Text,
                            Period = DateOnly.TryParse(worksheet.Cells[row, 8].Text, out DateOnly period) ? period : default,
                            Amount = decimal.TryParse(worksheet.Cells[row, 9].Text, out decimal amount) ? amount : 0,
                            CurrentAndPreviousAmount = decimal.TryParse(worksheet.Cells[row, 10].Text, out decimal currentAndPreviousAmount) ? currentAndPreviousAmount : 0,
                            UnearnedAmount = decimal.TryParse(worksheet.Cells[row, 11].Text, out decimal unearnedAmount) ? unearnedAmount : 0,
                            ServicesId = int.TryParse(worksheet.Cells[row, 12].Text, out int servicesId) ? servicesId : 0,
                            CreatedBy = worksheet.Cells[row, 13].Text,
                            CreatedDate = DateTime.TryParse(worksheet.Cells[row, 14].Text, out DateTime createdDate) ? createdDate : default,
                            PostedBy = worksheet.Cells[row, 20].Text,
                            PostedDate = DateTime.TryParse(worksheet.Cells[row, 21].Text, out DateTime postedDate) ? postedDate : default,
                            CancellationRemarks = worksheet.Cells[row, 15].Text,
                            OriginalSalesInvoiceId = int.TryParse(worksheet.Cells[row, 16].Text, out int originalSalesInvoiceId) ? originalSalesInvoiceId : 0,
                            OriginalSeriesNumber = worksheet.Cells[row, 17].Text,
                            OriginalServiceInvoiceId = int.TryParse(worksheet.Cells[row, 18].Text, out int originalServiceInvoiceId) ? originalServiceInvoiceId : 0,
                            OriginalDocumentId = int.TryParse(worksheet.Cells[row, 19].Text, out int originalDocumentId) ? originalDocumentId : 0,
                        };

                        if (!cmDictionary.TryAdd(creditMemo.OriginalSeriesNumber, true))
                        {
                            continue;
                        }

                        if (creditMemoList.Any(cm => cm.OriginalDocumentId == creditMemo.OriginalDocumentId))
                        {
                            var cmChanges = new Dictionary<string, (string OriginalValue, string NewValue)>();
                            var existingCm = await _dbContext.CreditMemos.FirstOrDefaultAsync(si => si.OriginalDocumentId == creditMemo.OriginalDocumentId, cancellationToken);
                            var existingCmInLogs = await _dbContext.ImportExportLogs
                                .Where(x => x.DocumentNo == existingCm.CreditMemoNo)
                                .ToListAsync(cancellationToken);

                            if (existingCm!.CreditMemoNo!.TrimStart().TrimEnd() != worksheet.Cells[row, 17].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingCm.CreditMemoNo.TrimStart().TrimEnd();
                                var adjustedValue = worksheet.Cells[row, 17].Text.TrimStart().TrimEnd();
                                var find  = existingCmInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cmChanges["CMNo"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingCm.TransactionDate.ToString("yyyy-MM-dd").TrimStart().TrimEnd() != worksheet.Cells[row, 1].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingCm.TransactionDate.ToString("yyyy-MM-dd").TrimStart().TrimEnd();
                                var adjustedValue = worksheet.Cells[row, 1].Text.TrimStart().TrimEnd();
                                var find  = existingCmInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cmChanges["TransactionDate"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingCm.CreditAmount.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet.Cells[row, 2].Text).ToString("F2").TrimStart().TrimEnd())
                            {
                                var originalValue = existingCm.CreditAmount.ToString("F2").TrimStart().TrimEnd();
                                var adjustedValue = decimal.Parse(worksheet.Cells[row, 2].Text).ToString("F2").TrimStart().TrimEnd();
                                var find  = existingCmInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cmChanges["CreditAmount"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingCm.Description.TrimStart().TrimEnd() != worksheet.Cells[row, 3].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingCm.Description.TrimStart().TrimEnd();
                                var adjustedValue = worksheet.Cells[row, 3].Text.TrimStart().TrimEnd();
                                var find  = existingCmInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cmChanges["Description"] = (originalValue, adjustedValue);
                                }
                            }

                            if ((existingCm.AdjustedPrice != null ? existingCm.AdjustedPrice?.ToString("F2").TrimStart().TrimEnd() : 0.ToString("F2")) != decimal.Parse(worksheet.Cells[row, 4].Text.TrimStart().TrimEnd() != "" ? worksheet.Cells[row, 4].Text.TrimStart().TrimEnd() : 0.ToString("F2")).ToString("F2").TrimStart().TrimEnd())
                            {
                                var originalValue = existingCm.AdjustedPrice?.ToString("F2").TrimStart().TrimEnd();
                                var adjustedValue = decimal
                                    .Parse(worksheet.Cells[row, 4].Text.TrimStart().TrimEnd() != ""
                                        ? worksheet.Cells[row, 4].Text.TrimStart().TrimEnd()
                                        : 0.ToString("F2")).ToString("F2").TrimStart().TrimEnd();
                                var find  = existingCmInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cmChanges["AdjustedPrice"] = (originalValue, adjustedValue);
                                }
                            }

                            if ((existingCm.Quantity != null ? existingCm.Quantity?.ToString("F0").TrimStart().TrimEnd() : 0.ToString("F0")) != decimal.Parse(worksheet.Cells[row, 5].Text.TrimStart().TrimEnd() != "" ? worksheet.Cells[row, 5].Text.TrimStart().TrimEnd() : 0.ToString("F0")).ToString("F0").TrimStart().TrimEnd())
                            {
                                var originalValue = existingCm.Quantity?.ToString("F0").TrimStart().TrimEnd();
                                var adjustedValue = decimal
                                    .Parse(worksheet.Cells[row, 5].Text.TrimStart().TrimEnd() != ""
                                        ? worksheet.Cells[row, 5].Text.TrimStart().TrimEnd()
                                        : 0.ToString("F0")).ToString("F0").TrimStart().TrimEnd();
                                var find  = existingCmInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cmChanges["Quantity"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingCm.Source.TrimStart().TrimEnd() != worksheet.Cells[row, 6].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingCm.Source.TrimStart().TrimEnd();
                                var adjustedValue = worksheet.Cells[row, 6].Text.TrimStart().TrimEnd();
                                var find  = existingCmInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cmChanges["Source"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingCm.Remarks!.TrimStart().TrimEnd() != worksheet.Cells[row, 7].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingCm.Remarks.TrimStart().TrimEnd();
                                var adjustedValue = worksheet.Cells[row, 7].Text.TrimStart().TrimEnd();
                                var find  = existingCmInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cmChanges["Remarks"] = (originalValue, adjustedValue);
                                }
                            }

                            string cellValue = worksheet.Cells[row, 8].Text.Trim();

                            if (DateOnly.TryParse(cellValue, out DateOnly parsedDate))
                            {
                                if (existingCm.Period.ToString("yyyy-MM") != parsedDate.ToString("yyyy-MM"))
                                {
                                    var originalValue = existingCm.Period.ToString("yyyy-MM");
                                    var adjustedValue = parsedDate.ToString("yyyy-MM");
                                    var find  = existingCmInLogs
                                        .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                    if (!find.Any())
                                    {
                                        cmChanges["Period"] = (originalValue, adjustedValue);
                                    }
                                }
                            }

                            if ((existingCm.Amount != null ? existingCm.Amount?.ToString("F2").TrimStart().TrimEnd() : 0.ToString("F2")) != decimal.Parse(worksheet.Cells[row, 9].Text.TrimStart().TrimEnd() != "" ? worksheet.Cells[row, 9].Text.TrimStart().TrimEnd() : 0.ToString("F2")).ToString("F2").TrimStart().TrimEnd())
                            {
                                var originalValue = existingCm.Amount?.ToString("F2").TrimStart().TrimEnd();
                                var adjustedValue = decimal.Parse(worksheet.Cells[row, 9].Text).ToString("F2").TrimStart().TrimEnd();
                                var find  = existingCmInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cmChanges["Amount"] = (originalValue, adjustedValue)!;
                                }
                            }

                            if (existingCm.CurrentAndPreviousAmount.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet.Cells[row, 10].Text).ToString("F2").TrimStart().TrimEnd())
                            {
                                var originalValue = existingCm.CurrentAndPreviousAmount.ToString("F2").TrimStart().TrimEnd();
                                var adjustedValue = decimal.Parse(worksheet.Cells[row, 10].Text).ToString("F2").TrimStart().TrimEnd();
                                var find  = existingCmInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cmChanges["CurrentAndPreviousAmount"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingCm.UnearnedAmount.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet.Cells[row, 11].Text).ToString("F2").TrimStart().TrimEnd())
                            {
                                var originalValue = existingCm.UnearnedAmount.ToString("F2").TrimStart().TrimEnd();
                                var adjustedValue = decimal.Parse(worksheet.Cells[row, 11].Text).ToString("F2").TrimStart().TrimEnd();
                                var find  = existingCmInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cmChanges["UnearnedAmount"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingCm.ServicesId.ToString() != (worksheet.Cells[row, 12].Text == "" ? 0.ToString() : worksheet.Cells[row, 12].Text))
                            {
                                var originalValue = existingCm.ServicesId.ToString()!.TrimStart().TrimEnd();
                                var adjustedValue = worksheet.Cells[row, 12].Text.TrimStart().TrimEnd() == ""
                                    ? 0.ToString()
                                    : worksheet.Cells[row, 12].Text.TrimStart().TrimEnd();
                                var find  = existingCmInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cmChanges["ServicesId"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingCm.CreatedBy!.TrimStart().TrimEnd() != worksheet.Cells[row, 13].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingCm.CreatedBy.TrimStart().TrimEnd();
                                var adjustedValue = worksheet.Cells[row, 13].Text.TrimStart().TrimEnd();
                                var find  = existingCmInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cmChanges["CreatedBy"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingCm.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff").TrimStart().TrimEnd() != worksheet.Cells[row, 14].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingCm.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff").TrimStart().TrimEnd();
                                var adjustedValue = worksheet.Cells[row, 14].Text.TrimStart().TrimEnd();
                                var find  = existingCmInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cmChanges["CreatedDate"] = (originalValue, adjustedValue);
                                }
                            }

                            if ((string.IsNullOrWhiteSpace(existingCm.CancellationRemarks?.TrimStart().TrimEnd()) ? "" : existingCm.CancellationRemarks.TrimStart().TrimEnd()) != worksheet.Cells[row, 15].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingCm.CancellationRemarks?.TrimStart().TrimEnd();
                                var adjustedValue = worksheet.Cells[row, 15].Text.TrimStart().TrimEnd();
                                var find  = existingCmInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cmChanges["CancellationRemarks"] = (originalValue, adjustedValue)!;
                                }
                            }

                            if (existingCm.OriginalSalesInvoiceId.ToString()!.TrimStart().TrimEnd() != (worksheet.Cells[row, 16].Text.TrimStart().TrimEnd() == "" ? 0.ToString() : worksheet.Cells[row, 16].Text.TrimStart().TrimEnd()))
                            {
                                var originalValue = existingCm.OriginalSalesInvoiceId.ToString()!.TrimStart().TrimEnd();
                                var adjustedValue = worksheet.Cells[row, 16].Text.TrimStart().TrimEnd() == ""
                                    ? 0.ToString()
                                    : worksheet.Cells[row, 16].Text.TrimStart().TrimEnd();
                                var find  = existingCmInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cmChanges["OriginalSalesInvoiceId"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingCm.OriginalSeriesNumber!.TrimStart().TrimEnd() != worksheet.Cells[row, 17].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingCm.OriginalSeriesNumber.TrimStart().TrimEnd();
                                var adjustedValue = worksheet.Cells[row, 17].Text.TrimStart().TrimEnd();
                                var find  = existingCmInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cmChanges["OriginalSeriesNumber"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingCm.OriginalServiceInvoiceId.ToString()!.TrimStart().TrimEnd() != (worksheet.Cells[row, 18].Text.TrimStart().TrimEnd() == "" ? 0.ToString() : worksheet.Cells[row, 18].Text.TrimStart().TrimEnd()))
                            {
                                var originalValue = existingCm.OriginalServiceInvoiceId.ToString()!.TrimStart().TrimEnd();
                                var adjustedValue = worksheet.Cells[row, 18].Text.TrimStart().TrimEnd() == ""
                                    ? 0.ToString()
                                    : worksheet.Cells[row, 18].Text.TrimStart().TrimEnd();
                                var find  = existingCmInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cmChanges["OriginalServiceInvoiceId"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingCm.OriginalDocumentId.ToString().TrimStart().TrimEnd() != (worksheet.Cells[row, 19].Text.TrimStart().TrimEnd() == "" ? 0.ToString() : worksheet.Cells[row, 19].Text.TrimStart().TrimEnd()))
                            {
                                var originalValue = existingCm.OriginalDocumentId.ToString().TrimStart().TrimEnd();
                                var adjustedValue = worksheet.Cells[row, 19].Text.TrimStart().TrimEnd() == ""
                                    ? 0.ToString()
                                    : worksheet.Cells[row, 19].Text.TrimStart().TrimEnd();
                                var find  = existingCmInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cmChanges["OriginalDocumentId"] = (originalValue, adjustedValue);
                                }
                            }

                            if (cmChanges.Any())
                            {
                                await _creditMemoRepo.LogChangesAsync(existingCm.OriginalDocumentId, cmChanges, _userManager.GetUserName(this.User), existingCm.CreditMemoNo);
                            }

                            continue;
                        }
                        else
                        {
                            #region --Audit Trail Recording

                            if (!creditMemo.CreatedBy.IsNullOrEmpty())
                            {
                                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                                AuditTrail auditTrailBook = new(creditMemo.CreatedBy, $"Create new credit memo# {creditMemo.CreditMemoNo}", "Credit Memo", ipAddress!, creditMemo.CreatedDate);
                                await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                            }
                            if (!creditMemo.PostedBy.IsNullOrEmpty())
                            {
                                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                                AuditTrail auditTrailBook = new(creditMemo.PostedBy, $"Posted credit memo# {creditMemo.CreditMemoNo}", "Credit Memo", ipAddress!, creditMemo.PostedDate);
                                await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                            }

                            #endregion --Audit Trail Recording
                        }

                        creditMemo.SalesInvoiceId = await _dbContext.SalesInvoices
                            .Where(c => c.OriginalDocumentId == creditMemo.OriginalSalesInvoiceId)
                            .Select(c => (int?)c.SalesInvoiceId)
                            .FirstOrDefaultAsync(cancellationToken);

                        creditMemo.ServiceInvoiceId = await _dbContext.ServiceInvoices
                            .Where(c => c.OriginalDocumentId == creditMemo.OriginalServiceInvoiceId)
                            .Select(c => (int?)c.ServiceInvoiceId)
                            .FirstOrDefaultAsync(cancellationToken);

                        if (creditMemo.SalesInvoiceId == null && creditMemo.ServiceInvoiceId == null)
                        {
                            throw new InvalidOperationException("Please upload the Excel file for the sales invoice or service invoice first.");
                        }

                        await _dbContext.CreditMemos.AddAsync(creditMemo, cancellationToken);
                    }
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    var checkChangesOfRecord = await _dbContext.ImportExportLogs
                        .Where(iel => iel.Action == string.Empty).ToListAsync(cancellationToken);
                    if (checkChangesOfRecord.Any())
                    {
                        TempData["importChanges"] = "";
                    }
                    #endregion -- Credit Memo Import --
                }
                catch (OperationCanceledException oce)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = oce.Message;
                    return RedirectToAction(nameof(Index), new { view = DynamicView.CreditMemo });
                }
                catch (InvalidOperationException ioe)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["warning"] = ioe.Message;
                    return RedirectToAction(nameof(Index), new { view = DynamicView.CreditMemo });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(Index), new { view = DynamicView.CreditMemo });
                }
            }
            TempData["success"] = "Uploading Success!";
            return RedirectToAction(nameof(Index), new { view = DynamicView.CreditMemo });
        }

        #endregion

        //Upload as .xlsx file.(Import)
        #region -- import xlsx record --

        [HttpPost]
        public async Task<IActionResult> AasImport(IFormFile file, CancellationToken cancellationToken)
        {
            if (file.Length == 0)
            {
                return RedirectToAction(nameof(Index));
            }

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream, cancellationToken);
                stream.Position = 0;
                await using var transaction = await _aasDbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    if (file.FileName.Contains(CS.Name))
                    {
                        using var package = new ExcelPackage(stream);
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "CreditMemo");

                    var worksheet2 = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "SalesInvoice");

                    var worksheet3 = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "ServiceInvoice");

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

                    #region -- Sales Invoice Import --

                    var siRowCount = worksheet2?.Dimension?.Rows ?? 0;
                    var siDictionary = new Dictionary<string, bool>();
                    var invoiceList = await _aasDbContext
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
                            SalesInvoiceNo = worksheet2.Cells[row, 21].Text,
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
                            PostedBy = worksheet2.Cells[row, 23].Text,
                            PostedDate = DateTime.TryParse(worksheet2.Cells[row, 24].Text, out DateTime postedDate)
                                ? postedDate
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
                            var existingSi = await _aasDbContext.SalesInvoices.FirstOrDefaultAsync(si => si.OriginalDocumentId == invoice.OriginalDocumentId, cancellationToken);
                            var existingSiInLogs = await _dbContext.ImportExportLogs
                                .Where(x => x.DocumentNo == existingSi.SalesInvoiceNo)
                                .ToListAsync(cancellationToken);

                            if (existingSi!.SalesInvoiceNo!.TrimStart().TrimEnd() != worksheet2.Cells[row, 21].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingSi.SalesInvoiceNo.TrimStart().TrimEnd();
                                var adjustedValue = worksheet2.Cells[row, 21].Text.TrimStart().TrimEnd();
                                var find  = existingSiInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    siChanges["SiNo"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSi.OriginalCustomerId.ToString()!.TrimStart().TrimEnd() != worksheet2.Cells[row, 18].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingSi.OriginalCustomerId.ToString()!.TrimStart().TrimEnd();
                                var adjustedValue = worksheet2.Cells[row, 18].Text.TrimStart().TrimEnd();
                                var find  = existingSiInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    siChanges["OriginalCustomerId"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSi.OriginalProductId.ToString()!.TrimStart().TrimEnd() != worksheet2.Cells[row, 20].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingSi.OriginalProductId.ToString()!.TrimStart().TrimEnd();
                                var adjustedValue = worksheet2.Cells[row, 20].Text.TrimStart().TrimEnd();
                                var find  = existingSiInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    siChanges["OriginalProductId"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSi.OtherRefNo.TrimStart().TrimEnd() != worksheet2.Cells[row, 1].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingSi.OtherRefNo.TrimStart().TrimEnd();
                                var adjustedValue = worksheet2.Cells[row, 1].Text.TrimStart().TrimEnd();
                                var find  = existingSiInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    siChanges["OtherRefNo"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSi.Quantity.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet2.Cells[row, 2].Text).ToString("F2").TrimStart().TrimEnd())
                            {
                                var originalValue = existingSi.Quantity.ToString("F2").TrimStart().TrimEnd();
                                var adjustedValue = decimal.Parse(worksheet2.Cells[row, 2].Text).ToString("F2").TrimStart().TrimEnd();
                                var find  = existingSiInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    siChanges["Quantity"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSi.UnitPrice.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet2.Cells[row, 3].Text).ToString("F2").TrimStart().TrimEnd())
                            {
                                var originalValue = existingSi.UnitPrice.ToString("F2").TrimStart().TrimEnd();
                                var adjustedValue = decimal.Parse(worksheet2.Cells[row, 3].Text).ToString("F2").TrimStart().TrimEnd();
                                var find  = existingSiInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    siChanges["UnitPrice"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSi.Amount.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet2.Cells[row, 4].Text).ToString("F2").TrimStart().TrimEnd())
                            {
                                var originalValue = existingSi.Amount.ToString("F2").TrimStart().TrimEnd();
                                var adjustedValue = decimal.Parse(worksheet2.Cells[row, 4].Text).ToString("F2").TrimStart().TrimEnd();
                                var find  = existingSiInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    siChanges["Amount"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSi.Remarks.TrimStart().TrimEnd() != worksheet2.Cells[row, 5].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingSi.Remarks.TrimStart().TrimEnd();
                                var adjustedValue = worksheet2.Cells[row, 5].Text.TrimStart().TrimEnd();
                                var find  = existingSiInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    siChanges["Remarks"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSi.Status.TrimStart().TrimEnd() != worksheet2.Cells[row, 6].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingSi.Status.TrimStart().TrimEnd();
                                var adjustedValue = worksheet2.Cells[row, 6].Text.TrimStart().TrimEnd();
                                var find  = existingSiInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    siChanges["Status"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSi.TransactionDate.ToString("yyyy-MM-dd").TrimStart().TrimEnd() != worksheet2.Cells[row, 7].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingSi.TransactionDate.ToString("yyyy-MM-dd").TrimStart().TrimEnd();
                                var adjustedValue = worksheet2.Cells[row, 7].Text.TrimStart().TrimEnd();
                                var find  = existingSiInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    siChanges["TransactionDate"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSi.Discount.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet2.Cells[row, 8].Text).ToString("F2").TrimStart().TrimEnd())
                            {
                                var originalValue = existingSi.Discount.ToString("F2").TrimStart().TrimEnd();
                                var adjustedValue = decimal.Parse(worksheet2.Cells[row, 8].Text).ToString("F2").TrimStart().TrimEnd();
                                var find  = existingSiInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    siChanges["Discount"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSi.DueDate.ToString("yyyy-MM-dd").TrimStart().TrimEnd() != worksheet2.Cells[row, 13].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingSi.DueDate.ToString("yyyy-MM-dd").TrimStart().TrimEnd();
                                var adjustedValue = worksheet2.Cells[row, 13].Text.TrimStart().TrimEnd();
                                var find  = existingSiInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    siChanges["DueDate"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSi.CreatedBy!.TrimStart().TrimEnd() != worksheet2.Cells[row, 14].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingSi.CreatedBy.TrimStart().TrimEnd();
                                var adjustedValue = worksheet2.Cells[row, 14].Text.TrimStart().TrimEnd();
                                var find  = existingSiInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    siChanges["CreatedBy"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSi.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff").TrimStart().TrimEnd() != worksheet2.Cells[row, 15].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingSi.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff").TrimStart().TrimEnd();
                                var adjustedValue = worksheet2.Cells[row, 15].Text.TrimStart().TrimEnd();
                                var find  = existingSiInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    siChanges["CreatedDate"] = (originalValue, adjustedValue);
                                }
                            }

                            if ((string.IsNullOrWhiteSpace(existingSi.CancellationRemarks?.TrimStart().TrimEnd()) ? "" : existingSi.CancellationRemarks.TrimStart().TrimEnd()) != worksheet2.Cells[row, 16].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingSi.CancellationRemarks?.TrimStart().TrimEnd() ?? String.Empty;
                                var adjustedValue = worksheet2.Cells[row, 16].Text.TrimStart().TrimEnd();
                                var find  = existingSiInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    siChanges["CancellationRemarks"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSi.OriginalSeriesNumber!.TrimStart().TrimEnd() != worksheet2.Cells[row, 21].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingSi.OriginalSeriesNumber.TrimStart().TrimEnd();
                                var adjustedValue = worksheet2.Cells[row, 21].Text.TrimStart().TrimEnd();
                                var find  = existingSiInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    siChanges["OriginalSeriesNumber"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSi.OriginalDocumentId.ToString().TrimStart().TrimEnd() != worksheet2.Cells[row, 22].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingSi.OriginalDocumentId.ToString().TrimStart().TrimEnd();
                                var adjustedValue = worksheet2.Cells[row, 22].Text.TrimStart().TrimEnd();
                                var find  = existingSiInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    siChanges["OriginalDocumentId"] = (originalValue, adjustedValue);
                                }
                            }

                            if (siChanges.Any())
                            {
                                await _salesInvoiceRepo.LogChangesAsync(existingSi.OriginalDocumentId, siChanges, _userManager.GetUserName(this.User), existingSi.SalesInvoiceNo);
                            }

                            continue;
                        }
                        else
                        {
                            #region --Audit Trail Recording

                            if (!invoice.CreatedBy.IsNullOrEmpty())
                            {
                                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                                AuditTrail auditTrailBook = new(invoice.CreatedBy, $"Create new invoice# {invoice.SalesInvoiceNo}", "Sales Invoice", ipAddress!, invoice.CreatedDate);
                                await _aasDbContext.AddAsync(auditTrailBook, cancellationToken);
                            }
                            if (!invoice.PostedBy.IsNullOrEmpty())
                            {
                                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                                AuditTrail auditTrailBook = new(invoice.PostedBy, $"Posted invoice# {invoice.SalesInvoiceNo}", "Sales Invoice", ipAddress!, invoice.PostedDate);
                                await _aasDbContext.AddAsync(auditTrailBook, cancellationToken);
                            }

                            #endregion --Audit Trail Recording
                        }

                        invoice.CustomerId = await _aasDbContext.Customers
                            .Where(c => c.OriginalCustomerId == invoice.OriginalCustomerId)
                            .Select(c => (int?)c.CustomerId)
                            .FirstOrDefaultAsync(cancellationToken) ?? throw new InvalidOperationException("Please upload the Excel file for the customer master file first.");

                        invoice.ProductId = await _aasDbContext.Products
                            .Where(c => c.OriginalProductId == invoice.OriginalProductId)
                            .Select(c => (int?)c.ProductId)
                            .FirstOrDefaultAsync(cancellationToken) ?? throw new InvalidOperationException("Please upload the Excel file for the product master file first.");

                        await _aasDbContext.SalesInvoices.AddAsync(invoice, cancellationToken);
                    }

                    await _aasDbContext.SaveChangesAsync(cancellationToken);

                    #endregion -- Sales Invoice Import --

                    #region -- Service Invoice Import --

                    var svRowCount = worksheet3?.Dimension?.Rows ?? 0;
                    var svDictionary = new Dictionary<string, bool>();
                    var serviceInvoiceList = await _aasDbContext
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
                            ServiceInvoiceNo = worksheet3.Cells[row, 17].Text,
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
                            PostedBy = worksheet3.Cells[row, 20].Text,
                            PostedDate = DateTime.TryParse(worksheet3.Cells[row, 21].Text, out DateTime postedDate) ? postedDate : default,
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
                            var existingSv = await _aasDbContext.ServiceInvoices.FirstOrDefaultAsync(si => si.OriginalDocumentId == serviceInvoice.OriginalDocumentId, cancellationToken);
                            var existingSvInLogs = await _dbContext.ImportExportLogs
                                .Where(x => x.DocumentNo == existingSv.ServiceInvoiceNo)
                                .ToListAsync(cancellationToken);

                            if (existingSv!.ServiceInvoiceNo!.TrimStart().TrimEnd() != worksheet3.Cells[row, 17].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingSv.ServiceInvoiceNo.TrimStart().TrimEnd();
                                var adjustedValue = worksheet3.Cells[row, 17].Text.TrimStart().TrimEnd();
                                var find  = existingSvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    svChanges["SvNo"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSv.DueDate.ToString("yyyy-MM-dd").TrimStart().TrimEnd() != worksheet3.Cells[row, 1].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingSv.DueDate.ToString("yyyy-MM-dd").TrimStart().TrimEnd();
                                var adjustedValue = worksheet3.Cells[row, 1].Text.TrimStart().TrimEnd();
                                var find  = existingSvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    svChanges["DueDate"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSv.Period.ToString("yyyy-MM-dd").TrimStart().TrimEnd() != worksheet3.Cells[row, 2].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingSv.Period.ToString("yyyy-MM-dd").TrimStart().TrimEnd();
                                var adjustedValue = worksheet3.Cells[row, 2].Text.TrimStart().TrimEnd();
                                var find  = existingSvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    svChanges["Period"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSv.Amount.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet3.Cells[row, 3].Text).ToString("F2").TrimStart().TrimEnd())
                            {
                                var originalValue = existingSv.Amount.ToString("F2").TrimStart().TrimEnd();
                                var adjustedValue = decimal.Parse(worksheet3.Cells[row, 3].Text).ToString("F2").TrimStart().TrimEnd();
                                var find  = existingSvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    svChanges["Amount"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSv.Total.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet3.Cells[row, 4].Text).ToString("F2").TrimStart().TrimEnd())
                            {
                                var originalValue = existingSv.Total.ToString("F2").TrimStart().TrimEnd();
                                var adjustedValue = decimal.Parse(worksheet3.Cells[row, 4].Text).ToString("F2").TrimStart().TrimEnd();
                                var find  = existingSvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    svChanges["Total"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSv.Discount.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet3.Cells[row, 5].Text).ToString("F2").TrimStart().TrimEnd())
                            {
                                var originalValue = existingSv.Discount.ToString("F2").TrimStart().TrimEnd();
                                var adjustedValue = decimal.Parse(worksheet3.Cells[row, 5].Text).ToString("F2").TrimStart().TrimEnd();
                                var find  = existingSvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    svChanges["Discount"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSv.CurrentAndPreviousAmount.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet3.Cells[row, 6].Text).ToString("F2").TrimStart().TrimEnd())
                            {
                                var originalValue = existingSv.CurrentAndPreviousAmount.ToString("F2").TrimStart().TrimEnd();
                                var adjustedValue = decimal.Parse(worksheet3.Cells[row, 6].Text).ToString("F2").TrimStart().TrimEnd();
                                var find  = existingSvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    svChanges["CurrentAndPreviousAmount"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSv.UnearnedAmount.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet3.Cells[row, 7].Text).ToString("F2").TrimStart().TrimEnd())
                            {
                                var originalValue = existingSv.UnearnedAmount.ToString("F2").TrimStart().TrimEnd();
                                var adjustedValue = decimal.Parse(worksheet3.Cells[row, 7].Text).ToString("F2").TrimStart().TrimEnd();
                                var find  = existingSvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    svChanges["UnearnedAmount"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSv.Status.TrimStart().TrimEnd() != worksheet3.Cells[row, 8].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingSv.Status.TrimStart().TrimEnd();
                                var adjustedValue = worksheet3.Cells[row, 8].Text.TrimStart().TrimEnd();
                                var find  = existingSvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    svChanges["Status"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSv.Instructions!.TrimStart().TrimEnd() != worksheet3.Cells[row, 11].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingSv.Instructions.TrimStart().TrimEnd();
                                var adjustedValue = worksheet3.Cells[row, 11].Text.TrimStart().TrimEnd();
                                var find  = existingSvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    svChanges["Instructions"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSv.CreatedBy!.TrimStart().TrimEnd() != worksheet3.Cells[row, 13].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingSv.CreatedBy.TrimStart().TrimEnd();
                                var adjustedValue = worksheet3.Cells[row, 13].Text.TrimStart().TrimEnd();
                                var find  = existingSvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    svChanges["CreatedBy"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSv.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff").TrimStart().TrimEnd() != worksheet3.Cells[row, 14].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingSv.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff").TrimStart().TrimEnd();
                                var adjustedValue = worksheet3.Cells[row, 14].Text.TrimStart().TrimEnd();
                                var find  = existingSvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    svChanges["CreatedDate"] = (originalValue, adjustedValue);
                                }
                            }

                            if ((string.IsNullOrWhiteSpace(existingSv.CancellationRemarks?.TrimStart().TrimEnd()) ? "" : existingSv.CancellationRemarks.TrimStart().TrimEnd()) != worksheet3.Cells[row, 15].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingSv.CancellationRemarks?.TrimStart().TrimEnd();
                                var adjustedValue = worksheet3.Cells[row, 15].Text.TrimStart().TrimEnd();
                                var find  = existingSvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    svChanges["CancellationRemarks"] = (originalValue, adjustedValue)!;
                                }
                            }

                            if (existingSv.OriginalCustomerId.ToString()!.TrimStart().TrimEnd() != worksheet3.Cells[row, 16].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingSv.OriginalCustomerId.ToString()!.TrimStart().TrimEnd();
                                var adjustedValue = worksheet3.Cells[row, 16].Text.TrimStart().TrimEnd();
                                var find  = existingSvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    svChanges["OriginalCustomerId"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSv.OriginalSeriesNumber!.TrimStart().TrimEnd() != worksheet3.Cells[row, 17].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingSv.OriginalSeriesNumber.TrimStart().TrimEnd();
                                var adjustedValue = worksheet3.Cells[row, 17].Text.TrimStart().TrimEnd();
                                var find  = existingSvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    svChanges["OriginalSeriesNumber"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSv.OriginalServicesId.ToString()!.TrimStart().TrimEnd() != worksheet3.Cells[row, 18].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingSv.OriginalServicesId.ToString()!.TrimStart().TrimEnd();
                                var adjustedValue = worksheet3.Cells[row, 18].Text.TrimStart().TrimEnd();
                                var find  = existingSvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    svChanges["OriginalServicesId"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingSv.OriginalDocumentId.ToString().TrimStart().TrimEnd() != worksheet3.Cells[row, 19].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingSv.OriginalDocumentId.ToString().TrimStart().TrimEnd();
                                var adjustedValue = worksheet3.Cells[row, 19].Text.TrimStart().TrimEnd();
                                var find  = existingSvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    svChanges["OriginalDocumentId"] = (originalValue, adjustedValue);
                                }
                            }

                            if (svChanges.Any())
                            {
                                await _serviceInvoiceRepo.LogChangesAsync(existingSv.OriginalDocumentId, svChanges, _userManager.GetUserName(this.User), existingSv.ServiceInvoiceNo);
                            }

                            continue;
                        }
                        else
                        {
                            #region --Audit Trail Recording

                            if (!serviceInvoice.CreatedBy.IsNullOrEmpty())
                            {
                                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                                AuditTrail auditTrailBook = new(serviceInvoice.CreatedBy, $"Create new service invoice# {serviceInvoice.ServiceInvoiceNo}", "Service Invoice", ipAddress!, serviceInvoice.CreatedDate);
                                await _aasDbContext.AddAsync(auditTrailBook, cancellationToken);
                            }
                            if (!serviceInvoice.PostedBy.IsNullOrEmpty())
                            {
                                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                                AuditTrail auditTrailBook = new(serviceInvoice.PostedBy, $"Posted service invoice# {serviceInvoice.ServiceInvoiceNo}", "Service Invoice", ipAddress!, serviceInvoice.PostedDate);
                                await _aasDbContext.AddAsync(auditTrailBook, cancellationToken);
                            }

                            #endregion --Audit Trail Recording
                        }

                        serviceInvoice.CustomerId = await _aasDbContext.Customers
                            .Where(sv => sv.OriginalCustomerId == serviceInvoice.OriginalCustomerId)
                            .Select(sv => (int?)sv.CustomerId)
                            .FirstOrDefaultAsync(cancellationToken) ?? throw new InvalidOperationException("Please upload the Excel file for the customer master file first.");

                        serviceInvoice.ServicesId = await _aasDbContext.Services
                            .Where(sv => sv.OriginalServiceId == serviceInvoice.OriginalServicesId)
                            .Select(sv => (int?)sv.ServiceId)
                            .FirstOrDefaultAsync(cancellationToken) ?? throw new InvalidOperationException("Please upload the Excel file for the service master file first.");

                        await _aasDbContext.ServiceInvoices.AddAsync(serviceInvoice, cancellationToken);
                    }
                    await _aasDbContext.SaveChangesAsync(cancellationToken);

                    #endregion -- Service Invoice Import --

                    #region -- Credit Memo Import --

                    var rowCount = worksheet.Dimension.Rows;
                    var cmDictionary = new Dictionary<string, bool>();
                    var creditMemoList = await _aasDbContext
                        .CreditMemos
                        .ToListAsync(cancellationToken);

                    for (int row = 2; row <= rowCount; row++)  // Assuming the first row is the header
                    {
                        var creditMemo = new CreditMemo
                        {
                            CreditMemoNo = worksheet.Cells[row, 17].Text,
                            TransactionDate = DateOnly.TryParse(worksheet.Cells[row, 1].Text, out DateOnly transactionDate) ? transactionDate : default,
                            CreditAmount = decimal.TryParse(worksheet.Cells[row, 2].Text, out decimal debitAmount) ? debitAmount : 0,
                            Description = worksheet.Cells[row, 3].Text,
                            AdjustedPrice = decimal.TryParse(worksheet.Cells[row, 4].Text, out decimal adjustedPrice) ? adjustedPrice : 0,
                            Quantity = decimal.TryParse(worksheet.Cells[row, 5].Text, out decimal quantity) ? quantity : 0,
                            Source = worksheet.Cells[row, 6].Text,
                            Remarks = worksheet.Cells[row, 7].Text,
                            Period = DateOnly.TryParse(worksheet.Cells[row, 8].Text, out DateOnly period) ? period : default,
                            Amount = decimal.TryParse(worksheet.Cells[row, 9].Text, out decimal amount) ? amount : 0,
                            CurrentAndPreviousAmount = decimal.TryParse(worksheet.Cells[row, 10].Text, out decimal currentAndPreviousAmount) ? currentAndPreviousAmount : 0,
                            UnearnedAmount = decimal.TryParse(worksheet.Cells[row, 11].Text, out decimal unearnedAmount) ? unearnedAmount : 0,
                            ServicesId = int.TryParse(worksheet.Cells[row, 12].Text, out int servicesId) ? servicesId : 0,
                            CreatedBy = worksheet.Cells[row, 13].Text,
                            CreatedDate = DateTime.TryParse(worksheet.Cells[row, 14].Text, out DateTime createdDate) ? createdDate : default,
                            PostedBy = worksheet.Cells[row, 20].Text,
                            PostedDate = DateTime.TryParse(worksheet.Cells[row, 21].Text, out DateTime postedDate) ? postedDate : default,
                            CancellationRemarks = worksheet.Cells[row, 15].Text,
                            OriginalSalesInvoiceId = int.TryParse(worksheet.Cells[row, 16].Text, out int originalSalesInvoiceId) ? originalSalesInvoiceId : 0,
                            OriginalSeriesNumber = worksheet.Cells[row, 17].Text,
                            OriginalServiceInvoiceId = int.TryParse(worksheet.Cells[row, 18].Text, out int originalServiceInvoiceId) ? originalServiceInvoiceId : 0,
                            OriginalDocumentId = int.TryParse(worksheet.Cells[row, 19].Text, out int originalDocumentId) ? originalDocumentId : 0,
                        };

                        if (!cmDictionary.TryAdd(creditMemo.OriginalSeriesNumber, true))
                        {
                            continue;
                        }

                        if (creditMemoList.Any(cm => cm.OriginalDocumentId == creditMemo.OriginalDocumentId))
                        {
                            var cmChanges = new Dictionary<string, (string OriginalValue, string NewValue)>();
                            var existingCm = await _aasDbContext.CreditMemos.FirstOrDefaultAsync(si => si.OriginalDocumentId == creditMemo.OriginalDocumentId, cancellationToken);
                            var existingCmInLogs = await _dbContext.ImportExportLogs
                                .Where(x => x.DocumentNo == existingCm.CreditMemoNo)
                                .ToListAsync(cancellationToken);

                            if (existingCm!.CreditMemoNo!.TrimStart().TrimEnd() != worksheet.Cells[row, 17].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingCm.CreditMemoNo.TrimStart().TrimEnd();
                                var adjustedValue = worksheet.Cells[row, 17].Text.TrimStart().TrimEnd();
                                var find  = existingCmInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cmChanges["CMNo"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingCm.TransactionDate.ToString("yyyy-MM-dd").TrimStart().TrimEnd() != worksheet.Cells[row, 1].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingCm.TransactionDate.ToString("yyyy-MM-dd").TrimStart().TrimEnd();
                                var adjustedValue = worksheet.Cells[row, 1].Text.TrimStart().TrimEnd();
                                var find  = existingCmInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cmChanges["TransactionDate"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingCm.CreditAmount.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet.Cells[row, 2].Text).ToString("F2").TrimStart().TrimEnd())
                            {
                                var originalValue = existingCm.CreditAmount.ToString("F2").TrimStart().TrimEnd();
                                var adjustedValue = decimal.Parse(worksheet.Cells[row, 2].Text).ToString("F2").TrimStart().TrimEnd();
                                var find  = existingCmInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cmChanges["CreditAmount"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingCm.Description.TrimStart().TrimEnd() != worksheet.Cells[row, 3].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingCm.Description.TrimStart().TrimEnd();
                                var adjustedValue = worksheet.Cells[row, 3].Text.TrimStart().TrimEnd();
                                var find  = existingCmInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cmChanges["Description"] = (originalValue, adjustedValue);
                                }
                            }

                            if ((existingCm.AdjustedPrice != null ? existingCm.AdjustedPrice?.ToString("F2").TrimStart().TrimEnd() : 0.ToString("F2")) != decimal.Parse(worksheet.Cells[row, 4].Text.TrimStart().TrimEnd() != "" ? worksheet.Cells[row, 4].Text.TrimStart().TrimEnd() : 0.ToString("F2")).ToString("F2").TrimStart().TrimEnd())
                            {
                                var originalValue = existingCm.AdjustedPrice?.ToString("F2").TrimStart().TrimEnd();
                                var adjustedValue = decimal
                                    .Parse(worksheet.Cells[row, 4].Text.TrimStart().TrimEnd() != ""
                                        ? worksheet.Cells[row, 4].Text.TrimStart().TrimEnd()
                                        : 0.ToString("F2")).ToString("F2").TrimStart().TrimEnd();
                                var find  = existingCmInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cmChanges["AdjustedPrice"] = (originalValue, adjustedValue);
                                }
                            }

                            if ((existingCm.Quantity != null ? existingCm.Quantity?.ToString("F0").TrimStart().TrimEnd() : 0.ToString("F0")) != decimal.Parse(worksheet.Cells[row, 5].Text.TrimStart().TrimEnd() != "" ? worksheet.Cells[row, 5].Text.TrimStart().TrimEnd() : 0.ToString("F0")).ToString("F0").TrimStart().TrimEnd())
                            {
                                var originalValue = existingCm.Quantity?.ToString("F0").TrimStart().TrimEnd();
                                var adjustedValue = decimal
                                    .Parse(worksheet.Cells[row, 5].Text.TrimStart().TrimEnd() != ""
                                        ? worksheet.Cells[row, 5].Text.TrimStart().TrimEnd()
                                        : 0.ToString("F0")).ToString("F0").TrimStart().TrimEnd();
                                var find  = existingCmInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cmChanges["Quantity"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingCm.Source.TrimStart().TrimEnd() != worksheet.Cells[row, 6].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingCm.Source.TrimStart().TrimEnd();
                                var adjustedValue = worksheet.Cells[row, 6].Text.TrimStart().TrimEnd();
                                var find  = existingCmInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cmChanges["Source"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingCm.Remarks!.TrimStart().TrimEnd() != worksheet.Cells[row, 7].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingCm.Remarks.TrimStart().TrimEnd();
                                var adjustedValue = worksheet.Cells[row, 7].Text.TrimStart().TrimEnd();
                                var find  = existingCmInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cmChanges["Remarks"] = (originalValue, adjustedValue);
                                }
                            }

                            string cellValue = worksheet.Cells[row, 8].Text.Trim();

                            if (DateOnly.TryParse(cellValue, out DateOnly parsedDate))
                            {
                                if (existingCm.Period.ToString("yyyy-MM") != parsedDate.ToString("yyyy-MM"))
                                {
                                    var originalValue = existingCm.Period.ToString("yyyy-MM");
                                    var adjustedValue = parsedDate.ToString("yyyy-MM");
                                    var find  = existingCmInLogs
                                        .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                    if (!find.Any())
                                    {
                                        cmChanges["Period"] = (originalValue, adjustedValue);
                                    }
                                }
                            }

                            if ((existingCm.Amount != null ? existingCm.Amount?.ToString("F2").TrimStart().TrimEnd() : 0.ToString("F2")) != decimal.Parse(worksheet.Cells[row, 9].Text.TrimStart().TrimEnd() != "" ? worksheet.Cells[row, 9].Text.TrimStart().TrimEnd() : 0.ToString("F2")).ToString("F2").TrimStart().TrimEnd())
                            {
                                var originalValue = existingCm.Amount?.ToString("F2").TrimStart().TrimEnd();
                                var adjustedValue = decimal.Parse(worksheet.Cells[row, 9].Text).ToString("F2").TrimStart().TrimEnd();
                                var find  = existingCmInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cmChanges["Amount"] = (originalValue, adjustedValue)!;
                                }
                            }

                            if (existingCm.CurrentAndPreviousAmount.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet.Cells[row, 10].Text).ToString("F2").TrimStart().TrimEnd())
                            {
                                var originalValue = existingCm.CurrentAndPreviousAmount.ToString("F2").TrimStart().TrimEnd();
                                var adjustedValue = decimal.Parse(worksheet.Cells[row, 10].Text).ToString("F2").TrimStart().TrimEnd();
                                var find  = existingCmInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cmChanges["CurrentAndPreviousAmount"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingCm.UnearnedAmount.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet.Cells[row, 11].Text).ToString("F2").TrimStart().TrimEnd())
                            {
                                var originalValue = existingCm.UnearnedAmount.ToString("F2").TrimStart().TrimEnd();
                                var adjustedValue = decimal.Parse(worksheet.Cells[row, 11].Text).ToString("F2").TrimStart().TrimEnd();
                                var find  = existingCmInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cmChanges["UnearnedAmount"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingCm.ServicesId.ToString() != (worksheet.Cells[row, 12].Text == "" ? 0.ToString() : worksheet.Cells[row, 12].Text))
                            {
                                var originalValue = existingCm.ServicesId.ToString()!.TrimStart().TrimEnd();
                                var adjustedValue = worksheet.Cells[row, 12].Text.TrimStart().TrimEnd() == ""
                                    ? 0.ToString()
                                    : worksheet.Cells[row, 12].Text.TrimStart().TrimEnd();
                                var find  = existingCmInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cmChanges["ServicesId"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingCm.CreatedBy!.TrimStart().TrimEnd() != worksheet.Cells[row, 13].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingCm.CreatedBy.TrimStart().TrimEnd();
                                var adjustedValue = worksheet.Cells[row, 13].Text.TrimStart().TrimEnd();
                                var find  = existingCmInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cmChanges["CreatedBy"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingCm.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff").TrimStart().TrimEnd() != worksheet.Cells[row, 14].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingCm.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff").TrimStart().TrimEnd();
                                var adjustedValue = worksheet.Cells[row, 14].Text.TrimStart().TrimEnd();
                                var find  = existingCmInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cmChanges["CreatedDate"] = (originalValue, adjustedValue);
                                }
                            }

                            if ((string.IsNullOrWhiteSpace(existingCm.CancellationRemarks?.TrimStart().TrimEnd()) ? "" : existingCm.CancellationRemarks.TrimStart().TrimEnd()) != worksheet.Cells[row, 15].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingCm.CancellationRemarks?.TrimStart().TrimEnd();
                                var adjustedValue = worksheet.Cells[row, 15].Text.TrimStart().TrimEnd();
                                var find  = existingCmInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cmChanges["CancellationRemarks"] = (originalValue, adjustedValue)!;
                                }
                            }

                            if (existingCm.OriginalSalesInvoiceId.ToString()!.TrimStart().TrimEnd() != (worksheet.Cells[row, 16].Text.TrimStart().TrimEnd() == "" ? 0.ToString() : worksheet.Cells[row, 16].Text.TrimStart().TrimEnd()))
                            {
                                var originalValue = existingCm.OriginalSalesInvoiceId.ToString()!.TrimStart().TrimEnd();
                                var adjustedValue = worksheet.Cells[row, 16].Text.TrimStart().TrimEnd() == ""
                                    ? 0.ToString()
                                    : worksheet.Cells[row, 16].Text.TrimStart().TrimEnd();
                                var find  = existingCmInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cmChanges["OriginalSalesInvoiceId"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingCm.OriginalSeriesNumber!.TrimStart().TrimEnd() != worksheet.Cells[row, 17].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingCm.OriginalSeriesNumber.TrimStart().TrimEnd();
                                var adjustedValue = worksheet.Cells[row, 17].Text.TrimStart().TrimEnd();
                                var find  = existingCmInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cmChanges["OriginalSeriesNumber"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingCm.OriginalServiceInvoiceId.ToString()!.TrimStart().TrimEnd() != (worksheet.Cells[row, 18].Text.TrimStart().TrimEnd() == "" ? 0.ToString() : worksheet.Cells[row, 18].Text.TrimStart().TrimEnd()))
                            {
                                var originalValue = existingCm.OriginalServiceInvoiceId.ToString()!.TrimStart().TrimEnd();
                                var adjustedValue = worksheet.Cells[row, 18].Text.TrimStart().TrimEnd() == ""
                                    ? 0.ToString()
                                    : worksheet.Cells[row, 18].Text.TrimStart().TrimEnd();
                                var find  = existingCmInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cmChanges["OriginalServiceInvoiceId"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingCm.OriginalDocumentId.ToString().TrimStart().TrimEnd() != (worksheet.Cells[row, 19].Text.TrimStart().TrimEnd() == "" ? 0.ToString() : worksheet.Cells[row, 19].Text.TrimStart().TrimEnd()))
                            {
                                var originalValue = existingCm.OriginalDocumentId.ToString().TrimStart().TrimEnd();
                                var adjustedValue = worksheet.Cells[row, 19].Text.TrimStart().TrimEnd() == ""
                                    ? 0.ToString()
                                    : worksheet.Cells[row, 19].Text.TrimStart().TrimEnd();
                                var find  = existingCmInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cmChanges["OriginalDocumentId"] = (originalValue, adjustedValue);
                                }
                            }

                            if (cmChanges.Any())
                            {
                                await _creditMemoRepo.LogChangesAsync(existingCm.OriginalDocumentId, cmChanges, _userManager.GetUserName(this.User), existingCm.CreditMemoNo);
                            }

                            continue;
                        }
                        else
                        {
                            #region --Audit Trail Recording

                            if (!creditMemo.CreatedBy.IsNullOrEmpty())
                            {
                                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                                AuditTrail auditTrailBook = new(creditMemo.CreatedBy, $"Create new credit memo# {creditMemo.CreditMemoNo}", "Credit Memo", ipAddress!, creditMemo.CreatedDate);
                                await _aasDbContext.AddAsync(auditTrailBook, cancellationToken);
                            }
                            if (!creditMemo.PostedBy.IsNullOrEmpty())
                            {
                                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                                AuditTrail auditTrailBook = new(creditMemo.PostedBy, $"Posted credit memo# {creditMemo.CreditMemoNo}", "Credit Memo", ipAddress!, creditMemo.PostedDate);
                                await _aasDbContext.AddAsync(auditTrailBook, cancellationToken);
                            }

                            #endregion --Audit Trail Recording
                        }

                        creditMemo.SalesInvoiceId = await _aasDbContext.SalesInvoices
                            .Where(c => c.OriginalDocumentId == creditMemo.OriginalSalesInvoiceId)
                            .Select(c => (int?)c.SalesInvoiceId)
                            .FirstOrDefaultAsync(cancellationToken);

                        creditMemo.ServiceInvoiceId = await _aasDbContext.ServiceInvoices
                            .Where(c => c.OriginalDocumentId == creditMemo.OriginalServiceInvoiceId)
                            .Select(c => (int?)c.ServiceInvoiceId)
                            .FirstOrDefaultAsync(cancellationToken);

                        if (creditMemo.SalesInvoiceId == null && creditMemo.ServiceInvoiceId == null)
                        {
                            throw new InvalidOperationException("Please upload the Excel file for the sales invoice or service invoice first.");
                        }

                        await _aasDbContext.CreditMemos.AddAsync(creditMemo, cancellationToken);
                    }
                    await _aasDbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    var checkChangesOfRecord = await _dbContext.ImportExportLogs
                        .Where(iel => iel.Action == string.Empty).ToListAsync(cancellationToken);
                    if (checkChangesOfRecord.Any())
                    {
                        TempData["importChanges"] = "";
                    }
                    #endregion -- Credit Memo Import --
                    }
                    else
                    {
                        TempData["warning"] = "The Uploaded Excel file is not related to AAS.";
                    }
                }
                catch (OperationCanceledException oce)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = oce.Message;
                    return RedirectToAction(nameof(Index), new { view = DynamicView.CreditMemo });
                }
                catch (InvalidOperationException ioe)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["warning"] = ioe.Message;
                    return RedirectToAction(nameof(Index), new { view = DynamicView.CreditMemo });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
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
