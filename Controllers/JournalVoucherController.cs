using Accounting_System.Data;
using Accounting_System.Models;
using Accounting_System.Repository;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Threading;

namespace Accounting_System.Controllers
{
    public class JournalVoucherController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly JournalVoucherRepo _journalVoucherRepo;

        private readonly IWebHostEnvironment _webHostEnvironment;

        public JournalVoucherController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, IWebHostEnvironment webHostEnvironment, JournalVoucherRepo journalVoucherRepo)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
            _journalVoucherRepo = journalVoucherRepo;
        }
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var headers = await _dbContext.JournalVoucherHeaders
                .Include(cvh => cvh.CheckVoucherHeader)
                .ThenInclude(supplier => supplier.Supplier)
                .OrderByDescending(jv => jv.Id)
                .ToListAsync(cancellationToken);

            // Create a list to store CheckVoucherVM objectssw
            var journalVoucherVMs = new List<JournalVoucherVM>();

            // Retrieve details for each header
            foreach (var header in headers)
            {
                var headerJVNo = header.JVNo;
                var headerDetails = await _dbContext.JournalVoucherDetails.Where(d => d.TransactionNo == headerJVNo).ToListAsync(cancellationToken);

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
        public async Task<IActionResult> Create(JournalVoucherVM? model, CancellationToken cancellationToken, string[] accountNumber, decimal[]? debit, decimal[]? credit)
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
                        .FirstOrDefaultAsync(coa => coa.Number == currentAccountNumber);
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


                await _dbContext.AddAsync(model.Header, cancellationToken);  // Add CheckVoucherHeader to the context
                await _dbContext.SaveChangesAsync(cancellationToken);
                return RedirectToAction("Index");
            }
            else
            {
                TempData["error"] = "The information you submitted is not valid!";
                return View(model);
            }
        }

        public async Task<IActionResult> GetCV(int id)
        {
            var header = _dbContext.CheckVoucherHeaders
                .Include(s => s.Supplier)
                .FirstOrDefault(cvh => cvh.Id == id);

            if (header == null)
            {
                return NotFound();
            }

            var details = await _dbContext.CheckVoucherDetails
                .Where(cvd => cvd.TransactionNo == header.CVNo)
                .ToListAsync();

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
                var amount = viewModel.Header.Amount;
                var particulars = viewModel.Header.Particulars;
                var checkNo = viewModel.Header.CheckNo;
                var totalDebit = viewModel.Header.TotalDebit;
                var totalCredit = viewModel.Header.TotalCredit;

                return Json(new { CVNo = cvNo,
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
                    TotalCredit = totalCredit
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
                jv.IsPrinted = true;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            return RedirectToAction("Print", new { id = id });
        }

        public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
        {
            var modelHeader = await _dbContext.JournalVoucherHeaders.FindAsync(id, cancellationToken);
            var modelDetails = await _dbContext.JournalVoucherDetails.Where(jvd => jvd.TransactionNo == modelHeader.JVNo).ToListAsync();

            if (modelHeader != null)
            {
                if (!modelHeader.IsPosted)
                {
                    modelHeader.IsPosted = true;
                    modelHeader.PostedBy = _userManager.GetUserName(this.User);
                    modelHeader.PostedDate = DateTime.Now;

                    #region --General Ledger Book Recording(CV)--

                    var ledgers = new List<GeneralLedgerBook>();
                    foreach (var details in modelDetails)
                    {
                        ledgers.Add(
                                new GeneralLedgerBook
                                {
                                    Date = modelHeader.Date.ToShortDateString(),
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

                    await _dbContext.GeneralLedgerBooks.AddRangeAsync(ledgers, cancellationToken);

                    #endregion --General Ledger Book Recording(CV)--

                    #region --Disbursement Book Recording(CV)--

                    var journalBook = new List<JournalBook>();
                    foreach (var details in modelDetails)
                    {
                        journalBook.Add(
                                new JournalBook
                                {
                                    Date = modelHeader.Date.ToShortDateString(),
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

                    #endregion --Disbursement Book Recording(CV)--

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(modelHeader.PostedBy, $"Posted journal voucher# {modelHeader.JVNo}", "Journal Voucher");
                    await _dbContext.AddAsync(auditTrail, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Journal Voucher has been Posted.";
                }
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }
        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {
            var model = await _dbContext.JournalVoucherHeaders.FindAsync(id, cancellationToken);

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

                    AuditTrail auditTrail = new(model.VoidedBy, $"Voided journal voucher# {model.JVNo}", "Journal Voucher");
                    await _dbContext.AddAsync(auditTrail, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Journal Voucher has been Voided.";
                }
                return RedirectToAction("Index");
            }

            return NotFound();
        }

        public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
        {
            var model = await _dbContext.JournalVoucherHeaders.FindAsync(id, cancellationToken);

            if (model != null)
            {
                if (!model.IsCanceled)
                {
                    model.IsCanceled = true;
                    model.CanceledBy = _userManager.GetUserName(this.User);
                    model.CanceledDate = DateTime.Now;

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(model.CanceledBy, $"Cancelled journal voucher# {model.JVNo}", "Journal Voucher");
                    await _dbContext.AddAsync(auditTrail, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Journal Voucher has been Cancelled.";
                }
                return RedirectToAction("Index");
            }

            return NotFound();
        }


    }
}
