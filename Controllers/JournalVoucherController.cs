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

        private readonly IWebHostEnvironment _webHostEnvironment;

        private readonly GeneralRepo _generalRepo;

        public JournalVoucherController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, IWebHostEnvironment webHostEnvironment, JournalVoucherRepo journalVoucherRepo, GeneralRepo generalRepo)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
            _journalVoucherRepo = journalVoucherRepo;
            _generalRepo = generalRepo;
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
                return RedirectToAction(nameof(Index));
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
            var modelDetails = await _dbContext.JournalVoucherDetails.Where(jvd => jvd.TransactionNo == modelHeader.JVNo).ToListAsync(cancellationToken);

            if (modelHeader != null)
            {
                try
                {
                    if (!modelHeader.IsPosted)
                    {
                        modelHeader.IsPosted = true;
                        modelHeader.PostedBy = _userManager.GetUserName(this.User);
                        modelHeader.PostedDate = DateTime.Now;

                        #region --General Ledger Book Recording(GL)--

                        var ledgers = new List<GeneralLedgerBook>();
                        foreach (var details in modelDetails)
                        {
                            ledgers.Add(
                                    new GeneralLedgerBook
                                    {
                                        Date = modelHeader.Date,
                                        Reference = modelHeader.JVNo,
                                        Description = modelHeader.Particulars,
                                        AccountNo = details.AccountNo,
                                        AccountTitle = details.AccountName,
                                        Debit = details.Debit,
                                        Credit = details.Credit,
                                        CreatedBy = modelHeader.CreatedBy,
                                        CreatedDate = modelHeader.CreatedDate
                                    }
                                );
                        }

                        if (!_generalRepo.IsDebitCreditBalanced(ledgers))
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
                        TempData["success"] = "Journal Voucher has been Posted.";
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
            var model = await _dbContext.JournalVoucherHeaders.FindAsync(id, cancellationToken);
            var findJVInJB = await _dbContext.JournalBooks.Where(jb => jb.Reference == model.JVNo).ToListAsync(cancellationToken);
            var findJVInGL = await _dbContext.GeneralLedgerBooks.Where(jb => jb.Reference == model.JVNo).ToListAsync(cancellationToken);

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
                    TempData["success"] = "Journal Voucher has been Voided.";
                }
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        public async Task<IActionResult> Cancel(int id, string cancellationRemarks, CancellationToken cancellationToken)
        {
            var model = await _dbContext.JournalVoucherHeaders.FindAsync(id, cancellationToken);

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
                    TempData["success"] = "Journal Voucher has been Cancelled.";
                }
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
                    model.CreatedBy = _userManager.GetUserName(this.User);

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

                    #region --Audit Trail Recording

                    // if (model.OriginalSeriesNumber == null && model.OriginalDocumentId == 0)
                    // {
                    //     var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    //     AuditTrail auditTrailBook = new(_userManager.GetUserName(this.User), $"Edit journal voucher# {viewModel.JVNo}", "Journal Voucher", ipAddress);
                    //     await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                    // }

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);  // await the SaveChangesAsync method
                    TempData["success"] = "Journal Voucher edited successfully";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
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
        public async Task<IActionResult> Export(string selectedRecord)
        {
            if (string.IsNullOrEmpty(selectedRecord))
            {
                // Handle the case where no invoices are selected
                return RedirectToAction(nameof(Index));
            }

            var recordIds = selectedRecord.Split(',').Select(int.Parse).ToList();

            // Retrieve the selected invoices from the database
            var selectedList = await _dbContext.JournalVoucherHeaders
                .Where(jv => recordIds.Contains(jv.Id))
                .OrderBy(jv => jv.JVNo)
                .ToListAsync();

            // Create the Excel package
            using (var package = new ExcelPackage())
            {
                // Add a new worksheet to the Excel package
                var worksheet = package.Workbook.Worksheets.Add("JournalVoucherHeader");
                var worksheet2 = package.Workbook.Worksheets.Add("JournalVoucherDetails");

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

                worksheet2.Cells["A1"].Value = "AccountNo";
                worksheet2.Cells["B1"].Value = "AccountName";
                worksheet2.Cells["C1"].Value = "TransactionNo";
                worksheet2.Cells["D1"].Value = "Debit";
                worksheet2.Cells["E1"].Value = "Credit";
                worksheet2.Cells["E1"].Value = "JVHeaderId";
                worksheet2.Cells["F1"].Value = "OriginalDocumentId";

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

                var jvNos = selectedList.Select(item => item.JVNo).ToList();

                var getJVDetails = await _dbContext.JournalVoucherDetails
                    .Where(jvd => jvNos.Contains(jvd.TransactionNo))
                    .OrderBy(jvd => jvd.Id)
                    .ToListAsync();

                int cvdRow = 2;

                foreach (var item in getJVDetails)
                {
                    worksheet2.Cells[cvdRow, 1].Value = item.AccountNo;
                    worksheet2.Cells[cvdRow, 2].Value = item.AccountName;
                    worksheet2.Cells[cvdRow, 3].Value = item.TransactionNo;
                    worksheet2.Cells[cvdRow, 4].Value = item.Debit;
                    worksheet2.Cells[cvdRow, 5].Value = item.Credit;
                    worksheet2.Cells[cvdRow, 5].Value = item.JVHeaderId;
                    worksheet2.Cells[cvdRow, 6].Value = item.Id;

                    cvdRow++;
                }


                // Convert the Excel package to a byte array
                var excelBytes = await package.GetAsByteArrayAsync();

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "JournalVoucherList.xlsx");
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

                        var rowCount = worksheet.Dimension.Rows;
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

                            if (journalVoucherHeaderList.Any(jv => jv.OriginalDocumentId == journalVoucherHeader.OriginalDocumentId))
                            {
                                continue;
                            }

                            journalVoucherHeader.CVId = await _dbContext.CheckVoucherHeaders
                                .Where(c => c.OriginalDocumentId == journalVoucherHeader.OriginalCVId)
                                .Select(c => (int?)c.Id)
                                .FirstOrDefaultAsync(cancellationToken) ?? throw new InvalidOperationException("Please upload the Excel file for the check voucher first.");

                            await _dbContext.JournalVoucherHeaders.AddAsync(journalVoucherHeader, cancellationToken);
                        }
                        await _dbContext.SaveChangesAsync(cancellationToken);

                        if(journalVoucherHeaderList.Count <= 0)
                        {
                            var jvdRowCount = worksheet2.Dimension.Rows;
                            var journalVoucherDetailsList = await _dbContext
                                .JournalVoucherDetails
                                .ToListAsync(cancellationToken);

                            for (int jvdRow = 2; jvdRow <= jvdRowCount; jvdRow++)
                            {
                                var journalVoucherDetails = new JournalVoucherDetail
                                {
                                    AccountNo = worksheet2.Cells[jvdRow, 1].Text,
                                    AccountName = worksheet2.Cells[jvdRow, 2].Text,
                                    Debit = decimal.TryParse(worksheet2.Cells[jvdRow, 4].Text, out decimal debit) ? debit : 0,
                                    Credit = decimal.TryParse(worksheet2.Cells[jvdRow, 5].Text, out decimal credit) ? credit : 0,
                                };

                                if (journalVoucherDetailsList.Any(jv => jv.JVHeaderId == int.Parse(worksheet2.Cells[jvdRow, 6].Text)))
                                {
                                    continue;
                                }

                                var jvHeader = await _dbContext.JournalVoucherHeaders
                                    .Where(jvh => jvh.OriginalSeriesNumber == worksheet2.Cells[jvdRow, 3].Text)
                                    .FirstOrDefaultAsync(cancellationToken);

                                journalVoucherDetails.JVHeaderId = jvHeader.Id;
                                journalVoucherDetails.TransactionNo = jvHeader.JVNo;

                                await _dbContext.JournalVoucherDetails.AddAsync(journalVoucherDetails, cancellationToken);
                            }
                            await _dbContext.SaveChangesAsync(cancellationToken);
                            await transaction.CommitAsync(cancellationToken);
                        }
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
