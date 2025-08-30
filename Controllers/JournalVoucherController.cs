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
using Microsoft.IdentityModel.Tokens;

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

        private readonly GeneralRepo _generalRepo;

        public JournalVoucherController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, JournalVoucherRepo journalVoucherRepo, GeneralRepo generalRepo, CheckVoucherRepo checkVoucherRepo, ReceivingReportRepo receivingReportRepo, PurchaseOrderRepo purchaseOrderRepo)
        {
            _dbContext = dbContext;
            _userManager = userManager;
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
                if (!string.IsNullOrEmpty(parameters.Search.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();
                    journalVouchers = journalVouchers
                        .Where(jv =>
                            jv.JournalVoucherHeaderNo!.ToLower().Contains(searchValue) ||
                            jv.Date.ToString("MMM dd, yyyy").ToLower().Contains(searchValue) ||
                            jv.References?.ToLower().Contains(searchValue) == true ||
                            jv.Particulars.ToLower().Contains(searchValue) ||
                            jv.CRNo?.ToLower().Contains(searchValue) == true ||
                            jv.JVReason.ToLower().Contains(searchValue) ||
                            jv.CheckVoucherHeader?.CheckVoucherHeaderNo?.ToLower().Contains(searchValue) == true ||
                            jv.CreatedBy!.ToLower().Contains(searchValue)
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
                                     .Select(jv => jv.JournalVoucherHeaderId) // Assuming Id is the primary key
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
                .Where(coa => !coa.HasChildren)
                .OrderBy(coa => coa.AccountId)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);
            viewModel.Header.CheckVoucherHeaders = await _dbContext.CheckVoucherHeaders
                .Where(cvh => cvh.IsPosted)
                .OrderBy(c => c.CheckVoucherHeaderId)
                .Select(cvh => new SelectListItem
                {
                    Value = cvh.CheckVoucherHeaderId.ToString(),
                    Text = cvh.CheckVoucherHeaderNo
                })
                .ToListAsync(cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(JournalVoucherVM? model, string[] accountNumber, decimal[]? debit, decimal[]? credit, CancellationToken cancellationToken)
        {
            model!.Header!.COA = await _dbContext.ChartOfAccounts
                .Where(coa => !coa.HasChildren)
                .OrderBy(coa => coa.AccountId)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            model.Header.CheckVoucherHeaders = await _dbContext.CheckVoucherHeaders
                .OrderBy(c => c.CheckVoucherHeaderId)
                .Select(cvh => new SelectListItem
                {
                    Value = cvh.CheckVoucherHeaderId.ToString(),
                    Text = cvh.CheckVoucherHeaderNo
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
                    model.Header.JournalVoucherHeaderNo = generateJvNo;
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
                        var currentDebit = debit![i];
                        var currentCredit = credit![i];
                        totalDebit += debit[i];
                        totalCredit += credit[i];

                        cvDetails.Add(
                            new JournalVoucherDetail
                            {
                                AccountNo = currentAccountNumber,
                                AccountName = accountTitle!.AccountName,
                                TransactionNo = generateJvNo,
                                Debit = currentDebit,
                                Credit = currentCredit,
                                JournalVoucherHeaderId = model.Header.JournalVoucherHeaderId
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

                    if (model.Header.OriginalSeriesNumber.IsNullOrEmpty() && model.Header.OriginalDocumentId == 0)
                    {
                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        AuditTrail auditTrailBook = new(model.Header.CreatedBy!, $"Create new journal voucher# {model.Header.JournalVoucherHeaderNo}", "Journal Voucher", ipAddress!);
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

            TempData["error"] = "The information you submitted is not valid!";
            return View(model);
        }

        public async Task<IActionResult> GetCV(int id, CancellationToken cancellationToken)
        {
            var model = await _dbContext.CheckVoucherHeaders
                .Include(s => s.Supplier)
                .Include(cvd => cvd.Details)
                .FirstOrDefaultAsync(cvh => cvh.CheckVoucherHeaderId == id, cancellationToken);

            if (model != null)
            {
                return Json(new
                {
                    CVNo = model.CheckVoucherHeaderNo,
                    model.Date,
                    Name = model.Supplier!.SupplierName,
                    Address = model.Supplier.SupplierAddress,
                    TinNo = model.Supplier.SupplierTin,
                    model.PONo,
                    model.SINo,
                    model.Payee,
                    Amount = model.Total,
                    model.Particulars,
                    model.CheckNo,
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
                .ThenInclude(supplier => supplier!.Supplier)
                .FirstOrDefaultAsync(jvh => jvh.JournalVoucherHeaderId == id.Value, cancellationToken);

            if (header == null)
            {
                return NotFound();
            }

            var details = await _dbContext.JournalVoucherDetails
                .Where(jvd => jvd.TransactionNo == header.JournalVoucherHeaderNo)
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
            var jv = await _dbContext.JournalVoucherHeaders.FirstOrDefaultAsync(x => x.JournalVoucherHeaderId == id, cancellationToken);
            if (jv != null && !jv.IsPrinted)
            {

                #region --Audit Trail Recording

                if (jv.OriginalSeriesNumber.IsNullOrEmpty() && jv.OriginalDocumentId == 0)
                {
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    var printedBy = _userManager.GetUserName(this.User);
                    AuditTrail auditTrailBook = new(printedBy!, $"Printed original copy of jv# {jv.JournalVoucherHeaderNo}", "Journal Voucher", ipAddress!);
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
            var modelHeader = await _dbContext.JournalVoucherHeaders.FirstOrDefaultAsync(x => x.JournalVoucherHeaderId == id, cancellationToken);

            if (modelHeader != null)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    var modelDetails = await _dbContext.JournalVoucherDetails.Where(jvd => jvd.TransactionNo == modelHeader.JournalVoucherHeaderNo).ToListAsync(cancellationToken);
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
                                        Reference = modelHeader.JournalVoucherHeaderNo!,
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
                                        Reference = modelHeader.JournalVoucherHeaderNo!,
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

                        if (modelHeader.OriginalSeriesNumber.IsNullOrEmpty() && modelHeader.OriginalDocumentId == 0)
                        {
                            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                            AuditTrail auditTrailBook = new(modelHeader.PostedBy!, $"Posted journal voucher# {modelHeader.JournalVoucherHeaderNo}", "Journal Voucher", ipAddress!);
                            await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                        }

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Journal Voucher has been Posted.";
                    }
                    return RedirectToAction(nameof(Print), new { id });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(Print), new { id });
                }
            }

            return NotFound();
        }

        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {
            var model = await _dbContext.JournalVoucherHeaders.FirstOrDefaultAsync(x => x.JournalVoucherHeaderId == id, cancellationToken);
            var findJournalVoucherInJournalBook = await _dbContext.JournalBooks.Where(jb => jb.Reference == model!.JournalVoucherHeaderNo).ToListAsync(cancellationToken);
            var findJournalVoucherInGeneralLedger = await _dbContext.GeneralLedgerBooks.Where(jb => jb.Reference == model!.JournalVoucherHeaderNo).ToListAsync(cancellationToken);
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

                        if (findJournalVoucherInJournalBook.Any())
                        {
                            await _generalRepo.RemoveRecords<JournalBook>(crb => crb.Reference == model.JournalVoucherHeaderNo, cancellationToken);
                        }
                        if (findJournalVoucherInGeneralLedger.Any())
                        {
                            await _generalRepo.RemoveRecords<GeneralLedgerBook>(gl => gl.Reference == model.JournalVoucherHeaderNo, cancellationToken);
                        }

                        #region --Audit Trail Recording

                        if (model.OriginalSeriesNumber.IsNullOrEmpty() && model.OriginalDocumentId == 0)
                        {
                            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                            AuditTrail auditTrailBook = new(model.VoidedBy!, $"Voided journal voucher# {model.JournalVoucherHeaderNo}", "Journal Voucher", ipAddress!);
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
            var model = await _dbContext.JournalVoucherHeaders.FirstOrDefaultAsync(x => x.JournalVoucherHeaderId == id, cancellationToken);
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
                            AuditTrail auditTrailBook = new(model.CanceledBy!, $"Cancelled journal voucher# {model.JournalVoucherHeaderNo}", "Journal Voucher", ipAddress!);
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
            if (id == 0)
            {
                return NotFound();
            }
            var existingHeaderModel = await _dbContext.JournalVoucherHeaders
                .Include(jv => jv.CheckVoucherHeader)
                .FirstOrDefaultAsync(cvh => cvh.JournalVoucherHeaderId == id, cancellationToken);
            var existingDetailsModel = await _dbContext.JournalVoucherDetails
                .Where(cvd => cvd.TransactionNo == existingHeaderModel!.JournalVoucherHeaderNo)
                .ToListAsync(cancellationToken);

            if (existingHeaderModel == null || !existingDetailsModel.Any())
            {
                return NotFound();
            }

            var accountNumbers = existingDetailsModel.Select(model => model.AccountNo).ToArray();
            var accountTitles = existingDetailsModel.Select(model => model.AccountName).ToArray();
            var debit = existingDetailsModel.Select(model => model.Debit).ToArray();
            var credit = existingDetailsModel.Select(model => model.Credit).ToArray();

            JournalVoucherViewModel model = new()
            {
                JVId = existingHeaderModel.JournalVoucherHeaderId,
                JVNo = existingHeaderModel.JournalVoucherHeaderNo,
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
                .OrderBy(c => c.CheckVoucherHeaderId)
                .Select(cvh => new SelectListItem
                {
                    Value = cvh.CheckVoucherHeaderId.ToString(),
                    Text = cvh.CheckVoucherHeaderNo
                })
                .ToListAsync(cancellationToken),
                COA = await _dbContext.ChartOfAccounts
                    .Where(coa => !new[] { "202010200", "202010100", "101010100" }.Any(excludedNumber => coa.AccountNumber!.Contains(excludedNumber)) && !coa.HasChildren)
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
            var existingModel = await _dbContext.JournalVoucherHeaders
                .Include(jvd => jvd.Details)
                .FirstOrDefaultAsync(jvh => jvh.JournalVoucherHeaderId == viewModel.JVId, cancellationToken);
            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    #region --Saving the default entries

                    existingModel!.JournalVoucherHeaderNo = viewModel.JVNo;
                    existingModel.Date = viewModel.TransactionDate;
                    existingModel.References = viewModel.References;
                    existingModel.CVId = viewModel.CVId;
                    existingModel.Particulars = viewModel.Particulars;
                    existingModel.CRNo = viewModel.CRNo;
                    existingModel.JVReason = viewModel.JVReason;

                    #endregion --Saving the default entries

                    #region --CV Details Entry

                    // Dictionary to keep track of AccountNo and their ids for comparison
                    var accountTitleDict = new Dictionary<string, List<int>>();
                    foreach (var details in existingModel.Details)
                    {
                        if (!accountTitleDict.ContainsKey(details.AccountNo))
                        {
                            accountTitleDict[details.AccountNo] = new List<int>();
                        }
                        accountTitleDict[details.AccountNo].Add(details.JournalVoucherDetailId);
                    }

                    // Add or update records
                    for (int i = 0; i < viewModel.AccountTitle?.Length; i++)
                    {

                        if (accountTitleDict.TryGetValue(viewModel.AccountNumber?[i]!, out var ids))
                        {
                            // Update the first matching record and remove it from the list
                            var detailsId = ids.First();
                            ids.RemoveAt(0);
                            var details = existingModel.Details.First(o => o.JournalVoucherDetailId == detailsId);

                            var acctNo = await _dbContext.ChartOfAccounts
                                .FirstOrDefaultAsync(x => x.AccountName == viewModel.AccountTitle[i], cancellationToken: cancellationToken);

                            details.AccountNo = acctNo?.AccountNumber ?? "";
                            details.AccountName = viewModel.AccountTitle[i];
                            details.Debit = viewModel.Debit[i];
                            details.Credit = viewModel.Credit[i];
                            details.TransactionNo = existingModel.JournalVoucherHeaderNo!;
                            details.JournalVoucherHeaderId = existingModel.JournalVoucherHeaderId;

                            if (ids.Count == 0)
                            {
                                accountTitleDict.Remove(viewModel.AccountNumber![i]);
                            }
                        }
                        else
                        {
                            // Add new record
                            var newDetails = new JournalVoucherDetail
                            {
                                AccountNo = viewModel.AccountNumber![i],
                                AccountName = viewModel.AccountTitle[i],
                                Debit = viewModel.Debit[i],
                                Credit = viewModel.Credit[i],
                                TransactionNo = existingModel.JournalVoucherHeaderNo!,
                                JournalVoucherHeaderId = existingModel.JournalVoucherHeaderId
                            };
                            await _dbContext.JournalVoucherDetails.AddAsync(newDetails, cancellationToken);
                        }
                    }

                    // Remove remaining records that were duplicates
                    foreach (var ids in accountTitleDict.Values)
                    {
                        foreach (var id in ids)
                        {
                            var details = existingModel.Details.First(o => o.JournalVoucherDetailId == id);
                            _dbContext.JournalVoucherDetails.Remove(details);
                        }
                    }

                    #endregion --CV Details Entry

                    if (_dbContext.ChangeTracker.HasChanges())
                    {
                        #region --Audit Trail Recording

                        if (existingModel.OriginalSeriesNumber.IsNullOrEmpty() && existingModel.OriginalDocumentId == 0)
                        {
                            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                            AuditTrail auditTrailBook = new(User.Identity!.Name!, $"Edit journal voucher# {viewModel.JVNo}", "Journal Voucher", ipAddress!);
                            await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                        }

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
                    viewModel.CheckVoucherHeaders = await _dbContext.CheckVoucherHeaders
                        .OrderBy(c => c.CheckVoucherHeaderId)
                        .Select(cvh => new SelectListItem
                        {
                            Value = cvh.CheckVoucherHeaderId.ToString(),
                            Text = cvh.CheckVoucherHeaderNo
                        })
                        .ToListAsync(cancellationToken);
                    viewModel.COA = await _dbContext.ChartOfAccounts
                        .Where(coa =>
                            !new[] { "202010200", "202010100", "101010100" }.Any(excludedNumber =>
                                coa.AccountNumber!.Contains(excludedNumber)) && !coa.HasChildren)
                        .Select(s => new SelectListItem
                        {
                            Value = s.AccountNumber,
                            Text = s.AccountNumber + " " + s.AccountName
                        })
                        .ToListAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(viewModel);
                }
            }

            TempData["error"] = "The information provided was invalid.";
            viewModel.CheckVoucherHeaders = await _dbContext.CheckVoucherHeaders
                .OrderBy(c => c.CheckVoucherHeaderId)
                .Select(cvh => new SelectListItem
                {
                    Value = cvh.CheckVoucherHeaderId.ToString(),
                    Text = cvh.CheckVoucherHeaderNo
                })
                .ToListAsync(cancellationToken);
            viewModel.COA = await _dbContext.ChartOfAccounts
                .Where(coa =>
                    !new[] { "202010200", "202010100", "101010100" }.Any(excludedNumber =>
                        coa.AccountNumber!.Contains(excludedNumber)) && !coa.HasChildren)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);
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
                    .Where(jv => recordIds.Contains(jv.JournalVoucherHeaderId))
                    .Include(jvh => jvh.CheckVoucherHeader)
                    .OrderBy(jv => jv.JournalVoucherHeaderNo)
                    .ToListAsync(cancellationToken: cancellationToken);

                // Create the Excel package
                using var package = new ExcelPackage();
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

                #region -- Check Voucher Trade Payments Table Header --

                var worksheet7 = package.Workbook.Worksheets.Add("CheckVoucherTradePayments");

                worksheet7.Cells["A1"].Value = "Id";
                worksheet7.Cells["B1"].Value = "DocumentId";
                worksheet7.Cells["C1"].Value = "DocumentType";
                worksheet7.Cells["D1"].Value = "CheckVoucherId";
                worksheet7.Cells["E1"].Value = "AmountPaid";

                #endregion -- Check Voucher Header Table Header --

                #region -- Check Voucher Multiple Payment Table Header --

                var worksheet8 = package.Workbook.Worksheets.Add("MultipleCheckVoucherPayments");

                worksheet8.Cells["A1"].Value = "Id";
                worksheet8.Cells["B1"].Value = "CheckVoucherHeaderPaymentId";
                worksheet8.Cells["C1"].Value = "CheckVoucherHeaderInvoiceId";
                worksheet8.Cells["D1"].Value = "AmountPaid";

                #endregion

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
                    worksheet.Cells[row, 10].Value = item.JournalVoucherHeaderNo;
                    worksheet.Cells[row, 11].Value = item.JournalVoucherHeaderId;

                    row++;
                }

                #endregion -- Journal Voucher Header Export --

                #region -- Check Voucher Header Export (Trade and Invoicing)--

                int cvhRow = 2;
                var currentCvTradeAndInvoicing = "";

                foreach (var item in selectedList)
                {
                    if (item.CheckVoucherHeader == null)
                    {
                        continue;
                    }
                    if (item.CheckVoucherHeader.CheckVoucherHeaderNo == currentCvTradeAndInvoicing)
                    {
                        continue;
                    }

                    currentCvTradeAndInvoicing = item.CheckVoucherHeader.CheckVoucherHeaderNo;
                    worksheet3.Cells[cvhRow, 1].Value = item.Date.ToString("yyyy-MM-dd");
                    if (item.CheckVoucherHeader.RRNo != null && !item.CheckVoucherHeader.RRNo.Contains(null))
                    {
                        worksheet3.Cells[cvhRow, 2].Value = string.Join(", ", item.CheckVoucherHeader.RRNo.Select(rrNo => rrNo.ToString()));
                    }
                    if (item.CheckVoucherHeader.SINo != null && !item.CheckVoucherHeader.SINo.Contains(null))
                    {
                        worksheet3.Cells[cvhRow, 3].Value = string.Join(", ", item.CheckVoucherHeader.SINo.Select(siNo => siNo.ToString()));
                    }
                    if (item.CheckVoucherHeader.PONo != null && !item.CheckVoucherHeader.PONo.Contains(null))
                    {
                        worksheet3.Cells[cvhRow, 4].Value = string.Join(", ", item.CheckVoucherHeader.PONo.Select(poNo => poNo.ToString()));
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
                    worksheet3.Cells[cvhRow, 14].Value = item.CheckVoucherHeader.LastCreatedDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                    worksheet3.Cells[cvhRow, 15].Value = item.CheckVoucherHeader.AmountPerMonth;
                    worksheet3.Cells[cvhRow, 16].Value = item.CheckVoucherHeader.IsComplete;
                    worksheet3.Cells[cvhRow, 17].Value = item.CheckVoucherHeader.AccruedType;
                    worksheet3.Cells[cvhRow, 18].Value = item.CheckVoucherHeader.Reference;
                    worksheet3.Cells[cvhRow, 19].Value = item.CheckVoucherHeader.CreatedBy;
                    worksheet3.Cells[cvhRow, 20].Value = item.CheckVoucherHeader.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                    worksheet3.Cells[cvhRow, 21].Value = item.CheckVoucherHeader.Total;
                    if (item.CheckVoucherHeader.Amount != null)
                    {
                        worksheet3.Cells[cvhRow, 22].Value = string.Join(" ", item.CheckVoucherHeader.Amount.Select(amount => amount.ToString("N4")));
                    }
                    worksheet3.Cells[cvhRow, 23].Value = item.CheckVoucherHeader.CheckAmount;
                    worksheet3.Cells[cvhRow, 24].Value = item.CheckVoucherHeader.CvType;
                    worksheet3.Cells[cvhRow, 25].Value = item.CheckVoucherHeader.AmountPaid;
                    worksheet3.Cells[cvhRow, 26].Value = item.CheckVoucherHeader.IsPaid;
                    worksheet3.Cells[cvhRow, 27].Value = item.CheckVoucherHeader.CancellationRemarks;
                    worksheet3.Cells[cvhRow, 28].Value = item.CheckVoucherHeader.BankId;
                    worksheet3.Cells[cvhRow, 29].Value = item.CheckVoucherHeader.CheckVoucherHeaderNo;
                    worksheet3.Cells[cvhRow, 30].Value = item.CheckVoucherHeader.SupplierId;
                    worksheet3.Cells[cvhRow, 31].Value = item.CheckVoucherHeader.CheckVoucherHeaderId;
                    worksheet3.Cells[cvhRow, 32].Value = item.CheckVoucherHeader.PostedBy;
                    worksheet3.Cells[cvhRow, 33].Value = item.CheckVoucherHeader.PostedDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff") ?? null;

                    cvhRow++;
                }

                var getCheckVoucherTradePayment = await _dbContext.CVTradePayments
                    .Where(cv => recordIds.Contains(cv.CheckVoucherId) && cv.DocumentType == "RR")
                    .ToListAsync();

                int cvRow = 2;
                foreach (var payment in getCheckVoucherTradePayment)
                {
                    worksheet7.Cells[cvRow, 1].Value = payment.Id;
                    worksheet7.Cells[cvRow, 2].Value = payment.DocumentId;
                    worksheet7.Cells[cvRow, 3].Value = payment.DocumentType;
                    worksheet7.Cells[cvRow, 4].Value = payment.CheckVoucherId;
                    worksheet7.Cells[cvRow, 5].Value = payment.AmountPaid;

                    cvRow++;
                }

                #endregion -- Check Voucher Header Export (Trade and Invoicing) --

                #region -- Check Voucher Header Export (Payment) --

                var cvNos = selectedList.Select(item => item.CheckVoucherHeader!.CheckVoucherHeaderNo).ToList();
                var currentCvPayment = "";

                var checkVoucherPayment = await _dbContext.CheckVoucherHeaders
                    .Where(cvh => cvh.Reference != null && cvNos.Contains(cvh.CheckVoucherHeaderNo))
                    .ToListAsync();

                foreach (var item in checkVoucherPayment)
                {
                    if (item.CheckVoucherHeaderNo == currentCvPayment)
                    {
                        continue;
                    }

                    currentCvPayment = item.CheckVoucherHeaderNo;
                    worksheet3.Cells[cvhRow, 1].Value = item.Date.ToString("yyyy-MM-dd");
                    if (item.RRNo != null && !item.RRNo.Contains(null))
                    {
                        worksheet3.Cells[cvhRow, 2].Value = string.Join(", ", item.RRNo.Select(rrNo => rrNo.ToString()));
                    }
                    if (item.SINo != null && !item.SINo.Contains(null))
                    {
                        worksheet3.Cells[cvhRow, 3].Value = string.Join(", ", item.SINo.Select(siNo => siNo.ToString()));
                    }
                    if (item.PONo != null && !item.PONo.Contains(null))
                    {
                        worksheet3.Cells[cvhRow, 4].Value = string.Join(", ", item.PONo.Select(poNo => poNo.ToString()));
                    }

                    worksheet3.Cells[cvhRow, 5].Value = item.Particulars;
                    worksheet3.Cells[cvhRow, 6].Value = item.CheckNo;
                    worksheet3.Cells[cvhRow, 7].Value = item.Category;
                    worksheet3.Cells[cvhRow, 8].Value = item.Payee;
                    worksheet3.Cells[cvhRow, 9].Value = item.CheckDate?.ToString("yyyy-MM-dd");
                    worksheet3.Cells[cvhRow, 10].Value = item.StartDate?.ToString("yyyy-MM-dd");
                    worksheet3.Cells[cvhRow, 11].Value = item.EndDate?.ToString("yyyy-MM-dd");
                    worksheet3.Cells[cvhRow, 12].Value = item.NumberOfMonths;
                    worksheet3.Cells[cvhRow, 13].Value = item.NumberOfMonthsCreated;
                    worksheet3.Cells[cvhRow, 14].Value = item.LastCreatedDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                    worksheet3.Cells[cvhRow, 15].Value = item.AmountPerMonth;
                    worksheet3.Cells[cvhRow, 16].Value = item.IsComplete;
                    worksheet3.Cells[cvhRow, 17].Value = item.AccruedType;
                    worksheet3.Cells[cvhRow, 18].Value = item.Reference;
                    worksheet3.Cells[cvhRow, 19].Value = item.CreatedBy;
                    worksheet3.Cells[cvhRow, 20].Value = item.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                    worksheet3.Cells[cvhRow, 21].Value = item.Total;
                    if (item.Amount != null)
                    {
                        worksheet3.Cells[cvhRow, 22].Value = string.Join(" ", item.Amount.Select(amount => amount.ToString("N4")));
                    }
                    worksheet3.Cells[cvhRow, 23].Value = item.CheckAmount;
                    worksheet3.Cells[cvhRow, 24].Value = item.CvType;
                    worksheet3.Cells[cvhRow, 25].Value = item.AmountPaid;
                    worksheet3.Cells[cvhRow, 26].Value = item.IsPaid;
                    worksheet3.Cells[cvhRow, 27].Value = item.CancellationRemarks;
                    worksheet3.Cells[cvhRow, 28].Value = item.BankId;
                    worksheet3.Cells[cvhRow, 29].Value = item.CheckVoucherHeaderNo;
                    worksheet3.Cells[cvhRow, 30].Value = item.SupplierId;
                    worksheet3.Cells[cvhRow, 31].Value = item.CheckVoucherHeaderId;
                    worksheet3.Cells[cvhRow, 32].Value = item.PostedBy;
                    worksheet3.Cells[cvhRow, 33].Value = item.PostedDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff") ?? null;

                    cvhRow++;
                }

                var cvPaymentId = checkVoucherPayment.Select(cvn => cvn.CheckVoucherHeaderId).ToList();
                var getCheckVoucherMultiplePayment = await _dbContext.MultipleCheckVoucherPayments
                    .Where(cv => cvPaymentId.Contains(cv.CheckVoucherHeaderPaymentId))
                    .ToListAsync();

                int cvn = 2;
                foreach (var payment in getCheckVoucherMultiplePayment)
                {
                    worksheet8.Cells[cvn, 1].Value = payment.Id;
                    worksheet8.Cells[cvn, 2].Value = payment.CheckVoucherHeaderPaymentId;
                    worksheet8.Cells[cvn, 3].Value = payment.CheckVoucherHeaderInvoiceId;
                    worksheet8.Cells[cvn, 4].Value = payment.AmountPaid;

                    cvn++;
                }

                #endregion -- Check Voucher Header Export (Payment) --

                #region -- Journal Voucher Details Export --

                var jvNos = selectedList.Select(item => item.JournalVoucherHeaderNo).ToList();

                var getJvDetails = await _dbContext.JournalVoucherDetails
                    .Where(jvd => jvNos.Contains(jvd.TransactionNo))
                    .OrderBy(jvd => jvd.JournalVoucherDetailId)
                    .ToListAsync(cancellationToken: cancellationToken);

                int jvdRow = 2;

                foreach (var item in getJvDetails)
                {
                    worksheet2.Cells[jvdRow, 1].Value = item.AccountNo;
                    worksheet2.Cells[jvdRow, 2].Value = item.AccountName;
                    worksheet2.Cells[jvdRow, 3].Value = item.TransactionNo;
                    worksheet2.Cells[jvdRow, 4].Value = item.Debit;
                    worksheet2.Cells[jvdRow, 5].Value = item.Credit;
                    worksheet2.Cells[jvdRow, 6].Value = item.JournalVoucherHeaderId;
                    worksheet2.Cells[jvdRow, 7].Value = item.JournalVoucherDetailId;

                    jvdRow++;
                }

                #endregion -- Journal Voucher Details Export --

                #region -- Check Voucher Details Export (Trade and Invoicing) --

                var getCvDetails = await _dbContext.CheckVoucherDetails
                    .Where(cvd => cvNos.Contains(cvd.TransactionNo))
                    .OrderBy(cvd => cvd.CheckVoucherHeaderId)
                    .ToListAsync();

                var cvdRow = 2;

                foreach (var item in getCvDetails)
                {
                    worksheet4.Cells[cvdRow, 1].Value = item.AccountNo;
                    worksheet4.Cells[cvdRow, 2].Value = item.AccountName;
                    worksheet4.Cells[cvdRow, 3].Value = item.TransactionNo;
                    worksheet4.Cells[cvdRow, 4].Value = item.Debit;
                    worksheet4.Cells[cvdRow, 5].Value = item.Credit;
                    worksheet4.Cells[cvdRow, 6].Value = item.CheckVoucherHeaderId;
                    worksheet4.Cells[cvdRow, 7].Value = item.CheckVoucherDetailId;
                    worksheet4.Cells[cvdRow, 8].Value = item.Amount;
                    worksheet4.Cells[cvdRow, 9].Value = item.AmountPaid;
                    worksheet4.Cells[cvdRow, 10].Value = item.SupplierId;
                    worksheet4.Cells[cvdRow, 11].Value = item.EwtPercent;
                    worksheet4.Cells[cvdRow, 12].Value = item.IsUserSelected;
                    worksheet4.Cells[cvdRow, 13].Value = item.IsVatable;

                    cvdRow++;
                }

                #endregion -- Check Voucher Details Export (Trade and Invoicing) --

                #region -- Check Voucher Details Export (Payment) --

                var getCvPaymentDetails = await _dbContext.CheckVoucherDetails
                    .Where(cvd => checkVoucherPayment.Select(cvh => cvh.CheckVoucherHeaderNo).Contains(cvd.TransactionNo))
                    .OrderBy(cvd => cvd.CheckVoucherHeaderId)
                    .ToListAsync();

                foreach (var item in getCvPaymentDetails)
                {
                    worksheet4.Cells[cvdRow, 1].Value = item.AccountNo;
                    worksheet4.Cells[cvdRow, 2].Value = item.AccountName;
                    worksheet4.Cells[cvdRow, 3].Value = item.TransactionNo;
                    worksheet4.Cells[cvdRow, 4].Value = item.Debit;
                    worksheet4.Cells[cvdRow, 5].Value = item.Credit;
                    worksheet4.Cells[cvdRow, 6].Value = item.CheckVoucherHeaderId;
                    worksheet4.Cells[cvdRow, 7].Value = item.CheckVoucherDetailId;
                    worksheet4.Cells[cvdRow, 8].Value = item.Amount;
                    worksheet4.Cells[cvdRow, 9].Value = item.AmountPaid;
                    worksheet4.Cells[cvdRow, 10].Value = item.SupplierId;
                    worksheet4.Cells[cvdRow, 11].Value = item.EwtPercent;
                    worksheet4.Cells[cvdRow, 12].Value = item.IsUserSelected;
                    worksheet4.Cells[cvdRow, 13].Value = item.IsVatable;

                    cvdRow++;
                }

                #endregion -- Check Voucher Details Export (Payment) --

                #region -- Receving Report Export --

                var selectedIds = selectedList.Select(item => item.CheckVoucherHeader.CheckVoucherHeaderId).ToList();

                var cvTradePaymentList = await _dbContext.CVTradePayments
                    .Where(p => selectedIds.Contains(p.CheckVoucherId))
                    .ToListAsync();

                var rrIds = cvTradePaymentList.Select(item => item.DocumentId).ToList();

                var getReceivingReport = await _dbContext.ReceivingReports
                    .Where(rr => rrIds.Contains(rr.ReceivingReportId))
                    .ToListAsync(cancellationToken);

                int rrRow = 2;
                var currentRr = "";

                foreach (var item in getReceivingReport)
                {
                    if (item.ReceivingReportNo == currentRr)
                    {
                        continue;
                    }

                    currentRr = item.ReceivingReportNo;
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
                    worksheet6.Cells[rrRow, 21].Value = item.ReceivingReportNo;
                    worksheet6.Cells[rrRow, 22].Value = item.ReceivingReportId;

                    rrRow++;
                }

                #endregion -- Receving Report Export --

                #region -- Purchase Order Export --

                var getPurchaseOrder = await _dbContext.PurchaseOrders
                    .Where(po => getReceivingReport.Select(item => item.POId).Contains(po.PurchaseOrderId))
                    .OrderBy(po => po.PurchaseOrderNo)
                    .ToListAsync(cancellationToken: cancellationToken);

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
                    worksheet5.Cells[poRow, 9].Value = item.ReceivedDate != default ? item.ReceivedDate.ToString("yyyy-MM-dd HH:mm:ss.ffffff zzz") : null;
                    worksheet5.Cells[poRow, 10].Value = item.Remarks;
                    worksheet5.Cells[poRow, 11].Value = item.CreatedBy;
                    worksheet5.Cells[poRow, 12].Value = item.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                    worksheet5.Cells[poRow, 13].Value = item.IsClosed;
                    worksheet5.Cells[poRow, 14].Value = item.CancellationRemarks;
                    worksheet5.Cells[poRow, 15].Value = item.ProductId;
                    worksheet5.Cells[poRow, 16].Value = item.PurchaseOrderNo;
                    worksheet5.Cells[poRow, 17].Value = item.SupplierId;
                    worksheet5.Cells[poRow, 18].Value = item.PurchaseOrderId;

                    poRow++;
                }

                #endregion -- Purchase Order Export --

                // Convert the Excel package to a byte array
                var excelBytes = await package.GetAsByteArrayAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"JournalVoucherList_IBS-RCD_{DateTime.Now:yyyyddMMHHmmss}.xlsx");
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
                            PurchaseOrderNo = worksheet3.Cells[row, 16].Text,
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
                            PostedBy = worksheet3.Cells[row, 19].Text,
                            PostedDate = DateTime.TryParse(worksheet3.Cells[row, 20].Text, out DateTime postedDate) ? postedDate : default,
                            IsClosed = bool.TryParse(worksheet3.Cells[row, 13].Text, out bool isClosed) && isClosed,
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
                            var existingPo = await _dbContext.PurchaseOrders.FirstOrDefaultAsync(si => si.OriginalDocumentId == purchaseOrder.OriginalDocumentId, cancellationToken);
                            var existingPoInLogs = await _dbContext.ImportExportLogs
                                .Where(x => x.DocumentNo == existingPo.PurchaseOrderNo)
                                .ToListAsync(cancellationToken);


                            if (existingPo!.PurchaseOrderNo!.TrimStart().TrimEnd() != worksheet3.Cells[row, 16].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingPo.PurchaseOrderNo.TrimStart().TrimEnd();
                                var adjustedValue = worksheet3.Cells[row, 16].Text.TrimStart().TrimEnd();
                                var find  = existingPoInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    poChanges["PONo"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingPo.Date.ToString("yyyy-MM-dd").TrimStart().TrimEnd() != worksheet3.Cells[row, 1].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingPo.Date.ToString("yyyy-MM-dd").TrimStart().TrimEnd();
                                var adjustedValue = worksheet3.Cells[row, 1].Text.TrimStart().TrimEnd();
                                var find  = existingPoInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    poChanges["Date"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingPo.Terms.TrimStart().TrimEnd() != worksheet3.Cells[row, 2].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingPo.Terms.TrimStart().TrimEnd();
                                var adjustedValue = worksheet3.Cells[row, 2].Text.TrimStart().TrimEnd();
                                var find  = existingPoInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    poChanges["Terms"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingPo.Quantity.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet3.Cells[row, 3].Text).ToString("F2").TrimStart().TrimEnd())
                            {
                                var originalValue = existingPo.Quantity.ToString("F2").TrimStart().TrimEnd();
                                var adjustedValue = decimal.Parse(worksheet3.Cells[row, 3].Text).ToString("F2").TrimStart().TrimEnd();
                                var find  = existingPoInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    poChanges["Quantity"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingPo.Price.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet3.Cells[row, 4].Text).ToString("F2").TrimStart().TrimEnd())
                            {
                                var originalValue = existingPo.Price.ToString("F2").TrimStart().TrimEnd();
                                var adjustedValue = decimal.Parse(worksheet3.Cells[row, 4].Text).ToString("F2").TrimStart().TrimEnd();
                                var find  = existingPoInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    poChanges["Price"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingPo.Amount.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet3.Cells[row, 5].Text).ToString("F2").TrimStart().TrimEnd())
                            {
                                var originalValue = existingPo.Amount.ToString("F2").TrimStart().TrimEnd();
                                var adjustedValue = decimal.Parse(worksheet3.Cells[row, 5].Text).ToString("F2").TrimStart().TrimEnd();
                                var find  = existingPoInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    poChanges["Amount"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingPo.FinalPrice?.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet3.Cells[row, 6].Text).ToString("F2").TrimStart().TrimEnd())
                            {
                                var originalValue = existingPo.FinalPrice?.ToString("F2").TrimStart().TrimEnd();
                                var adjustedValue = decimal.Parse(worksheet3.Cells[row, 6].Text).ToString("F2").TrimStart().TrimEnd();
                                var find  = existingPoInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    poChanges["FinalPrice"] = (originalValue, adjustedValue)!;
                                }
                            }

                            if (existingPo.Remarks.TrimStart().TrimEnd() != worksheet3.Cells[row, 10].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingPo.Remarks.TrimStart().TrimEnd();
                                var adjustedValue = worksheet3.Cells[row, 10].Text.TrimStart().TrimEnd();
                                var find  = existingPoInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    poChanges["Remarks"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingPo.CreatedBy!.TrimStart().TrimEnd() != worksheet3.Cells[row, 11].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingPo.CreatedBy.TrimStart().TrimEnd();
                                var adjustedValue = worksheet3.Cells[row, 11].Text.TrimStart().TrimEnd();
                                var find  = existingPoInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    poChanges["CreatedBy"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingPo.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff").TrimStart().TrimEnd() != worksheet3.Cells[row, 12].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingPo.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff").TrimStart().TrimEnd();
                                var adjustedValue = worksheet3.Cells[row, 12].Text.TrimStart().TrimEnd();
                                var find  = existingPoInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    poChanges["CreatedDate"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingPo.IsClosed.ToString().ToUpper().TrimStart().TrimEnd() != worksheet3.Cells[row, 13].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingPo.IsClosed.ToString().ToUpper().TrimStart().TrimEnd();
                                var adjustedValue = worksheet3.Cells[row, 13].Text.TrimStart().TrimEnd();
                                var find  = existingPoInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    poChanges["IsClosed"] = (originalValue, adjustedValue);
                                }
                            }

                            if ((string.IsNullOrWhiteSpace(existingPo.CancellationRemarks?.TrimStart().TrimEnd()) ? "" : existingPo.CancellationRemarks.TrimStart().TrimEnd()) != worksheet3.Cells[row, 14].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingPo.CancellationRemarks?.TrimStart().TrimEnd();
                                var adjustedValue = worksheet3.Cells[row, 14].Text.TrimStart().TrimEnd();
                                var find  = existingPoInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    poChanges["CancellationRemarks"] = (originalValue, adjustedValue)!;
                                }
                            }

                            if (existingPo.OriginalProductId.ToString()!.TrimStart().TrimEnd() != (worksheet3.Cells[row, 15].Text.TrimStart().TrimEnd() == "" ? 0.ToString() : worksheet3.Cells[row, 15].Text.TrimStart().TrimEnd()))
                            {
                                var originalValue = existingPo.OriginalProductId.ToString()!.TrimStart().TrimEnd();
                                var adjustedValue = worksheet3.Cells[row, 15].Text.TrimStart().TrimEnd() == ""
                                    ? 0.ToString()
                                    : worksheet3.Cells[row, 15].Text.TrimStart().TrimEnd();
                                var find  = existingPoInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    poChanges["OriginalProductId"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingPo.OriginalSeriesNumber!.TrimStart().TrimEnd() != worksheet3.Cells[row, 16].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingPo.OriginalSeriesNumber.TrimStart().TrimEnd();
                                var adjustedValue = worksheet3.Cells[row, 16].Text.TrimStart().TrimEnd();
                                var find  = existingPoInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    poChanges["OriginalSeriesNumber"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingPo.OriginalSupplierId.ToString()!.TrimStart().TrimEnd() != (worksheet3.Cells[row, 17].Text.TrimStart().TrimEnd() == "" ? 0.ToString() : worksheet3.Cells[row, 17].Text.TrimStart().TrimEnd()))
                            {
                                var originalValue = existingPo.SupplierId.ToString()!.TrimStart().TrimEnd();
                                var adjustedValue = worksheet3.Cells[row, 17].Text.TrimStart().TrimEnd() == ""
                                    ? 0.ToString()
                                    : worksheet3.Cells[row, 17].Text.TrimStart().TrimEnd();
                                var find  = existingPoInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    poChanges["SupplierId"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingPo.OriginalDocumentId.ToString().TrimStart().TrimEnd() != (worksheet3.Cells[row, 18].Text.TrimStart().TrimEnd() == "" ? 0.ToString() : worksheet3.Cells[row, 18].Text.TrimStart().TrimEnd()))
                            {
                                var originalValue = existingPo.OriginalDocumentId.ToString().TrimStart().TrimEnd();
                                var adjustedValue = worksheet3.Cells[row, 18].Text.TrimStart().TrimEnd() == ""
                                    ? 0.ToString()
                                    : worksheet3.Cells[row, 18].Text.TrimStart().TrimEnd();
                                var find  = existingPoInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    poChanges["OriginalDocumentId"] = (originalValue, adjustedValue);
                                }
                            }

                            if (poChanges.Any())
                            {
                                await _purchaseOrderRepo.LogChangesAsync(existingPo.OriginalDocumentId, poChanges, _userManager.GetUserName(this.User), existingPo.PurchaseOrderNo);
                            }

                            continue;
                        }
                        else
                        {
                            #region --Audit Trail Recording

                            if (!purchaseOrder.CreatedBy.IsNullOrEmpty())
                            {
                                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                                AuditTrail auditTrailBook = new(purchaseOrder.CreatedBy, $"Create new purchase order# {purchaseOrder.PurchaseOrderNo}", "Purchase Order", ipAddress!, purchaseOrder.CreatedDate);
                                await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                            }
                            if (!purchaseOrder.PostedBy.IsNullOrEmpty())
                            {
                                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                                AuditTrail auditTrailBook = new(purchaseOrder.PostedBy, $"Posted purchase order# {purchaseOrder.PurchaseOrderNo}", "Purchase Order", ipAddress!, purchaseOrder.PostedDate);
                                await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                            }

                            #endregion --Audit Trail Recording
                        }

                        var getProduct = await _dbContext.Products
                            .Where(p => p.OriginalProductId == purchaseOrder.OriginalProductId)
                            .FirstOrDefaultAsync(cancellationToken);

                        if (getProduct != null)
                        {
                            purchaseOrder.ProductId = getProduct.ProductId;

                            purchaseOrder.ProductNo = getProduct.ProductCode;
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
                            purchaseOrder.SupplierId = getSupplier.SupplierId;

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
                            ReceivingReportNo = worksheet4.Cells[row, 21].Text,
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
                            PostedBy = worksheet4.Cells[row, 23].Text,
                            PostedDate = DateTime.TryParse(worksheet4.Cells[row, 24].Text, out DateTime postedDate) ? postedDate : default,
                            CancellationRemarks = worksheet4.Cells[row, 18].Text != "" ? worksheet4.Cells[row, 18].Text : null,
                            ReceivedDate = DateOnly.TryParse(worksheet4.Cells[row, 19].Text, out DateOnly receivedDate) ? receivedDate : default,
                            OriginalPOId = int.TryParse(worksheet4.Cells[row, 20].Text, out int originalPoId) ? originalPoId : 0,
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
                            var existingRr = await _dbContext.ReceivingReports.FirstOrDefaultAsync(rr => rr.OriginalDocumentId == receivingReport.OriginalDocumentId, cancellationToken);
                            var existingRrInLogs = await _dbContext.ImportExportLogs
                                .Where(x => x.DocumentNo == existingRr.ReceivingReportNo)
                                .ToListAsync(cancellationToken);

                            if (existingRr!.ReceivingReportNo!.TrimStart().TrimEnd() != worksheet4.Cells[row, 21].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingRr.ReceivingReportNo.TrimStart().TrimEnd();
                                var adjustedValue = worksheet4.Cells[row, 21].Text.TrimStart().TrimEnd();
                                var find  = existingRrInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["RRNo"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingRr.Date.ToString("yyyy-MM-dd").TrimStart().TrimEnd() != worksheet4.Cells[row, 1].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingRr.Date.ToString("yyyy-MM-dd").TrimStart().TrimEnd();
                                var adjustedValue = worksheet4.Cells[row, 1].Text.TrimStart().TrimEnd();
                                var find  = existingRrInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["Date"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingRr.DueDate.ToString("yyyy-MM-dd").TrimStart().TrimEnd() != worksheet4.Cells[row, 2].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingRr.DueDate.ToString("yyyy-MM-dd").TrimStart().TrimEnd();
                                var adjustedValue = worksheet4.Cells[row, 2].Text.TrimStart().TrimEnd();
                                var find  = existingRrInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["DueDate"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingRr.SupplierInvoiceNumber?.TrimStart().TrimEnd() != (worksheet4.Cells[row, 3].Text.TrimStart().TrimEnd() == "" ? null : worksheet4.Cells[row, 3].Text.TrimStart().TrimEnd()))
                            {
                                var originalValue = existingRr.SupplierInvoiceNumber?.TrimStart().TrimEnd();
                                var adjustedValue = worksheet4.Cells[row, 3].Text.TrimStart().TrimEnd() == ""
                                    ? null
                                    : worksheet4.Cells[row, 3].Text.TrimStart().TrimEnd();
                                var find  = existingRrInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["SupplierInvoiceNumber"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingRr.SupplierInvoiceDate?.TrimStart().TrimEnd() != worksheet4.Cells[row, 4].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingRr.SupplierInvoiceDate?.TrimStart().TrimEnd();
                                var adjustedValue = worksheet4.Cells[row, 4].Text.TrimStart().TrimEnd();
                                var find  = existingRrInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["SupplierInvoiceDate"] = (originalValue, adjustedValue)!;
                                }
                            }

                            if (existingRr.TruckOrVessels.TrimStart().TrimEnd() != worksheet4.Cells[row, 5].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingRr.TruckOrVessels.TrimStart().TrimEnd();
                                var adjustedValue = worksheet4.Cells[row, 5].Text.TrimStart().TrimEnd();
                                var find  = existingRrInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["TruckOrVessels"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingRr.QuantityDelivered.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet4.Cells[row, 6].Text).ToString("F2").TrimStart().TrimEnd())
                            {
                                var originalValue = existingRr.QuantityDelivered.ToString("F2").TrimStart().TrimEnd();
                                var adjustedValue = decimal.Parse(worksheet4.Cells[row, 6].Text).ToString("F2").TrimStart().TrimEnd();
                                var find  = existingRrInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["QuantityDelivered"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingRr.QuantityReceived.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet4.Cells[row, 7].Text).ToString("F2").TrimStart().TrimEnd())
                            {
                                var originalValue = existingRr.QuantityReceived.ToString("F2").TrimStart().TrimEnd();
                                var adjustedValue = decimal.Parse(worksheet4.Cells[row, 7].Text).ToString("F2").TrimStart().TrimEnd();
                                var find  = existingRrInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["QuantityReceived"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingRr.GainOrLoss.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet4.Cells[row, 8].Text).ToString("F2").TrimStart().TrimEnd())
                            {
                                var originalValue = existingRr.GainOrLoss.ToString("F2").TrimStart().TrimEnd();
                                var adjustedValue = decimal.Parse(worksheet4.Cells[row, 8].Text).ToString("F2").TrimStart().TrimEnd();
                                var find  = existingRrInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["GainOrLoss"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingRr.Amount.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet4.Cells[row, 9].Text).ToString("F2").TrimStart().TrimEnd())
                            {
                                var originalValue = existingRr.Amount.ToString("F2").TrimStart().TrimEnd();
                                var adjustedValue = decimal.Parse(worksheet4.Cells[row, 9].Text).ToString("F2").TrimStart().TrimEnd();
                                var find  = existingRrInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["Amount"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingRr.OtherRef?.TrimStart().TrimEnd() != (worksheet4.Cells[row, 10].Text.TrimStart().TrimEnd() == "" ? null : worksheet4.Cells[row, 10].Text.TrimStart().TrimEnd()))
                            {
                                var originalValue = existingRr.OtherRef?.TrimStart().TrimEnd();
                                var adjustedValue = worksheet4.Cells[row, 10].Text.TrimStart().TrimEnd() == ""
                                    ? null
                                    : worksheet4.Cells[row, 10].Text.TrimStart().TrimEnd();
                                var find  = existingRrInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["OtherRef"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingRr.Remarks.TrimStart().TrimEnd() != worksheet4.Cells[row, 11].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingRr.Remarks.TrimStart().TrimEnd();
                                var adjustedValue = worksheet4.Cells[row, 11].Text.TrimStart().TrimEnd();
                                var find  = existingRrInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["Remarks"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingRr.CreatedBy?.TrimStart().TrimEnd() != worksheet4.Cells[row, 16].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingRr.CreatedBy?.TrimStart().TrimEnd();
                                var adjustedValue = worksheet4.Cells[row, 16].Text.TrimStart().TrimEnd();
                                var find  = existingRrInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["CreatedBy"] = (originalValue, adjustedValue)!;
                                }
                            }

                            if (existingRr.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff").TrimStart().TrimEnd() != worksheet4.Cells[row, 17].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingRr.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff").TrimStart().TrimEnd();
                                var adjustedValue = worksheet4.Cells[row, 17].Text.TrimStart().TrimEnd();
                                var find  = existingRrInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["CreatedDate"] = (originalValue, adjustedValue);
                                }
                            }

                            if ((string.IsNullOrWhiteSpace(existingRr.CancellationRemarks?.TrimStart().TrimEnd()) ? "" : existingRr.CancellationRemarks.TrimStart().TrimEnd()) != worksheet4.Cells[row, 18].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingRr.CancellationRemarks?.TrimStart().TrimEnd();
                                var adjustedValue = worksheet4.Cells[row, 18].Text.TrimStart().TrimEnd();
                                var find  = existingRrInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["CancellationRemarks"] = (originalValue, adjustedValue)!;
                                }
                            }

                            if (existingRr.ReceivedDate?.ToString("yyyy-MM-dd").TrimStart().TrimEnd() != (worksheet4.Cells[row, 19].Text.TrimStart().TrimEnd() == "" ? DateOnly.MinValue.ToString("yyyy-MM-dd").TrimStart().TrimEnd() : worksheet4.Cells[row, 19].Text.TrimStart().TrimEnd()))
                            {
                                var originalValue = existingRr.ReceivedDate?.ToString("yyyy-MM-dd").TrimStart().TrimEnd();
                                var adjustedValue = worksheet4.Cells[row, 19].Text.TrimStart().TrimEnd();
                                var find  = existingRrInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["ReceivedDate"] = (originalValue, adjustedValue)!;
                                }
                            }

                            if (existingRr.OriginalPOId?.ToString().TrimStart().TrimEnd() != (worksheet4.Cells[row, 20].Text.TrimStart().TrimEnd() == "" ? 0.ToString() : worksheet4.Cells[row, 20].Text.TrimStart().TrimEnd()))
                            {
                                var originalValue = existingRr.OriginalPOId?.ToString().TrimStart().TrimEnd();
                                var adjustedValue = worksheet4.Cells[row, 20].Text.TrimStart().TrimEnd() == "" ? 0.ToString() : worksheet4.Cells[row, 20].Text.TrimStart().TrimEnd();
                                var find  = existingRrInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["OriginalPOId"] = (originalValue, adjustedValue)!;
                                }
                            }

                            if (existingRr.OriginalSeriesNumber?.TrimStart().TrimEnd() != worksheet4.Cells[row, 21].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingRr.OriginalSeriesNumber?.TrimStart().TrimEnd();
                                var adjustedValue = worksheet4.Cells[row, 21].Text.TrimStart().TrimEnd();
                                var find  = existingRrInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["OriginalSeriesNumber"] = (originalValue, adjustedValue)!;
                                }
                            }

                            if (existingRr.OriginalDocumentId.ToString().TrimStart().TrimEnd() != (worksheet4.Cells[row, 22].Text.TrimStart().TrimEnd() == "" ? 0.ToString() : worksheet4.Cells[row, 22].Text.TrimStart().TrimEnd()))
                            {
                                var originalValue = existingRr.OriginalDocumentId.ToString().TrimStart().TrimEnd();
                                var adjustedValue = worksheet4.Cells[row, 22].Text.TrimStart().TrimEnd() == ""
                                    ? 0.ToString()
                                    : worksheet4.Cells[row, 22].Text.TrimStart().TrimEnd();
                                var find  = existingRrInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    rrChanges["OriginalDocumentId"] = (originalValue, adjustedValue);
                                }
                            }

                            if (rrChanges.Any())
                            {
                                await _receivingReportRepo.LogChangesAsync(existingRr.OriginalDocumentId, rrChanges, _userManager.GetUserName(this.User), existingRr.ReceivingReportNo);
                            }

                            continue;
                        }
                        else
                        {
                            #region --Audit Trail Recording

                            if (!receivingReport.CreatedBy.IsNullOrEmpty())
                            {
                                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                                AuditTrail auditTrailBook = new(receivingReport.CreatedBy, $"Create new receiving report# {receivingReport.ReceivingReportNo}", "Receiving Report", ipAddress!, receivingReport.CreatedDate);
                                await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                            }
                            if (!receivingReport.PostedBy.IsNullOrEmpty())
                            {
                                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                                AuditTrail auditTrailBook = new(receivingReport.PostedBy, $"Posted receiving report# {receivingReport.ReceivingReportNo}", "Receiving Report", ipAddress!, receivingReport.PostedDate);
                                await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                            }

                            #endregion --Audit Trail Recording
                        }

                        var getPo = await _dbContext
                            .PurchaseOrders
                            .Where(po => po.OriginalDocumentId == receivingReport.OriginalPOId)
                            .FirstOrDefaultAsync(cancellationToken);

                        receivingReport.POId = getPo!.PurchaseOrderId;
                        receivingReport.PONo = getPo.PurchaseOrderNo;

                        await _dbContext.ReceivingReports.AddAsync(receivingReport, cancellationToken);
                    }

                    await _dbContext.SaveChangesAsync(cancellationToken);

                    #endregion -- Receiving Report Import --

                    #region -- Check Voucher Header Import --

                    var cvhRowCount = worksheet5.Dimension.Rows;
                    var cvDictionary = new Dictionary<string, bool>();
                    var checkVoucherHeadersList = await _dbContext
                        .CheckVoucherHeaders
                        .ToListAsync(cancellationToken);

                    for (int row = 2; row <= cvhRowCount; row++) // Assuming the first row is the header
                    {
                        var checkVoucherHeader = new CheckVoucherHeader
                        {
                            CheckVoucherHeaderNo = await _checkVoucherRepo.GenerateCVNo(cancellationToken),
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
                            IsComplete = bool.TryParse(worksheet5.Cells[row, 16].Text, out bool isComplete) && isComplete,
                            AccruedType = worksheet5.Cells[row, 17].Text,
                            Reference = worksheet5.Cells[row, 18].Text,
                            CreatedBy = worksheet5.Cells[row, 19].Text,
                            CreatedDate = DateTime.TryParse(worksheet5.Cells[row, 20].Text, out DateTime createdDate)
                                ? createdDate
                                : default,
                            PostedBy = worksheet5.Cells[row, 32].Text,
                            PostedDate = DateTime.TryParse(worksheet5.Cells[row, 33].Text, out DateTime postedDate)
                                ? postedDate
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
                            IsPaid = bool.TryParse(worksheet5.Cells[row, 26].Text, out bool isPaid) && isPaid,
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

                        if (!cvDictionary.TryAdd(checkVoucherHeader.OriginalSeriesNumber, true) || checkVoucherHeader.OriginalSeriesNumber.Contains("CVNU") || checkVoucherHeader.OriginalSeriesNumber.Contains("INVU") || checkVoucherHeader.OriginalSeriesNumber.Contains("CVU"))
                        {
                            continue;
                        }

                        if (checkVoucherHeadersList.Any(cm => cm.OriginalDocumentId == checkVoucherHeader.OriginalDocumentId))
                        {
                            var cvChanges = new Dictionary<string, (string OriginalValue, string NewValue)>();
                            var existingCv = await _dbContext.CheckVoucherHeaders.FirstOrDefaultAsync(si => si.OriginalDocumentId == checkVoucherHeader.OriginalDocumentId, cancellationToken);
                            var existingCvInLogs = await _dbContext.ImportExportLogs
                                .Where(x => x.DocumentNo == existingCv.CheckVoucherHeaderNo)
                                .ToListAsync(cancellationToken);

                            if (existingCv!.Date.ToString("yyyy-MM-dd").TrimStart().TrimEnd() != worksheet5.Cells[row, 1].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingCv.Date.ToString("yyyy-MM-dd").TrimStart().TrimEnd();
                                var adjustedValue = worksheet5.Cells[row, 1].Text.TrimStart().TrimEnd();
                                var find  = existingCvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cvChanges["Date"] = (originalValue, adjustedValue);
                                }
                            }

                            var rrNo = existingCv.RRNo != null
                                ? string.Join(", ", existingCv.RRNo.Select(si => si.ToString()))
                                : null;
                            if (rrNo != null && rrNo.TrimStart().TrimEnd() != worksheet5.Cells[row, 2].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = string.Join(", ", existingCv.RRNo!.Select(si => si.ToString().TrimStart().TrimEnd()));
                                var adjustedValue = worksheet5.Cells[row, 2].Text.TrimStart().TrimEnd();
                                var find  = existingCvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cvChanges["RRNo"] = (originalValue, adjustedValue);
                                }
                            }

                            var siNo = existingCv.SINo != null
                                ? string.Join(", ", existingCv.SINo.Select(si => si.ToString()))
                                : null;
                            if (siNo != null && siNo.TrimStart().TrimEnd() != worksheet5.Cells[row, 3].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = string.Join(", ", existingCv.SINo!.Select(si => si.ToString().TrimStart().TrimEnd()));
                                var adjustedValue = worksheet5.Cells[row, 3].Text;
                                var find  = existingCvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cvChanges["SINo"] = (originalValue, adjustedValue);
                                }
                            }

                            var poNo = existingCv.PONo != null
                                ? string.Join(", ", existingCv.PONo.Select(si => si.ToString()))
                                : null;
                            if (poNo != null && poNo.TrimStart().TrimEnd() != worksheet5.Cells[row, 4].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = string.Join(", ", existingCv.PONo!.Select(si => si.ToString().TrimStart().TrimEnd()));
                                var adjustedValue = worksheet5.Cells[row, 4].Text.TrimStart().TrimEnd();
                                var find  = existingCvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cvChanges["PONo"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingCv.Particulars!.TrimStart().TrimEnd() != worksheet5.Cells[row, 5].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingCv.Particulars.TrimStart().TrimEnd();
                                var adjustedValue = worksheet5.Cells[row, 5].Text.TrimStart().TrimEnd();
                                var find  = existingCvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cvChanges["Particulars"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingCv.CheckNo!.TrimStart().TrimEnd() != worksheet5.Cells[row, 6].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingCv.CheckNo.TrimStart().TrimEnd();
                                var adjustedValue = worksheet5.Cells[row, 6].Text.TrimStart().TrimEnd();
                                var find  = existingCvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cvChanges["CheckNo"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingCv.Category != worksheet5.Cells[row, 7].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingCv.Category.TrimStart().TrimEnd();
                                var adjustedValue = worksheet5.Cells[row, 7].Text.TrimStart().TrimEnd();
                                var find  = existingCvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cvChanges["Category"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingCv.Payee!.TrimStart().TrimEnd() != worksheet5.Cells[row, 8].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingCv.Payee.TrimStart().TrimEnd();
                                var adjustedValue = worksheet5.Cells[row, 8].Text.TrimStart().TrimEnd();
                                var find  = existingCvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cvChanges["Payee"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingCv.CheckDate?.ToString("yyyy-MM-dd").TrimStart().TrimEnd() != (worksheet5.Cells[row, 9].Text.TrimStart().TrimEnd() == "" ? DateOnly.MinValue.ToString("yyyy-MM-dd").TrimStart().TrimEnd() : worksheet5.Cells[row, 9].Text.TrimStart().TrimEnd()))
                            {
                                var originalValue = existingCv.CheckDate?.ToString("yyyy-MM-dd").TrimStart().TrimEnd();
                                var adjustedValue = worksheet5.Cells[row, 9].Text.TrimStart().TrimEnd() == "" ? DateOnly.MinValue.ToString("yyyy-MM-dd").TrimStart().TrimEnd() : worksheet5.Cells[row, 9].Text.TrimStart().TrimEnd();
                                var find  = existingCvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cvChanges["CheckDate"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingCv.StartDate?.ToString("yyyy-MM-dd").TrimStart().TrimEnd() != (worksheet5.Cells[row, 10].Text.TrimStart().TrimEnd() == "" ? DateOnly.MinValue.ToString("yyyy-MM-dd") : worksheet5.Cells[row, 10].Text).TrimStart().TrimEnd())
                            {
                                var originalValue = existingCv.StartDate?.ToString("yyyy-MM-dd").TrimStart().TrimEnd();
                                var adjustedValue = worksheet5.Cells[row, 10].Text.TrimStart().TrimEnd() == ""
                                    ? DateOnly.MinValue.ToString("yyyy-MM-dd")
                                    : worksheet5.Cells[row, 10].Text.TrimStart().TrimEnd();
                                var find  = existingCvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cvChanges["StartDate"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingCv.EndDate?.ToString("yyyy-MM-dd").TrimStart().TrimEnd() != (worksheet5.Cells[row, 11].Text.TrimStart().TrimEnd() == "" ? DateOnly.MinValue.ToString("yyyy-MM-dd") : worksheet5.Cells[row, 11].Text.TrimStart().TrimEnd()))
                            {
                                var originalValue = existingCv.EndDate?.ToString("yyyy-MM-dd").TrimStart().TrimEnd();
                                var adjustedValue = worksheet5.Cells[row, 11].Text.TrimStart().TrimEnd() == ""
                                    ? DateOnly.MinValue.ToString("yyyy-MM-dd").TrimStart().TrimEnd()
                                    : worksheet5.Cells[row, 11].Text.TrimStart().TrimEnd();
                                var find  = existingCvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cvChanges["EndDate"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingCv.NumberOfMonths.ToString().TrimStart().TrimEnd() != worksheet5.Cells[row, 12].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingCv.NumberOfMonths.ToString().TrimStart().TrimEnd();
                                var adjustedValue = worksheet5.Cells[row, 12].Text.TrimStart().TrimEnd();
                                var find  = existingCvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cvChanges["NumberOfMonths"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingCv.NumberOfMonthsCreated.ToString().TrimStart().TrimEnd() != worksheet5.Cells[row, 13].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingCv.NumberOfMonthsCreated.ToString().TrimStart().TrimEnd();
                                var adjustedValue = worksheet5.Cells[row, 13].Text.TrimStart().TrimEnd();
                                var find  = existingCvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cvChanges["NumberOfMonthsCreated"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingCv.LastCreatedDate?.ToString("yyyy-MM-dd").TrimStart().TrimEnd() != (worksheet5.Cells[row, 14].Text.TrimStart().TrimEnd() == "" ? DateOnly.MinValue.ToString("yyyy-MM-dd").TrimStart().TrimEnd() : worksheet5.Cells[row, 14].Text.TrimStart().TrimEnd()))
                            {
                                var originalValue = existingCv.LastCreatedDate?.ToString("yyyy-MM-dd").TrimStart().TrimEnd();
                                var adjustedValue = worksheet5.Cells[row, 14].Text.TrimStart().TrimEnd() == ""
                                    ? DateOnly.MinValue.ToString("yyyy-MM-dd").TrimStart().TrimEnd()
                                    : worksheet5.Cells[row, 14].Text.TrimStart().TrimEnd();
                                var find  = existingCvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cvChanges["LastCreatedDate"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingCv.AmountPerMonth.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet5.Cells[row, 15].Text).ToString("F2").TrimStart().TrimEnd())
                            {
                                var originalValue = existingCv.AmountPerMonth.ToString("F2").TrimStart().TrimEnd();
                                var adjustedValue = decimal.Parse(worksheet5.Cells[row, 15].Text).ToString("F2").TrimStart().TrimEnd();
                                var find  = existingCvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cvChanges["AmountPerMonth"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingCv.IsComplete.ToString().ToUpper().TrimStart().TrimEnd() != worksheet5.Cells[row, 16].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingCv.IsComplete.ToString().ToUpper().TrimStart().TrimEnd();
                                var adjustedValue = worksheet5.Cells[row, 16].Text.TrimStart().TrimEnd();
                                var find  = existingCvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cvChanges["IsComplete"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingCv.AccruedType!.TrimStart().TrimEnd() != worksheet5.Cells[row, 17].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingCv.AccruedType.TrimStart().TrimEnd();
                                var adjustedValue = worksheet5.Cells[row, 17].Text.TrimStart().TrimEnd();
                                var find  = existingCvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cvChanges["AccruedType"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingCv.CvType!.TrimStart().TrimEnd() == "Payment")
                            {
                                var getCvInvoicing = await _dbContext.CheckVoucherHeaders.FirstOrDefaultAsync(cvh => existingCv.Reference == cvh.CheckVoucherHeaderNo, cancellationToken);
                                if (getCvInvoicing != null && getCvInvoicing.OriginalSeriesNumber!.TrimStart().TrimEnd() != worksheet5.Cells[row, 18].Text.TrimStart().TrimEnd())
                                {
                                    var originalValue = getCvInvoicing.OriginalSeriesNumber.TrimStart().TrimEnd();
                                    var adjustedValue = worksheet5.Cells[row, 18].Text.TrimStart().TrimEnd();
                                    var find  = existingCvInLogs
                                        .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                    if (!find.Any())
                                    {
                                        cvChanges["Reference"] = (originalValue, adjustedValue);
                                    }
                                }
                            }

                            if (existingCv.CreatedBy!.TrimStart().TrimEnd() != worksheet5.Cells[row, 19].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingCv.CreatedBy.TrimStart().TrimEnd();
                                var adjustedValue = worksheet5.Cells[row, 19].Text.TrimStart().TrimEnd();
                                var find  = existingCvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cvChanges["CreatedBy"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingCv.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff").TrimStart().TrimEnd() != worksheet5.Cells[row, 20].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingCv.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff").TrimStart().TrimEnd();
                                var adjustedValue = worksheet5.Cells[row, 20].Text.TrimStart().TrimEnd();
                                var find  = existingCvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cvChanges["CreatedDate"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingCv.Total.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet5.Cells[row, 21].Text).ToString("F2").TrimStart().TrimEnd())
                            {
                                var originalValue = existingCv.Total.ToString("F2").TrimStart().TrimEnd();
                                var adjustedValue = decimal.Parse(worksheet5.Cells[row, 21].Text).ToString("F2").TrimStart().TrimEnd();
                                var find  = existingCvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cvChanges["Total"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingCv.Category.TrimStart().TrimEnd() == "Trade")
                            {
                                var cellText = worksheet5.Cells[row, 22].Text.TrimStart().TrimEnd();
                                if (decimal.TryParse(cellText, out var parsedAmount))
                                {
                                    var amount = existingCv.Amount != null
                                        ? string.Join(" ", existingCv.Amount.Select(si => si.ToString("F2")))
                                        : null;

                                    if (amount != null && amount != parsedAmount.ToString("F2"))
                                    {
                                        var originalValue = amount;
                                        var adjustedValue = parsedAmount.ToString("F2");
                                        var find  = existingCvInLogs
                                            .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                        if (!find.Any())
                                        {
                                            cvChanges["Amount"] = (originalValue, adjustedValue);
                                        }
                                    }
                                }
                            }

                            if (existingCv.CheckAmount.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet5.Cells[row, 23].Text).ToString("F2").TrimStart().TrimEnd())
                            {
                                var originalValue = existingCv.CheckAmount.ToString("F2").TrimStart().TrimEnd();
                                var adjustedValue = decimal.Parse(worksheet5.Cells[row, 23].Text).ToString("F2").TrimStart().TrimEnd();
                                var find  = existingCvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cvChanges["CheckAmount"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingCv.CvType.TrimStart().TrimEnd() != worksheet5.Cells[row, 24].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingCv.CvType.TrimStart().TrimEnd();
                                var adjustedValue = worksheet5.Cells[row, 24].Text.TrimStart().TrimEnd();
                                var find  = existingCvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cvChanges["CvType"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingCv.AmountPaid.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet5.Cells[row, 25].Text).ToString("F2").TrimStart().TrimEnd())
                            {
                                var originalValue = existingCv.AmountPaid.ToString("F2").TrimStart().TrimEnd();
                                var adjustedValue = decimal.Parse(worksheet5.Cells[row, 25].Text).ToString("F2").TrimStart().TrimEnd();
                                var find  = existingCvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cvChanges["AmountPaid"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingCv.IsPaid.ToString().ToUpper().TrimStart().TrimEnd() != worksheet5.Cells[row, 26].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingCv.IsPaid.ToString().ToUpper().TrimStart().TrimEnd();
                                var adjustedValue = worksheet5.Cells[row, 26].Text.TrimStart().TrimEnd();
                                var find  = existingCvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cvChanges["IsPaid"] = (originalValue, adjustedValue);
                                }
                            }

                            if ((string.IsNullOrWhiteSpace(existingCv.CancellationRemarks) ? "" : existingCv.CancellationRemarks.TrimStart().TrimEnd()) != worksheet5.Cells[row, 27].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingCv.CancellationRemarks!.TrimStart().TrimEnd();
                                var adjustedValue = worksheet5.Cells[row, 27].Text.TrimStart().TrimEnd();
                                var find  = existingCvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cvChanges["CancellationRemarks"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingCv.OriginalBankId.ToString()!.TrimStart().TrimEnd() != (worksheet5.Cells[row, 28].Text.TrimStart().TrimEnd() != "" ? worksheet5.Cells[row, 28].Text.TrimStart().TrimEnd() : 0.ToString()))
                            {
                                var originalValue = existingCv.OriginalBankId.ToString()!.TrimStart().TrimEnd();
                                var adjustedValue = worksheet5.Cells[row, 28].Text.TrimStart().TrimEnd() != ""
                                    ? worksheet5.Cells[row, 28].Text.TrimStart().TrimEnd()
                                    : 0.ToString();
                                var find  = existingCvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cvChanges["OriginalBankId"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingCv.OriginalSeriesNumber!.TrimStart().TrimEnd() != worksheet5.Cells[row, 29].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingCv.OriginalSeriesNumber.TrimStart().TrimEnd();
                                var adjustedValue = worksheet5.Cells[row, 29].Text.TrimStart().TrimEnd();
                                var find  = existingCvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cvChanges["OriginalSeriesNumber"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingCv.OriginalSupplierId.ToString()!.TrimStart().TrimEnd() != worksheet5.Cells[row, 30].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingCv.OriginalSupplierId.ToString()!.TrimStart().TrimEnd();
                                var adjustedValue = worksheet5.Cells[row, 30].Text.TrimStart().TrimEnd();
                                var find  = existingCvInLogs
                                    .Where(x => x.OriginalValue == existingCv.OriginalSupplierId.ToString() && x.AdjustedValue == worksheet5.Cells[row, 30].Text);
                                if (!find.Any())
                                {
                                    cvChanges["OriginalSupplierId"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingCv.OriginalDocumentId.ToString().TrimStart().TrimEnd() != worksheet5.Cells[row, 31].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingCv.OriginalDocumentId.ToString().TrimStart().TrimEnd();
                                var adjustedValue = worksheet5.Cells[row, 31].Text.TrimStart().TrimEnd();
                                var find  = existingCvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    cvChanges["OriginalDocumentId"] = (originalValue, adjustedValue);
                                }
                            }

                            if (cvChanges.Any())
                            {
                                await _checkVoucherRepo.LogChangesAsync(existingCv.OriginalDocumentId, cvChanges, _userManager.GetUserName(this.User), existingCv.CheckVoucherHeaderNo);
                            }

                            continue;
                        }
                        else
                        {
                            #region --Audit Trail Recording

                            if (!checkVoucherHeader.CreatedBy.IsNullOrEmpty())
                            {
                                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                                AuditTrail auditTrailBook = new(checkVoucherHeader.CreatedBy, $"Create new check vouchcer# {checkVoucherHeader.CheckVoucherHeaderNo}", $"Check Voucher {(checkVoucherHeader.CvType == "Invoicing" ? "Non Trade Invoice" : checkVoucherHeader.CvType == "Payment" ? "Non Trade Payment" : "Trade")}", ipAddress!, checkVoucherHeader.CreatedDate);
                                await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                            }
                            if (!checkVoucherHeader.PostedBy.IsNullOrEmpty())
                            {
                                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                                AuditTrail auditTrailBook = new(checkVoucherHeader.PostedBy, $"Posted check voucher# {checkVoucherHeader.CheckVoucherHeaderNo}", $"Check Voucher {(checkVoucherHeader.CvType == "Invoicing" ? "Non Trade Invoice" : checkVoucherHeader.CvType == "Payment" ? "Non Trade Payment" : "Trade")}", ipAddress!, checkVoucherHeader.PostedDate);
                                await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                            }

                            #endregion --Audit Trail Recording
                        }

                        if (checkVoucherHeader.CvType == "Payment")
                        {
                            var getCvInvoicing = await _dbContext.CheckVoucherHeaders.FirstOrDefaultAsync(cvh => cvh.OriginalSeriesNumber == checkVoucherHeader.Reference, cancellationToken);
                            checkVoucherHeader.Reference = getCvInvoicing?.CheckVoucherHeaderNo;
                        }

                        checkVoucherHeader.SupplierId = await _dbContext.Suppliers
                                                            .Where(supp =>
                                                                supp.OriginalSupplierId ==
                                                                checkVoucherHeader.OriginalSupplierId)
                                                            .Select(supp => (int?)supp.SupplierId)
                                                            .FirstOrDefaultAsync(cancellationToken) ??
                                                        throw new InvalidOperationException(
                                                            "Please upload the Excel file for the supplier master file first.");

                        if (checkVoucherHeader.CvType != "Invoicing")
                        {
                            checkVoucherHeader.BankId = await _dbContext.BankAccounts
                                                            .Where(bank =>
                                                                bank.OriginalBankId ==
                                                                checkVoucherHeader.OriginalBankId)
                                                            .Select(bank => (int?)bank.BankAccountId)
                                                            .FirstOrDefaultAsync(cancellationToken) ??
                                                        throw new InvalidOperationException(
                                                            "Please upload the Excel file for the bank account master file first.");
                        }

                        await _dbContext.CheckVoucherHeaders.AddAsync(checkVoucherHeader, cancellationToken);
                        await _dbContext.SaveChangesAsync(cancellationToken);
                    }

                    #endregion -- Check Voucher Header Import --

                    #region -- Check Voucher Details Import --

                    var cvdRowCount = worksheet6.Dimension.Rows;
                    var checkVoucherDetailList = await _dbContext
                        .CheckVoucherDetails
                        .ToListAsync(cancellationToken);

                    for (var cvdRow = 2; cvdRow <= cvdRowCount; cvdRow++)
                    {
                        var cvRow = cvdRow;

                        var checkVoucherDetails = new CheckVoucherDetail
                        {
                            AccountNo = worksheet6.Cells[cvdRow, 1].Text,
                            AccountName = worksheet6.Cells[cvdRow, 2].Text,
                            Debit = decimal.TryParse(worksheet6.Cells[cvdRow, 4].Text, out decimal debit) ? debit : 0,
                            Credit = decimal.TryParse(worksheet6.Cells[cvdRow, 5].Text, out decimal credit) ? credit : 0,
                            OriginalDocumentId = int.TryParse(worksheet6.Cells[cvdRow, 7].Text, out int originalDocumentId) ? originalDocumentId : 0,
                            Amount = decimal.TryParse(worksheet6.Cells[cvdRow, 8].Text, out decimal amount) ? amount : 0,
                            AmountPaid = decimal.TryParse(worksheet6.Cells[cvdRow, 9].Text, out decimal amountPaid) ? amountPaid : 0,
                            EwtPercent = decimal.TryParse(worksheet6.Cells[cvdRow, 11].Text, out decimal ewtPercent) ? ewtPercent : 0,
                            IsUserSelected = bool.TryParse(worksheet6.Cells[cvdRow, 12].Text, out bool isUserSelected) && isUserSelected,
                            IsVatable = bool.TryParse(worksheet6.Cells[cvdRow, 13].Text, out bool isVatable) && isVatable
                        };

                        var cvHeader = await _dbContext.CheckVoucherHeaders
                            .Where(cvh => cvh.OriginalDocumentId.ToString() == worksheet6.Cells[cvRow, 6].Text.TrimStart().TrimEnd())
                            .FirstOrDefaultAsync(cancellationToken);

                        if (cvHeader != null)
                        {
                            var getSupplier = await _dbContext.Suppliers
                                .Where(cvh => cvh.OriginalSupplierId.ToString() == worksheet6.Cells[cvRow, 10].Text.TrimStart().TrimEnd())
                                .FirstOrDefaultAsync(cancellationToken);

                            checkVoucherDetails.SupplierId = getSupplier?.SupplierId ?? null;
                            checkVoucherDetails.CheckVoucherHeaderId = cvHeader.CheckVoucherHeaderId;
                            checkVoucherDetails.TransactionNo = cvHeader.CheckVoucherHeaderNo!;
                        }

                        if (!checkVoucherDetailList.Any(cm => cm.OriginalDocumentId == checkVoucherDetails.OriginalDocumentId) && !worksheet6.Cells[cvdRow, 3].Text.TrimStart().TrimEnd().Contains("CVNU") && !worksheet6.Cells[cvdRow, 3].Text.TrimStart().TrimEnd().Contains("INVU") && !worksheet6.Cells[cvdRow, 3].Text.TrimStart().TrimEnd().Contains("CVU"))
                        {
                            await _dbContext.CheckVoucherDetails.AddAsync(checkVoucherDetails, cancellationToken);
                        }

                        if (checkVoucherDetailList.Any(cm => cm.OriginalDocumentId == checkVoucherDetails.OriginalDocumentId) && !worksheet6.Cells[cvdRow, 3].Text.TrimStart().TrimEnd().Contains("CVNU") && !worksheet6.Cells[cvdRow, 3].Text.TrimStart().TrimEnd().Contains("INVU") && !worksheet6.Cells[cvdRow, 3].Text.TrimStart().TrimEnd().Contains("CVU"))
                        {
                            var cvdChanges = new Dictionary<string, (string OriginalValue, string NewValue)>();
                            var existingCvd = await _dbContext.CheckVoucherDetails
                                .Include(cvd => cvd.CheckVoucherHeader)
                                .FirstOrDefaultAsync(cvd => cvd.OriginalDocumentId == checkVoucherDetails.OriginalDocumentId, cancellationToken);
                            var existingCvdInLogs = await _dbContext.ImportExportLogs
                                .Where(x => x.DocumentRecordId == existingCvd.CheckVoucherDetailId)
                                .ToListAsync(cancellationToken);

                            if (existingCvd != null)
                            {
                                if (existingCvd.AccountNo.TrimStart().TrimEnd() != worksheet6.Cells[cvdRow, 1].Text.TrimStart().TrimEnd())
                                {
                                    var originalValue = existingCvd.AccountNo.TrimStart().TrimEnd();
                                    var adjustedValue = worksheet6.Cells[cvdRow, 1].Text.TrimStart().TrimEnd();
                                    var find  = existingCvdInLogs
                                        .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                    if (!find.Any())
                                    {
                                        cvdChanges["AccountNo"] = (originalValue, adjustedValue);
                                    }
                                }

                                if (existingCvd.AccountName.TrimStart().TrimEnd() != worksheet6.Cells[cvdRow, 2].Text.TrimStart().TrimEnd())
                                {
                                    var originalValue = existingCvd.AccountName.TrimStart().TrimEnd();
                                    var adjustedValue = worksheet6.Cells[cvdRow, 2].Text.TrimStart().TrimEnd();
                                    var find  = existingCvdInLogs
                                        .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                    if (!find.Any())
                                    {
                                        cvdChanges["AccountName"] = (originalValue, adjustedValue);
                                    }
                                }

                                if (existingCvd.Debit.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet6.Cells[cvdRow, 4].Text).ToString("F2").TrimStart().TrimEnd())
                                {
                                    var originalValue = existingCvd.Debit.ToString("F2").TrimStart().TrimEnd();
                                    var adjustedValue = decimal.Parse(worksheet6.Cells[cvdRow, 4].Text).ToString("F2").TrimStart().TrimEnd();
                                    var find  = existingCvdInLogs
                                        .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                    if (!find.Any())
                                    {
                                        cvdChanges["Debit"] = (originalValue, adjustedValue);
                                    }
                                }

                                if (existingCvd.Credit.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet6.Cells[cvdRow, 5].Text).ToString("F2").TrimStart().TrimEnd())
                                {
                                    var originalValue = existingCvd.Credit.ToString("F2").TrimStart().TrimEnd();
                                    var adjustedValue = decimal.Parse(worksheet6.Cells[cvdRow, 5].Text).ToString("F2").TrimStart().TrimEnd();
                                    var find  = existingCvdInLogs
                                        .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                    if (!find.Any())
                                    {
                                        cvdChanges["Credit"] = (originalValue, adjustedValue);
                                    }
                                }

                                if (existingCvd.CheckVoucherHeader?.OriginalDocumentId.ToString().TrimStart().TrimEnd() != decimal.Parse(worksheet6.Cells[cvdRow, 6].Text).ToString("F0").TrimStart().TrimEnd())
                                {
                                    var originalValue = existingCvd.CheckVoucherHeader?.OriginalDocumentId.ToString().TrimStart().TrimEnd();
                                    var adjustedValue = decimal.Parse(worksheet6.Cells[cvdRow, 6].Text).ToString("F0").TrimStart().TrimEnd();
                                    var find  = existingCvdInLogs
                                        .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                    if (!find.Any())
                                    {
                                        cvdChanges["CVHeaderId"] = (originalValue, adjustedValue)!;
                                    }
                                }

                                if (cvdChanges.Any())
                                {
                                    await _checkVoucherRepo.LogChangesForCVDAsync(existingCvd.OriginalDocumentId, cvdChanges, _userManager.GetUserName(this.User), existingCvd.TransactionNo);
                                }
                            }
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
                            JournalVoucherHeaderNo = worksheet.Cells[row, 10].Text,
                            Date = DateOnly.TryParse(worksheet.Cells[row, 1].Text, out DateOnly date) ? date : default,
                            References = worksheet.Cells[row, 2].Text,
                            Particulars = worksheet.Cells[row, 3].Text,
                            CRNo = worksheet.Cells[row, 4].Text,
                            JVReason = worksheet.Cells[row, 5].Text,
                            CreatedBy = worksheet.Cells[row, 6].Text,
                            CreatedDate = DateTime.TryParse(worksheet.Cells[row, 7].Text, out DateTime createdDate) ? createdDate : default,
                            PostedBy = worksheet.Cells[row, 12].Text,
                            PostedDate = DateTime.TryParse(worksheet.Cells[row, 13].Text, out DateTime postedDate) ? postedDate : default,
                            CancellationRemarks = worksheet.Cells[row, 8].Text,
                            OriginalCVId = int.TryParse(worksheet.Cells[row, 9].Text, out int originalCvId) ? originalCvId : 0,
                            OriginalSeriesNumber = worksheet.Cells[row, 10].Text,
                            OriginalDocumentId = int.TryParse(worksheet.Cells[row, 11].Text, out int originalDocumentId) ? originalDocumentId : 0,
                        };

                        var jvhList = journalVoucherHeaderList.Any(cv => cv.OriginalDocumentId == journalVoucherHeader.OriginalDocumentId);
                        var existingJournalVoucher = await _dbContext.JournalVoucherHeaders.FirstOrDefaultAsync(si => si.OriginalDocumentId == journalVoucherHeader.OriginalDocumentId, cancellationToken);
                        jvDictionary.TryAdd((existingJournalVoucher != null ? existingJournalVoucher.JournalVoucherHeaderNo : journalVoucherHeader.JournalVoucherHeaderNo) ?? journalVoucherHeader.JournalVoucherHeaderNo, jvhList);

                        if (jvhList)
                        {
                            var existingJvInLogs = await _dbContext.ImportExportLogs
                                .Where(x => x.DocumentNo == existingJournalVoucher.JournalVoucherHeaderNo)
                                .ToListAsync(cancellationToken);
                            var jvChanges = new Dictionary<string, (string OriginalValue, string NewValue)>();

                            if (existingJournalVoucher!.JournalVoucherHeaderNo!.TrimStart().TrimEnd() != worksheet.Cells[row, 10].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingJournalVoucher.JournalVoucherHeaderNo.TrimStart().TrimEnd();
                                var adjustedValue = worksheet.Cells[row, 10].Text.TrimStart().TrimEnd();
                                var find  = existingJvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    jvChanges["JVNo"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingJournalVoucher.Date.ToString("yyyy-MM-dd").TrimStart().TrimEnd() != worksheet.Cells[row, 1].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingJournalVoucher.Date.ToString("yyyy-MM-dd").TrimStart().TrimEnd();
                                var adjustedValue = worksheet.Cells[row, 1].Text.TrimStart().TrimEnd();
                                var find  = existingJvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    jvChanges["Date"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingJournalVoucher.References!.TrimStart().TrimEnd() != worksheet.Cells[row, 2].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingJournalVoucher.References.TrimStart().TrimEnd();
                                var adjustedValue = worksheet.Cells[row, 2].Text.TrimStart().TrimEnd();
                                var find  = existingJvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    jvChanges["References"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingJournalVoucher.Particulars.TrimStart().TrimEnd() != worksheet.Cells[row, 3].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingJournalVoucher.Particulars.TrimStart().TrimEnd();
                                var adjustedValue = worksheet.Cells[row, 3].Text.TrimStart().TrimEnd();
                                var find  = existingJvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    jvChanges["Particulars"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingJournalVoucher.CRNo!.TrimStart().TrimEnd() != worksheet.Cells[row, 4].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingJournalVoucher.CRNo.TrimStart().TrimEnd();
                                var adjustedValue = worksheet.Cells[row, 4].Text.TrimStart().TrimEnd();
                                var find  = existingJvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    jvChanges["CRNo"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingJournalVoucher.JVReason.TrimStart().TrimEnd() != worksheet.Cells[row, 5].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingJournalVoucher.JVReason.TrimStart().TrimEnd();
                                var adjustedValue = worksheet.Cells[row, 5].Text.TrimStart().TrimEnd();
                                var find  = existingJvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    jvChanges["JVReason"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingJournalVoucher.CreatedBy!.TrimStart().TrimEnd() != worksheet.Cells[row, 6].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingJournalVoucher.CreatedBy.TrimStart().TrimEnd();
                                var adjustedValue = worksheet.Cells[row, 6].Text.TrimStart().TrimEnd();
                                var find  = existingJvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    jvChanges["CreatedBy"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingJournalVoucher.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff").TrimStart().TrimEnd() != worksheet.Cells[row, 7].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingJournalVoucher.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff").TrimStart().TrimEnd();
                                var adjustedValue = worksheet.Cells[row, 7].Text.TrimStart().TrimEnd();
                                var find  = existingJvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    jvChanges["CreatedDate"] = (originalValue, adjustedValue);
                                }
                            }

                            if ((string.IsNullOrWhiteSpace(existingJournalVoucher.CancellationRemarks?.TrimStart().TrimEnd()) ? "" : existingJournalVoucher.CancellationRemarks.TrimStart().TrimEnd()) != worksheet.Cells[row, 8].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingJournalVoucher.CancellationRemarks?.TrimStart().TrimEnd();
                                var adjustedValue = worksheet.Cells[row, 8].Text.TrimStart().TrimEnd();
                                var find  = existingJvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    jvChanges["CancellationRemarks"] = (originalValue, adjustedValue)!;
                                }
                            }

                            if (existingJournalVoucher.OriginalCVId.ToString()!.TrimStart().TrimEnd() != worksheet.Cells[row, 9].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingJournalVoucher.OriginalCVId.ToString()!.TrimStart().TrimEnd();
                                var adjustedValue = worksheet.Cells[row, 9].Text.TrimStart().TrimEnd();
                                var find  = existingJvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    jvChanges["OriginalCVId"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingJournalVoucher.OriginalSeriesNumber!.TrimStart().TrimEnd() != worksheet.Cells[row, 10].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingJournalVoucher.OriginalSeriesNumber.TrimStart().TrimEnd();
                                var adjustedValue = worksheet.Cells[row, 10].Text.TrimStart().TrimEnd();
                                var find  = existingJvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    jvChanges["OriginalSeriesNumber"] = (originalValue, adjustedValue);
                                }
                            }

                            if (existingJournalVoucher.OriginalDocumentId.ToString().TrimStart().TrimEnd() != worksheet.Cells[row, 11].Text.TrimStart().TrimEnd())
                            {
                                var originalValue = existingJournalVoucher.OriginalDocumentId.ToString().TrimStart().TrimEnd();
                                var adjustedValue = worksheet.Cells[row, 11].Text.TrimStart().TrimEnd();
                                var find  = existingJvInLogs
                                    .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                if (!find.Any())
                                {
                                    jvChanges["OriginalDocumentId"] = (originalValue, adjustedValue);
                                }
                            }

                            if (jvChanges.Any())
                            {
                                await _journalVoucherRepo.LogChangesAsync(existingJournalVoucher.OriginalDocumentId, jvChanges, _userManager.GetUserName(this.User), existingJournalVoucher.JournalVoucherHeaderNo);
                            }

                            continue;
                        }
                        else
                        {
                            #region --Audit Trail Recording

                            if (!journalVoucherHeader.CreatedBy.IsNullOrEmpty())
                            {
                                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                                AuditTrail auditTrailBook = new(journalVoucherHeader.CreatedBy, $"Create new journal vouchcer# {journalVoucherHeader.JournalVoucherHeaderNo}", "Journal Voucher", ipAddress!, journalVoucherHeader.CreatedDate);
                                await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                            }
                            if (!journalVoucherHeader.PostedBy.IsNullOrEmpty())
                            {
                                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                                AuditTrail auditTrailBook = new(journalVoucherHeader.PostedBy, $"Posted journal voucher# {journalVoucherHeader.JournalVoucherHeaderNo}", "journal Voucher", ipAddress!, journalVoucherHeader.PostedDate);
                                await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                            }

                            #endregion --Audit Trail Recording
                        }

                        journalVoucherHeader.CVId = await _dbContext.CheckVoucherHeaders
                            .Where(c => c.OriginalDocumentId == journalVoucherHeader.OriginalCVId)
                            .Select(c => (int?)c.CheckVoucherHeaderId)
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
                            var jvRow = jvdRow;
                            var journalVoucherDetails = new JournalVoucherDetail
                            {
                                AccountNo = worksheet2.Cells[jvdRow, 1].Text,
                                AccountName = worksheet2.Cells[jvdRow, 2].Text,
                                Debit = decimal.TryParse(worksheet2.Cells[jvdRow, 4].Text, out decimal debit) ? debit : 0,
                                Credit = decimal.TryParse(worksheet2.Cells[jvdRow, 5].Text, out decimal credit) ? credit : 0,
                                OriginalDocumentId = int.TryParse(worksheet2.Cells[jvdRow, 7].Text, out int originalDocumentId) ? originalDocumentId : 0
                            };

                            var jvHeader = await _dbContext.JournalVoucherHeaders
                                .Where(jvh => jvh.OriginalSeriesNumber == worksheet2.Cells[jvRow, 3].Text)
                                .FirstOrDefaultAsync(cancellationToken);

                            if (jvHeader != null)
                            {
                                journalVoucherDetails.JournalVoucherHeaderId = jvHeader.JournalVoucherHeaderId;
                                journalVoucherDetails.TransactionNo = jvHeader.JournalVoucherHeaderNo!;
                            }

                            if (jvDictionary.TryGetValue(journalVoucherDetails.TransactionNo, out var value) && !value)
                            {
                                await _dbContext.JournalVoucherDetails.AddAsync(journalVoucherDetails, cancellationToken);
                            }

                            if (jvDictionary.TryGetValue(journalVoucherDetails.TransactionNo, out var boolean) && boolean)
                            {
                                var jvdChanges = new Dictionary<string, (string OriginalValue, string NewValue)>();
                                var existingJournalVoucherDetails = await _dbContext.JournalVoucherDetails
                                    .Include(cvd => cvd.JournalVoucherHeader)
                                    .FirstOrDefaultAsync(cvd => cvd.OriginalDocumentId == journalVoucherDetails.OriginalDocumentId, cancellationToken);
                                var existingJvdInLogs = await _dbContext.ImportExportLogs
                                    .Where(x => x.DocumentRecordId == existingJournalVoucherDetails.JournalVoucherDetailId)
                                    .ToListAsync(cancellationToken);

                                if (existingJournalVoucherDetails != null)
                                {
                                    if (existingJournalVoucherDetails.AccountNo.TrimStart().TrimEnd() != worksheet2.Cells[jvdRow, 1].Text.TrimStart().TrimEnd())
                                    {
                                        var originalValue = existingJournalVoucherDetails.AccountNo.TrimStart().TrimEnd();
                                        var adjustedValue = worksheet2.Cells[jvdRow, 1].Text.TrimStart().TrimEnd();
                                        var find  = existingJvdInLogs
                                            .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                        if (!find.Any())
                                        {
                                            jvdChanges["AccountNo"] = (originalValue, adjustedValue);
                                        }
                                    }

                                    if (existingJournalVoucherDetails.AccountName.TrimStart().TrimEnd() != worksheet2.Cells[jvdRow, 2].Text.TrimStart().TrimEnd())
                                    {
                                        var originalValue = existingJournalVoucherDetails.AccountName.TrimStart().TrimEnd();
                                        var adjustedValue = worksheet2.Cells[jvdRow, 2].Text.TrimStart().TrimEnd();
                                        var find  = existingJvdInLogs
                                            .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                        if (!find.Any())
                                        {
                                            jvdChanges["AccountName"] = (originalValue, adjustedValue);
                                        }
                                    }

                                    if (existingJournalVoucherDetails.JournalVoucherHeader.OriginalSeriesNumber!.TrimStart().TrimEnd() != worksheet2.Cells[jvdRow, 3].Text.TrimStart().TrimEnd())
                                    {
                                        var originalValue = existingJournalVoucherDetails.JournalVoucherHeader.OriginalSeriesNumber.TrimStart().TrimEnd();
                                        var adjustedValue = worksheet2.Cells[jvdRow, 3].Text.TrimStart().TrimEnd();
                                        var find  = existingJvdInLogs
                                            .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                        if (!find.Any())
                                        {
                                            jvdChanges["TransactionNo"] = (originalValue, adjustedValue);
                                        }
                                    }

                                    if (existingJournalVoucherDetails.Debit.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet2.Cells[jvdRow, 4].Text).ToString("F2").TrimStart().TrimEnd())
                                    {
                                        var originalValue = existingJournalVoucherDetails.Debit.ToString("F2").TrimStart().TrimEnd();
                                        var adjustedValue = decimal.Parse(worksheet2.Cells[jvdRow, 4].Text).ToString("F2").TrimStart().TrimEnd();
                                        var find  = existingJvdInLogs
                                            .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                        if (!find.Any())
                                        {
                                            jvdChanges["Debit"] = (originalValue, adjustedValue);
                                        }
                                    }

                                    if (existingJournalVoucherDetails.Credit.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet2.Cells[jvdRow, 5].Text).ToString("F2").TrimStart().TrimEnd())
                                    {
                                        var originalValue = existingJournalVoucherDetails.Credit.ToString("F2").TrimStart().TrimEnd();
                                        var adjustedValue = decimal.Parse(worksheet2.Cells[jvdRow, 5].Text).ToString("F2").TrimStart().TrimEnd();
                                        var find  = existingJvdInLogs
                                            .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                        if (!find.Any())
                                        {
                                            jvdChanges["Credit"] = (originalValue, adjustedValue);
                                        }
                                    }

                                    if (existingJournalVoucherDetails.JournalVoucherHeader.OriginalDocumentId.ToString("F0").TrimStart().TrimEnd() != decimal.Parse(worksheet2.Cells[jvdRow, 6].Text).ToString("F0").TrimStart().TrimEnd())
                                    {
                                        var originalValue = existingJournalVoucherDetails.JournalVoucherHeader.OriginalDocumentId.ToString("F0").TrimStart().TrimEnd();
                                        var adjustedValue = decimal.Parse(worksheet2.Cells[jvdRow, 6].Text).ToString("F0").TrimStart().TrimEnd();
                                        var find  = existingJvdInLogs
                                            .Where(x => x.OriginalValue == originalValue && x.AdjustedValue == adjustedValue);
                                        if (!find.Any())
                                        {
                                            jvdChanges["JVHeaderId"] = (originalValue, adjustedValue);
                                        }
                                    }

                                    if (jvdChanges.Any())
                                    {
                                        await _journalVoucherRepo.LogChangesForJVDAsync(existingJournalVoucherDetails.OriginalDocumentId, jvdChanges, _userManager.GetUserName(this.User), existingJournalVoucherDetails.TransactionNo);
                                    }
                                }
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
