using Accounting_System.Data;
using Accounting_System.Models;
using Accounting_System.Models.AccountsPayable;
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
    public class JournalVoucherController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly JournalVoucherRepo _journalVoucherRepo;

        private readonly CheckVoucherRepo _checkVoucherRepo;

        private readonly ReceivingReportRepo _receivingReportRepo;

        private readonly PurchaseOrderRepo _purchaseOrderRepo;

        private readonly IWebHostEnvironment _webHostEnvironment;

        private readonly GeneralRepo _generalRepo;

        public JournalVoucherController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, IWebHostEnvironment webHostEnvironment, JournalVoucherRepo journalVoucherRepo, GeneralRepo generalRepo, CheckVoucherRepo checkVoucherRepo, ReceivingReportRepo receivingReportRepo, PurchaseOrderRepo purchaseOrderRepo)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
            _journalVoucherRepo = journalVoucherRepo;
            _generalRepo = generalRepo;
            _checkVoucherRepo = checkVoucherRepo;
            _receivingReportRepo = receivingReportRepo;
            _purchaseOrderRepo = purchaseOrderRepo;
        }

        public async Task<IActionResult> Index(string? view, CancellationToken cancellationToken)
        {
            if (view == nameof(DynamicView.JournalVoucher))
            {
                var journalVouchers = await _journalVoucherRepo.GetJournalVouchersAsync(cancellationToken);

                return View("ImportExportIndex", journalVouchers);
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetJournalVouchers([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var journalVouchers = await _journalVoucherRepo.GetJournalVouchersAsync(cancellationToken);
                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search?.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();
                    journalVouchers = journalVouchers
                        .Where(jv =>
                            jv.JVNo.ToLower().Contains(searchValue) ||
                            jv.Date.ToString("MMM dd, yyyy").ToLower().Contains(searchValue) ||
                            jv.References?.ToLower().Contains(searchValue) == true ||
                            jv.Particulars.ToLower().Contains(searchValue) ||
                            jv.CRNo?.ToLower().Contains(searchValue) == true ||
                            jv.JVReason.ToLower().Contains(searchValue) ||
                            jv.CheckVoucherHeader?.CVNo?.ToLower().Contains(searchValue) == true ||
                            jv.CreatedBy.ToLower().Contains(searchValue)
                            )
                        .ToList();
                }
                // Sorting
                if (parameters.Order != null && parameters.Order.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Data;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";
                    journalVouchers = journalVouchers
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}")
                        .ToList();
                }
                var totalRecords = journalVouchers.Count();
                var pagedData = journalVouchers
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
        public async Task<IActionResult> GetAllJournalVoucherIds(CancellationToken cancellationToken)
        {
            var journalVoucherIds = await _dbContext.JournalVoucherHeaders
                                     .Select(jv => jv.Id) // Assuming Id is the primary key
                                     .ToListAsync(cancellationToken);
            return Json(journalVoucherIds);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var viewModel = new JournalVoucherVM
            {
                Header = new JournalVoucherHeader(),
                Details = new List<JournalVoucherDetail>()
            };

            viewModel.Header.COA = await _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == 4 || coa.Level == 5)
                .OrderBy(coa => coa.AccountId)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);
            viewModel.Header.CheckVoucherHeaders = await _dbContext.CheckVoucherHeaders
                .Where(cvh => cvh.IsPosted)
                .OrderBy(c => c.Id)
                .Select(cvh => new SelectListItem
                {
                    Value = cvh.Id.ToString(),
                    Text = cvh.CVNo
                })
                .ToListAsync(cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(JournalVoucherVM? model, string[] accountNumber, decimal[]? debit, decimal[]? credit, CancellationToken cancellationToken)
        {
            model.Header.COA = await _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == 4 || coa.Level == 5)
                .OrderBy(coa => coa.AccountId)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            model.Header.CheckVoucherHeaders = await _dbContext.CheckVoucherHeaders
                .OrderBy(c => c.Id)
                .Select(cvh => new SelectListItem
                {
                    Value = cvh.Id.ToString(),
                    Text = cvh.CVNo
                })
                .ToListAsync(cancellationToken);

            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    #region --Validating series

                    var generateJvNo = await _journalVoucherRepo.GenerateJVNo(cancellationToken);
                    var getLastNumber = long.Parse(generateJvNo.Substring(2));

                    if (getLastNumber > 9999999999)
                    {
                        TempData["error"] = "You reached the maximum Series Number";
                        return View(model);
                    }

                    var totalRemainingSeries = 9999999999 - getLastNumber;
                    if (getLastNumber >= 9999999899)
                    {
                        TempData["warning"] = $"Check Voucher created successfully, Warning {totalRemainingSeries} series numbers remaining";
                    }
                    else
                    {
                        TempData["success"] = "Check Voucher created successfully";
                    }

                    #endregion --Validating series

                    #region --Saving the default entries

                    //JV Header Entry
                    model.Header.JVNo = generateJvNo;
                    model.Header.CreatedBy = _userManager.GetUserName(this.User);

                    await _dbContext.AddAsync(model.Header, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    #endregion --Saving the default entries

                    #region --CV Details Entry

                    var cvDetails = new List<JournalVoucherDetail>();

                    var totalDebit = 0m;
                    var totalCredit = 0m;
                    for (int i = 0; i < accountNumber.Length; i++)
                    {
                        var currentAccountNumber = accountNumber[i];
                        var accountTitle = await _dbContext.ChartOfAccounts
                            .FirstOrDefaultAsync(coa => coa.AccountNumber == currentAccountNumber, cancellationToken);
                        var currentDebit = debit[i];
                        var currentCredit = credit[i];
                        totalDebit += debit[i];
                        totalCredit += credit[i];

                        cvDetails.Add(
                            new JournalVoucherDetail
                            {
                                AccountNo = currentAccountNumber,
                                AccountName = accountTitle.AccountName,
                                TransactionNo = generateJvNo,
                                Debit = currentDebit,
                                Credit = currentCredit,
                                JVHeaderId = model.Header.Id
                            }
                        );
                    }
                    if (totalDebit != totalCredit)
                    {
                        TempData["error"] = "The debit and credit should be equal!";
                        return View(model);
                    }

                    await _dbContext.JournalVoucherDetails.AddRangeAsync(cvDetails, cancellationToken);

                    #endregion --CV Details Entry

                    #region --Audit Trail Recording

                    if (model.Header.OriginalSeriesNumber == null && model.Header.OriginalDocumentId == 0)
                    {
                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        AuditTrail auditTrailBook = new(model.Header.CreatedBy, $"Create new journal voucher# {model.Header.JVNo}", "Journal Voucher", ipAddress);
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                    }

                    #endregion --Audit Trail Recording

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
            else
            {
                TempData["error"] = "The information you submitted is not valid!";
                return View(model);
            }
        }

        public async Task<IActionResult> GetCV(int id, CancellationToken cancellationToken)
        {
            var model = await _dbContext.CheckVoucherHeaders
                .Include(s => s.Supplier)
                .Include(cvd => cvd.Details)
                .FirstOrDefaultAsync(cvh => cvh.Id == id, cancellationToken);

            if (model != null)
            {
                return Json(new
                {
                    CVNo = model.CVNo,
                    Date = model.Date,
                    Name = model.Supplier.Name,
                    Address = model.Supplier.Address,
                    TinNo = model.Supplier.TinNo,
                    PONo = model.PONo,
                    SINo = model.SINo,
                    Payee = model.Payee,
                    Amount = model.Total,
                    Particulars = model.Particulars,
                    CheckNo = model.CheckNo,
                    AccountNo = model.Details.Select(jvd => jvd.AccountNo),
                    AccountName = model.Details.Select(jvd => jvd.AccountName),
                    Debit = model.Details.Select(jvd => jvd.Debit),
                    Credit = model.Details.Select(jvd => jvd.Credit),
                    TotalDebit = model.Details.Select(cvd => cvd.Debit).Sum(),
                    TotalCredit = model.Details.Select(cvd => cvd.Credit).Sum(),
                });
            }

            return Json(null);
        }

        [HttpGet]
        public async Task<IActionResult> Print(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            var header = await _dbContext.JournalVoucherHeaders
                .Include(cv => cv.CheckVoucherHeader)
                .ThenInclude(supplier => supplier.Supplier)
                .FirstOrDefaultAsync(jvh => jvh.Id == id.Value, cancellationToken);

            if (header == null)
            {
                return NotFound();
            }

            var details = await _dbContext.JournalVoucherDetails
                .Where(jvd => jvd.TransactionNo == header.JVNo)
                .ToListAsync(cancellationToken);

            //if (header.Category == "Trade")
            //{
            //    var siArray = new string[header.RRNo.Length];
            //    for (int i = 0; i < header.RRNo.Length; i++)
            //    {
            //        var rrValue = header.RRNo[i];

            //        var rr = await _dbContext.ReceivingReports
            //                    .FirstOrDefaultAsync(p => p.RRNo == rrValue);

            //        siArray[i] = rr.SupplierInvoiceNumber;
            //    }

            //    ViewBag.SINoArray = siArray;
            //}

            var viewModel = new JournalVoucherVM
            {
                Header = header,
                Details = details
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Printed(int id, CancellationToken cancellationToken)
        {
            var jv = await _dbContext.JournalVoucherHeaders.FindAsync(id, cancellationToken);
            if (jv != null && !jv.IsPrinted)
            {

                #region --Audit Trail Recording

                if (jv.OriginalSeriesNumber == null && jv.OriginalDocumentId == 0)
                {
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    var printedBy = _userManager.GetUserName(this.User);
                    AuditTrail auditTrailBook = new(printedBy, $"Printed original copy of jv# {jv.JVNo}", "Journal Voucher", ipAddress);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                }

                #endregion --Audit Trail Recording

                jv.IsPrinted = true;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            return RedirectToAction(nameof(Print), new { id });
        }

        public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
        {
            var modelHeader = await _dbContext.JournalVoucherHeaders.FindAsync(id, cancellationToken);

            if (modelHeader != null)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    var modelDetails = await _dbContext.JournalVoucherDetails.Where(jvd => jvd.TransactionNo == modelHeader.JVNo).ToListAsync(cancellationToken);
                    if (!modelHeader.IsPosted)
                    {
                        modelHeader.IsPosted = true;
                        modelHeader.PostedBy = _userManager.GetUserName(this.User);
                        modelHeader.PostedDate = DateTime.Now;

                        #region --General Ledger Book Recording(GL)--

                        var accountTitlesDto = await _generalRepo.GetListOfAccountTitleDto(cancellationToken);
                        var ledgers = new List<GeneralLedgerBook>();
                        foreach (var details in modelDetails)
                        {
                            var account = accountTitlesDto.Find(c => c.AccountNumber == details.AccountNo) ?? throw new ArgumentException($"Account number '{details.AccountNo}', Account title '{details.AccountName}' not found.");
                            ledgers.Add(
                                    new GeneralLedgerBook
                                    {
                                        Date = modelHeader.Date,
                                        Reference = modelHeader.JVNo,
                                        Description = modelHeader.Particulars,
                                        AccountNo = account.AccountNumber,
                                        AccountTitle = account.AccountName,
                                        Debit = details.Debit,
                                        Credit = details.Credit,
                                        CreatedBy = modelHeader.CreatedBy,
                                        CreatedDate = modelHeader.CreatedDate
                                    }
                                );
                        }

                        if (!_generalRepo.IsJournalEntriesBalanced(ledgers))
                        {
                            throw new ArgumentException("Debit and Credit is not equal, check your entries.");
                        }

                        await _dbContext.GeneralLedgerBooks.AddRangeAsync(ledgers, cancellationToken);

                        #endregion --General Ledger Book Recording(GL)--

                        #region --Journal Book Recording(JV)--

                        var journalBook = new List<JournalBook>();
                        foreach (var details in modelDetails)
                        {
                            journalBook.Add(
                                    new JournalBook
                                    {
                                        Date = modelHeader.Date,
                                        Reference = modelHeader.JVNo,
                                        Description = modelHeader.Particulars,
                                        AccountTitle = details.AccountNo + " " + details.AccountName,
                                        Debit = details.Debit,
                                        Credit = details.Credit,
                                        CreatedBy = modelHeader.CreatedBy,
                                        CreatedDate = modelHeader.CreatedDate
                                    }
                                );
                        }

                        await _dbContext.JournalBooks.AddRangeAsync(journalBook, cancellationToken);

                        #endregion --Journal Book Recording(JV)--

                        #region --Audit Trail Recording

                        if (modelHeader.OriginalSeriesNumber == null && modelHeader.OriginalDocumentId == 0)
                        {
                            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                            AuditTrail auditTrailBook = new(modelHeader.PostedBy, $"Posted journal voucher# {modelHeader.JVNo}", "Journal Voucher", ipAddress);
                            await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                        }

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Journal Voucher has been Posted.";
                    }
                    return RedirectToAction(nameof(Print), new { id = id });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(Print), new { id = id });
                }
            }

            return NotFound();
        }

        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {
            var model = await _dbContext.JournalVoucherHeaders.FindAsync(id, cancellationToken);
            var findJVInJB = await _dbContext.JournalBooks.Where(jb => jb.Reference == model.JVNo).ToListAsync(cancellationToken);
            var findJVInGL = await _dbContext.GeneralLedgerBooks.Where(jb => jb.Reference == model.JVNo).ToListAsync(cancellationToken);
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

                        if (findJVInJB.Any())
                        {
                            await _generalRepo.RemoveRecords<JournalBook>(crb => crb.Reference == model.JVNo, cancellationToken);
                        }
                        if (findJVInGL.Any())
                        {
                            await _generalRepo.RemoveRecords<GeneralLedgerBook>(gl => gl.Reference == model.JVNo, cancellationToken);
                        }

                        #region --Audit Trail Recording

                        if (model.OriginalSeriesNumber == null && model.OriginalDocumentId == 0)
                        {
                            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                            AuditTrail auditTrailBook = new(model.VoidedBy, $"Voided journal voucher# {model.JVNo}", "Journal Voucher", ipAddress);
                            await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                        }

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Journal Voucher has been Voided.";
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
            var model = await _dbContext.JournalVoucherHeaders.FindAsync(id, cancellationToken);
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

                        if (model.OriginalSeriesNumber == null && model.OriginalDocumentId == 0)
                        {
                            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                            AuditTrail auditTrailBook = new(model.CanceledBy, $"Cancelled journal voucher# {model.JVNo}", "Journal Voucher", ipAddress);
                            await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                        }

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Journal Voucher has been Cancelled.";
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
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }
            var exisitngJV = await _dbContext.JournalVoucherHeaders.FindAsync(id, cancellationToken);
            var existingHeaderModel = await _dbContext.JournalVoucherHeaders
                .Include(jv => jv.CheckVoucherHeader)
                .FirstOrDefaultAsync(cvh => cvh.Id == id, cancellationToken);
            var existingDetailsModel = await _dbContext.JournalVoucherDetails
                .Where(cvd => cvd.TransactionNo == existingHeaderModel.JVNo)
                .ToListAsync(cancellationToken);

            if (existingHeaderModel == null || existingDetailsModel == null)
            {
                return NotFound();
            }

            var accountNumbers = existingDetailsModel.Select(model => model.AccountNo).ToArray();
            var accountTitles = existingDetailsModel.Select(model => model.AccountName).ToArray();
            var debit = existingDetailsModel.Select(model => model.Debit).ToArray();
            var credit = existingDetailsModel.Select(model => model.Credit).ToArray();
            var poIds = await _dbContext.PurchaseOrders.Where(model => exisitngJV.CheckVoucherHeader.PONo.Contains(model.PONo)).Select(model => model.Id).ToArrayAsync(cancellationToken);
            var rrIds = await _dbContext.ReceivingReports.Where(model => exisitngJV.CheckVoucherHeader.RRNo.Contains(model.RRNo)).Select(model => model.Id).ToArrayAsync(cancellationToken);

            var coa = await _dbContext.ChartOfAccounts
                        .Where(coa => !new[] { "202010200", "202010100", "101010100" }.Any(excludedNumber => coa.AccountNumber.Contains(excludedNumber)) && coa.Level == 4 || coa.Level == 5)
                        .Select(s => new SelectListItem
                        {
                            Value = s.AccountNumber,
                            Text = s.AccountNumber + " " + s.AccountName
                        })
                        .ToListAsync(cancellationToken);

            JournalVoucherViewModel model = new()
            {
                JVId = existingHeaderModel.Id,
                JVNo = existingHeaderModel.JVNo,
                TransactionDate = existingHeaderModel.Date,
                References = existingHeaderModel.References,
                CVId = existingHeaderModel.CVId,
                Particulars = existingHeaderModel.Particulars,
                CRNo = existingHeaderModel.CRNo,
                JVReason = existingHeaderModel.JVReason,
                AccountNumber = accountNumbers,
                AccountTitle = accountTitles,
                Debit = debit,
                Credit = credit,
                CheckVoucherHeaders = await _dbContext.CheckVoucherHeaders
                .OrderBy(c => c.Id)
                .Select(cvh => new SelectListItem
                {
                    Value = cvh.Id.ToString(),
                    Text = cvh.CVNo
                })
                .ToListAsync(cancellationToken),
                COA = await _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == 4 || coa.Level == 5)
                .OrderBy(coa => coa.AccountId)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken)
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(JournalVoucherViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    var model = await _dbContext.JournalVoucherHeaders
                                    .Include(jvd => jvd.Details)
                                    .FirstOrDefaultAsync(jvh => jvh.Id == viewModel.JVId, cancellationToken);

                    #region --Saving the default entries

                    model.JVNo = viewModel.JVNo;
                    model.Date = viewModel.TransactionDate;
                    model.References = viewModel.References;
                    model.CVId = viewModel.CVId;
                    model.Particulars = viewModel.Particulars;
                    model.CRNo = viewModel.CRNo;
                    model.JVReason = viewModel.JVReason;

                    #endregion --Saving the default entries

                    #region --CV Details Entry

                    // Dictionary to keep track of AccountNo and their ids for comparison
                    var accountTitleDict = new Dictionary<string, List<int>>();
                    foreach (var details in model.Details)
                    {
                        if (!accountTitleDict.ContainsKey(details.AccountNo))
                        {
                            accountTitleDict[details.AccountNo] = new List<int>();
                        }
                        accountTitleDict[details.AccountNo].Add(details.Id);
                    }

                    // Add or update records
                    for (int i = 0; i < viewModel.AccountTitle?.Length; i++)
                    {

                        if (accountTitleDict.TryGetValue(viewModel.AccountNumber?[i], out var ids))
                        {
                            // Update the first matching record and remove it from the list
                            var detailsId = ids.First();
                            ids.RemoveAt(0);
                            var details = model.Details.First(o => o.Id == detailsId);

                            var acctNo = await _dbContext.ChartOfAccounts
                                .FirstOrDefaultAsync(x => x.AccountName == viewModel.AccountTitle[i]);

                            details.AccountNo = acctNo?.AccountNumber ?? "";
                            details.AccountName = viewModel.AccountTitle[i];
                            details.Debit = viewModel.Debit[i];
                            details.Credit = viewModel.Credit[i];
                            details.TransactionNo = model.JVNo;
                            details.JVHeaderId = model.Id;

                            if (ids.Count == 0)
                            {
                                accountTitleDict.Remove(viewModel.AccountNumber[i]);
                            }
                        }
                        else
                        {
                            // Add new record
                            var newDetails = new JournalVoucherDetail
                            {
                                AccountNo = viewModel.AccountNumber[i],
                                AccountName = viewModel.AccountTitle[i],
                                Debit = viewModel.Debit[i],
                                Credit = viewModel.Credit[i],
                                TransactionNo = model.JVNo,
                                JVHeaderId = model.Id
                            };
                            await _dbContext.JournalVoucherDetails.AddAsync(newDetails, cancellationToken);
                        }
                    }

                    // Remove remaining records that were duplicates
                    foreach (var ids in accountTitleDict.Values)
                    {
                        foreach (var id in ids)
                        {
                            var details = model.Details.First(o => o.Id == id);
                            _dbContext.JournalVoucherDetails.Remove(details);
                        }
                    }

                    #endregion --CV Details Entry

                    if (_dbContext.ChangeTracker.HasChanges())
                    {
                        #region --Audit Trail Recording

                        // if (model.OriginalSeriesNumber == null && model.OriginalDocumentId == 0)
                        // {
                        //     var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        //     AuditTrail auditTrailBook = new(_userManager.GetUserName(this.User), $"Edit journal voucher# {viewModel.JVNo}", "Journal Voucher", ipAddress);
                        //     await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                        // }

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);  // await the SaveChangesAsync method
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Journal Voucher edited successfully";
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
                    TempData["error"] = ex.Message;
                    return View(viewModel);
                }
            }

            TempData["error"] = "The information provided was invalid.";
            return View(viewModel);
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
                var selectedList = await _dbContext.JournalVoucherHeaders
                    .Where(jv => recordIds.Contains(jv.Id))
                    .Include(jvh => jvh.CheckVoucherHeader)
                    .OrderBy(jv => jv.JVNo)
                    .ToListAsync();

                // Create the Excel package
                using (var package = new ExcelPackage())
                {
                    // Add a new worksheet to the Excel package
                    #region -- Purchase Order Table Header --

                    var worksheet5 = package.Workbook.Worksheets.Add("PurchaseOrder");

                    worksheet5.Cells["A1"].Value = "Date";
                    worksheet5.Cells["B1"].Value = "Terms";
                    worksheet5.Cells["C1"].Value = "Quantity";
                    worksheet5.Cells["D1"].Value = "Price";
                    worksheet5.Cells["E1"].Value = "Amount";
                    worksheet5.Cells["F1"].Value = "FinalPrice";
                    worksheet5.Cells["G1"].Value = "QuantityReceived";
                    worksheet5.Cells["H1"].Value = "IsReceived";
                    worksheet5.Cells["I1"].Value = "ReceivedDate";
                    worksheet5.Cells["J1"].Value = "Remarks";
                    worksheet5.Cells["K1"].Value = "CreatedBy";
                    worksheet5.Cells["L1"].Value = "CreatedDate";
                    worksheet5.Cells["M1"].Value = "IsClosed";
                    worksheet5.Cells["N1"].Value = "CancellationRemarks";
                    worksheet5.Cells["O1"].Value = "OriginalProductId";
                    worksheet5.Cells["P1"].Value = "OriginalPONo";
                    worksheet5.Cells["Q1"].Value = "OriginalSupplierId";
                    worksheet5.Cells["R1"].Value = "OriginalDocumentId";

                    #endregion -- Purchase Order Table Header --

                    #region -- Receiving Report Table Header --

                    var worksheet6 = package.Workbook.Worksheets.Add("ReceivingReport");

                    worksheet6.Cells["A1"].Value = "Date";
                    worksheet6.Cells["B1"].Value = "DueDate";
                    worksheet6.Cells["C1"].Value = "SupplierInvoiceNumber";
                    worksheet6.Cells["D1"].Value = "SupplierInvoiceDate";
                    worksheet6.Cells["E1"].Value = "TruckOrVessels";
                    worksheet6.Cells["F1"].Value = "QuantityDelivered";
                    worksheet6.Cells["G1"].Value = "QuantityReceived";
                    worksheet6.Cells["H1"].Value = "GainOrLoss";
                    worksheet6.Cells["I1"].Value = "Amount";
                    worksheet6.Cells["J1"].Value = "OtherRef";
                    worksheet6.Cells["K1"].Value = "Remarks";
                    worksheet6.Cells["L1"].Value = "AmountPaid";
                    worksheet6.Cells["M1"].Value = "IsPaid";
                    worksheet6.Cells["N1"].Value = "PaidDate";
                    worksheet6.Cells["O1"].Value = "CanceledQuantity";
                    worksheet6.Cells["P1"].Value = "CreatedBy";
                    worksheet6.Cells["Q1"].Value = "CreatedDate";
                    worksheet6.Cells["R1"].Value = "CancellationRemarks";
                    worksheet6.Cells["S1"].Value = "ReceivedDate";
                    worksheet6.Cells["T1"].Value = "OriginalPOId";
                    worksheet6.Cells["U1"].Value = "OriginalRRNo";
                    worksheet6.Cells["V1"].Value = "OriginalDocumentId";

                    #endregion -- Receiving Report Table Header --

                    #region -- Check Voucher Header Table Header --

                        var worksheet3 = package.Workbook.Worksheets.Add("CheckVoucherHeader");

                        worksheet3.Cells["A1"].Value = "TransactionDate";
                        worksheet3.Cells["B1"].Value = "ReceivingReportNo";
                        worksheet3.Cells["C1"].Value = "SalesInvoiceNo";
                        worksheet3.Cells["D1"].Value = "PurchaseOrderNo";
                        worksheet3.Cells["E1"].Value = "Particulars";
                        worksheet3.Cells["F1"].Value = "CheckNo";
                        worksheet3.Cells["G1"].Value = "Category";
                        worksheet3.Cells["H1"].Value = "Payee";
                        worksheet3.Cells["I1"].Value = "CheckDate";
                        worksheet3.Cells["J1"].Value = "StartDate";
                        worksheet3.Cells["K1"].Value = "EndDate";
                        worksheet3.Cells["L1"].Value = "NumberOfMonths";
                        worksheet3.Cells["M1"].Value = "NumberOfMonthsCreated";
                        worksheet3.Cells["N1"].Value = "LastCreatedDate";
                        worksheet3.Cells["O1"].Value = "AmountPerMonth";
                        worksheet3.Cells["P1"].Value = "IsComplete";
                        worksheet3.Cells["Q1"].Value = "AccruedType";
                        worksheet3.Cells["R1"].Value = "Reference";
                        worksheet3.Cells["S1"].Value = "CreatedBy";
                        worksheet3.Cells["T1"].Value = "CreatedDate";
                        worksheet3.Cells["U1"].Value = "Total";
                        worksheet3.Cells["V1"].Value = "Amount";
                        worksheet3.Cells["W1"].Value = "CheckAmount";
                        worksheet3.Cells["X1"].Value = "CVType";
                        worksheet3.Cells["Y1"].Value = "AmountPaid";
                        worksheet3.Cells["Z1"].Value = "IsPaid";
                        worksheet3.Cells["AA1"].Value = "CancellationRemarks";
                        worksheet3.Cells["AB1"].Value = "OriginalBankId";
                        worksheet3.Cells["AC1"].Value = "OriginalCVNo";
                        worksheet3.Cells["AD1"].Value = "OriginalSupplierId";
                        worksheet3.Cells["AE1"].Value = "OriginalDocumentId";

                    #endregion -- Check Voucher Header Table Header --

                    #region -- Check Voucher Details Table Header--

                        var worksheet4 = package.Workbook.Worksheets.Add("CheckVoucherDetails");

                        worksheet4.Cells["A1"].Value = "AccountNo";
                        worksheet4.Cells["B1"].Value = "AccountName";
                        worksheet4.Cells["C1"].Value = "TransactionNo";
                        worksheet4.Cells["D1"].Value = "Debit";
                        worksheet4.Cells["E1"].Value = "Credit";
                        worksheet4.Cells["F1"].Value = "CVHeaderId";
                        worksheet4.Cells["G1"].Value = "OriginalDocumentId";

                    #endregion -- Check Voucher Details Table Header --

                    #region -- Journal Voucher Header Table Header --

                    var worksheet = package.Workbook.Worksheets.Add("JournalVoucherHeader");

                    worksheet.Cells["A1"].Value = "TransactionDate";
                    worksheet.Cells["B1"].Value = "Reference";
                    worksheet.Cells["C1"].Value = "Particulars";
                    worksheet.Cells["D1"].Value = "CRNo";
                    worksheet.Cells["E1"].Value = "JVReason";
                    worksheet.Cells["F1"].Value = "CreatedBy";
                    worksheet.Cells["G1"].Value = "CreatedDate";
                    worksheet.Cells["H1"].Value = "CancellationRemarks";
                    worksheet.Cells["I1"].Value = "OriginalCVId";
                    worksheet.Cells["J1"].Value = "OriginalJVNo";
                    worksheet.Cells["K1"].Value = "OriginalDocumentId";

                    #endregion -- Journal Voucher Header Table Header --

                    #region -- Journal Voucher Details Table Header --

                    var worksheet2 = package.Workbook.Worksheets.Add("JournalVoucherDetails");

                    worksheet2.Cells["A1"].Value = "AccountNo";
                    worksheet2.Cells["B1"].Value = "AccountName";
                    worksheet2.Cells["C1"].Value = "TransactionNo";
                    worksheet2.Cells["D1"].Value = "Debit";
                    worksheet2.Cells["E1"].Value = "Credit";
                    worksheet2.Cells["F1"].Value = "JVHeaderId";
                    worksheet2.Cells["G1"].Value = "OriginalDocumentId";

                    #endregion -- Journal Voucher Details Table Header --

                    #region -- Journal Voucher Header Export --

                    int row = 2;

                    foreach (var item in selectedList)
                    {
                        worksheet.Cells[row, 1].Value = item.Date.ToString("yyyy-MM-dd");
                        worksheet.Cells[row, 2].Value = item.References;
                        worksheet.Cells[row, 3].Value = item.Particulars;
                        worksheet.Cells[row, 4].Value = item.CRNo;
                        worksheet.Cells[row, 5].Value = item.JVReason;
                        worksheet.Cells[row, 6].Value = item.CreatedBy;
                        worksheet.Cells[row, 7].Value = item.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                        worksheet.Cells[row, 8].Value = item.CancellationRemarks;
                        worksheet.Cells[row, 9].Value = item.CVId;
                        worksheet.Cells[row, 10].Value = item.JVNo;
                        worksheet.Cells[row, 11].Value = item.Id;

                        row++;
                    }

                    #endregion -- Journal Voucher Header Export --

                    #region -- Check Voucher Header Export (Trade and Invoicing) --

                    int cvhRow = 2;

                    foreach (var item in selectedList)
                    {
                        if (item.CheckVoucherHeader == null)
                        {
                            continue;
                        }
                        worksheet3.Cells[cvhRow, 1].Value = item.CheckVoucherHeader.Date.ToString("yyyy-MM-dd");
                        if (item.CheckVoucherHeader.RRNo != null && !item.CheckVoucherHeader.RRNo.Contains(null))
                        {
                            worksheet3.Cells[cvhRow, 2].Value =
                                string.Join(", ", item.CheckVoucherHeader.RRNo.Select(rrNo => rrNo.ToString()));
                        }

                        if (item.CheckVoucherHeader.SINo != null && !item.CheckVoucherHeader.SINo.Contains(null))
                        {
                            worksheet3.Cells[cvhRow, 3].Value =
                                string.Join(", ", item.CheckVoucherHeader.SINo.Select(siNo => siNo.ToString()));
                        }

                        if (item.CheckVoucherHeader.PONo != null && !item.CheckVoucherHeader.PONo.Contains(null))
                        {
                            worksheet3.Cells[cvhRow, 4].Value =
                                string.Join(", ", item.CheckVoucherHeader.PONo.Select(poNo => poNo.ToString()));
                        }

                        worksheet3.Cells[cvhRow, 5].Value = item.CheckVoucherHeader.Particulars;
                        worksheet3.Cells[cvhRow, 6].Value = item.CheckVoucherHeader.CheckNo;
                        worksheet3.Cells[cvhRow, 7].Value = item.CheckVoucherHeader.Category;
                        worksheet3.Cells[cvhRow, 8].Value = item.CheckVoucherHeader.Payee;
                        worksheet3.Cells[cvhRow, 9].Value = item.CheckVoucherHeader.CheckDate?.ToString("yyyy-MM-dd");
                        worksheet3.Cells[cvhRow, 10].Value = item.CheckVoucherHeader.StartDate?.ToString("yyyy-MM-dd");
                        worksheet3.Cells[cvhRow, 11].Value = item.CheckVoucherHeader.EndDate?.ToString("yyyy-MM-dd");
                        worksheet3.Cells[cvhRow, 12].Value = item.CheckVoucherHeader.NumberOfMonths;
                        worksheet3.Cells[cvhRow, 13].Value = item.CheckVoucherHeader.NumberOfMonthsCreated;
                        worksheet3.Cells[cvhRow, 14].Value =
                            item.CheckVoucherHeader.LastCreatedDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                        worksheet3.Cells[cvhRow, 15].Value = item.CheckVoucherHeader.AmountPerMonth;
                        worksheet3.Cells[cvhRow, 16].Value = item.CheckVoucherHeader.IsComplete;
                        worksheet3.Cells[cvhRow, 17].Value = item.CheckVoucherHeader.AccruedType;
                        worksheet3.Cells[cvhRow, 18].Value = item.CheckVoucherHeader.Reference;
                        worksheet3.Cells[cvhRow, 19].Value = item.CheckVoucherHeader.CreatedBy;
                        worksheet3.Cells[cvhRow, 20].Value =
                            item.CheckVoucherHeader.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                        worksheet3.Cells[cvhRow, 21].Value = item.CheckVoucherHeader.Total;
                        if (item.CheckVoucherHeader.Amount != null)
                        {
                            worksheet3.Cells[cvhRow, 22].Value =
                                string.Join(" ", item.CheckVoucherHeader.Amount.Select(amount => amount.ToString("N2")));
                        }

                        worksheet3.Cells[cvhRow, 23].Value = item.CheckVoucherHeader.CheckAmount;
                        worksheet3.Cells[cvhRow, 24].Value = item.CheckVoucherHeader.CvType;
                        worksheet3.Cells[cvhRow, 25].Value = item.CheckVoucherHeader.AmountPaid;
                        worksheet3.Cells[cvhRow, 26].Value = item.CheckVoucherHeader.IsPaid;
                        worksheet3.Cells[cvhRow, 27].Value = item.CheckVoucherHeader.CancellationRemarks;
                        worksheet3.Cells[cvhRow, 28].Value = item.CheckVoucherHeader.BankId;
                        worksheet3.Cells[cvhRow, 29].Value = item.CheckVoucherHeader.CVNo;
                        worksheet3.Cells[cvhRow, 30].Value = item.CheckVoucherHeader.SupplierId;
                        worksheet3.Cells[cvhRow, 31].Value = item.CheckVoucherHeader.Id;

                        cvhRow++;
                    }
                    #endregion -- Check Voucher Header Export (Trade and Invoicing) --

                    #region -- Check Voucher Header Export (Payment) --

                    var cvNos = selectedList.Select(item => item.CheckVoucherHeader.CVNo).ToList();

                        var checkVoucherPayment = await _dbContext.CheckVoucherHeaders
                            .Where(cvh => cvh.Reference != null && cvNos.Contains(cvh.Reference))
                            .ToListAsync();

                        foreach (var item in checkVoucherPayment)
                        {
                            worksheet3.Cells[row, 1].Value = item.Date.ToString("yyyy-MM-dd");
                            worksheet3.Cells[row, 2].Value = item.RRNo;
                            worksheet3.Cells[row, 3].Value = item.SINo;
                            worksheet3.Cells[row, 4].Value = item.PONo;
                            worksheet3.Cells[row, 5].Value = item.Particulars;
                            worksheet3.Cells[row, 6].Value = item.CheckNo;
                            worksheet3.Cells[row, 7].Value = item.Category;
                            worksheet3.Cells[row, 8].Value = item.Payee;
                            worksheet3.Cells[row, 9].Value = item.CheckDate?.ToString("yyyy-MM-dd");
                            worksheet3.Cells[row, 10].Value = item.StartDate?.ToString("yyyy-MM-dd");
                            worksheet3.Cells[row, 11].Value = item.EndDate?.ToString("yyyy-MM-dd");
                            worksheet3.Cells[row, 12].Value = item.NumberOfMonths;
                            worksheet3.Cells[row, 13].Value = item.NumberOfMonthsCreated;
                            worksheet3.Cells[row, 14].Value = item.LastCreatedDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                            worksheet3.Cells[row, 15].Value = item.AmountPerMonth;
                            worksheet3.Cells[row, 16].Value = item.IsComplete;
                            worksheet3.Cells[row, 17].Value = item.AccruedType;
                            worksheet3.Cells[row, 18].Value = item.Reference;
                            worksheet3.Cells[row, 19].Value = item.CreatedBy;
                            worksheet3.Cells[row, 20].Value = item.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                            worksheet3.Cells[row, 21].Value = item.Total;
                            worksheet3.Cells[row, 22].Value = item.Amount != null ? string.Join(" ", item.Amount.Select(amount => amount.ToString("N2"))) : 0.00;
                            worksheet3.Cells[row, 23].Value = item.CheckAmount;
                            worksheet3.Cells[row, 24].Value = item.CvType;
                            worksheet3.Cells[row, 25].Value = item.AmountPaid;
                            worksheet3.Cells[row, 26].Value = item.IsPaid;
                            worksheet3.Cells[row, 27].Value = item.CancellationRemarks;
                            worksheet3.Cells[row, 28].Value = item.BankId;
                            worksheet3.Cells[row, 29].Value = item.CVNo;
                            worksheet3.Cells[row, 30].Value = item.SupplierId;
                            worksheet3.Cells[row, 31].Value = item.Id;

                            row++;
                        }

                    #endregion -- Check Voucher Header Export (Payment) --

                    #region -- Journal Voucher Details Export --

                    var jvNos = selectedList.Select(item => item.JVNo).ToList();

                    var getJVDetails = await _dbContext.JournalVoucherDetails
                        .Where(jvd => jvNos.Contains(jvd.TransactionNo))
                        .OrderBy(jvd => jvd.Id)
                        .ToListAsync();

                    int jvdRow = 2;

                    foreach (var item in getJVDetails)
                    {
                        worksheet2.Cells[jvdRow, 1].Value = item.AccountNo;
                        worksheet2.Cells[jvdRow, 2].Value = item.AccountName;
                        worksheet2.Cells[jvdRow, 3].Value = item.TransactionNo;
                        worksheet2.Cells[jvdRow, 4].Value = item.Debit;
                        worksheet2.Cells[jvdRow, 5].Value = item.Credit;
                        worksheet2.Cells[jvdRow, 6].Value = item.JVHeaderId;
                        worksheet2.Cells[jvdRow, 7].Value = item.Id;

                        jvdRow++;
                    }

                    #endregion -- Journal Voucher Details Export --

                    #region -- Check Voucher Details Export (Trade and Invoicing) --

                    List<CheckVoucherDetail> getCVDetails = new List<CheckVoucherDetail>();

                    getCVDetails = await _dbContext.CheckVoucherDetails
                        .Where(cvd => selectedList.Select(jvh => jvh.CheckVoucherHeader.CVNo).Contains(cvd.TransactionNo))
                        .OrderBy(cvd => cvd.Id)
                        .ToListAsync();

                    int cvdRow = 2;

                    foreach (var item in getCVDetails)
                    {
                        worksheet4.Cells[cvdRow, 1].Value = item.AccountNo;
                        worksheet4.Cells[cvdRow, 2].Value = item.AccountName;
                        worksheet4.Cells[cvdRow, 3].Value = item.TransactionNo;
                        worksheet4.Cells[cvdRow, 4].Value = item.Debit;
                        worksheet4.Cells[cvdRow, 5].Value = item.Credit;
                        worksheet4.Cells[cvdRow, 6].Value = item.CVHeaderId;
                        worksheet4.Cells[cvdRow, 7].Value = item.Id;

                        cvdRow++;
                    }

                    #endregion -- Check Voucher Details Export (Trade and Invoicing) --

                    #region -- Check Voucher Details Export (Payment) --

                    List<CheckVoucherDetail> getCvPaymentDetails = new List<CheckVoucherDetail>();

                    getCvPaymentDetails = await _dbContext.CheckVoucherDetails
                        .Where(cvd => checkVoucherPayment.Select(cvh => cvh.CVNo).Contains(cvd.TransactionNo))
                        .OrderBy(cvd => cvd.Id)
                        .ToListAsync();

                    foreach (var item in getCvPaymentDetails)
                    {
                        worksheet3.Cells[cvdRow, 1].Value = item.AccountNo;
                        worksheet3.Cells[cvdRow, 2].Value = item.AccountName;
                        worksheet3.Cells[cvdRow, 3].Value = item.TransactionNo;
                        worksheet3.Cells[cvdRow, 4].Value = item.Debit;
                        worksheet3.Cells[cvdRow, 5].Value = item.Credit;
                        worksheet3.Cells[cvdRow, 6].Value = item.CVHeaderId;
                        worksheet3.Cells[cvdRow, 7].Value = item.Id;

                        cvdRow++;
                    }

                    #endregion -- Check Voucher Details Export (Payment) --

                    #region -- Receving Report Export --

                    List<ReceivingReport> getReceivingReport = new List<ReceivingReport>();

                    getReceivingReport = _dbContext.ReceivingReports
                        .AsEnumerable()
                        .Where(rr => selectedList?.Select(item => item?.CheckVoucherHeader?.RRNo).Any(rrs => rrs?.Contains(rr.RRNo) == true) == true)
                        .OrderBy(rr => rr.RRNo)
                        .ToList();

                    int rrRow = 2;

                    foreach (var item in getReceivingReport)
                    {
                        worksheet6.Cells[rrRow, 1].Value = item.Date.ToString("yyyy-MM-dd");
                        worksheet6.Cells[rrRow, 2].Value = item.DueDate.ToString("yyyy-MM-dd");
                        worksheet6.Cells[rrRow, 3].Value = item.SupplierInvoiceNumber;
                        worksheet6.Cells[rrRow, 4].Value = item.SupplierInvoiceDate;
                        worksheet6.Cells[rrRow, 5].Value = item.TruckOrVessels;
                        worksheet6.Cells[rrRow, 6].Value = item.QuantityDelivered;
                        worksheet6.Cells[rrRow, 7].Value = item.QuantityReceived;
                        worksheet6.Cells[rrRow, 8].Value = item.GainOrLoss;
                        worksheet6.Cells[rrRow, 9].Value = item.Amount;
                        worksheet6.Cells[rrRow, 10].Value = item.OtherRef;
                        worksheet6.Cells[rrRow, 11].Value = item.Remarks;
                        worksheet6.Cells[rrRow, 12].Value = item.AmountPaid;
                        worksheet6.Cells[rrRow, 13].Value = item.IsPaid;
                        worksheet6.Cells[rrRow, 14].Value = item.PaidDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                        worksheet6.Cells[rrRow, 15].Value = item.CanceledQuantity;
                        worksheet6.Cells[rrRow, 16].Value = item.CreatedBy;
                        worksheet6.Cells[rrRow, 17].Value = item.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                        worksheet6.Cells[rrRow, 18].Value = item.CancellationRemarks;
                        worksheet6.Cells[rrRow, 19].Value = item.ReceivedDate?.ToString("yyyy-MM-dd");
                        worksheet6.Cells[rrRow, 20].Value = item.POId;
                        worksheet6.Cells[rrRow, 21].Value = item.RRNo;
                        worksheet6.Cells[rrRow, 22].Value = item.Id;

                        rrRow++;
                    }

                    #endregion -- Receving Report Export --

                    #region -- Purchase Order Export --

                    List<PurchaseOrder> getPurchaseOrder = new List<PurchaseOrder>();

                    getPurchaseOrder = await _dbContext.PurchaseOrders
                        .Where(po => getReceivingReport.Select(item => item.POId).Contains(po.Id))
                        .OrderBy(po => po.PONo)
                        .ToListAsync();

                    int poRow = 2;

                    foreach (var item in getPurchaseOrder)
                    {
                        worksheet5.Cells[poRow, 1].Value = item.Date.ToString("yyyy-MM-dd");
                        worksheet5.Cells[poRow, 2].Value = item.Terms;
                        worksheet5.Cells[poRow, 3].Value = item.Quantity;
                        worksheet5.Cells[poRow, 4].Value = item.Price;
                        worksheet5.Cells[poRow, 5].Value = item.Amount;
                        worksheet5.Cells[poRow, 6].Value = item.FinalPrice;
                        worksheet5.Cells[poRow, 7].Value = item.QuantityReceived;
                        worksheet5.Cells[poRow, 8].Value = item.IsReceived;
                        worksheet5.Cells[poRow, 9].Value = item.ReceivedDate != default ? item.ReceivedDate.ToString("yyyy-MM-dd HH:mm:ss.ffffff zzz") : default;
                        worksheet5.Cells[poRow, 10].Value = item.Remarks;
                        worksheet5.Cells[poRow, 11].Value = item.CreatedBy;
                        worksheet5.Cells[poRow, 12].Value = item.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                        worksheet5.Cells[poRow, 13].Value = item.IsClosed;
                        worksheet5.Cells[poRow, 14].Value = item.CancellationRemarks;
                        worksheet5.Cells[poRow, 15].Value = item.ProductId;
                        worksheet5.Cells[poRow, 16].Value = item.PONo;
                        worksheet5.Cells[poRow, 17].Value = item.SupplierId;
                        worksheet5.Cells[poRow, 18].Value = item.Id;

                        poRow++;
                    }

                    #endregion -- Purchase Order Export --

                    // Convert the Excel package to a byte array
                    var excelBytes = await package.GetAsByteArrayAsync();
                    await transaction.CommitAsync(cancellationToken);
                    return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "JournalVoucherList.xlsx");
                }
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
                        var worksheet = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "JournalVoucherHeader");

                        var worksheet2 = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "JournalVoucherDetails");

                        var worksheet3 = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "PurchaseOrder");

                        var worksheet4 = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "ReceivingReport");

                        var worksheet5 = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "CheckVoucherHeader");

                        var worksheet6 = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "CheckVoucherDetails");

                        if (worksheet == null)
                        {
                            TempData["error"] = "The Excel file contains no worksheets of journal voucher header.";
                            return RedirectToAction(nameof(Index), new { view = DynamicView.JournalVoucher });
                        }
                        if (worksheet2 == null)
                        {
                            TempData["error"] = "The Excel file contains no worksheets of journal voucher details.";
                            return RedirectToAction(nameof(Index), new { view = DynamicView.JournalVoucher });
                        }
                        if (worksheet.ToString() != "JournalVoucherHeader")
                        {
                            TempData["error"] = "The Excel file is not related to journal voucher.";
                            return RedirectToAction(nameof(Index), new { view = DynamicView.JournalVoucher });
                        }

                        #region -- Purchase Order Import --

                        var poRowCount = worksheet3?.Dimension?.Rows ?? 0;
                        var poDictionary = new Dictionary<string, bool>();
                        var purchaseOrderList = await _dbContext
                            .PurchaseOrders
                            .ToListAsync(cancellationToken);

                        for (int row = 2; row <= poRowCount; row++)  // Assuming the first row is the header
                        {
                            if (worksheet3 == null || poRowCount == 0)
                            {
                                continue;
                            }
                            var purchaseOrder = new PurchaseOrder
                            {
                                PONo = worksheet3.Cells[row, 16].Text,
                                Date = DateOnly.TryParse(worksheet3.Cells[row, 1].Text, out DateOnly dueDate) ? dueDate : default,
                                Terms = worksheet3.Cells[row, 2].Text,
                                Quantity = decimal.TryParse(worksheet3.Cells[row, 3].Text, out decimal quantity) ? quantity : 0,
                                Price = decimal.TryParse(worksheet3.Cells[row, 4].Text, out decimal price) ? price : 0,
                                Amount = decimal.TryParse(worksheet3.Cells[row, 5].Text, out decimal amount) ? amount : 0,
                                FinalPrice = decimal.TryParse(worksheet3.Cells[row, 6].Text, out decimal finalPrice) ? finalPrice : 0,
                                // QuantityReceived = decimal.TryParse(worksheet.Cells[row, 7].Text, out decimal quantityReceived) ? quantityReceived : 0,
                                // IsReceived = bool.TryParse(worksheet.Cells[row, 8].Text, out bool isReceived) ? isReceived : default,
                                // ReceivedDate = DateTime.TryParse(worksheet.Cells[row, 9].Text, out DateTime receivedDate) ? receivedDate : default,
                                Remarks = worksheet3.Cells[row, 10].Text,
                                CreatedBy = worksheet3.Cells[row, 11].Text,
                                CreatedDate = DateTime.TryParse(worksheet3.Cells[row, 12].Text, out DateTime createdDate) ? createdDate : default,
                                IsClosed = bool.TryParse(worksheet3.Cells[row, 13].Text, out bool isClosed) ? isClosed : default,
                                CancellationRemarks = worksheet3.Cells[row, 14].Text != "" ? worksheet3.Cells[row, 14].Text : null,
                                OriginalProductId = int.TryParse(worksheet3.Cells[row, 15].Text, out int originalProductId) ? originalProductId : 0,
                                OriginalSeriesNumber = worksheet3.Cells[row, 16].Text,
                                OriginalSupplierId = int.TryParse(worksheet3.Cells[row, 17].Text, out int originalSupplierId) ? originalSupplierId : 0,
                                OriginalDocumentId = int.TryParse(worksheet3.Cells[row, 18].Text, out int originalDocumentId) ? originalDocumentId : 0,
                            };

                            if (!poDictionary.TryAdd(purchaseOrder.OriginalSeriesNumber, true))
                            {
                                continue;
                            }

                            if (purchaseOrderList.Any(po => po.OriginalDocumentId == purchaseOrder.OriginalDocumentId))
                            {
                                var poChanges = new Dictionary<string, (string OriginalValue, string NewValue)>();
                                var existingPO = await _dbContext.PurchaseOrders.FirstOrDefaultAsync(si => si.OriginalDocumentId == purchaseOrder.OriginalDocumentId, cancellationToken);

                                if (existingPO.PONo.TrimStart().TrimEnd() != worksheet3.Cells[row, 16].Text.TrimStart().TrimEnd())
                                {
                                    poChanges["PONo"] = (existingPO.PONo.TrimStart().TrimEnd(), worksheet3.Cells[row, 16].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingPO.Date.ToString("yyyy-MM-dd").TrimStart().TrimEnd() != worksheet3.Cells[row, 1].Text.TrimStart().TrimEnd())
                                {
                                    poChanges["Date"] = (existingPO.Date.ToString("yyyy-MM-dd").TrimStart().TrimEnd(), worksheet3.Cells[row, 1].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingPO.Terms.TrimStart().TrimEnd() != worksheet3.Cells[row, 2].Text.TrimStart().TrimEnd())
                                {
                                    poChanges["Terms"] = (existingPO.Terms.TrimStart().TrimEnd(), worksheet3.Cells[row, 2].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingPO.Quantity.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet3.Cells[row, 3].Text).ToString("F2").TrimStart().TrimEnd())
                                {
                                    poChanges["Quantity"] = (existingPO.Quantity.ToString("F2").TrimStart().TrimEnd(), decimal.Parse(worksheet3.Cells[row, 3].Text).ToString("F2").TrimStart().TrimEnd())!;
                                }

                                if (existingPO.Price.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet3.Cells[row, 4].Text).ToString("F2").TrimStart().TrimEnd())
                                {
                                    poChanges["Price"] = (existingPO.Price.ToString("F2").TrimStart().TrimEnd(), decimal.Parse(worksheet3.Cells[row, 4].Text).ToString("F2").TrimStart().TrimEnd())!;
                                }

                                if (existingPO.Amount.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet3.Cells[row, 5].Text).ToString("F2").TrimStart().TrimEnd())
                                {
                                    poChanges["Amount"] = (existingPO.Amount.ToString("F2").TrimStart().TrimEnd(), decimal.Parse(worksheet3.Cells[row, 5].Text).ToString("F2").TrimStart().TrimEnd())!;
                                }

                                if (existingPO.FinalPrice?.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet3.Cells[row, 6].Text).ToString("F2").TrimStart().TrimEnd())
                                {
                                    poChanges["FinalPrice"] = (existingPO.FinalPrice?.ToString("F2").TrimStart().TrimEnd(), decimal.Parse(worksheet3.Cells[row, 6].Text).ToString("F2").TrimStart().TrimEnd())!;
                                }

                                if (existingPO.Remarks.TrimStart().TrimEnd() != worksheet3.Cells[row, 10].Text.TrimStart().TrimEnd())
                                {
                                    poChanges["Remarks"] = (existingPO.Remarks.TrimStart().TrimEnd(), worksheet3.Cells[row, 10].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingPO.CreatedBy.TrimStart().TrimEnd() != worksheet3.Cells[row, 11].Text.TrimStart().TrimEnd())
                                {
                                    poChanges["CreatedBy"] = (existingPO.CreatedBy.TrimStart().TrimEnd(), worksheet3.Cells[row, 11].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingPO.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff").TrimStart().TrimEnd() != worksheet3.Cells[row, 12].Text.TrimStart().TrimEnd())
                                {
                                    poChanges["CreatedDate"] = (existingPO.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff").TrimStart().TrimEnd(), worksheet3.Cells[row, 12].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingPO.IsClosed.ToString().ToUpper().TrimStart().TrimEnd() != worksheet3.Cells[row, 13].Text.TrimStart().TrimEnd())
                                {
                                    poChanges["IsClosed"] = (existingPO.IsClosed.ToString().ToUpper().TrimStart().TrimEnd(), worksheet3.Cells[row, 13].Text.TrimStart().TrimEnd())!;
                                }

                                if ((string.IsNullOrWhiteSpace(existingPO.CancellationRemarks?.TrimStart().TrimEnd()) ? "" : existingPO.CancellationRemarks.TrimStart().TrimEnd()) != worksheet3.Cells[row, 14].Text.TrimStart().TrimEnd())
                                {
                                    poChanges["CancellationRemarks"] = (existingPO.CancellationRemarks?.TrimStart().TrimEnd(), worksheet3.Cells[row, 14].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingPO.OriginalProductId.ToString().TrimStart().TrimEnd() != (worksheet3.Cells[row, 15].Text.TrimStart().TrimEnd() == "" ? 0.ToString() : worksheet3.Cells[row, 15].Text.TrimStart().TrimEnd()))
                                {
                                    poChanges["OriginalProductId"] = (existingPO.OriginalProductId.ToString().TrimStart().TrimEnd(), worksheet3.Cells[row, 15].Text.TrimStart().TrimEnd() == "" ? 0.ToString() : worksheet3.Cells[row, 15].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingPO.OriginalSeriesNumber.TrimStart().TrimEnd() != worksheet3.Cells[row, 16].Text.TrimStart().TrimEnd())
                                {
                                    poChanges["OriginalSeriesNumber"] = (existingPO.OriginalSeriesNumber.TrimStart().TrimEnd(), worksheet3.Cells[row, 16].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingPO.OriginalSupplierId.ToString().TrimStart().TrimEnd() != (worksheet3.Cells[row, 17].Text.TrimStart().TrimEnd() == "" ? 0.ToString() : worksheet3.Cells[row, 17].Text.TrimStart().TrimEnd()))
                                {
                                    poChanges["SupplierId"] = (existingPO.SupplierId.ToString().TrimStart().TrimEnd(), worksheet3.Cells[row, 17].Text.TrimStart().TrimEnd() == "" ? 0.ToString() : worksheet3.Cells[row, 17].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingPO.OriginalDocumentId.ToString().TrimStart().TrimEnd() != (worksheet3.Cells[row, 18].Text.TrimStart().TrimEnd() == "" ? 0.ToString() : worksheet3.Cells[row, 18].Text.TrimStart().TrimEnd()))
                                {
                                    poChanges["OriginalDocumentId"] = (existingPO.OriginalDocumentId.ToString().TrimStart().TrimEnd(), worksheet3.Cells[row, 18].Text.TrimStart().TrimEnd() == "" ? 0.ToString() : worksheet3.Cells[row, 18].Text.TrimStart().TrimEnd())!;
                                }

                                if (poChanges.Any())
                                {
                                    await _purchaseOrderRepo.LogChangesAsync(existingPO.OriginalDocumentId, poChanges, _userManager.GetUserName(this.User));
                                }

                                continue;
                            }

                            var getProduct = await _dbContext.Products
                                .Where(p => p.OriginalProductId == purchaseOrder.OriginalProductId)
                                .FirstOrDefaultAsync(cancellationToken);

                            if (getProduct != null)
                            {
                                purchaseOrder.ProductId = getProduct.Id;

                                purchaseOrder.ProductNo = getProduct.Code;
                            }
                            else
                            {
                                throw new InvalidOperationException("Please upload the Excel file for the product master file first.");
                            }

                            var getSupplier = await _dbContext.Suppliers
                                .Where(c => c.OriginalSupplierId == purchaseOrder.OriginalSupplierId)
                                .FirstOrDefaultAsync(cancellationToken);

                            if (getSupplier != null)
                            {
                                purchaseOrder.SupplierId = getSupplier.Id;

                                purchaseOrder.SupplierNo = getSupplier.Number;
                            }
                            else
                            {
                                throw new InvalidOperationException("Please upload the Excel file for the supplier master file first.");
                            }

                            await _dbContext.PurchaseOrders.AddAsync(purchaseOrder, cancellationToken);
                        }
                        await _dbContext.SaveChangesAsync(cancellationToken);

                        #endregion -- Purchase Order Import --

                        #region -- Receiving Report Import --

                        var rrRowCount = worksheet4?.Dimension?.Rows ?? 0;
                        var rrDictionary = new Dictionary<string, bool>();
                        var receivingReportList = await _dbContext
                            .ReceivingReports
                            .ToListAsync(cancellationToken);
                        for (int row = 2; row <= rrRowCount; row++)  // Assuming the first row is the header
                        {
                            if (worksheet4 == null || rrRowCount == 0)
                            {
                                continue;
                            }
                            var receivingReport = new ReceivingReport
                            {
                                RRNo = worksheet4.Cells[row, 21].Text,
                                Date = DateOnly.TryParse(worksheet4.Cells[row, 1].Text, out DateOnly date) ? date : default,
                                DueDate = DateOnly.TryParse(worksheet4.Cells[row, 2].Text, out DateOnly dueDate) ? dueDate : default,
                                SupplierInvoiceNumber = worksheet4.Cells[row, 3].Text != "" ? worksheet4.Cells[row, 3].Text : null,
                                SupplierInvoiceDate = worksheet4.Cells[row, 4].Text,
                                TruckOrVessels = worksheet4.Cells[row, 5].Text,
                                QuantityDelivered = decimal.TryParse(worksheet4.Cells[row, 6].Text, out decimal quantityDelivered) ? quantityDelivered : 0,
                                QuantityReceived = decimal.TryParse(worksheet4.Cells[row, 7].Text, out decimal quantityReceived) ? quantityReceived : 0,
                                GainOrLoss = decimal.TryParse(worksheet4.Cells[row, 8].Text, out decimal gainOrLoss) ? gainOrLoss : 0,
                                Amount = decimal.TryParse(worksheet4.Cells[row, 9].Text, out decimal amount) ? amount : 0,
                                OtherRef = worksheet4.Cells[row, 10].Text != "" ? worksheet4.Cells[row, 10].Text : null,
                                Remarks = worksheet4.Cells[row, 11].Text,
                                // AmountPaid = decimal.TryParse(worksheet.Cells[row, 12].Text, out decimal amountPaid) ? amountPaid : 0,
                                // IsPaid = bool.TryParse(worksheet.Cells[row, 13].Text, out bool IsPaid) ? IsPaid : default,
                                // PaidDate = DateTime.TryParse(worksheet.Cells[row, 14].Text, out DateTime paidDate) ? paidDate : DateTime.MinValue,
                                // CanceledQuantity = decimal.TryParse(worksheet.Cells[row, 15].Text, out decimal netAmountOfEWT) ? netAmountOfEWT : 0,
                                CreatedBy = worksheet4.Cells[row, 16].Text,
                                CreatedDate = DateTime.TryParse(worksheet4.Cells[row, 17].Text, out DateTime createdDate) ? createdDate : default,
                                CancellationRemarks = worksheet4.Cells[row, 18].Text != "" ? worksheet4.Cells[row, 18].Text : null,
                                ReceivedDate = DateOnly.TryParse(worksheet4.Cells[row, 19].Text, out DateOnly receivedDate) ? receivedDate : default,
                                OriginalPOId = int.TryParse(worksheet4.Cells[row, 20].Text, out int OriginalPOId) ? OriginalPOId : 0,
                                OriginalSeriesNumber = worksheet4.Cells[row, 21].Text,
                                OriginalDocumentId = int.TryParse(worksheet4.Cells[row, 22].Text, out int originalDocumentId) ? originalDocumentId : 0,
                            };

                            if (!rrDictionary.TryAdd(receivingReport.OriginalSeriesNumber, true))
                            {
                                continue;
                            }

                            //Checking for duplicate record
                            if (receivingReportList.Any(rr => rr.OriginalDocumentId == receivingReport.OriginalDocumentId))
                            {
                                var rrChanges = new Dictionary<string, (string OriginalValue, string NewValue)>();
                                var existingRR = await _dbContext.ReceivingReports.FirstOrDefaultAsync(rr => rr.OriginalDocumentId == receivingReport.OriginalDocumentId, cancellationToken);

                                if (existingRR.RRNo.TrimStart().TrimEnd() != worksheet4.Cells[row, 21].Text.TrimStart().TrimEnd())
                                {
                                    rrChanges["RRNo"] = (existingRR.RRNo.TrimStart().TrimEnd(), worksheet4.Cells[row, 21].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingRR.Date.ToString("yyyy-MM-dd").TrimStart().TrimEnd() != worksheet4.Cells[row, 1].Text.TrimStart().TrimEnd())
                                {
                                    rrChanges["Date"] = (existingRR.Date.ToString("yyyy-MM-dd").TrimStart().TrimEnd(), worksheet4.Cells[row, 1].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingRR.DueDate.ToString("yyyy-MM-dd").TrimStart().TrimEnd() != worksheet4.Cells[row, 2].Text.TrimStart().TrimEnd())
                                {
                                    rrChanges["DueDate"] = (existingRR.DueDate.ToString("yyyy-MM-dd").TrimStart().TrimEnd(), worksheet4.Cells[row, 2].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingRR.SupplierInvoiceNumber.TrimStart().TrimEnd() != (worksheet4.Cells[row, 3].Text.TrimStart().TrimEnd() == "" ? null : worksheet4.Cells[row, 3].Text.TrimStart().TrimEnd()))
                                {
                                    rrChanges["SupplierInvoiceNumber"] = (existingRR.SupplierInvoiceNumber.TrimStart().TrimEnd(), worksheet4.Cells[row, 3].Text.TrimStart().TrimEnd() == "" ? null : worksheet4.Cells[row, 3].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingRR.SupplierInvoiceDate.TrimStart().TrimEnd() != worksheet4.Cells[row, 4].Text.TrimStart().TrimEnd())
                                {
                                    rrChanges["SupplierInvoiceDate"] = (existingRR.SupplierInvoiceDate.TrimStart().TrimEnd(), worksheet4.Cells[row, 4].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingRR.TruckOrVessels.TrimStart().TrimEnd() != worksheet4.Cells[row, 5].Text.TrimStart().TrimEnd())
                                {
                                    rrChanges["TruckOrVessels"] = (existingRR.TruckOrVessels.TrimStart().TrimEnd(), worksheet4.Cells[row, 5].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingRR.QuantityDelivered.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet4.Cells[row, 6].Text).ToString("F2").TrimStart().TrimEnd())
                                {
                                    rrChanges["QuantityDelivered"] = (existingRR.QuantityDelivered.ToString("F2").TrimStart().TrimEnd(), decimal.Parse(worksheet4.Cells[row, 6].Text).ToString("F2").TrimStart().TrimEnd())!;
                                }

                                if (existingRR.QuantityReceived.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet4.Cells[row, 7].Text).ToString("F2").TrimStart().TrimEnd())
                                {
                                    rrChanges["QuantityReceived"] = (existingRR.QuantityReceived.ToString("F2").TrimStart().TrimEnd(), decimal.Parse(worksheet4.Cells[row, 7].Text).ToString("F2").TrimStart().TrimEnd())!;
                                }

                                if (existingRR.GainOrLoss.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet4.Cells[row, 8].Text).ToString("F2").TrimStart().TrimEnd())
                                {
                                    rrChanges["GainOrLoss"] = (existingRR.GainOrLoss.ToString("F2").TrimStart().TrimEnd(), decimal.Parse(worksheet4.Cells[row, 8].Text).ToString("F2").TrimStart().TrimEnd())!;
                                }

                                if (existingRR.Amount.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet4.Cells[row, 9].Text).ToString("F2").TrimStart().TrimEnd())
                                {
                                    rrChanges["Amount"] = (existingRR.Amount.ToString("F2").TrimStart().TrimEnd(), decimal.Parse(worksheet4.Cells[row, 9].Text).ToString("F2").TrimStart().TrimEnd())!;
                                }

                                if (existingRR.OtherRef.TrimStart().TrimEnd() != (worksheet4.Cells[row, 10].Text.TrimStart().TrimEnd() == "" ? null : worksheet4.Cells[row, 10].Text.TrimStart().TrimEnd()))
                                {
                                    rrChanges["OtherRef"] = (existingRR.OtherRef.TrimStart().TrimEnd(), worksheet4.Cells[row, 10].Text.TrimStart().TrimEnd() == "" ? null : worksheet4.Cells[row, 10].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingRR.Remarks.TrimStart().TrimEnd() != worksheet4.Cells[row, 11].Text.TrimStart().TrimEnd())
                                {
                                    rrChanges["Remarks"] = (existingRR.Remarks.TrimStart().TrimEnd(), worksheet4.Cells[row, 11].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingRR.CreatedBy.TrimStart().TrimEnd() != worksheet4.Cells[row, 16].Text.TrimStart().TrimEnd())
                                {
                                    rrChanges["CreatedBy"] = (existingRR.CreatedBy.TrimStart().TrimEnd(), worksheet4.Cells[row, 16].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingRR.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff").TrimStart().TrimEnd() != worksheet4.Cells[row, 17].Text.TrimStart().TrimEnd())
                                {
                                    rrChanges["CreatedDate"] = (existingRR.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff").TrimStart().TrimEnd(), worksheet4.Cells[row, 17].Text.TrimStart().TrimEnd())!;
                                }

                                if ((string.IsNullOrWhiteSpace(existingRR.CancellationRemarks?.TrimStart().TrimEnd()) ? "" : existingRR.CancellationRemarks.TrimStart().TrimEnd()) != worksheet4.Cells[row, 18].Text.TrimStart().TrimEnd())
                                {
                                    rrChanges["CancellationRemarks"] = (existingRR.CancellationRemarks?.TrimStart().TrimEnd(), worksheet4.Cells[row, 18].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingRR.ReceivedDate?.ToString("yyyy-MM-dd").TrimStart().TrimEnd() != (worksheet4.Cells[row, 19].Text.TrimStart().TrimEnd() == "" ? DateOnly.MinValue.ToString("yyyy-MM-dd").TrimStart().TrimEnd() : worksheet4.Cells[row, 19].Text.TrimStart().TrimEnd()))
                                {
                                    rrChanges["ReceivedDate"] = (existingRR.ReceivedDate?.ToString("yyyy-MM-dd").TrimStart().TrimEnd(), worksheet4.Cells[row, 19].Text.TrimStart().TrimEnd() == "" ? DateOnly.MinValue.ToString("yyyy-MM-dd").TrimStart().TrimEnd() : worksheet4.Cells[row, 19].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingRR.OriginalPOId.ToString().TrimStart().TrimEnd() != (worksheet4.Cells[row, 20].Text.TrimStart().TrimEnd() == "" ? 0.ToString() : worksheet4.Cells[row, 20].Text.TrimStart().TrimEnd()))
                                {
                                    rrChanges["OriginalPOId"] = (existingRR.OriginalPOId.ToString().TrimStart().TrimEnd(), worksheet4.Cells[row, 20].Text.TrimStart().TrimEnd() == "" ? 0.ToString() : worksheet4.Cells[row, 20].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingRR.OriginalSeriesNumber.TrimStart().TrimEnd() != worksheet4.Cells[row, 21].Text.TrimStart().TrimEnd())
                                {
                                    rrChanges["OriginalSeriesNumber"] = (existingRR.OriginalSeriesNumber.TrimStart().TrimEnd(), worksheet4.Cells[row, 21].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingRR.OriginalDocumentId.ToString().TrimStart().TrimEnd() != (worksheet4.Cells[row, 22].Text.TrimStart().TrimEnd() == "" ? 0.ToString() : worksheet4.Cells[row, 22].Text.TrimStart().TrimEnd()))
                                {
                                    rrChanges["OriginalDocumentId"] = (existingRR.OriginalDocumentId.ToString().TrimStart().TrimEnd(), worksheet4.Cells[row, 22].Text.TrimStart().TrimEnd() == "" ? 0.ToString() : worksheet4.Cells[row, 22].Text.TrimStart().TrimEnd())!;
                                }

                                if (rrChanges.Any())
                                {
                                    await _receivingReportRepo.LogChangesAsync(existingRR.OriginalDocumentId, rrChanges, _userManager.GetUserName(this.User));
                                }

                                continue;
                            }

                            var getPo = await _dbContext
                                .PurchaseOrders
                                .Where(po => po.OriginalDocumentId == receivingReport.OriginalPOId)
                                .FirstOrDefaultAsync(cancellationToken);

                            receivingReport.POId = getPo.Id;
                            receivingReport.PONo = getPo.PONo;

                            await _dbContext.ReceivingReports.AddAsync(receivingReport, cancellationToken);
                        }

                        await _dbContext.SaveChangesAsync(cancellationToken);

                        #endregion -- Receiving Report Import --

                        #region -- Check Voucher Header Import --

                        var cvhRowCount = worksheet5?.Dimension?.Rows ?? 0;
                        var cvDictionary = new Dictionary<string, bool>();
                        var checkVoucherHeadersList = await _dbContext
                            .CheckVoucherHeaders
                            .ToListAsync(cancellationToken);

                        for (int row = 2; row <= cvhRowCount; row++) // Assuming the first row is the header
                        {
                            if (worksheet5 == null || cvhRowCount == 0)
                            {
                                continue;
                            }
                            var checkVoucherHeader = new CheckVoucherHeader
                            {
                                CVNo = await _checkVoucherRepo.GenerateCVNo(cancellationToken),
                                Date = DateOnly.TryParse(worksheet5.Cells[row, 1].Text, out DateOnly date)
                                    ? date
                                    : default,
                                RRNo = worksheet5.Cells[row, 2].Text.Split(',').Select(rrNo => rrNo.Trim()).ToArray(),
                                SINo = worksheet5.Cells[row, 3].Text.Split(',').Select(siNo => siNo.Trim()).ToArray(),
                                PONo = worksheet5.Cells[row, 4].Text.Split(',').Select(poNo => poNo.Trim()).ToArray(),
                                Particulars = worksheet5.Cells[row, 5].Text,
                                CheckNo = worksheet5.Cells[row, 6].Text,
                                Category = worksheet5.Cells[row, 7].Text,
                                Payee = worksheet5.Cells[row, 8].Text,
                                CheckDate = DateOnly.TryParse(worksheet5.Cells[row, 9].Text, out DateOnly checkDate)
                                    ? checkDate
                                    : default,
                                StartDate = DateOnly.TryParse(worksheet5.Cells[row, 10].Text, out DateOnly startDate)
                                    ? startDate
                                    : default,
                                EndDate = DateOnly.TryParse(worksheet5.Cells[row, 11].Text, out DateOnly endDate)
                                    ? endDate
                                    : default,
                                NumberOfMonths = int.TryParse(worksheet5.Cells[row, 12].Text, out int numberOfMonths)
                                    ? numberOfMonths
                                    : 0,
                                NumberOfMonthsCreated =
                                    int.TryParse(worksheet5.Cells[row, 13].Text, out int numberOfMonthsCreated)
                                        ? numberOfMonthsCreated
                                        : 0,
                                LastCreatedDate =
                                    DateTime.TryParse(worksheet5.Cells[row, 14].Text, out DateTime lastCreatedDate)
                                        ? lastCreatedDate
                                        : default,
                                AmountPerMonth =
                                    decimal.TryParse(worksheet5.Cells[row, 15].Text, out decimal amountPerMonth)
                                        ? amountPerMonth
                                        : 0,
                                IsComplete = bool.TryParse(worksheet5.Cells[row, 16].Text, out bool isComplete)
                                    ? isComplete
                                    : false,
                                AccruedType = worksheet5.Cells[row, 17].Text,
                                Reference = worksheet5.Cells[row, 18].Text,
                                CreatedBy = worksheet5.Cells[row, 19].Text,
                                CreatedDate = DateTime.TryParse(worksheet5.Cells[row, 20].Text, out DateTime createdDate)
                                    ? createdDate
                                    : default,
                                Total = decimal.TryParse(worksheet5.Cells[row, 21].Text, out decimal total) ? total : 0,
                                Amount = worksheet5.Cells[row, 22].Text.Split(' ').Select(arrayAmount =>
                                    decimal.TryParse(arrayAmount.Trim(), out decimal amount) ? amount : 0).ToArray(),
                                CheckAmount = decimal.TryParse(worksheet5.Cells[row, 23].Text, out decimal checkAmount)
                                    ? checkAmount
                                    : 0,
                                CvType = worksheet5.Cells[row, 24].Text,
                                AmountPaid = decimal.TryParse(worksheet5.Cells[row, 25].Text, out decimal amountPaid)
                                    ? amountPaid
                                    : 0,
                                IsPaid = bool.TryParse(worksheet5.Cells[row, 26].Text, out bool isPaid) ? isPaid : false,
                                CancellationRemarks = worksheet5.Cells[row, 27].Text,
                                OriginalBankId = int.TryParse(worksheet5.Cells[row, 28].Text, out int originalBankId)
                                    ? originalBankId
                                    : 0,
                                OriginalSeriesNumber = worksheet5.Cells[row, 29].Text,
                                OriginalSupplierId =
                                    int.TryParse(worksheet5.Cells[row, 30].Text, out int originalSupplierId)
                                        ? originalSupplierId
                                        : 0,
                                OriginalDocumentId =
                                    int.TryParse(worksheet5.Cells[row, 31].Text, out int originalDocumentId)
                                        ? originalDocumentId
                                        : 0,
                            };

                            var cvhList = checkVoucherHeadersList.Any(cv => cv.OriginalDocumentId == checkVoucherHeader.OriginalDocumentId);
                            var existingCV = await _dbContext.CheckVoucherHeaders.FirstOrDefaultAsync(si => si.OriginalDocumentId == checkVoucherHeader.OriginalDocumentId, cancellationToken);
                            cvDictionary.TryAdd((existingCV != null ? existingCV.CVNo : checkVoucherHeader.CVNo) ?? checkVoucherHeader.CVNo, cvhList);

                            if (cvhList)
                            {
                                var cvChanges = new Dictionary<string, (string OriginalValue, string NewValue)>();

                                if (existingCV.Date.ToString("yyyy-MM-dd").TrimStart().TrimEnd() != worksheet5.Cells[row, 1].Text.TrimStart().TrimEnd())
                                {
                                    cvChanges["Date"] = (existingCV.Date.ToString("yyyy-MM-dd").TrimStart().TrimEnd(), worksheet5.Cells[row, 1].Text.TrimStart().TrimEnd())!;
                                }

                                var rrNo = existingCV.RRNo != null
                                    ? string.Join(", ", existingCV.RRNo.Select(si => si.ToString()))
                                    : null;
                                if (rrNo != null && rrNo.TrimStart().TrimEnd() != worksheet5.Cells[row, 2].Text.TrimStart().TrimEnd())
                                {
                                    cvChanges["RRNo"] = (string.Join(", ", existingCV.RRNo.Select(si => si.ToString().TrimStart().TrimEnd())), worksheet5.Cells[row, 2].Text.TrimStart().TrimEnd())!;
                                }

                                var siNo = existingCV.SINo != null
                                    ? string.Join(", ", existingCV.SINo.Select(si => si.ToString()))
                                    : null;
                                if (siNo != null && siNo.TrimStart().TrimEnd() != worksheet5.Cells[row, 3].Text.TrimStart().TrimEnd())
                                {
                                    cvChanges["SINo"] = (string.Join(", ", existingCV.SINo.Select(si => si.ToString().TrimStart().TrimEnd())), worksheet5.Cells[row, 3].Text.TrimStart().TrimEnd())!;
                                }

                                var poNo = existingCV.PONo != null
                                    ? string.Join(", ", existingCV.PONo.Select(si => si.ToString()))
                                    : null;
                                if (poNo != null && poNo.TrimStart().TrimEnd() != worksheet5.Cells[row, 4].Text.TrimStart().TrimEnd())
                                {
                                    cvChanges["PONo"] = (string.Join(", ", existingCV.PONo.Select(si => si.ToString().TrimStart().TrimEnd())), worksheet5.Cells[row, 4].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingCV.Particulars.TrimStart().TrimEnd() != worksheet5.Cells[row, 5].Text.TrimStart().TrimEnd())
                                {
                                    cvChanges["Particulars"] = (existingCV.Particulars.TrimStart().TrimEnd(), worksheet5.Cells[row, 5].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingCV.CheckNo.TrimStart().TrimEnd() != worksheet5.Cells[row, 6].Text.TrimStart().TrimEnd())
                                {
                                    cvChanges["CheckNo"] = (existingCV.CheckNo.TrimStart().TrimEnd(), worksheet5.Cells[row, 6].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingCV.Category.TrimStart().TrimEnd() != worksheet5.Cells[row, 7].Text.TrimStart().TrimEnd())
                                {
                                    cvChanges["Category"] = (existingCV.Category.TrimStart().TrimEnd(), worksheet5.Cells[row, 7].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingCV.Payee.TrimStart().TrimEnd() != worksheet5.Cells[row, 8].Text.TrimStart().TrimEnd())
                                {
                                    cvChanges["Payee"] = (existingCV.Payee.TrimStart().TrimEnd(), worksheet5.Cells[row, 8].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingCV.CheckDate?.ToString("yyyy-MM-dd").TrimStart().TrimEnd() != (worksheet5.Cells[row, 9].Text.TrimStart().TrimEnd() == "" ? DateOnly.MinValue.ToString("yyyy-MM-dd").TrimStart().TrimEnd() : worksheet5.Cells[row, 9].Text.TrimStart().TrimEnd()))
                                {
                                    cvChanges["CheckDate"] = (existingCV.CheckDate?.ToString("yyyy-MM-dd").TrimStart().TrimEnd(), worksheet5.Cells[row, 9].Text.TrimStart().TrimEnd() == "" ? DateOnly.MinValue.ToString("yyyy-MM-dd").TrimStart().TrimEnd() : worksheet5.Cells[row, 9].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingCV.StartDate?.ToString("yyyy-MM-dd").TrimStart().TrimEnd() != (worksheet5.Cells[row, 10].Text.TrimStart().TrimEnd() == "" ? DateOnly.MinValue.ToString("yyyy-MM-dd").TrimStart().TrimEnd() : worksheet5.Cells[row, 10].Text.TrimStart().TrimEnd()))
                                {
                                    cvChanges["StartDate"] = (existingCV.StartDate?.ToString("yyyy-MM-dd").TrimStart().TrimEnd(), worksheet5.Cells[row, 10].Text.TrimStart().TrimEnd() == "" ? DateOnly.MinValue.ToString("yyyy-MM-dd").TrimStart().TrimEnd() : worksheet5.Cells[row, 10].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingCV.EndDate?.ToString("yyyy-MM-dd").TrimStart().TrimEnd() != (worksheet5.Cells[row, 11].Text.TrimStart().TrimEnd() == "" ? DateOnly.MinValue.ToString("yyyy-MM-dd").TrimStart().TrimEnd() : worksheet5.Cells[row, 11].Text.TrimStart().TrimEnd()))
                                {
                                    cvChanges["EndDate"] = (existingCV.EndDate?.ToString("yyyy-MM-dd").TrimStart().TrimEnd(), worksheet5.Cells[row, 11].Text.TrimStart().TrimEnd() == "" ? DateOnly.MinValue.ToString("yyyy-MM-dd").TrimStart().TrimEnd() : worksheet5.Cells[row, 11].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingCV.NumberOfMonths.ToString().TrimStart().TrimEnd() != worksheet5.Cells[row, 12].Text.TrimStart().TrimEnd())
                                {
                                    cvChanges["NumberOfMonths"] = (existingCV.NumberOfMonths.ToString().TrimStart().TrimEnd(), worksheet5.Cells[row, 12].Text.TrimStart().TrimEnd());
                                }

                                if (existingCV.NumberOfMonthsCreated.ToString().TrimStart().TrimEnd() != worksheet5.Cells[row, 13].Text.TrimStart().TrimEnd())
                                {
                                    cvChanges["NumberOfMonthsCreated"] = (existingCV.NumberOfMonthsCreated.ToString().TrimStart().TrimEnd(), worksheet5.Cells[row, 13].Text.TrimStart().TrimEnd());
                                }

                                if (existingCV.LastCreatedDate?.ToString("yyyy-MM-dd").TrimStart().TrimEnd() != (worksheet5.Cells[row, 14].Text.TrimStart().TrimEnd() == "" ? DateOnly.MinValue.ToString("yyyy-MM-dd").TrimStart().TrimEnd() : worksheet5.Cells[row, 14].Text.TrimStart().TrimEnd()))
                                {
                                    cvChanges["LastCreatedDate"] = (existingCV.LastCreatedDate?.ToString("yyyy-MM-dd").TrimStart().TrimEnd(), worksheet5.Cells[row, 14].Text.TrimStart().TrimEnd() == "" ? DateOnly.MinValue.ToString("yyyy-MM-dd").TrimStart().TrimEnd() : worksheet5.Cells[row, 14].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingCV.AmountPerMonth.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet5.Cells[row, 15].Text).ToString("F2").TrimStart().TrimEnd())
                                {
                                    cvChanges["AmountPerMonth"] = (existingCV.AmountPerMonth.ToString("F2").TrimStart().TrimEnd(), decimal.Parse(worksheet5.Cells[row, 15].Text).ToString("F2").TrimStart().TrimEnd());
                                }

                                if (existingCV.IsComplete.ToString().ToUpper().TrimStart().TrimEnd() != worksheet5.Cells[row, 16].Text.TrimStart().TrimEnd())
                                {
                                    cvChanges["IsComplete"] = (existingCV.IsComplete.ToString().ToUpper().TrimStart().TrimEnd(), worksheet5.Cells[row, 16].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingCV.AccruedType.TrimStart().TrimEnd() != worksheet5.Cells[row, 17].Text.TrimStart().TrimEnd())
                                {
                                    cvChanges["AccruedType"] = (existingCV.AccruedType.TrimStart().TrimEnd(), worksheet5.Cells[row, 17].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingCV.CvType.TrimStart().TrimEnd() == "Payment")
                                {
                                    var getCvInvoicing = await _dbContext.CheckVoucherHeaders.FirstOrDefaultAsync(cvh => existingCV.Reference == cvh.CVNo, cancellationToken);
                                    if (getCvInvoicing != null && getCvInvoicing.OriginalSeriesNumber.TrimStart().TrimEnd() != worksheet5.Cells[row, 18].Text.TrimStart().TrimEnd())
                                    {
                                        cvChanges["Reference"] = (getCvInvoicing.OriginalSeriesNumber.TrimStart().TrimEnd(), worksheet5.Cells[row, 18].Text.TrimStart().TrimEnd())!;
                                    }
                                }

                                if (existingCV.CreatedBy.TrimStart().TrimEnd() != worksheet5.Cells[row, 19].Text.TrimStart().TrimEnd())
                                {
                                    cvChanges["CreatedBy"] = (existingCV.CreatedBy.TrimStart().TrimEnd(), worksheet5.Cells[row, 19].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingCV.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff").TrimStart().TrimEnd() != worksheet5.Cells[row, 20].Text.TrimStart().TrimEnd())
                                {
                                    cvChanges["CreatedDate"] = (existingCV.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff").TrimStart().TrimEnd(), worksheet5.Cells[row, 20].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingCV.Total.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet5.Cells[row, 21].Text).ToString("F2").TrimStart().TrimEnd())
                                {
                                    cvChanges["Total"] = (existingCV.Total.ToString("F2").TrimStart().TrimEnd(), decimal.Parse(worksheet5.Cells[row, 21].Text).ToString("F2").TrimStart().TrimEnd());
                                }

                                if (existingCV.Category.TrimStart().TrimEnd() == "Trade")
                                {
                                    var amount = existingCV.Amount != null
                                        ? string.Join(" ", existingCV.Amount.Select(si => si.ToString("N4")))
                                        : null;
                                    if (amount != null && amount.TrimStart().TrimEnd() != worksheet5.Cells[row, 22].Text.TrimStart().TrimEnd())
                                    {
                                        cvChanges["Amount"] = (string.Join(" ", existingCV.Amount.Select(si => si.ToString("F4").TrimStart().TrimEnd())), worksheet5.Cells[row, 22].Text.TrimStart().TrimEnd())!;
                                    }
                                }

                                if (existingCV.CheckAmount.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet5.Cells[row, 23].Text).ToString("F2").TrimStart().TrimEnd())
                                {
                                    cvChanges["CheckAmount"] = (existingCV.CheckAmount.ToString("F2").TrimStart().TrimEnd(), decimal.Parse(worksheet5.Cells[row, 23].Text).ToString("F2").TrimStart().TrimEnd());
                                }

                                if (existingCV.CvType.TrimStart().TrimEnd() != worksheet5.Cells[row, 24].Text.TrimStart().TrimEnd())
                                {
                                    cvChanges["CvType"] = (existingCV.CvType.TrimStart().TrimEnd(), worksheet5.Cells[row, 24].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingCV.AmountPaid.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet5.Cells[row, 25].Text).ToString("F2").TrimStart().TrimEnd())
                                {
                                    cvChanges["AmountPaid"] = (existingCV.AmountPaid.ToString("F2").TrimStart().TrimEnd(), decimal.Parse(worksheet5.Cells[row, 25].Text).ToString("F2").TrimStart().TrimEnd());
                                }

                                if (existingCV.IsPaid.ToString().ToUpper().TrimStart().TrimEnd() != worksheet5.Cells[row, 26].Text.TrimStart().TrimEnd())
                                {
                                    cvChanges["IsPaid"] = (existingCV.IsPaid.ToString().ToUpper().TrimStart().TrimEnd(), worksheet5.Cells[row, 26].Text.TrimStart().TrimEnd())!;
                                }

                                if ((string.IsNullOrWhiteSpace(existingCV.CancellationRemarks?.TrimStart().TrimEnd()) ? "" : existingCV.CancellationRemarks.TrimStart().TrimEnd()) != worksheet5.Cells[row, 27].Text.TrimStart().TrimEnd())
                                {
                                    cvChanges["CancellationRemarks"] = (existingCV.CancellationRemarks.TrimStart().TrimEnd(), worksheet5.Cells[row, 27].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingCV.OriginalBankId.ToString().TrimStart().TrimEnd() != (worksheet5.Cells[row, 28].Text.TrimStart().TrimEnd() != "" ? worksheet5.Cells[row, 28].Text.TrimStart().TrimEnd() : 0.ToString()))
                                {
                                    cvChanges["OriginalBankId"] = (existingCV.OriginalBankId.ToString().TrimStart().TrimEnd(), worksheet5.Cells[row, 28].Text.TrimStart().TrimEnd() != "" ? worksheet5.Cells[row, 28].Text.TrimStart().TrimEnd() : 0.ToString())!;
                                }

                                if (existingCV.OriginalSeriesNumber.TrimStart().TrimEnd() != worksheet5.Cells[row, 29].Text.TrimStart().TrimEnd())
                                {
                                    cvChanges["OriginalSeriesNumber"] = (existingCV.OriginalSeriesNumber.TrimStart().TrimEnd(), worksheet5.Cells[row, 29].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingCV.OriginalSupplierId.ToString().TrimStart().TrimEnd() != worksheet5.Cells[row, 30].Text.TrimStart().TrimEnd())
                                {
                                    cvChanges["OriginalSupplierId"] = (existingCV.OriginalSupplierId.ToString().TrimStart().TrimEnd(), worksheet5.Cells[row, 30].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingCV.OriginalDocumentId.ToString().TrimStart().TrimEnd() != worksheet5.Cells[row, 31].Text.TrimStart().TrimEnd())
                                {
                                    cvChanges["OriginalDocumentId"] = (existingCV.OriginalDocumentId.ToString().TrimStart().TrimEnd(), worksheet5.Cells[row, 31].Text.TrimStart().TrimEnd())!;
                                }

                                if (cvChanges.Any())
                                {
                                    await _checkVoucherRepo.LogChangesAsync(existingCV.OriginalDocumentId, cvChanges, _userManager.GetUserName(this.User));
                                }

                                continue;
                            }

                            checkVoucherHeader.SupplierId = await _dbContext.Suppliers
                                                                .Where(supp =>
                                                                    supp.OriginalSupplierId ==
                                                                    checkVoucherHeader.OriginalSupplierId)
                                                                .Select(supp => (int?)supp.Id)
                                                                .FirstOrDefaultAsync(cancellationToken) ??
                                                            throw new InvalidOperationException(
                                                                "Please upload the Excel file for the supplier master file first.");

                            if (checkVoucherHeader.CvType != "Invoicing")
                            {
                                checkVoucherHeader.BankId = await _dbContext.BankAccounts
                                                                .Where(bank =>
                                                                    bank.OriginalBankId ==
                                                                    checkVoucherHeader.OriginalBankId)
                                                                .Select(bank => (int?)bank.Id)
                                                                .FirstOrDefaultAsync(cancellationToken) ??
                                                            throw new InvalidOperationException(
                                                                "Please upload the Excel file for the bank account master file first.");
                            }

                            await _dbContext.CheckVoucherHeaders.AddAsync(checkVoucherHeader, cancellationToken);
                            await _dbContext.SaveChangesAsync(cancellationToken);
                        }

                        #endregion -- Check Voucher Header Import --

                        #region -- Check Voucher Details Import --

                        var cvdRowCount = worksheet6?.Dimension?.Rows ?? 0;

                        for (int cvdRow = 2; cvdRow <= cvdRowCount; cvdRow++)
                        {
                            var checkVoucherDetails = new CheckVoucherDetail
                            {
                                AccountNo = worksheet6.Cells[cvdRow, 1].Text,
                                AccountName = worksheet6.Cells[cvdRow, 2].Text,
                                Debit = decimal.TryParse(worksheet6.Cells[cvdRow, 4].Text, out decimal debit)
                                    ? debit
                                    : 0,
                                Credit = decimal.TryParse(worksheet6.Cells[cvdRow, 5].Text, out decimal credit)
                                    ? credit
                                    : 0,
                                OriginalDocumentId = int.Parse(worksheet6.Cells[cvdRow, 7].Text)
                            };

                            var cvHeader = await _dbContext.CheckVoucherHeaders
                                .Where(cvh => cvh.OriginalDocumentId.ToString() == worksheet6.Cells[cvdRow, 6].Text)
                                .FirstOrDefaultAsync(cancellationToken);

                            checkVoucherDetails.CVHeaderId = cvHeader.Id;
                            checkVoucherDetails.TransactionNo = cvHeader.CVNo;

                            if (cvDictionary.TryGetValue(checkVoucherDetails.TransactionNo, out var value) && !value)
                            {
                                await _dbContext.CheckVoucherDetails.AddAsync(checkVoucherDetails, cancellationToken);
                            }

                            if (cvDictionary.TryGetValue(checkVoucherDetails.TransactionNo, out var boolean) && boolean)
                            {
                                var cvdChanges = new Dictionary<string, (string OriginalValue, string NewValue)>();
                                var existingCVD = await _dbContext.CheckVoucherDetails
                                    .Include(cvd => cvd.Header)
                                    .FirstOrDefaultAsync(cvd => cvd.OriginalDocumentId == checkVoucherDetails.OriginalDocumentId, cancellationToken);

                                if (existingCVD != null)
                                {
                                    if (existingCVD.AccountNo.TrimStart().TrimEnd() != worksheet6.Cells[cvdRow, 1].Text.TrimStart().TrimEnd())
                                    {
                                        cvdChanges["AccountNo"] = (existingCVD.AccountNo.TrimStart().TrimEnd(), worksheet6.Cells[cvdRow, 1].Text.TrimStart().TrimEnd())!;
                                    }

                                    if (existingCVD.AccountName.TrimStart().TrimEnd() != worksheet6.Cells[cvdRow, 2].Text.TrimStart().TrimEnd())
                                    {
                                        cvdChanges["AccountName"] = (existingCVD.AccountName.TrimStart().TrimEnd(), worksheet6.Cells[cvdRow, 2].Text.TrimStart().TrimEnd())!;
                                    }

                                    if (existingCVD.Header.OriginalSeriesNumber.TrimStart().TrimEnd() != worksheet6.Cells[cvdRow, 3].Text.TrimStart().TrimEnd())
                                    {
                                        cvdChanges["TransactionNo"] = (existingCVD.Header.OriginalSeriesNumber.TrimStart().TrimEnd(), worksheet6.Cells[cvdRow, 3].Text.TrimStart().TrimEnd())!;
                                    }

                                    if (existingCVD.Debit.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet6.Cells[cvdRow, 4].Text).ToString("F2").TrimStart().TrimEnd())
                                    {
                                        cvdChanges["Debit"] = (existingCVD.Debit.ToString("F2").TrimStart().TrimEnd(), decimal.Parse(worksheet6.Cells[cvdRow, 4].Text).ToString("F2").TrimStart().TrimEnd());
                                    }

                                    if (existingCVD.Credit.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet6.Cells[cvdRow, 5].Text).ToString("F2").TrimStart().TrimEnd())
                                    {
                                        cvdChanges["Credit"] = (existingCVD.Credit.ToString("F2").TrimStart().TrimEnd(), decimal.Parse(worksheet6.Cells[cvdRow, 5].Text).ToString("F2").TrimStart().TrimEnd());
                                    }

                                    if (existingCVD.Header.OriginalDocumentId.ToString("F0").TrimStart().TrimEnd() != decimal.Parse(worksheet6.Cells[cvdRow, 6].Text).ToString("F0").TrimStart().TrimEnd())
                                    {
                                        cvdChanges["CVHeaderId"] = (existingCVD.Header.OriginalDocumentId.ToString("F0").TrimStart().TrimEnd(), decimal.Parse(worksheet6.Cells[cvdRow, 6].Text).ToString("F0").TrimStart().TrimEnd());
                                    }

                                    if (cvdChanges.Any())
                                    {
                                        await _checkVoucherRepo.LogChangesForCVDAsync(existingCVD.OriginalDocumentId, cvdChanges, _userManager.GetUserName(this.User));
                                    }
                                }
                                continue;
                            }
                        }

                        await _dbContext.SaveChangesAsync(cancellationToken);

                        #endregion -- Check Voucher Details Import --

                        #region -- Journal Voucher Header Import --

                        var rowCount = worksheet.Dimension.Rows;
                        var jvDictionary = new Dictionary<string, bool>();
                        var journalVoucherHeaderList = await _dbContext
                            .JournalVoucherHeaders
                            .ToListAsync(cancellationToken);

                        for (int row = 2; row <= rowCount; row++)  // Assuming the first row is the header
                        {
                            var journalVoucherHeader = new JournalVoucherHeader
                            {
                                JVNo = worksheet.Cells[row, 10].Text,
                                Date = DateOnly.TryParse(worksheet.Cells[row, 1].Text, out DateOnly date) ? date : default,
                                References = worksheet.Cells[row, 2].Text,
                                Particulars = worksheet.Cells[row, 3].Text,
                                CRNo = worksheet.Cells[row, 4].Text,
                                JVReason = worksheet.Cells[row, 5].Text,
                                CreatedBy = worksheet.Cells[row, 6].Text,
                                CreatedDate = DateTime.TryParse(worksheet.Cells[row, 7].Text, out DateTime createdDate) ? createdDate : default,
                                CancellationRemarks = worksheet.Cells[row, 8].Text,
                                OriginalCVId = int.TryParse(worksheet.Cells[row, 9].Text, out int originalCVId) ? originalCVId : 0,
                                OriginalSeriesNumber = worksheet.Cells[row, 10].Text,
                                OriginalDocumentId = int.TryParse(worksheet.Cells[row, 11].Text, out int originalDocumentId) ? originalDocumentId : 0,
                            };

                            var jvhList = journalVoucherHeaderList.Any(cv => cv.OriginalDocumentId == journalVoucherHeader.OriginalDocumentId);
                            var existingJV = await _dbContext.JournalVoucherHeaders.FirstOrDefaultAsync(si => si.OriginalDocumentId == journalVoucherHeader.OriginalDocumentId, cancellationToken);
                            jvDictionary.TryAdd((existingJV != null ? existingJV.JVNo : journalVoucherHeader.JVNo) ?? journalVoucherHeader.JVNo, jvhList);

                            if (jvhList)
                            {
                                var jvChanges = new Dictionary<string, (string OriginalValue, string NewValue)>();

                                if (existingJV.JVNo.TrimStart().TrimEnd() != worksheet.Cells[row, 10].Text.TrimStart().TrimEnd())
                                {
                                    jvChanges["JVNo"] = (existingJV.JVNo.TrimStart().TrimEnd(), worksheet.Cells[row, 10].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingJV.Date.ToString("yyyy-MM-dd").TrimStart().TrimEnd() != worksheet.Cells[row, 1].Text.TrimStart().TrimEnd())
                                {
                                    jvChanges["Date"] = (existingJV.Date.ToString("yyyy-MM-dd").TrimStart().TrimEnd(), worksheet.Cells[row, 1].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingJV.References.TrimStart().TrimEnd() != worksheet.Cells[row, 2].Text.TrimStart().TrimEnd())
                                {
                                    jvChanges["References"] = (existingJV.References.TrimStart().TrimEnd(), worksheet.Cells[row, 2].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingJV.Particulars.TrimStart().TrimEnd() != worksheet.Cells[row, 3].Text.TrimStart().TrimEnd())
                                {
                                    jvChanges["Particulars"] = (existingJV.Particulars.TrimStart().TrimEnd(), worksheet.Cells[row, 3].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingJV.CRNo.TrimStart().TrimEnd() != worksheet.Cells[row, 4].Text.TrimStart().TrimEnd())
                                {
                                    jvChanges["CRNo"] = (existingJV.CRNo.TrimStart().TrimEnd(), worksheet.Cells[row, 4].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingJV.JVReason.TrimStart().TrimEnd() != worksheet.Cells[row, 5].Text.TrimStart().TrimEnd())
                                {
                                    jvChanges["JVReason"] = (existingJV.JVReason.TrimStart().TrimEnd(), worksheet.Cells[row, 5].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingJV.CreatedBy.TrimStart().TrimEnd() != worksheet.Cells[row, 6].Text.TrimStart().TrimEnd())
                                {
                                    jvChanges["CreatedBy"] = (existingJV.CreatedBy.TrimStart().TrimEnd(), worksheet.Cells[row, 6].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingJV.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff").TrimStart().TrimEnd() != worksheet.Cells[row, 7].Text.TrimStart().TrimEnd())
                                {
                                    jvChanges["CreatedDate"] = (existingJV.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff").TrimStart().TrimEnd(), worksheet.Cells[row, 7].Text.TrimStart().TrimEnd())!;
                                }

                                if ((string.IsNullOrWhiteSpace(existingJV.CancellationRemarks?.TrimStart().TrimEnd()) ? "" : existingJV.CancellationRemarks.TrimStart().TrimEnd()) != worksheet.Cells[row, 8].Text.TrimStart().TrimEnd())
                                {
                                    jvChanges["CancellationRemarks"] = (existingJV.CancellationRemarks?.TrimStart().TrimEnd(), worksheet.Cells[row, 8].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingJV.OriginalCVId.ToString().TrimStart().TrimEnd() != worksheet.Cells[row, 9].Text.TrimStart().TrimEnd())
                                {
                                    jvChanges["OriginalCVId"] = (existingJV.OriginalCVId.ToString().TrimStart().TrimEnd(), worksheet.Cells[row, 9].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingJV.OriginalSeriesNumber.TrimStart().TrimEnd() != worksheet.Cells[row, 10].Text.TrimStart().TrimEnd())
                                {
                                    jvChanges["OriginalSeriesNumber"] = (existingJV.OriginalSeriesNumber.TrimStart().TrimEnd(), worksheet.Cells[row, 10].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingJV.OriginalDocumentId.ToString().TrimStart().TrimEnd() != worksheet.Cells[row, 11].Text.TrimStart().TrimEnd())
                                {
                                    jvChanges["OriginalDocumentId"] = (existingJV.OriginalDocumentId.ToString().TrimStart().TrimEnd(), worksheet.Cells[row, 11].Text.TrimStart().TrimEnd())!;
                                }

                                if (jvChanges.Any())
                                {
                                    await _journalVoucherRepo.LogChangesAsync(existingJV.OriginalDocumentId, jvChanges, _userManager.GetUserName(this.User));
                                }

                                continue;
                            }

                            journalVoucherHeader.CVId = await _dbContext.CheckVoucherHeaders
                                .Where(c => c.OriginalDocumentId == journalVoucherHeader.OriginalCVId)
                                .Select(c => (int?)c.Id)
                                .FirstOrDefaultAsync(cancellationToken) ?? throw new InvalidOperationException("Please upload the Excel file for the check voucher first.");

                            await _dbContext.JournalVoucherHeaders.AddAsync(journalVoucherHeader, cancellationToken);
                        }
                        await _dbContext.SaveChangesAsync(cancellationToken);

                        #endregion -- Journal Voucher Header Import --

                        #region -- Journal Voucher Details Import --

                        if(journalVoucherHeaderList.Count >= 0)
                        {
                            var jvdRowCount = worksheet2.Dimension.Rows;

                            for (int jvdRow = 2; jvdRow <= jvdRowCount; jvdRow++)
                            {
                                var journalVoucherDetails = new JournalVoucherDetail
                                {
                                    AccountNo = worksheet2.Cells[jvdRow, 1].Text,
                                    AccountName = worksheet2.Cells[jvdRow, 2].Text,
                                    Debit = decimal.TryParse(worksheet2.Cells[jvdRow, 4].Text, out decimal debit) ? debit : 0,
                                    Credit = decimal.TryParse(worksheet2.Cells[jvdRow, 5].Text, out decimal credit) ? credit : 0,
                                    OriginalDocumentId = int.TryParse(worksheet2.Cells[jvdRow, 7].Text, out int originalDocumentId) ? originalDocumentId : 0
                                };

                                var jvHeader = await _dbContext.JournalVoucherHeaders
                                    .Where(jvh => jvh.OriginalSeriesNumber == worksheet2.Cells[jvdRow, 3].Text)
                                    .FirstOrDefaultAsync(cancellationToken);

                                if (jvHeader != null)
                                {
                                    journalVoucherDetails.JVHeaderId = jvHeader.Id;
                                    journalVoucherDetails.TransactionNo = jvHeader.JVNo;
                                }

                                if (jvDictionary.TryGetValue(journalVoucherDetails.TransactionNo, out var value) && !value)
                                {
                                    await _dbContext.JournalVoucherDetails.AddAsync(journalVoucherDetails, cancellationToken);
                                }

                                if (jvDictionary.TryGetValue(journalVoucherDetails.TransactionNo, out var boolean) && boolean)
                                {
                                    var jvdChanges = new Dictionary<string, (string OriginalValue, string NewValue)>();
                                    var existingJVD = await _dbContext.JournalVoucherDetails
                                        .Include(cvd => cvd.Header)
                                        .FirstOrDefaultAsync(cvd => cvd.OriginalDocumentId == journalVoucherDetails.OriginalDocumentId, cancellationToken);

                                    if (existingJVD != null)
                                    {
                                        if (existingJVD.AccountNo.TrimStart().TrimEnd() != worksheet2.Cells[jvdRow, 1].Text.TrimStart().TrimEnd())
                                        {
                                            jvdChanges["AccountNo"] = (existingJVD.AccountNo.TrimStart().TrimEnd(),
                                                worksheet2.Cells[jvdRow, 1].Text.TrimStart().TrimEnd())!;
                                        }

                                        if (existingJVD.AccountName.TrimStart().TrimEnd() != worksheet2.Cells[jvdRow, 2].Text.TrimStart().TrimEnd())
                                        {
                                            jvdChanges["AccountName"] = (existingJVD.AccountName.TrimStart().TrimEnd(), worksheet2.Cells[jvdRow, 2].Text.TrimStart().TrimEnd())!;
                                        }

                                        if (existingJVD.Header.OriginalSeriesNumber.TrimStart().TrimEnd() != worksheet2.Cells[jvdRow, 3].Text.TrimStart().TrimEnd())
                                        {
                                            jvdChanges["TransactionNo"] = (existingJVD.Header.OriginalSeriesNumber.TrimStart().TrimEnd(), worksheet2.Cells[jvdRow, 3].Text.TrimStart().TrimEnd())!;
                                        }

                                        if (existingJVD.Debit.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet2.Cells[jvdRow, 4].Text).ToString("F2").TrimStart().TrimEnd())
                                        {
                                            jvdChanges["Debit"] = (existingJVD.Debit.ToString("F2").TrimStart().TrimEnd(), decimal.Parse(worksheet2.Cells[jvdRow, 4].Text).ToString("F2").TrimStart().TrimEnd());
                                        }

                                        if (existingJVD.Credit.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet2.Cells[jvdRow, 5].Text).ToString("F2").TrimStart().TrimEnd())
                                        {
                                            jvdChanges["Credit"] = (existingJVD.Credit.ToString("F2").TrimStart().TrimEnd(), decimal.Parse(worksheet2.Cells[jvdRow, 5].Text).ToString("F2").TrimStart().TrimEnd());
                                        }

                                        if (existingJVD.Header.OriginalDocumentId.ToString("F0").TrimStart().TrimEnd() != decimal.Parse(worksheet2.Cells[jvdRow, 6].Text).ToString("F0").TrimStart().TrimEnd())
                                        {
                                            jvdChanges["JVHeaderId"] = (existingJVD.Header.OriginalDocumentId.ToString("F0").TrimStart().TrimEnd(), decimal.Parse(worksheet2.Cells[jvdRow, 6].Text).ToString("F0").TrimStart().TrimEnd());
                                        }

                                        if (jvdChanges.Any())
                                        {
                                            await _journalVoucherRepo.LogChangesForJVDAsync(existingJVD.OriginalDocumentId, jvdChanges, _userManager.GetUserName(this.User));
                                        }
                                    }

                                    continue;
                                }
                            }

                            await _dbContext.SaveChangesAsync(cancellationToken);
                            await transaction.CommitAsync(cancellationToken);

                            var checkChangesOfRecord = await _dbContext.ImportExportLogs
                                .Where(iel => iel.Action == string.Empty).ToListAsync(cancellationToken);
                            if (checkChangesOfRecord.Any())
                            {
                                TempData["importChanges"] = "";
                            }
                        }

                        #endregion -- Journal Voucher Details Import --
                    }
                }
                catch (OperationCanceledException oce)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = oce.Message;
                    return RedirectToAction(nameof(Index), new { view = DynamicView.JournalVoucher });
                }
                catch (InvalidOperationException ioe)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["warning"] = ioe.Message;
                    return RedirectToAction(nameof(Index), new { view = DynamicView.JournalVoucher });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(Index), new { view = DynamicView.JournalVoucher });
                }
            }
            TempData["success"] = "Uploading Success!";
            return RedirectToAction(nameof(Index), new { view = DynamicView.JournalVoucher });
        }

        #endregion -- import xlsx record --
    }
}
