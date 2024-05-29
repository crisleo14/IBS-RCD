using Accounting_System.Data;
using Accounting_System.Models;
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
    public class CheckVoucherController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly CheckVoucherRepo _checkVoucherRepo;

        private readonly IWebHostEnvironment _webHostEnvironment;

        public CheckVoucherController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, CheckVoucherRepo checkVoucherRepo, IWebHostEnvironment webHostEnvironment)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _checkVoucherRepo = checkVoucherRepo;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var headers = await _dbContext.CheckVoucherHeaders
                .Include(s => s.Supplier)
                .ToListAsync(cancellationToken);

            var details = await _dbContext.CheckVoucherDetails
                .ToListAsync(cancellationToken);

            // Create a list to store CheckVoucherVM objectssw
            var checkVoucherVMs = new List<CheckVoucherVM>();

            // Retrieve details for each header
            foreach (var header in headers)
            {
                string cvNo = "";
                if (header.CVNo != null)
                {
                    cvNo = header.CVNo;
                }
                else
                {
                    cvNo = header.Reference + "-" + header.Sequence.ToString("D3");
                }
                var headerDetails = details.Where(d => d.TransactionNo == cvNo).ToList();

                if (header.Category == "Trade")
                {
                    var siArray = new string[header.RRNo.Length];
                    for (int i = 0; i < header.RRNo.Length; i++)
                    {
                        var rrValue = header.RRNo[i];

                        var rr = await _dbContext.ReceivingReports
                                    .FirstOrDefaultAsync(p => p.RRNo == rrValue);

                        siArray[i] = rr.SupplierInvoiceNumber;
                    }

                    ViewBag.SINoArray = siArray;
                }
                // Create a new CheckVoucherVM object for each header and its associated details
                var checkVoucherVM = new CheckVoucherVM
                {
                    Header = header,
                    Details = headerDetails
                };

                // Add the CheckVoucherVM object to the list
                checkVoucherVMs.Add(checkVoucherVM);
            }

            return View(checkVoucherVMs);
        }
        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var viewModel = new CheckVoucherVM
            {
                Header = new CheckVoucherHeader(),
                Details = new List<CheckVoucherDetail>()
            };

            viewModel.Header.COA = await _dbContext.ChartOfAccounts
                .Where(coa => !new[] { "2010102", "2010101", "1010101" }.Any(excludedNumber => coa.Number.Contains(excludedNumber)) && coa.Level == 4 || coa.Level == 5)
                .Select(s => new SelectListItem
                {
                    Value = s.Number,
                    Text = s.Number + " " + s.Name
                })
                .ToListAsync(cancellationToken);

            viewModel.Header.Suppliers = await _dbContext.Suppliers
                .Select(sup => new SelectListItem
                {
                    Value = sup.Id.ToString(),
                    Text = sup.Name
                })
                .ToListAsync();
            viewModel.Header.BankAccounts = await _dbContext.BankAccounts
                .Select(ba => new SelectListItem
                {
                    Value = ba.Id.ToString(),
                    Text = ba.AccountNo + " " + ba.AccountName
                })
                .ToListAsync();

            viewModel.Header.CheckVouchers = await _dbContext.CheckVoucherHeaders
                .Where(cvh => cvh.AccruedType == "Invoicing")
                .Select(cvh => new SelectListItem
                {
                    Value = cvh.CVNo,
                    Text = cvh.CVNo
                })
                .ToListAsync();

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CheckVoucherVM? model, CancellationToken cancellationToken, string[] accountNumber, decimal[]? debit, decimal[]? credit, string? siNo, string? poNo, decimal[] amount, decimal netOfEWT, decimal expandedWTaxDebitAmount, decimal cashInBankAmount, IFormFile? file, DateOnly? startDate, DateOnly? endDate, string? accountNoAndTitle, decimal apNonTradePayable)
        {

            model.Header.Suppliers = await _dbContext.Suppliers
                .Select(sup => new SelectListItem
                {
                    Value = sup.Id.ToString(),
                    Text = sup.Name
                })
                .ToListAsync();
            model.Header.BankAccounts = await _dbContext.BankAccounts
                .Select(ba => new SelectListItem
                {
                    Value = ba.Id.ToString(),
                    Text = ba.AccountName
                })
                .ToListAsync();
            model.Header.COA = await _dbContext.ChartOfAccounts
                .Where(coa => !new[] { "2010102", "2010101", "1010101" }.Any(excludedNumber => coa.Number.Contains(excludedNumber)) && coa.Level == 4 || coa.Level == 5)
                .Select(s => new SelectListItem
                {
                    Value = s.Number,
                    Text = s.Number + " " + s.Name
                })
                .ToListAsync(cancellationToken);

            if (ModelState.IsValid)
            {
                #region --Validating series
                var getLastNumber = await _checkVoucherRepo.GetLastSeriesNumberCV(model.Header.Reference, cancellationToken);
                var getLastSequence = await _checkVoucherRepo.GetLastSequenceNumberCV(cancellationToken);

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

                #region --Multiple input of SI and PO No.
                if (poNo != null)
                {
                    string[] inputs = poNo.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                    // Display each input
                    for (int i = 0; i < inputs.Length; i++)
                    {
                        model.Header.PONo = inputs;
                    }
                }

                if (siNo != null)
                {
                    string[] inputs = siNo.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                    // Display each input
                    for (int i = 0; i < inputs.Length; i++)
                    {
                        model.Header.SINo = inputs;
                    }
                }
                #endregion --Multiple input of SI and PO No.

                #region --Check if duplicate record
                if (model.Header.CheckNo != null && !model.Header.CheckNo.Contains("DM"))
                {
                    var cv = await _dbContext
                    .CheckVoucherHeaders
                    .Where(cv => cv.CheckNo == model.Header.CheckNo && cv.BankId == model.Header.BankId)
                    .ToListAsync(cancellationToken);
                    if (cv.Any())
                    {
                        TempData["error"] = "Check No. Is already exist";
                        return View(model);
                    }
                }
                #endregion --Check if duplicate record

                #region --Retrieve Supplier

                var existingCV = await _dbContext
                            .CheckVoucherHeaders
                            .FirstOrDefaultAsync(cvh => cvh.CVNo == model.Header.Reference, cancellationToken);

                var reference = existingCV != null ? existingCV.SupplierId : model.Header.SupplierId;

                var supplier = await _dbContext
                            .Suppliers
                            .FirstOrDefaultAsync(po => po.Id == reference, cancellationToken);

                #endregion --Retrieve Supplier

                #region -- Accrued entries --

                if (existingCV != null && model.Header.AccruedType == "Payment")
                {
                    model.Header.SINo = existingCV.SINo;
                    model.Header.PONo = existingCV.PONo;
                    model.Header.SupplierId = existingCV.SupplierId;
                    model.Header.Sequence = getLastSequence;
                }

                #endregion -- Accrued entrires --

                #region --CV Details Entry
                var generateCVNo = await _checkVoucherRepo.GenerateCVNo(cancellationToken);
                var cvDetails = new List<CheckVoucherDetail>();
                var totalNetAmountOfEWT = await _dbContext.ReceivingReports
                                .Where(rr => model.Header.RRNo.Contains(rr.RRNo))
                                .ToListAsync(cancellationToken);

                var totalAmount = 0m;
                foreach (var total in amount)
                {
                    totalAmount += total;
                }

                if (model.Header.Category == "Trade")
                {
                    cvDetails.Add(
                        new CheckVoucherDetail
                        {
                            AccountNo = "2010101",
                            AccountName = "AP-Trade Payable",
                            TransactionNo = model.Header.Reference != null ? model.Header.Reference + "-" + model.Header.Sequence.ToString("D3") : generateCVNo,
                            Debit = supplier.TaxType == "Withholding Tax" ? netOfEWT : totalAmount,
                            Credit = 0
                        }
                    );
                }

                if (supplier.TaxType == "Withholding Tax")
                {
                    if (model.Header.AccruedType != "Invoicing")
                    {
                        cvDetails.Add(
                            new CheckVoucherDetail
                            {
                                AccountNo = "2010302",
                                AccountName = "Expanded Witholding Tax 1%",
                                TransactionNo = model.Header.Reference != null ? model.Header.Reference + "-" + model.Header.Sequence.ToString("D3") : generateCVNo,
                                Debit = expandedWTaxDebitAmount,
                                Credit = 0
                            }
                        );
                        cvDetails.Add(
                            new CheckVoucherDetail
                            {
                                AccountNo = "2010302",
                                AccountName = "Expanded Witholding Tax 1%",
                                TransactionNo = model.Header.Reference != null ? model.Header.Reference + "-" + model.Header.Sequence.ToString("D3") : generateCVNo,
                                Debit = 0,
                                Credit = expandedWTaxDebitAmount
                            }
                        );
                    }
                }

                decimal totalCredit = 0m;
                for (int i = 0; i < accountNumber.Length; i++)
                {
                    var currentAccountNumber = accountNumber[i];
                    var accountTitle = await _dbContext.ChartOfAccounts
                        .FirstOrDefaultAsync(coa => coa.Number == currentAccountNumber);
                    var currentDebit = debit[i];
                    var currentCredit = credit[i];
                    totalCredit += credit[i];

                    cvDetails.Add(
                        new CheckVoucherDetail
                        {
                            AccountNo = currentAccountNumber,
                            AccountName = accountTitle.Name,
                            TransactionNo = model.Header.Reference != null ? model.Header.Reference + "-" + model.Header.Sequence.ToString("D3") : generateCVNo,
                            Debit = currentDebit,
                            Credit = currentCredit
                        }
                    );
                }
                if (model.Header.Category == "Non-Trade" && model.Header.AccruedType == "Invoicing")
                {
                    cvDetails.Add(
                        new CheckVoucherDetail
                        {
                            AccountNo = "2010101",
                            AccountName = "AP-Non Trade Payable",
                            TransactionNo = model.Header.Reference != null ? model.Header.Reference + "-" + model.Header.Sequence.ToString("D3") : generateCVNo,
                            Debit = 0,
                            Credit = apNonTradePayable - totalCredit
                        }
                    );
                }
                if (model.Header.Category == "Non-Trade" && model.Header.AccruedType == "Payment")
                {
                    cvDetails.Add(
                        new CheckVoucherDetail
                        {
                            AccountNo = "2010101",
                            AccountName = "AP-Trade Payable",
                            TransactionNo = model.Header.Reference != null ? model.Header.Reference + "-" + model.Header.Sequence.ToString("D3") : generateCVNo,
                            Debit = supplier.TaxType == "Withholding Tax" ? netOfEWT : model.Header.Amount,
                            Credit = 0
                        }
                    );
                }

                string accountNo = "";
                string accountName = "";
                if (accountNoAndTitle != null)
                {
                    string[] words = accountNoAndTitle.Split(" ");
                    string[] remainingWords = words.Skip(1).ToArray();
                    accountNo = words.First();
                    accountName = string.Join(" ", remainingWords);
                }

                if (model.Header.AccruedType != "Invoicing")
                {
                    cvDetails.Add(
                        new CheckVoucherDetail
                        {
                            AccountNo = accountNo,
                            AccountName = accountName,
                            TransactionNo = model.Header.Reference != null ? model.Header.Reference + "-" + model.Header.Sequence.ToString("D3") : generateCVNo,
                            Debit = 0,
                            Credit = cashInBankAmount
                        }
                    );
                }

                await _dbContext.AddRangeAsync(cvDetails, cancellationToken);

                #endregion --CV Details Entry

                #region -- SINo Entry
                if (model.Header.Category == "Trade")
                {
                    var siArray = new string[model.Header.RRNo.Length];
                    for (int i = 0; i < model.Header.RRNo.Length; i++)
                    {
                        var rrValue = model.Header.RRNo[i];

                        var rr = await _dbContext.ReceivingReports
                                    .FirstOrDefaultAsync(p => p.RRNo == rrValue);

                        siArray[i] = rr.SupplierInvoiceNumber;
                    }

                    model.Header.SINo = siArray;
                }
                #endregion -- SINo Entry

                #region --Saving the default entries

                //CV Header Entry
                var list = cvDetails.Where(cv => cv.TransactionNo == generateCVNo).ToList();

                model.Header.SeriesNumber = getLastNumber;
                model.Header.CVNo = model.Header.Reference == null ? generateCVNo : null;
                model.Header.CreatedBy = _userManager.GetUserName(this.User);
                model.Header.TotalDebit = list.Sum(cvd => cvd.Debit);
                model.Header.TotalCredit = list.Sum(cvd => cvd.Credit);
                model.Header.StartDate = startDate;
                model.Header.EndDate = endDate;
                int computationPerMonth = 0;
                foreach (var item in list.Where(cvd => cvd.AccountNo.StartsWith("10201") || cvd.AccountNo.StartsWith("10105")))
                {
                    var depreciationAmount = item.Debit != 0 ? item.Debit : item.Credit;

                    int year = model.Header.EndDate.Value.Year - model.Header.StartDate.Value.Year;
                    int month = model.Header.EndDate.Value.Month - model.Header.StartDate.Value.Month;
                    int result = (year * 12) + month;
                    computationPerMonth = result + 1;

                    var amountPerMonth = depreciationAmount / computationPerMonth;
                    model.Header.AmountPerMonth = amountPerMonth;

                }
                model.Header.NumberOfMonths = computationPerMonth;

                if (model.Header.TotalDebit != model.Header.TotalCredit)
                {
                    TempData["error"] = "The debit and credit should be equal!";
                    return View(model);
                }
                if (cashInBankAmount != 0)
                {
                    model.Header.Amount = cashInBankAmount;
                }

                #endregion --Saving the default entries

                #region -- Partial payment of RR's
                if (amount != null && model.Header.Category == "Trade")
                {
                    var receivingReport = new ReceivingReport();
                    for (int i = 0; i < model.Header.RRNo.Length; i++)
                    {
                        var rrValue = model.Header.RRNo[i];
                        receivingReport = await _dbContext.ReceivingReports
                                    .FirstOrDefaultAsync(p => p.RRNo == rrValue);

                        receivingReport.AmountPaid += amount[i];

                        if (receivingReport.Amount <= receivingReport.AmountPaid)
                        {
                            receivingReport.IsPaid = true;
                            receivingReport.PaidDate = DateTime.Now;
                        }
                    }
                }

                #endregion -- Partial payment of RR's

                #region -- Uploading file --
                if (file != null && file.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Supporting CV Files", model.Header.CVNo);

                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string fileName = Path.GetFileName(file.FileName);
                    string fileSavePath = Path.Combine(uploadsFolder, fileName);

                    using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    //if necessary add field to store location path
                    // model.Header.SupportingFilePath = fileSavePath
                }

                #region --Audit Trail Recording

                AuditTrail auditTrail = new(model.Header.CreatedBy, $"Create new check voucher# {model.Header.CVNo}", "Check Voucher");
                _dbContext.Add(auditTrail);

                #endregion --Audit Trail Recording

                await _dbContext.AddAsync(model.Header, cancellationToken);  // Add CheckVoucherHeader to the context
                await _dbContext.SaveChangesAsync(cancellationToken);  // await the SaveChangesAsync method
                return RedirectToAction("Index");
                #endregion -- Uploading file --

            }
            else
            {
                TempData["error"] = "The information you submitted is not valid!";
                return View(model);
            }
        }
        public async Task<IActionResult> GetPOs(int supplierId)
        {
            var purchaseOrders = await _dbContext.PurchaseOrders
                .Where(po => po.SupplierId == supplierId)
                .ToListAsync();

            if (purchaseOrders != null && purchaseOrders.Count > 0)
            {
                var poList = purchaseOrders.Select(po => new { Id = po.Id, PONumber = po.PONo }).ToList();
                return Json(poList);
            }

            return Json(null);
        }
        public async Task<IActionResult> GetRRs(string[] poNumber, string? criteria)
        {

            var receivingReports = await _dbContext.ReceivingReports
            .Where(rr => poNumber.Contains(rr.PONo) && !rr.IsPaid)
            .OrderBy(rr => criteria == "Transaction Date" ? rr.Date : rr.DueDate)
            .ToListAsync();

            if (receivingReports != null && receivingReports.Count > 0)
            {
                var rrList = receivingReports.Select(rr => new { Id = rr.Id, RRNumber = rr.RRNo }).ToList();
                return Json(rrList);
            }

            return Json(null);
        }

        public async Task<IActionResult> GetSI(int? supplierId, string? cvId)
        {
            var cvh = await _dbContext.CheckVoucherHeaders
               .FirstOrDefaultAsync(cvh => cvh.CVNo == cvId);

            var id = cvId != null ? cvh.SupplierId : supplierId;

            var supplier = await _dbContext.Suppliers
                .FirstOrDefaultAsync(po => po.Id == id);

            if (supplier != null)
            {
                var si = supplier.TaxType;
                var address = supplier.Address;
                var tinNo = supplier.TinNo;
                var name = "";
                if (cvId != null)
                {
                    name = supplier.Name;
                }
                return Json(new { TaxType = si, TinNo = tinNo, Address = address, Name = name });
            }

            return Json(null);
        }
        public async Task<IActionResult> RRBalance(string rrNo)
        {
            var receivingReport = await _dbContext.ReceivingReports
                .FirstOrDefaultAsync(rr => rr.RRNo == rrNo);
            if (receivingReport != null)
            {
                var amount = receivingReport.Amount;
                var amountPaid = receivingReport.AmountPaid;
                var netAmount = receivingReport.NetAmount;
                var vatAmount = receivingReport.VatAmount;
                var ewtAmount = receivingReport.EwtAmount;
                var balance = amount - amountPaid;

                return Json(new
                {
                    Amount = amount,
                    AmountPaid = amountPaid,
                    NetAmount = netAmount,
                    VatAmount = vatAmount,
                    EwtAmount = ewtAmount,
                    Balance = balance
                });
            }
            return Json(null);
        }

        public async Task<IActionResult> GetBankAccount(int bankId)
        {
            if (bankId != 0)
            {
                var existingBankAccount = await _dbContext.BankAccounts.FindAsync(bankId);
                return Json(new { AccountNoCOA = existingBankAccount.AccountNoCOA, AccountNo = existingBankAccount.AccountNo, AccountName = existingBankAccount.AccountName });
            }
            return Json(null);
        }

        public async Task<IActionResult> GetAutomaticEntry(DateTime startDate, DateTime? endDate)
        {
            if (startDate != default && endDate != default)
            {
                return Json(true);
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

            var header = await _dbContext.CheckVoucherHeaders
                .Include(s => s.Supplier)
                .FirstOrDefaultAsync(cvh => cvh.Id == id.Value, cancellationToken);

            if (header == null)
            {
                return NotFound();
            }

            string cvNo = "";
            if (header.CVNo != null)
            {
                cvNo = header.CVNo;
            }
            else
            {
                cvNo = header.Reference + "-" + header.Sequence.ToString("D3");
            }
            var details = await _dbContext.CheckVoucherDetails
                .Where(cvd => cvd.TransactionNo == cvNo)
                .ToListAsync(cancellationToken);


            if (header.Category == "Trade")
            {
                var siArray = new string[header.RRNo.Length];
                for (int i = 0; i < header.RRNo.Length; i++)
                {
                    var rrValue = header.RRNo[i];

                    var rr = await _dbContext.ReceivingReports
                                .FirstOrDefaultAsync(p => p.RRNo == rrValue);

                    siArray[i] = rr.SupplierInvoiceNumber;
                }

                ViewBag.SINoArray = siArray;
            }

            var viewModel = new CheckVoucherVM
            {
                Header = header,
                Details = details
            };

            return View(viewModel);
        }


        public async Task<IActionResult> Printed(int id, CancellationToken cancellationToken)
        {
            var cv = await _dbContext.CheckVoucherHeaders.FindAsync(id, cancellationToken);
            if (cv != null && !cv.IsPrinted)
            {
                #region --Audit Trail Recording

                var printedBy = _userManager.GetUserName(this.User);
                AuditTrail auditTrail = new(printedBy, $"Printed original copy of cv# {cv.CVNo}", "Check Vouchers");
                await _dbContext.AddAsync(auditTrail, cancellationToken);

                #endregion --Audit Trail Recording

                cv.IsPrinted = true;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            return RedirectToAction("Print", new { id = id });
        }

        public async Task<IActionResult> Post(int cvId, CancellationToken cancellationToken)
        {
            var modelHeader = await _dbContext.CheckVoucherHeaders.FindAsync(cvId, cancellationToken);
            var modelDetails = await _dbContext.CheckVoucherDetails.Where(cvd => cvd.TransactionNo == modelHeader.CVNo).ToListAsync();

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
                                    Date = modelHeader.Date,
                                    Reference = modelHeader.CVNo,
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

                    var disbursement = new List<DisbursementBook>();
                    foreach (var details in modelDetails)
                    {
                        var bank = _dbContext.BankAccounts.FirstOrDefault(model => model.Id == modelHeader.BankId);
                        disbursement.Add(
                                new DisbursementBook
                                {
                                    Date = modelHeader.Date,
                                    CVNo = modelHeader.CVNo,
                                    Payee = modelHeader.Payee,
                                    Amount = modelHeader.Amount,
                                    Particulars = modelHeader.Particulars,
                                    Bank = bank.Branch,
                                    CheckNo = modelHeader.CheckNo,
                                    CheckDate = modelHeader.CheckDate.ToShortDateString(),
                                    DateCleared = DateTime.Now.ToShortDateString(),
                                    ChartOfAccount = details.AccountNo + " " + details.AccountName,
                                    Debit = details.Debit,
                                    Credit = details.Credit,
                                    CreatedBy = modelHeader.CreatedBy,
                                    CreatedDate = modelHeader.CreatedDate
                                }
                            );
                    }

                    await _dbContext.DisbursementBooks.AddRangeAsync(disbursement, cancellationToken);

                    #endregion --Disbursement Book Recording(CV)--

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(modelHeader.PostedBy, $"Posted check voucher# {modelHeader.CVNo}", "Check Voucher");
                    await _dbContext.AddAsync(auditTrail, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Check Voucher has been Posted.";
                }
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id, CheckVoucherVM model, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            var existingHeaderModel = await _dbContext.CheckVoucherHeaders.FindAsync(id, cancellationToken);
            var existingDetailsModel = await _dbContext.CheckVoucherDetails
                .Where(cvd => cvd.TransactionNo == existingHeaderModel.CVNo)
                .ToListAsync();

            if (existingHeaderModel == null || existingDetailsModel == null)
            {
                return NotFound();
            }

            existingHeaderModel.RR = await _dbContext.ReceivingReports
                .Select(rr => new SelectListItem
                {
                    Value = rr.Id.ToString(),
                    Text = rr.RRNo
                })
                .ToListAsync(cancellationToken);

            existingHeaderModel.COA = await _dbContext.ChartOfAccounts
                .Select(coa => new SelectListItem
                {
                    Value = coa.Id.ToString(),
                    Text = coa.Number + " " + coa.Name
                })
                .ToListAsync(cancellationToken);

            model.Header = existingHeaderModel; // Assign the updated header model to the view model
            model.Details = existingDetailsModel; // Assign the updated details model to the view model

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(CheckVoucherVM model, CancellationToken cancellationToken)
        {
            model.Header.RR = await _dbContext.ReceivingReports
               .Select(rr => new SelectListItem
               {
                   Value = rr.Id.ToString(),
                   Text = rr.RRNo
               })
               .ToListAsync(cancellationToken);

            model.Header.COA = await _dbContext.ChartOfAccounts
                .Select(coa => new SelectListItem
                {
                    Value = coa.Id.ToString(),
                    Text = coa.Number + " " + coa.Name
                })
                .ToListAsync(cancellationToken);



            if (ModelState.IsValid)
            {
                var getLastNumber = await _checkVoucherRepo.GetLastSeriesNumberCV(model.Header.Reference, cancellationToken);

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

                var existingHeaderModel = await _dbContext.CheckVoucherHeaders.FindAsync(model.Header.Id, cancellationToken);
                var existingDetailsModel = await _dbContext.CheckVoucherDetails
                    .Where(cvd => cvd.TransactionNo == existingHeaderModel.CVNo)
                    .ToListAsync();

                if (existingHeaderModel == null)
                {
                    return NotFound();
                }
                if (existingDetailsModel == null)
                {
                    return NotFound();
                }

                //CV Header Entry
                var generateCVNo = await _checkVoucherRepo.GenerateCVNo(cancellationToken);

                existingHeaderModel.SeriesNumber = model.Header.SeriesNumber;
                existingHeaderModel.CVNo = model.Header.CVNo;
                existingHeaderModel.CreatedBy = model.Header.CreatedBy;

                await _dbContext.SaveChangesAsync(cancellationToken);  // await the SaveChangesAsync method
                return RedirectToAction("Index");
            }
            else
            {
                TempData["error"] = "The information you submitted is not valid!";
                return View(model);
            }
        }

        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {
            var model = await _dbContext.CheckVoucherHeaders.FindAsync(id, cancellationToken);

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

                    AuditTrail auditTrail = new(model.VoidedBy, $"Voided check voucher# {model.CVNo}", "Check Voucher");
                    await _dbContext.AddAsync(auditTrail, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Check Voucher has been Voided.";
                }
                return RedirectToAction("Index");
            }

            return NotFound();
        }

        public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
        {
            var model = await _dbContext.CheckVoucherHeaders.FindAsync(id, cancellationToken);

            if (model != null)
            {
                if (!model.IsCanceled)
                {
                    model.IsCanceled = true;
                    model.CanceledBy = _userManager.GetUserName(this.User);
                    model.CanceledDate = DateTime.Now;

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(model.CanceledBy, $"Cancelled check voucher# {model.CVNo}", "Check Voucher");
                    await _dbContext.AddAsync(auditTrail, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Check Voucher has been Cancelled.";
                }
                return RedirectToAction("Index");
            }

            return NotFound();
        }
    }
}