using Accounting_System.Data;
using Accounting_System.Models;
using Accounting_System.Models.AccountsPayable;
using Accounting_System.Models.Reports;
using Accounting_System.Models.ViewModels;
using Accounting_System.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

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

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var headers = await _dbContext.JournalVoucherHeaders
                .Include(j => j.CheckVoucherHeader)
                .ThenInclude(cv => cv.Supplier)
                .ToListAsync(cancellationToken);

            var details = await _dbContext.JournalVoucherDetails
                .ToListAsync(cancellationToken);

            // Create a list to store CheckVoucherVM objectssw
            var journalVoucherVMs = new List<JournalVoucherVM>();

            // Retrieve details for each header
            foreach (var header in headers)
            {
                var headerJVNo = header.JVNo;
                var headerDetails = details.Where(d => d.TransactionNo == headerJVNo).ToList();

                // Create a new CheckVoucherVM object for each header and its associated details
                var journalVoucherVM = new JournalVoucherVM
                {
                    Header = header,
                    Details = headerDetails
                };

                // Add the CheckVoucherVM object to the list
                journalVoucherVMs.Add(journalVoucherVM);
            }

            return View(journalVoucherVMs);
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
                .OrderBy(coa => coa.Id)
                .Select(s => new SelectListItem
                {
                    Value = s.Number,
                    Text = s.Number + " " + s.Name
                })
                .ToListAsync(cancellationToken);
            viewModel.Header.CheckVoucherHeaders = await _dbContext.CheckVoucherHeaders
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
                .OrderBy(coa => coa.Id)
                .Select(s => new SelectListItem
                {
                    Value = s.Number,
                    Text = s.Number + " " + s.Name
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

                var getLastNumber = await _journalVoucherRepo.GetLastSeriesNumberJV(cancellationToken);

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

                #region --CV Details Entry

                var generateJVNo = await _journalVoucherRepo.GenerateJVNo(cancellationToken);
                var cvDetails = new List<JournalVoucherDetail>();

                var totalDebit = 0m;
                var totalCredit = 0m;
                for (int i = 0; i < accountNumber.Length; i++)
                {
                    var currentAccountNumber = accountNumber[i];
                    var accountTitle = await _dbContext.ChartOfAccounts
                        .FirstOrDefaultAsync(coa => coa.Number == currentAccountNumber, cancellationToken);
                    var currentDebit = debit[i];
                    var currentCredit = credit[i];
                    totalDebit += debit[i];
                    totalCredit += credit[i];

                    cvDetails.Add(
                        new JournalVoucherDetail
                        {
                            AccountNo = currentAccountNumber,
                            AccountName = accountTitle.Name,
                            TransactionNo = generateJVNo,
                            Debit = currentDebit,
                            Credit = currentCredit
                        }
                    );
                }
                if (totalDebit != totalCredit)
                {
                    TempData["error"] = "The debit and credit should be equal!";
                    return View(model);
                }

                await _dbContext.AddRangeAsync(cvDetails, cancellationToken);

                #endregion --CV Details Entry

                #region --Saving the default entries

                //JV Header Entry
                model.Header.SeriesNumber = getLastNumber;
                model.Header.JVNo = generateJVNo;
                model.Header.CreatedBy = _userManager.GetUserName(this.User);

                #endregion --Saving the default entries

                #region --Audit Trail Recording

                AuditTrail auditTrail = new(model.Header.CreatedBy, $"Create new journal voucher# {model.Header.JVNo}", "Journal Voucher");
                await _dbContext.AddAsync(auditTrail, cancellationToken);

                #endregion --Audit Trail Recording

                await _dbContext.AddAsync(model.Header, cancellationToken);  // Add CheckVoucherHeader to the context
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
            var header = await _dbContext.CheckVoucherHeaders
                .Include(s => s.Supplier)
                .FirstOrDefaultAsync(cvh => cvh.Id == id, cancellationToken);

            if (header == null)
            {
                return NotFound();
            }

            var details = await _dbContext.CheckVoucherDetails
                .Where(cvd => cvd.TransactionNo == header.CVNo)
                .ToListAsync(cancellationToken);

            var viewModel = new CheckVoucherVM
            {
                Header = header,
                Details = details
            };

            if (viewModel != null)
            {
                var cvNo = viewModel.Header.CVNo;
                var date = viewModel.Header.Date;
                var name = viewModel.Header.Supplier.Name;
                var address = viewModel.Header.Supplier.Address;
                var tinNo = viewModel.Header.Supplier.TinNo;
                var poNo = viewModel.Header.PONo;
                var siNo = viewModel.Header.SINo;
                var payee = viewModel.Header.Payee;
                var amount = viewModel.Header.Total;
                var particulars = viewModel.Header.Particulars;
                var checkNo = viewModel.Header.CheckNo;
                var totalDebit = viewModel.Details.Select(cvd => cvd.Debit).Sum();
                var totalCredit = viewModel.Details.Select(cvd => cvd.Credit).Sum();

                return Json(new
                {
                    CVNo = cvNo,
                    Date = date,
                    Name = name,
                    Address = address,
                    TinNo = tinNo,
                    PONo = poNo,
                    SINo = siNo,
                    Payee = payee,
                    Amount = amount,
                    Particulars = particulars,
                    CheckNo = checkNo,
                    ViewModel = viewModel,
                    TotalDebit = totalDebit,
                    TotalCredit = totalCredit,
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

                var printedBy = _userManager.GetUserName(this.User);
                AuditTrail auditTrail = new(printedBy, $"Printed original copy of jv# {jv.JVNo}", "Journal Vouchers");
                await _dbContext.AddAsync(auditTrail, cancellationToken);

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

                        AuditTrail auditTrail = new(modelHeader.PostedBy, $"Posted journal voucher# {modelHeader.JVNo}", "Journal Voucher");
                        await _dbContext.AddAsync(auditTrail, cancellationToken);

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        TempData["success"] = "Journal Voucher has been Posted.";
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

                    AuditTrail auditTrail = new(model.VoidedBy, $"Voided journal voucher# {model.JVNo}", "Journal Voucher");
                    await _dbContext.AddAsync(auditTrail, cancellationToken);

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

                    AuditTrail auditTrail = new(model.CanceledBy, $"Cancelled journal voucher# {model.JVNo}", "Journal Voucher");
                    await _dbContext.AddAsync(auditTrail, cancellationToken);

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
                        .Where(coa => !new[] { "2010102", "2010101", "1010101" }.Any(excludedNumber => coa.Number.Contains(excludedNumber)) && coa.Level == 4 || coa.Level == 5)
                        .Select(s => new SelectListItem
                        {
                            Value = s.Number,
                            Text = s.Number + " " + s.Name
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
                .OrderBy(coa => coa.Id)
                .Select(s => new SelectListItem
                {
                    Value = s.Number,
                    Text = s.Number + " " + s.Name
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
                    #region --CV Details Entry

                    var existingHeaderModel = await _dbContext.JournalVoucherHeaders.FindAsync(viewModel.JVId, cancellationToken);
                    var existingDetailsModel = await _dbContext.JournalVoucherDetails.Where(d => d.TransactionNo == existingHeaderModel.JVNo).ToListAsync(cancellationToken);
                    JournalVoucherDetail detailsModel = new();

                    for (int i = 0; i < existingDetailsModel.Count(); i++)
                    {
                        var cvd = existingDetailsModel[i];
                        cvd.AccountNo = viewModel.AccountNumber[i];
                        cvd.AccountName = viewModel.AccountTitle[i];
                        cvd.Debit = viewModel.Debit[i];
                        cvd.Credit = viewModel.Credit[i];
                        cvd.TransactionNo = viewModel.JVNo;
                    }

                    var newDetailsModel = new List<JournalVoucherDetail>(); // Replace with the actual new details
                    existingDetailsModel.AddRange(newDetailsModel);

                    #endregion --CV Details Entry

                    #region --Saving the default entries

                    existingHeaderModel.JVNo = viewModel.JVNo;
                    existingHeaderModel.Date = viewModel.TransactionDate;
                    existingHeaderModel.References = viewModel.References;
                    existingHeaderModel.CVId = viewModel.CVId;
                    existingHeaderModel.Particulars = viewModel.Particulars;
                    existingHeaderModel.CRNo = viewModel.CRNo;
                    existingHeaderModel.JVReason = viewModel.JVReason;
                    existingHeaderModel.CreatedBy = _userManager.GetUserName(this.User);

                    #endregion --Saving the default entries

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(_userManager.GetUserName(this.User), $"Edit check voucher# {viewModel.JVNo}", "Check Voucher");
                    await _dbContext.AddAsync(auditTrail, cancellationToken);

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
    }
}