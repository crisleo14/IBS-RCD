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

        private readonly AasDbContext _aasDbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly JournalVoucherRepo _journalVoucherRepo;

        private readonly CheckVoucherRepo _checkVoucherRepo;

        private readonly ReceivingReportRepo _receivingReportRepo;

        private readonly PurchaseOrderRepo _purchaseOrderRepo;

        private readonly GeneralRepo _generalRepo;

        public JournalVoucherController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, JournalVoucherRepo journalVoucherRepo, GeneralRepo generalRepo, CheckVoucherRepo checkVoucherRepo, ReceivingReportRepo receivingReportRepo, PurchaseOrderRepo purchaseOrderRepo, AasDbContext aasDbContext)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _journalVoucherRepo = journalVoucherRepo;
            _generalRepo = generalRepo;
            _checkVoucherRepo = checkVoucherRepo;
            _receivingReportRepo = receivingReportRepo;
            _purchaseOrderRepo = purchaseOrderRepo;
            _aasDbContext = aasDbContext;
        }

        public IActionResult Index(string? view)
        {
            if (view == nameof(DynamicView.JournalVoucher))
            {
                return View("ImportExportIndex");
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
                var createdBy = await _generalRepo.GetUserFullNameAsync(User.Identity!.Name!);
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
                    model.Header.CreatedBy = createdBy;

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
                        AuditTrail auditTrailBook = new(createdBy, $"Create new journal voucher# {model.Header.JournalVoucherHeaderNo}", "Journal Voucher", ipAddress!);
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
            var createdBy = await _generalRepo.GetUserFullNameAsync(User.Identity!.Name!);
            if (jv != null && !jv.IsPrinted)
            {

                #region --Audit Trail Recording

                if (jv.OriginalSeriesNumber.IsNullOrEmpty() && jv.OriginalDocumentId == 0)
                {
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    AuditTrail auditTrailBook = new(createdBy, $"Printed original copy of jv# {jv.JournalVoucherHeaderNo}", "Journal Voucher", ipAddress!);
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
                var createdBy = !modelHeader.OriginalSeriesNumber.IsNullOrEmpty() && modelHeader.OriginalDocumentId != 0 ? modelHeader.PostedBy : await _generalRepo.GetUserFullNameAsync(User.Identity!.Name!);
                var date = !modelHeader.OriginalSeriesNumber.IsNullOrEmpty() && modelHeader.OriginalDocumentId != 0 ? modelHeader.PostedDate : DateTime.Now;
                try
                {
                    var modelDetails = await _dbContext.JournalVoucherDetails.Where(jvd => jvd.TransactionNo == modelHeader.JournalVoucherHeaderNo).ToListAsync(cancellationToken);
                    if (!modelHeader.IsPosted)
                    {
                        modelHeader.IsPosted = true;
                        modelHeader.PostedBy = createdBy;
                        modelHeader.PostedDate = date;

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
                            AuditTrail auditTrailBook = new(createdBy, $"Posted journal voucher# {modelHeader.JournalVoucherHeaderNo}", "Journal Voucher", ipAddress!);
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
            var createdBy = !model.OriginalSeriesNumber.IsNullOrEmpty() && model.OriginalDocumentId != 0 ? model.VoidedBy : await _generalRepo.GetUserFullNameAsync(User.Identity!.Name!);
            var date = !model.OriginalSeriesNumber.IsNullOrEmpty() && model.OriginalDocumentId != 0 ? model.VoidedDate : DateTime.Now;

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
                        model.VoidedBy = createdBy;
                        model.VoidedDate = date;

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
                            AuditTrail auditTrailBook = new(createdBy, $"Voided journal voucher# {model.JournalVoucherHeaderNo}", "Journal Voucher", ipAddress!);
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
            var createdBy = !model.OriginalSeriesNumber.IsNullOrEmpty() && model.OriginalDocumentId != 0 ? model.CanceledBy : await _generalRepo.GetUserFullNameAsync(User.Identity!.Name!);
            var date = !model.OriginalSeriesNumber.IsNullOrEmpty() && model.OriginalDocumentId != 0 ? model.CanceledDate : DateTime.Now;

            try
            {
                if (model != null)
                {
                    if (!model.IsCanceled)
                    {
                        model.IsCanceled = true;
                        model.CanceledBy = createdBy;
                        model.CanceledDate = date;
                        model.CancellationRemarks = cancellationRemarks;

                        #region --Audit Trail Recording

                        if (model.OriginalSeriesNumber.IsNullOrEmpty() && model.OriginalDocumentId == 0)
                        {
                            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                            AuditTrail auditTrailBook = new(createdBy, $"Cancelled journal voucher# {model.JournalVoucherHeaderNo}", "Journal Voucher", ipAddress!);
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
                var createdBy = await _generalRepo.GetUserFullNameAsync(User.Identity!.Name!);
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
                        var getAccountName = await _dbContext.ChartOfAccounts.FirstOrDefaultAsync(x => x.AccountNumber == viewModel.AccountNumber![i], cancellationToken);

                        if (accountTitleDict.TryGetValue(viewModel.AccountNumber?[i], out var ids))
                        {
                            // Update the first matching record and remove it from the list
                            var detailsId = ids.First();
                            ids.RemoveAt(0);
                            var details = existingModel.Details.First(o => o.JournalVoucherDetailId == detailsId);
                            var getOriginalDocumentId =
                                existingModel.Details.FirstOrDefault(x => x.AccountNo == details.AccountNo);

                            var acctNo = await _dbContext.ChartOfAccounts
                                .FirstOrDefaultAsync(x => x.AccountNumber == viewModel.AccountNumber![i], cancellationToken: cancellationToken);

                            details.AccountNo = acctNo!.AccountNumber;
                            details.AccountName = getAccountName.AccountName;
                            details.Debit = viewModel.Debit[i];
                            details.Credit = viewModel.Credit[i];
                            details.TransactionNo = existingModel.JournalVoucherHeaderNo!;
                            details.JournalVoucherHeaderId = existingModel.JournalVoucherHeaderId;
                            details.OriginalDocumentId = getOriginalDocumentId?.OriginalDocumentId;

                            if (ids.Count == 0)
                            {
                                accountTitleDict.Remove(viewModel.AccountNumber![i]);
                            }
                        }
                        else
                        {
                            var getOriginalDocumentId = existingModel.Details.ToArray();
                            // Add new record
                            var newDetails = new JournalVoucherDetail
                            {
                                AccountNo = viewModel.AccountNumber![i],
                                AccountName = getAccountName.AccountName,
                                Debit = viewModel.Debit[i],
                                Credit = viewModel.Credit[i],
                                TransactionNo = existingModel.JournalVoucherHeaderNo!,
                                JournalVoucherHeaderId = existingModel.JournalVoucherHeaderId,
                                OriginalDocumentId = getOriginalDocumentId[i].OriginalDocumentId
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
                            AuditTrail auditTrailBook = new(createdBy, $"Edit journal voucher# {viewModel.JVNo}", "Journal Voucher", ipAddress!);
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

        [HttpPost]
        public async Task<IActionResult> GetJournalVoucherList(CancellationToken cancellationToken)
        {
            try
            {
                var journalVouchers = await _journalVoucherRepo.GetJournalVouchersAsync(cancellationToken);

                return Json(new
                {
                    data = journalVouchers
                });
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
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
                worksheet5.Cells["S1"].Value = "EditedBy";
                worksheet5.Cells["T1"].Value = "EditedDate";
                worksheet5.Cells["U1"].Value = "CanceledBy";
                worksheet5.Cells["V1"].Value = "CanceledDate";
                worksheet5.Cells["W1"].Value = "VoidedBy";
                worksheet5.Cells["X1"].Value = "VoidedDate";

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
                worksheet6.Cells["W1"].Value = "EditedBy";
                worksheet6.Cells["X1"].Value = "EditedDate";
                worksheet6.Cells["Y1"].Value = "CanceledBy";
                worksheet6.Cells["Z1"].Value = "CanceledDate";
                worksheet6.Cells["AA1"].Value = "VoidedBy";
                worksheet6.Cells["AB1"].Value = "VoidedDate";

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
                worksheet3.Cells["AF1"].Value = "EditedBy";
                worksheet3.Cells["AG1"].Value = "EditedDate";
                worksheet3.Cells["AH1"].Value = "CanceledBy";
                worksheet3.Cells["AI1"].Value = "CanceledDate";
                worksheet3.Cells["AJ1"].Value = "VoidedBy";
                worksheet3.Cells["AK1"].Value = "VoidedDate";

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
                worksheet.Cells["L1"].Value = "EditedBy";
                worksheet.Cells["M1"].Value = "EditedDate";
                worksheet.Cells["N1"].Value = "CanceledBy";
                worksheet.Cells["O1"].Value = "CanceledDate";
                worksheet.Cells["P1"].Value = "VoidedBy";
                worksheet.Cells["Q1"].Value = "VoidedDate";

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
                    worksheet.Cells[row, 12].Value = item.EditedBy;
                    worksheet.Cells[row, 13].Value = item.EditedDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                    worksheet.Cells[row, 14].Value = item.CanceledBy;
                    worksheet.Cells[row, 15].Value = item.CanceledDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                    worksheet.Cells[row, 16].Value = item.VoidedBy;
                    worksheet.Cells[row, 17].Value = item.VoidedDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff");

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
                    worksheet3.Cells[cvhRow, 34].Value = item.CheckVoucherHeader.EditedBy;
                    worksheet3.Cells[cvhRow, 35].Value = item.CheckVoucherHeader.EditedDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                    worksheet3.Cells[cvhRow, 36].Value = item.CheckVoucherHeader.CanceledBy;
                    worksheet3.Cells[cvhRow, 37].Value = item.CheckVoucherHeader.CanceledDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                    worksheet3.Cells[cvhRow, 38].Value = item.CheckVoucherHeader.VoidedBy;
                    worksheet3.Cells[cvhRow, 39].Value = item.CheckVoucherHeader.VoidedDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff");

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
                    worksheet3.Cells[cvhRow, 34].Value = item.EditedBy;
                    worksheet3.Cells[cvhRow, 35].Value = item.EditedDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                    worksheet3.Cells[cvhRow, 36].Value = item.CanceledBy;
                    worksheet3.Cells[cvhRow, 37].Value = item.CanceledDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                    worksheet3.Cells[cvhRow, 38].Value = item.VoidedBy;
                    worksheet3.Cells[cvhRow, 39].Value = item.VoidedDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff");

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
                    worksheet6.Cells[rrRow, 23].Value = item.EditedBy;
                    worksheet6.Cells[rrRow, 24].Value = item.EditedDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                    worksheet6.Cells[rrRow, 25].Value = item.CanceledBy;
                    worksheet6.Cells[rrRow, 26].Value = item.CanceledDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                    worksheet6.Cells[rrRow, 27].Value = item.VoidedBy;
                    worksheet6.Cells[rrRow, 28].Value = item.VoidedDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff");

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
                    worksheet5.Cells[poRow, 19].Value = item.EditedBy;
                    worksheet5.Cells[poRow, 20].Value = item.EditedDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                    worksheet5.Cells[poRow, 21].Value = item.CanceledBy;
                    worksheet5.Cells[poRow, 22].Value = item.CanceledDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                    worksheet5.Cells[poRow, 23].Value = item.VoidedBy;
                    worksheet5.Cells[poRow, 24].Value = item.VoidedDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff");

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
                var createdBy = await _generalRepo.GetUserFullNameAsync(User.Identity!.Name!);
                try
                {
                    using var package = new ExcelPackage(stream);
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "JournalVoucherHeader");

                    var worksheet2 = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "JournalVoucherDetails");

                    var worksheet3 = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "PurchaseOrder");

                    var worksheet4 = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "ReceivingReport");

                    var worksheet5 = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "CheckVoucherHeader");

                    var worksheet6 = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "CheckVoucherDetails");

                    var worksheet7 = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "MultipleCheckVoucherPayments");

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

                    #region Purchase Order Import

                    if (worksheet3 != null)
                    {
                        var rows = _purchaseOrderRepo.ParseWorksheet(worksheet3);
                        var lookup = await _purchaseOrderRepo.BuildLookupPurchaseOrderContextAsync(rows, cancellationToken);

                        var purchaseOrders = new List<PurchaseOrder>();
                        var auditTrails = new List<AuditTrail>();
                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        var checkingDuplicateSeriesNo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                        foreach (var row in rows)
                        {
                            if (!lookup.ExistingPurchaseOrder.TryGetValue(row.OriginalSeriesNumber, out var existing))
                            {
                                if (!checkingDuplicateSeriesNo.Add(row.OriginalSeriesNumber))
                                {
                                    continue;
                                }

                                purchaseOrders.Add(_purchaseOrderRepo.MapToPurchaseOrderEntity(row, lookup));
                                auditTrails.AddRange(_purchaseOrderRepo.AuditTrails(row, ipAddress ?? string.Empty));
                            }
                            else
                            {
                                var changes = _purchaseOrderRepo.Detect(existing, row, lookup.ExistingLogs);
                                if (changes.Any())
                                {
                                    await _purchaseOrderRepo.LogChangesAsync(
                                        existing.OriginalDocumentId,
                                        changes,
                                        await _generalRepo.GetUserFullNameAsync(User.Identity!.Name!),
                                        existing.PurchaseOrderNo,
                                        "IBS-RCD");
                                }
                            }
                        }

                        _dbContext.PurchaseOrders.AddRange(purchaseOrders);
                        _dbContext.AuditTrails.AddRange(auditTrails);
                        await _dbContext.SaveChangesAsync(cancellationToken);
                    }

                    #endregion

                    #region Receiving Report Import

                    if (worksheet4 != null)
                    {
                        var rows = _receivingReportRepo.ParseWorksheet(worksheet4);
                        var lookup = await _receivingReportRepo.BuildLookupReceivingReportContextAsync(rows, cancellationToken);

                        var receivingReports = new List<ReceivingReport>();
                        var auditTrails = new List<AuditTrail>();
                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        var checkingDuplicateSeriesNo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                        foreach (var row in rows)
                        {
                            if (!lookup.ExistingReceivingReport.TryGetValue(row.OriginalSeriesNumber, out var existing))
                            {
                                if (!checkingDuplicateSeriesNo.Add(row.OriginalSeriesNumber))
                                {
                                    continue;
                                }

                                receivingReports.Add(_receivingReportRepo.MapToReceivingReportEntity(row, lookup));
                                auditTrails.AddRange(_receivingReportRepo.AuditTrails(row, ipAddress ?? string.Empty));
                            }
                            else
                            {
                                var changes = _receivingReportRepo.Detect(existing, row, lookup.ExistingLogs);
                                if (changes.Any())
                                {
                                    await _receivingReportRepo.LogChangesAsync(
                                        existing.OriginalDocumentId,
                                        changes,
                                        await _generalRepo.GetUserFullNameAsync(User.Identity!.Name!),
                                        existing.ReceivingReportNo,
                                        "IBS-RCD");
                                }
                            }
                        }

                        _dbContext.ReceivingReports.AddRange(receivingReports);
                        _dbContext.AuditTrails.AddRange(auditTrails);
                        await _dbContext.SaveChangesAsync(cancellationToken);
                    }

                    #endregion

                    #region Check Voucher Header Import

                    if (worksheet5 != null!)
                    {
                        var rows = await _checkVoucherRepo.ParseWorksheet(worksheet5, cancellationToken);
                        var lookup = await _checkVoucherRepo.BuildLookupCheckVoucherHeaderContextAsync(rows, cancellationToken);

                        var checkVoucherHeaders = new List<CheckVoucherHeader>();
                        var auditTrails = new List<AuditTrail>();
                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        var checkingDuplicateSeriesNo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        var getLastCheckVoucherHeaderNo = await _dbContext.CheckVoucherHeaders
                            .OrderByDescending(x => x.CheckVoucherHeaderNo)
                            .Select(x => x.CheckVoucherHeaderNo)
                            .FirstOrDefaultAsync(cancellationToken);

                        foreach (var row in rows.OrderBy(x => x.Date).ThenBy(x => x.CreatedDate))
                        {
                            if (!lookup.ExistingCheckVoucherHeader.TryGetValue(row.OriginalSeriesNumber, out var existing))
                            {
                                if (!checkingDuplicateSeriesNo.Add(row.OriginalSeriesNumber))
                                {
                                    continue;
                                }

                                getLastCheckVoucherHeaderNo = _checkVoucherRepo.GenerateCodeForUploadingExcelFile(getLastCheckVoucherHeaderNo, cancellationToken);
                                checkVoucherHeaders.Add(_checkVoucherRepo.MapToCheckVoucherHeaderEntity(row, checkVoucherHeaders, lookup, getLastCheckVoucherHeaderNo));
                                auditTrails.AddRange(_checkVoucherRepo.AuditTrails(row, ipAddress ?? string.Empty));
                            }
                            else
                            {
                                var changes = _checkVoucherRepo.Detect(existing, row, lookup.ExistingLogs);
                                if (changes.Any())
                                {
                                    await _checkVoucherRepo.LogChangesAsync(
                                        existing.OriginalDocumentId,
                                        changes,
                                        await _generalRepo.GetUserFullNameAsync(User.Identity!.Name!),
                                        existing.CheckVoucherHeaderNo!,
                                        "IBS-RCD");
                                }
                            }
                        }

                        _dbContext.CheckVoucherHeaders.AddRange(checkVoucherHeaders);
                        _dbContext.AuditTrails.AddRange(auditTrails);
                        await _dbContext.SaveChangesAsync(cancellationToken);
                    }

                    #endregion

                    #region -- Check Voucher Multiple Payment Import --

                    try
                    {
                        if (worksheet7 != null)
                        {
                            var rows = _checkVoucherRepo.ParseWorksheetCvMultiplePayment(worksheet7);
                            var lookup = await _checkVoucherRepo.BuildLookupCvMultiplePaymentContextAsync(rows, cancellationToken);

                            var multipleCheckVoucherPayments = new List<MultipleCheckVoucherPayment>();

                            foreach (var row in rows)
                            {
                                multipleCheckVoucherPayments.Add(_checkVoucherRepo.MapToCvMultiplePaymentEntity(row, lookup));
                            }

                            _dbContext.MultipleCheckVoucherPayments.AddRange(multipleCheckVoucherPayments);
                            await _dbContext.SaveChangesAsync(cancellationToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        TempData["error"] = ex.Message;
                    }

                    #endregion -- Check Voucher Multiple Payment Import --

                    #region -- Check Voucher Details Import --

                    if (worksheet6 != null)
                    {
                        var rows = _checkVoucherRepo.ParseWorksheetCheckVoucherDetails(worksheet6);
                        var lookup = await _checkVoucherRepo.BuildLookupCheckVoucherDetailsContextAsync(rows, cancellationToken);

                        var checkVoucherDetails = new List<CheckVoucherDetail>();
                        var originalDocumentId = new HashSet<int>();

                        foreach (var row in rows)
                        {
                            if (!lookup.ExistingCheckVoucherDetail.TryGetValue(row.OriginalDocumentId, out var existing))
                            {
                                if (!originalDocumentId.Add(row.OriginalDocumentId))
                                {
                                    continue;
                                }

                                checkVoucherDetails.Add(_checkVoucherRepo.MapToCheckVoucherDetailsEntity(row, lookup));
                            }
                            else
                            {
                                var changes = _checkVoucherRepo.DetectCvDetails(existing, row, lookup.ExistingLogs);
                                if (changes.Any())
                                {
                                    await _checkVoucherRepo.LogChangesForCVDAsync(
                                        existing.OriginalDocumentId ?? 0,
                                        changes,
                                        await _generalRepo.GetUserFullNameAsync(User.Identity!.Name!),
                                        existing.CheckVoucherHeader!.CheckVoucherHeaderNo,
                                        "IBS-RCD");
                                }
                            }
                        }

                        _dbContext.CheckVoucherDetails.AddRange(checkVoucherDetails);
                        await _dbContext.SaveChangesAsync(cancellationToken);
                    }

                    #endregion -- Check Voucher Details Import --

                    #region -- Journal Voucher Header Import --

                    if (worksheet != null)
                    {
                        var rows = _journalVoucherRepo.ParseWorksheet(worksheet);
                        var lookup = await _journalVoucherRepo.BuildLookupCheckVoucherHeaderContextAsync(rows, cancellationToken);

                        var journalVoucherHeader = new List<JournalVoucherHeader>();
                        var auditTrails = new List<AuditTrail>();
                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        var checkingDuplicateSeriesNo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                        foreach (var row in rows)
                        {
                            if (!lookup.ExistingJournalVoucherHeader.TryGetValue(row.OriginalSeriesNumber, out var existing))
                            {
                                if (!checkingDuplicateSeriesNo.Add(row.OriginalSeriesNumber))
                                {
                                    continue;
                                }

                                journalVoucherHeader.Add(_journalVoucherRepo.MapToJournalVoucherEntity(row, lookup));
                                auditTrails.AddRange(_journalVoucherRepo.AuditTrails(row, ipAddress ?? string.Empty));
                            }
                            else
                            {
                                var changes = _journalVoucherRepo.Detect(existing, row, lookup.ExistingLogs);
                                if (changes.Any())
                                {
                                    await _journalVoucherRepo.LogChangesAsync(
                                        existing.OriginalDocumentId,
                                        changes,
                                        await _generalRepo.GetUserFullNameAsync(User.Identity!.Name!),
                                        existing.JournalVoucherHeaderNo!,
                                        "IBS-RCD");
                                }
                            }
                        }

                        _dbContext.JournalVoucherHeaders.AddRange(journalVoucherHeader);
                        _dbContext.AuditTrails.AddRange(auditTrails);
                        await _dbContext.SaveChangesAsync(cancellationToken);
                    }

                    #endregion -- Journal Voucher Header Import --

                    #region -- Journal Voucher Details Import --

                    if (worksheet2 != null)
                    {
                        var rows = _journalVoucherRepo.ParseWorksheetJournalVoucherDetails(worksheet2);
                        var lookup = await _journalVoucherRepo.BuildLookupJournalVoucherDetailsContextAsync(rows, cancellationToken);

                        var journalVoucherDetails = new List<JournalVoucherDetail>();
                        var originalDocumentId = new HashSet<int>();

                        foreach (var row in rows)
                        {
                            if (!lookup.ExistingJournalVoucherDetail.TryGetValue(row.OriginalDocumentId, out var existing))
                            {
                                if (!originalDocumentId.Add(row.OriginalDocumentId))
                                {
                                    continue;
                                }

                                journalVoucherDetails.Add(_journalVoucherRepo.MapToJournalVoucherDetailsEntity(row, lookup));
                            }
                            else
                            {
                                var changes = _journalVoucherRepo.DetectJvDetails(existing, row, lookup.ExistingLogs);
                                if (changes.Any())
                                {
                                    await _journalVoucherRepo.LogChangesForJVDAsync(
                                        existing.OriginalDocumentId ?? 0,
                                        changes,
                                        await _generalRepo.GetUserFullNameAsync(User.Identity!.Name!),
                                        existing.JournalVoucherHeader!.JournalVoucherHeaderNo,
                                        "IBS-RCD");
                                }
                            }
                        }

                        _dbContext.JournalVoucherDetails.AddRange(journalVoucherDetails);
                        await _dbContext.SaveChangesAsync(cancellationToken);
                    }

                    #endregion -- Journal Voucher Details Import --

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    var checkChangesOfRecord = await _dbContext.ImportExportLogs
                        .Where(iel => iel.Action == string.Empty).ToListAsync(cancellationToken);
                    if (checkChangesOfRecord.Any())
                    {
                        TempData["importChanges"] = "";
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

        #endregion

        //Upload as .xlsx file.(Import)
        #region -- import xlsx record to AAS --

        [HttpPost]
        public async Task<IActionResult> AasImport(IFormFile file, CancellationToken cancellationToken)
        {
            if (file.Length == 0)
            {
                TempData["error"] = "The Excel file length is zero!.";
                return RedirectToAction(nameof(Index), new { view = DynamicView.JournalVoucher });
            }

            await using var transaction = await _aasDbContext.Database.BeginTransactionAsync(cancellationToken);
            var createdBy = await _generalRepo.GetUserFullNameAsync(User.Identity!.Name!);

            try
            {
                await using var stream = file.OpenReadStream();
                using var package = new ExcelPackage(stream);

                var worksheet = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "JournalVoucherHeader");
                var worksheet2 = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "JournalVoucherDetails");
                var worksheet3 = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "PurchaseOrder");
                var worksheet4 = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "ReceivingReport");
                var worksheet5 = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "CheckVoucherHeader");
                var worksheet6 = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "CheckVoucherDetails");
                var worksheet7 = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "MultipleCheckVoucherPayments");

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

                #region Purchase Order Import

                if (worksheet3 != null)
                {
                    var rows = _purchaseOrderRepo.ParseWorksheet(worksheet3);
                    var lookup = await _purchaseOrderRepo.BuildLookupPurchaseOrderContextForAasAsync(rows, cancellationToken);

                    var purchaseOrders = new List<PurchaseOrder>();
                    var auditTrails = new List<AuditTrail>();
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    var checkingDuplicateSeriesNo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var row in rows)
                    {
                        if (!lookup.ExistingPurchaseOrder.TryGetValue(row.OriginalSeriesNumber, out var existing))
                        {
                            if (!checkingDuplicateSeriesNo.Add(row.OriginalSeriesNumber))
                            {
                                continue;
                            }

                            purchaseOrders.Add(_purchaseOrderRepo.MapToPurchaseOrderEntity(row, lookup));
                            auditTrails.AddRange(_purchaseOrderRepo.AuditTrails(row, ipAddress ?? string.Empty));
                        }
                        else
                        {
                            var changes = _purchaseOrderRepo.Detect(existing, row, lookup.ExistingLogs);
                            if (changes.Any())
                            {
                                await _purchaseOrderRepo.LogChangesAsync(
                                    existing.OriginalDocumentId,
                                    changes,
                                    createdBy,
                                    existing.PurchaseOrderNo,
                                    "AAS");
                            }
                        }
                    }

                    _aasDbContext.PurchaseOrders.AddRange(purchaseOrders);
                    _aasDbContext.AuditTrails.AddRange(auditTrails);
                    await _aasDbContext.SaveChangesAsync(cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }

                #endregion

                #region Receiving Report Import

                if (worksheet4 != null)
                {
                    var rows = _receivingReportRepo.ParseWorksheet(worksheet4);
                    var lookup = await _receivingReportRepo.BuildLookupReceivingReportContextForAasAsync(rows, cancellationToken);

                    var receivingReports = new List<ReceivingReport>();
                    var auditTrails = new List<AuditTrail>();
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    var checkingDuplicateSeriesNo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var row in rows)
                    {
                        if (!lookup.ExistingReceivingReport.TryGetValue(row.OriginalSeriesNumber, out var existing))
                        {
                            if (!checkingDuplicateSeriesNo.Add(row.OriginalSeriesNumber))
                            {
                                continue;
                            }

                            receivingReports.Add(_receivingReportRepo.MapToReceivingReportEntity(row, lookup));
                            auditTrails.AddRange(_receivingReportRepo.AuditTrails(row, ipAddress ?? string.Empty));
                        }
                        else
                        {
                            var changes = _receivingReportRepo.Detect(existing, row, lookup.ExistingLogs);
                            if (changes.Any())
                            {
                                await _receivingReportRepo.LogChangesAsync(
                                    existing.OriginalDocumentId,
                                    changes,
                                    createdBy,
                                    existing.ReceivingReportNo,
                                    "AAS");
                            }
                        }
                    }

                    _aasDbContext.ReceivingReports.AddRange(receivingReports);
                    _aasDbContext.AuditTrails.AddRange(auditTrails);
                    await _aasDbContext.SaveChangesAsync(cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }

                #endregion

                #region Check Voucher Header Import

                if (worksheet5 != null)
                {
                    var rows = await _checkVoucherRepo.ParseWorksheet(worksheet5, cancellationToken);
                    var lookup = await _checkVoucherRepo.BuildLookupCheckVoucherHeaderContextForAasAsync(rows, cancellationToken);

                    var checkVoucherHeaders = new List<CheckVoucherHeader>();
                    var auditTrails = new List<AuditTrail>();
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    var checkingDuplicateSeriesNo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    var getLastCheckVoucherHeaderNo = await _aasDbContext.CheckVoucherHeaders
                        .OrderByDescending(x => x.CheckVoucherHeaderNo)
                        .Select(x => x.CheckVoucherHeaderNo)
                        .FirstOrDefaultAsync(cancellationToken);

                    foreach (var row in rows.OrderBy(x => x.Date).ThenBy(x => x.CreatedDate))
                    {
                        if (!lookup.ExistingCheckVoucherHeader.TryGetValue(row.OriginalSeriesNumber, out var existing))
                        {
                            if (!checkingDuplicateSeriesNo.Add(row.OriginalSeriesNumber))
                            {
                                continue;
                            }

                            getLastCheckVoucherHeaderNo = _checkVoucherRepo.GenerateCodeForUploadingExcelFile(getLastCheckVoucherHeaderNo, cancellationToken);
                            checkVoucherHeaders.Add(_checkVoucherRepo.MapToCheckVoucherHeaderEntity(row, checkVoucherHeaders, lookup, getLastCheckVoucherHeaderNo));
                            auditTrails.AddRange(_checkVoucherRepo.AuditTrails(row, ipAddress ?? string.Empty));
                        }
                        else
                        {
                            var changes = _checkVoucherRepo.Detect(existing, row, lookup.ExistingLogs);
                            if (changes.Any())
                            {
                                await _checkVoucherRepo.LogChangesAsync(
                                    existing.OriginalDocumentId,
                                    changes,
                                    createdBy,
                                    existing.CheckVoucherHeaderNo!,
                                    "AAS");
                            }
                        }
                    }

                    _aasDbContext.CheckVoucherHeaders.AddRange(checkVoucherHeaders);
                    _aasDbContext.AuditTrails.AddRange(auditTrails);
                    await _aasDbContext.SaveChangesAsync(cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }

                #endregion

                #region -- Check Voucher Multiple Payment Import --

                if (worksheet7 != null)
                {
                    var rows = _checkVoucherRepo.ParseWorksheetCvMultiplePayment(worksheet7);
                    var lookup = await _checkVoucherRepo.BuildLookupCvMultiplePaymentContextForAasAsync(rows, cancellationToken);

                    var multipleCheckVoucherPayments = new List<MultipleCheckVoucherPayment>();

                    foreach (var row in rows)
                    {
                        multipleCheckVoucherPayments.Add(_checkVoucherRepo.MapToCvMultiplePaymentEntity(row, lookup));
                    }

                    _aasDbContext.MultipleCheckVoucherPayments.AddRange(multipleCheckVoucherPayments);
                    await _aasDbContext.SaveChangesAsync(cancellationToken);
                }

                #endregion -- Check Voucher Multiple Payment Import --

                #region -- Check Voucher Details Import --

                if (worksheet6 != null)
                {
                    var rows = _checkVoucherRepo.ParseWorksheetCheckVoucherDetails(worksheet6);
                    var lookup = await _checkVoucherRepo.BuildLookupCheckVoucherDetailsContextForAasAsync(rows, cancellationToken);

                    var checkVoucherDetails = new List<CheckVoucherDetail>();
                    var originalDocumentId = new HashSet<int>();

                    foreach (var row in rows)
                    {
                        if (!lookup.ExistingCheckVoucherDetail.TryGetValue(row.OriginalDocumentId, out var existing))
                        {
                            if (!originalDocumentId.Add(row.OriginalDocumentId))
                            {
                                continue;
                            }

                            checkVoucherDetails.Add(_checkVoucherRepo.MapToCheckVoucherDetailsEntity(row, lookup));
                        }
                        else
                        {
                            var changes = _checkVoucherRepo.DetectCvDetails(existing, row, lookup.ExistingLogs);
                            if (changes.Any())
                            {
                                await _checkVoucherRepo.LogChangesForCVDAsync(
                                    existing.OriginalDocumentId ?? 0,
                                    changes,
                                    createdBy,
                                    existing.CheckVoucherHeader!.CheckVoucherHeaderNo,
                                    "AAS");
                            }
                        }
                    }

                    _aasDbContext.CheckVoucherDetails.AddRange(checkVoucherDetails);
                    await _aasDbContext.SaveChangesAsync(cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }

                #endregion -- Check Voucher Details Import --

                #region -- Journal Voucher Header Import --

                if (worksheet != null!)
                {
                    var rows = _journalVoucherRepo.ParseWorksheet(worksheet);
                    var lookup = await _journalVoucherRepo.BuildLookupJournalVoucherHeaderContextForAasAsync(rows, cancellationToken);

                    var journalVoucherHeaders = new List<JournalVoucherHeader>();
                    var auditTrails = new List<AuditTrail>();
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    var checkingDuplicateSeriesNo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var row in rows)
                    {
                        if (!lookup.ExistingJournalVoucherHeader.TryGetValue(row.OriginalSeriesNumber, out var existing))
                        {
                            if (!checkingDuplicateSeriesNo.Add(row.OriginalSeriesNumber))
                            {
                                continue;
                            }

                            journalVoucherHeaders.Add(_journalVoucherRepo.MapToJournalVoucherEntity(row, lookup));
                            auditTrails.AddRange(_journalVoucherRepo.AuditTrails(row, ipAddress ?? string.Empty));
                        }
                        else
                        {
                            var changes = _journalVoucherRepo.Detect(existing, row, lookup.ExistingLogs);
                            if (changes.Any())
                            {
                                await _journalVoucherRepo.LogChangesAsync(
                                    existing.OriginalDocumentId,
                                    changes,
                                    createdBy,
                                    existing.JournalVoucherHeaderNo!,
                                    "AAS");
                            }
                        }
                    }

                    _aasDbContext.JournalVoucherHeaders.AddRange(journalVoucherHeaders);
                    _aasDbContext.AuditTrails.AddRange(auditTrails);
                    await _aasDbContext.SaveChangesAsync(cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }

                #endregion -- Journal Voucher Header Import --

                #region -- Journal Voucher Details Import --

                if (worksheet2 != null)
                {
                    var rows = _journalVoucherRepo.ParseWorksheetJournalVoucherDetails(worksheet2);
                    var lookup = await _journalVoucherRepo.BuildLookupJournalVoucherDetailsContextForAasAsync(rows, cancellationToken);

                    var journalVoucherDetails = new List<JournalVoucherDetail>();
                    var originalDocumentId = new HashSet<int>();

                    foreach (var row in rows)
                    {
                        if (!lookup.ExistingJournalVoucherDetail.TryGetValue(row.OriginalDocumentId, out var existing))
                        {
                            if (!originalDocumentId.Add(row.OriginalDocumentId))
                            {
                                continue;
                            }

                            journalVoucherDetails.Add(_journalVoucherRepo.MapToJournalVoucherDetailsEntity(row, lookup));
                        }
                        else
                        {
                            var changes = _journalVoucherRepo.DetectJvDetails(existing, row, lookup.ExistingLogs);
                            if (changes.Any())
                            {
                                await _journalVoucherRepo.LogChangesForJVDAsync(
                                    existing.OriginalDocumentId,
                                    changes,
                                    createdBy,
                                    existing.TransactionNo!,
                                    "AAS");
                            }
                        }
                    }

                    _aasDbContext.JournalVoucherDetails.AddRange(journalVoucherDetails);
                    await _aasDbContext.SaveChangesAsync(cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }

                #endregion -- Journal Voucher Details Import --

                var checkChangesOfRecord = await _dbContext.ImportExportLogs
                    .Where(iel => iel.Action == string.Empty).ToListAsync(cancellationToken);
                if (checkChangesOfRecord.Any())
                {
                    TempData["importChanges"] = "";
                }

                await transaction.CommitAsync(cancellationToken);
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

            TempData["success"] = "Uploading Success!";
            return RedirectToAction(nameof(Index), new { view = DynamicView.JournalVoucher });
        }


        #endregion
    }
}
