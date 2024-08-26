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
    public class CheckVoucherController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly CheckVoucherRepo _checkVoucherRepo;

        private readonly IWebHostEnvironment _webHostEnvironment;

        private readonly GeneralRepo _generalRepo;

        public CheckVoucherController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, CheckVoucherRepo checkVoucherRepo, IWebHostEnvironment webHostEnvironment, GeneralRepo generalRepo)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _checkVoucherRepo = checkVoucherRepo;
            _webHostEnvironment = webHostEnvironment;
            _generalRepo = generalRepo;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var headers = await _dbContext.CheckVoucherHeaders
                .Include(s => s.Supplier)
                .ToListAsync(cancellationToken);

            var details = await _dbContext.CheckVoucherDetails
                .ToListAsync(cancellationToken);

            // Create a list to store CheckVoucherVM objects
            var checkVoucherVMs = new List<CheckVoucherVM>();

            // Retrieve details for each header
            foreach (var header in headers)
            {
                var headerDetails = details.Where(d => d.TransactionNo == header.CVNo).ToList();

                if (header.Category == "Trade" && header.RRNo != null)
                {
                    var siArray = new string[header.RRNo.Length];
                    for (int i = 0; i < header.RRNo.Length; i++)
                    {
                        var rrValue = header.RRNo[i];

                        var rr = await _dbContext.ReceivingReports
                                    .FirstOrDefaultAsync(p => p.RRNo == rrValue);
                        if (rr != null)
                        {
                            siArray[i] = rr.SupplierInvoiceNumber;
                        }
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

        public async Task<IActionResult> GetPOs(int supplierId, CancellationToken cancellationToken)
        {
            var purchaseOrders = await _dbContext.PurchaseOrders
                .Where(po => po.SupplierId == supplierId && po.IsPosted)
                .ToListAsync(cancellationToken);

            if (purchaseOrders != null && purchaseOrders.Count > 0)
            {
                var poList = purchaseOrders.Select(po => new { Id = po.Id, PONumber = po.PONo }).ToList();
                return Json(poList);
            }

            return Json(null);
        }

        public async Task<IActionResult> GetRRs(string[] poNumber, string? criteria, CancellationToken cancellationToken)
        {
            var receivingReports = await _dbContext.ReceivingReports
            .Where(rr => poNumber.Contains(rr.PONo) && !rr.IsPaid && rr.IsPosted)
            .OrderBy(rr => criteria == "Transaction Date" ? rr.Date : rr.DueDate)
            .ToListAsync(cancellationToken);

            if (receivingReports != null && receivingReports.Count > 0)
            {
                var rrList = receivingReports.Select(rr => new { Id = rr.Id, RRNumber = rr.RRNo }).ToList();
                return Json(rrList);
            }

            return Json(null);
        }

        public async Task<IActionResult> GetSI(int? supplierId, string? cvId, CancellationToken cancellationToken)
        {
            var cvh = await _dbContext.CheckVoucherHeaders
               .FirstOrDefaultAsync(cvh => cvh.CVNo == cvId, cancellationToken);

            var id = cvId != null ? cvh.SupplierId : supplierId;

            var supplier = await _dbContext.Suppliers
                .FirstOrDefaultAsync(po => po.Id == id, cancellationToken);

            if (supplier != null)
            {
                var si = supplier.TaxType;
                var address = supplier.Address;
                var tinNo = supplier.TinNo;
                var name = "";
                if (cvId != null || supplierId != null)
                {
                    name = supplier.Name;
                }
                return Json(new { TaxType = si, TinNo = tinNo, Address = address, Name = name });
            }

            return Json(null);
        }

        public async Task<IActionResult> GetSupplierDetails(int? supplierId, CancellationToken cancellationToken)
        {
            if (supplierId != null)
            {
                var supplier = await _dbContext.Suppliers
                    .FindAsync(supplierId, cancellationToken);

                if (supplier != null)
                {
                    return Json(new
                    {
                        SupplierName = supplier.Name,
                        SupplierAddress = supplier.Address,
                        SupplierTinNo = supplier.TinNo,
                        TaxType = supplier.TaxType,
                        Category = supplier.Category,
                        TaxPercent = supplier.WithholdingTaxPercent,
                        VatType = supplier.VatType,
                        DefaultExpense = supplier.DefaultExpenseNumber,
                        WithholdingTax = supplier.WithholdingTaxtitle

                    });
                }
                return Json(null);
            }
            return Json(null);
        }

        public async Task<IActionResult> RRBalance(string rrNo, CancellationToken cancellationToken)
        {
            var receivingReport = await _dbContext.ReceivingReports
                .FirstOrDefaultAsync(rr => rr.RRNo == rrNo, cancellationToken);
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

        public async Task<IActionResult> GetBankAccount(int bankId, CancellationToken cancellationToken)
        {
            if (bankId != 0)
            {
                var existingBankAccount = await _dbContext.BankAccounts.FindAsync(bankId, cancellationToken);
                return Json(new { AccountNoCOA = existingBankAccount.AccountNoCOA, AccountNo = existingBankAccount.AccountNo, AccountName = existingBankAccount.AccountName });
            }
            return Json(null);
        }

        public IActionResult GetAutomaticEntry(DateTime startDate, DateTime? endDate)
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

            var details = await _dbContext.CheckVoucherDetails
                .Where(cvd => cvd.TransactionNo == header.CVNo)
                .ToListAsync(cancellationToken);

            if (header.Category == "Trade" && header.RRNo != null)
            {
                var siArray = new string[header.RRNo.Length];
                for (int i = 0; i < header.RRNo.Length; i++)
                {
                    var rrValue = header.RRNo[i];

                    var rr = await _dbContext.ReceivingReports
                                .FirstOrDefaultAsync(p => p.RRNo == rrValue, cancellationToken);

                    if (rr != null)
                    {
                        siArray[i] = rr.SupplierInvoiceNumber;
                    }
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
            return RedirectToAction(nameof(Print), new { id });
        }

        public async Task<IActionResult> Post(int cvId, CancellationToken cancellationToken)
        {
            var modelHeader = await _dbContext.CheckVoucherHeaders.FindAsync(cvId, cancellationToken);
            if (modelHeader != null)
            {
                var modelDetails = await _dbContext.CheckVoucherDetails.Where(cvd => cvd.TransactionNo == modelHeader.CVNo).ToListAsync(cancellationToken);
                try
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

                        if (!_generalRepo.IsDebitCreditBalanced(ledgers))
                        {
                            throw new ArgumentException("Debit and Credit is not equal, check your entries.");
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
                                        Amount = modelHeader.Total,
                                        Particulars = modelHeader.Particulars,
                                        Bank = bank != null ? bank.Branch : "N/A",
                                        CheckNo = !string.IsNullOrEmpty(modelHeader.CheckNo) ? modelHeader.CheckNo : "N/A",
                                        CheckDate = modelHeader.CheckDate != null ? modelHeader.CheckDate?.ToString("MM/dd/yyyy") : "N/A",
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
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(Index));
                }
            }

            return NotFound();
        }

        [HttpGet]
        public async Task<IActionResult> EditTrade(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }
            var exisitngCV = await _dbContext.CheckVoucherHeaders.FindAsync(id, cancellationToken);
            var existingHeaderModel = await _dbContext.CheckVoucherHeaders
                .Include(supp => supp.Supplier)
                .FirstOrDefaultAsync(cvh => cvh.Id == id, cancellationToken);
            var existingDetailsModel = await _dbContext.CheckVoucherDetails
                .Where(cvd => cvd.TransactionNo == existingHeaderModel.CVNo)
                .ToListAsync(cancellationToken);

            if (existingHeaderModel == null || existingDetailsModel == null)
            {
                return NotFound();
            }

            var accountNumbers = existingDetailsModel.Select(model => model.AccountNo).ToArray();
            var accountTitles = existingDetailsModel.Select(model => model.AccountName).ToArray();
            var debit = existingDetailsModel.Select(model => model.Debit).ToArray();
            var credit = existingDetailsModel.Select(model => model.Credit).ToArray();
            var poIds = _dbContext.PurchaseOrders.Where(model => exisitngCV.PONo.Contains(model.PONo)).Select(model => model.Id).ToArray();
            var rrIds = _dbContext.ReceivingReports.Where(model => exisitngCV.RRNo.Contains(model.RRNo)).Select(model => model.Id).ToArray();

            var coa = await _dbContext.ChartOfAccounts
                        .Where(coa => !new[] { "2010102", "2010101", "1010101" }.Any(excludedNumber => coa.Number.Contains(excludedNumber)) && coa.Level == 4 || coa.Level == 5)
                        .Select(s => new SelectListItem
                        {
                            Value = s.Number,
                            Text = s.Number + " " + s.Name
                        })
                        .ToListAsync(cancellationToken);

            CheckVoucherTradeViewModel model = new()
            {
                SupplierId = existingHeaderModel.SupplierId ?? 0,
                Payee = existingHeaderModel.Payee,
                SupplierAddress = existingHeaderModel.Supplier.Address,
                SupplierTinNo = existingHeaderModel.Supplier.TinNo,
                Suppliers = await _generalRepo.GetSupplierListAsync(cancellationToken),
                RRSeries = existingHeaderModel.RRNo,
                RR = await _generalRepo.GetReceivingReportListAsync(existingHeaderModel.RRNo, cancellationToken),
                POSeries = existingHeaderModel.PONo,
                PONo = await _generalRepo.GetPurchaseOrderListAsync(cancellationToken),
                TransactionDate = existingHeaderModel.Date,
                BankAccounts = await _generalRepo.GetBankAccountListAsync(cancellationToken),
                BankId = existingHeaderModel.BankId,
                CheckNo = existingHeaderModel.CheckNo,
                CheckDate = existingHeaderModel.CheckDate ?? DateOnly.MinValue,
                Particulars = existingHeaderModel.Particulars,
                Amount = existingHeaderModel.Amount,
                AccountNumber = accountNumbers,
                AccountTitle = accountTitles,
                Debit = debit,
                Credit = credit,
                COA = coa,
                CVId = exisitngCV.Id,
                CVNo = exisitngCV.CVNo,
                CreatedBy = _userManager.GetUserName(this.User),
                POId = poIds,
                RRId = rrIds

            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditTrade(CheckVoucherTradeViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    #region --Check if duplicate CheckNo
                    var existingHeaderModel = await _dbContext.CheckVoucherHeaders.FindAsync(viewModel.CVId, cancellationToken);

                    if (viewModel.CheckNo != null && !viewModel.CheckNo.Contains("DM"))
                    {
                        var cv = await _dbContext
                        .CheckVoucherHeaders
                        .Where(cv => cv.BankId == viewModel.BankId && cv.CheckNo == viewModel.CheckNo && !cv.CheckNo.Equals(existingHeaderModel.CheckNo))
                        .ToListAsync(cancellationToken);
                        if (cv.Any())
                        {
                            TempData["error"] = "Check No. Is already exist";
                            return View(viewModel);
                        }
                    }
                    #endregion --Check if duplicate CheckNo

                    #region --CV Details Entry

                    var existingDetailsModel = await _dbContext.CheckVoucherDetails.Where(d => d.TransactionNo == existingHeaderModel.CVNo).ToListAsync(cancellationToken);

                    // Dictionary to keep track of AccountNo and their ids for comparison
                    var accountTitleDict = new Dictionary<string, List<int>>();
                    foreach (var details in existingDetailsModel)
                    {
                        if (!accountTitleDict.ContainsKey(details.AccountNo))
                        {
                            accountTitleDict[details.AccountNo] = new List<int>();
                        }
                        accountTitleDict[details.AccountNo].Add(details.Id);
                    }

                    var cashInBank = 0m;
                    // Add or update records
                    for (int i = 0; i < viewModel.AccountTitle.Length; i++)
                    {
                        cashInBank = viewModel.Credit[3];

                        if (accountTitleDict.TryGetValue(viewModel.AccountNumber[i], out var ids))
                        {
                            // Update the first matching record and remove it from the list
                            var detailsId = ids.First();
                            ids.RemoveAt(0);
                            var details = existingDetailsModel.First(o => o.Id == detailsId);

                            details.AccountNo = viewModel.AccountNumber[i];
                            details.AccountName = viewModel.AccountTitle[i];
                            details.Debit = viewModel.Debit[i];
                            details.Credit = viewModel.Credit[i];
                            details.TransactionNo = viewModel.CVNo;

                            if (ids.Count == 0)
                            {
                                accountTitleDict.Remove(viewModel.AccountNumber[i]);
                            }
                        }
                        else
                        {
                            // Add new record
                            var newDetails = new CheckVoucherDetail
                            {
                                AccountNo = viewModel.AccountNumber[i],
                                AccountName = viewModel.AccountTitle[i],
                                Debit = viewModel.Debit[i],
                                Credit = viewModel.Credit[i],
                                TransactionNo = viewModel.CVNo
                            };
                            await _dbContext.CheckVoucherDetails.AddAsync(newDetails, cancellationToken);
                        }
                    }

                    // Remove remaining records that were duplicates
                    foreach (var ids in accountTitleDict.Values)
                    {
                        foreach (var id in ids)
                        {
                            var details = existingDetailsModel.First(o => o.Id == id);
                            _dbContext.CheckVoucherDetails.Remove(details);
                        }
                    }

                    #endregion --CV Details Entry

                    #region -- Partial payment of RR's

                    if (viewModel.Amount != null)
                    {
                        var receivingReport = new ReceivingReport();
                        for (int i = 0; i < viewModel.RRSeries.Length; i++)
                        {
                            var rrValue = viewModel.RRSeries[i];
                            receivingReport = await _dbContext.ReceivingReports
                                        .FirstOrDefaultAsync(p => p.RRNo == rrValue, cancellationToken);

                            if (i < existingHeaderModel.Amount.Length)
                            {
                                var amount = Math.Round(viewModel.Amount[i] - existingHeaderModel.Amount[i], 2);
                                receivingReport.AmountPaid += amount;
                            }
                            else
                            {
                                receivingReport.AmountPaid += viewModel.Amount[i];
                            }

                            if (receivingReport.Amount <= receivingReport.AmountPaid)
                            {
                                receivingReport.IsPaid = true;
                                receivingReport.PaidDate = DateTime.Now;
                            }
                            else
                            {
                                receivingReport.IsPaid = false;
                                receivingReport.PaidDate = DateTime.MaxValue;
                            }
                        }
                    }

                    #endregion -- Partial payment of RR's

                    #region --Saving the default entries

                    existingHeaderModel.CVNo = viewModel.CVNo;
                    existingHeaderModel.Date = viewModel.TransactionDate;
                    existingHeaderModel.RRNo = viewModel.RRSeries;
                    existingHeaderModel.PONo = viewModel.POSeries;
                    existingHeaderModel.SupplierId = viewModel.SupplierId;
                    existingHeaderModel.Particulars = viewModel.Particulars;
                    existingHeaderModel.BankId = viewModel.BankId;
                    existingHeaderModel.CheckNo = viewModel.CheckNo;
                    existingHeaderModel.Category = "Trade";
                    existingHeaderModel.Payee = viewModel.Payee;
                    existingHeaderModel.CheckDate = viewModel.CheckDate;
                    existingHeaderModel.Total = cashInBank;
                    existingHeaderModel.Amount = viewModel.Amount;
                    existingHeaderModel.CreatedBy = viewModel.CreatedBy;

                    #endregion --Saving the default entries

                    #region -- Uploading file --

                    if (file != null && file.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Supporting CV Files", viewModel.CVNo);

                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        string fileName = Path.GetFileName(file.FileName);
                        string fileSavePath = Path.Combine(uploadsFolder, fileName);

                        using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream, cancellationToken);
                        }

                        //if necessary add field to store location path
                        // model.Header.SupportingFilePath = fileSavePath
                    }

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(viewModel.CreatedBy, $"Create new check voucher# {viewModel.CVNo}", "Check Voucher");
                    await _dbContext.AddAsync(auditTrail, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);  // await the SaveChangesAsync method
                    TempData["success"] = "Trade edited successfully";
                    return RedirectToAction(nameof(Index));
                    #endregion -- Uploading file --
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

        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {
            var model = await _dbContext.CheckVoucherHeaders.FindAsync(id, cancellationToken);

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

                    await _generalRepo.RemoveRecords<DisbursementBook>(db => db.CVNo == model.CVNo, cancellationToken);
                    await _generalRepo.RemoveRecords<GeneralLedgerBook>(gl => gl.Reference == model.CVNo, cancellationToken);

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(model.VoidedBy, $"Voided check voucher# {model.CVNo}", "Check Voucher");
                    await _dbContext.AddAsync(auditTrail, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Check Voucher has been Voided.";
                }
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        public async Task<IActionResult> Cancel(int id, string cancellationRemarks, CancellationToken cancellationToken)
        {
            var model = await _dbContext.CheckVoucherHeaders.FindAsync(id, cancellationToken);

            if (model != null)
            {
                if (!model.IsCanceled)
                {
                    model.IsCanceled = true;
                    model.CanceledBy = _userManager.GetUserName(this.User);
                    model.CanceledDate = DateTime.Now;
                    model.CancellationRemarks = cancellationRemarks;

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(model.CanceledBy, $"Cancelled check voucher# {model.CVNo}", "Check Voucher");
                    await _dbContext.AddAsync(auditTrail, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Check Voucher has been Cancelled.";
                }
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        [HttpGet]
        public async Task<IActionResult> Trade(CancellationToken cancellationToken)
        {
            CheckVoucherTradeViewModel model = new();
            model.COA = await _dbContext.ChartOfAccounts
                .Where(coa => !new[] { "2010102", "2010101", "1010101" }.Any(excludedNumber => coa.Number.Contains(excludedNumber)) && coa.Level == 4 || coa.Level == 5)
                .Select(s => new SelectListItem
                {
                    Value = s.Number,
                    Text = s.Number + " " + s.Name
                })
                .ToListAsync(cancellationToken);

            model.Suppliers = await _dbContext.Suppliers
                .Where(supp => supp.Category == "Trade")
                .Select(sup => new SelectListItem
                {
                    Value = sup.Id.ToString(),
                    Text = sup.Name
                })
                .ToListAsync(cancellationToken);
            model.BankAccounts = await _dbContext.BankAccounts
                .Select(ba => new SelectListItem
                {
                    Value = ba.Id.ToString(),
                    Text = ba.AccountNo + " " + ba.AccountName
                })
                .ToListAsync(cancellationToken);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Trade(CheckVoucherTradeViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    #region --Validating series
                    var getLastNumber = await _checkVoucherRepo.GetLastSeriesNumberCV(cancellationToken);

                    if (getLastNumber > 9999999999)
                    {
                        TempData["error"] = "You reached the maximum Series Number";
                        return View(viewModel);
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

                    #region --Check if duplicate record
                    if (viewModel.CheckNo != null && !viewModel.CheckNo.Contains("DM"))
                    {
                        var cv = await _dbContext
                        .CheckVoucherHeaders
                        .Where(cv => cv.CheckNo == viewModel.CheckNo && cv.BankId == viewModel.BankId)
                        .ToListAsync(cancellationToken);
                        if (cv.Any())
                        {
                            viewModel.COA = await _dbContext.ChartOfAccounts
                                .Where(coa => !new[] { "2010102", "2010101", "1010101" }.Any(excludedNumber => coa.Number.Contains(excludedNumber)) && coa.Level == 4 || coa.Level == 5)
                                .Select(s => new SelectListItem
                                {
                                    Value = s.Number,
                                    Text = s.Number + " " + s.Name
                                })
                                .ToListAsync(cancellationToken);

                            viewModel.Suppliers = await _dbContext.Suppliers
                                .Where(supp => supp.Category == "Trade")
                                .Select(sup => new SelectListItem
                                {
                                    Value = sup.Id.ToString(),
                                    Text = sup.Name
                                })
                                .ToListAsync(cancellationToken);

                            viewModel.PONo = await _dbContext.PurchaseOrders
                                .Where(po => po.SupplierId == viewModel.SupplierId && po.IsPosted)
                                .Select(po => new SelectListItem
                                {
                                    Value = po.PONo.ToString(),
                                    Text = po.PONo
                                })
                                .ToListAsync(cancellationToken);

                            viewModel.RR = await _dbContext.ReceivingReports
                                .Where(rr => viewModel.POSeries.Contains(rr.PONo) && !rr.IsPaid && rr.IsPosted)
                                .Select(rr => new SelectListItem
                                {
                                    Value = rr.RRNo.ToString(),
                                    Text = rr.RRNo
                                })
                                .ToListAsync(cancellationToken);

                            viewModel.BankAccounts = await _dbContext.BankAccounts
                                .Select(ba => new SelectListItem
                                {
                                    Value = ba.Id.ToString(),
                                    Text = ba.AccountNo + " " + ba.AccountName
                                })
                                .ToListAsync(cancellationToken);

                            TempData["error"] = "Check No. Is already exist";
                            return View(viewModel);
                        }
                    }
                    #endregion --Check if duplicate record

                    #region --Retrieve Supplier
                    var supplier = await _dbContext
                                .Suppliers
                                .FirstOrDefaultAsync(po => po.Id == viewModel.SupplierId, cancellationToken);

                    #endregion --Retrieve Supplier

                    #region --CV Details Entry
                    var generateCVNo = await _checkVoucherRepo.GenerateCVNo(cancellationToken);
                    var cvDetails = new List<CheckVoucherDetail>();
                    var cashInBank = 0m;
                    for (int i = 0; i < viewModel.AccountNumber.Length; i++)
                    {
                        if (viewModel.Debit[i] != 0 || viewModel.Credit[i] != 0)
                        {
                            cashInBank = viewModel.Credit[3];
                            cvDetails.Add(
                            new CheckVoucherDetail
                            {
                                AccountNo = viewModel.AccountNumber[i],
                                AccountName = viewModel.AccountTitle[i],
                                Debit = viewModel.Debit[i],
                                Credit = viewModel.Credit[i],
                                TransactionNo = generateCVNo
                            });
                        }
                    }

                    await _dbContext.CheckVoucherDetails.AddRangeAsync(cvDetails, cancellationToken);
                    #endregion --CV Details Entry

                    #region --Saving the default entries
                    var cvh = new List<CheckVoucherHeader>();
                    cvh.Add(
                            new CheckVoucherHeader
                            {
                                CVNo = generateCVNo,
                                SeriesNumber = getLastNumber,
                                Date = viewModel.TransactionDate,
                                RRNo = viewModel.RRSeries,
                                PONo = viewModel.POSeries,
                                SupplierId = viewModel.SupplierId,
                                Particulars = viewModel.Particulars,
                                BankId = viewModel.BankId,
                                CheckNo = viewModel.CheckNo,
                                Category = "Trade",
                                Payee = viewModel.Payee,
                                CheckDate = viewModel.CheckDate,
                                Total = cashInBank,
                                Amount = viewModel.Amount,
                                CreatedBy = _userManager.GetUserName(this.User)
                            }
                    );

                    await _dbContext.CheckVoucherHeaders.AddRangeAsync(cvh, cancellationToken);

                    #endregion --Saving the default entries

                    #region -- Partial payment of RR's
                    if (viewModel.Amount != null)
                    {
                        var receivingReport = new ReceivingReport();
                        for (int i = 0; i < viewModel.RRSeries.Length; i++)
                        {
                            var rrValue = viewModel.RRSeries[i];
                            receivingReport = await _dbContext.ReceivingReports
                                        .FirstOrDefaultAsync(p => p.RRNo == rrValue, cancellationToken);

                            receivingReport.AmountPaid += viewModel.Amount[i];

                            if (receivingReport.Amount <= receivingReport.AmountPaid)
                            {
                                receivingReport.IsPaid = true;
                                receivingReport.PaidDate = DateTime.Now;
                            }
                        }
                    }

                    #endregion -- Partial payment of RR's

                    #region -- Uploading file --
                    foreach (var item in cvh.ToList())
                    {
                        if (file != null && file.Length > 0)
                        {
                            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Supporting CV Files", item.CVNo);

                            if (!Directory.Exists(uploadsFolder))
                            {
                                Directory.CreateDirectory(uploadsFolder);
                            }

                            string fileName = Path.GetFileName(file.FileName);
                            string fileSavePath = Path.Combine(uploadsFolder, fileName);

                            using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream, cancellationToken);
                            }

                            //if necessary add field to store location path
                            // model.Header.SupportingFilePath = fileSavePath
                        }

                        #region --Audit Trail Recording

                        AuditTrail auditTrail = new(item.CreatedBy, $"Create new check voucher# {item.CVNo}", "Check Voucher");
                        await _dbContext.AddAsync(auditTrail, cancellationToken);

                        #endregion --Audit Trail Recording
                    }
                    await _dbContext.SaveChangesAsync(cancellationToken);  // await the SaveChangesAsync method
                    return RedirectToAction(nameof(Index));
                    #endregion -- Uploading file --
                }
                catch (Exception ex)
                {
                    viewModel.COA = await _dbContext.ChartOfAccounts
                        .Where(coa => !new[] { "2010102", "2010101", "1010101" }.Any(excludedNumber => coa.Number.Contains(excludedNumber)) && coa.Level == 4 || coa.Level == 5)
                        .Select(s => new SelectListItem
                        {
                            Value = s.Number,
                            Text = s.Number + " " + s.Name
                        })
                        .ToListAsync(cancellationToken);

                    viewModel.Suppliers = await _dbContext.Suppliers
                            .Where(supp => supp.Category == "Trade")
                            .Select(sup => new SelectListItem
                            {
                                Value = sup.Id.ToString(),
                                Text = sup.Name
                            })
                            .ToListAsync(cancellationToken);

                    viewModel.PONo = await _dbContext.PurchaseOrders
                                .Where(po => po.SupplierId == viewModel.SupplierId && po.IsPosted)
                                .Select(po => new SelectListItem
                                {
                                    Value = po.PONo.ToString(),
                                    Text = po.PONo
                                })
                                .ToListAsync(cancellationToken);

                    viewModel.RR = await _dbContext.ReceivingReports
                        .Where(rr => viewModel.POSeries.Contains(rr.PONo) && !rr.IsPaid && rr.IsPosted)
                        .Select(rr => new SelectListItem
                        {
                            Value = rr.RRNo.ToString(),
                            Text = rr.RRNo
                        })
                        .ToListAsync(cancellationToken);

                    viewModel.BankAccounts = await _dbContext.BankAccounts
                        .Select(ba => new SelectListItem
                        {
                            Value = ba.Id.ToString(),
                            Text = ba.AccountNo + " " + ba.AccountName
                        })
                        .ToListAsync(cancellationToken);

                    TempData["error"] = ex.Message;
                    return View(viewModel);
                }
            }
            viewModel.COA = await _dbContext.ChartOfAccounts
                .Where(coa => !new[] { "2010102", "2010101", "1010101" }.Any(excludedNumber => coa.Number.Contains(excludedNumber)) && coa.Level == 4 || coa.Level == 5)
                .Select(s => new SelectListItem
                {
                    Value = s.Number,
                    Text = s.Number + " " + s.Name
                })
                .ToListAsync(cancellationToken);

            viewModel.Suppliers = await _dbContext.Suppliers
                .Where(supp => supp.Category == "Trade")
                .Select(sup => new SelectListItem
                {
                    Value = sup.Id.ToString(),
                    Text = sup.Name
                })
                .ToListAsync(cancellationToken);

            viewModel.PONo = await _dbContext.PurchaseOrders
                .Where(po => po.SupplierId == viewModel.SupplierId && po.IsPosted)
                .Select(po => new SelectListItem
                {
                    Value = po.PONo.ToString(),
                    Text = po.PONo
                })
                .ToListAsync(cancellationToken);

            viewModel.RR = await _dbContext.ReceivingReports
                .Where(rr => viewModel.POSeries.Contains(rr.PONo) && !rr.IsPaid && rr.IsPosted)
                .Select(rr => new SelectListItem
                {
                    Value = rr.RRNo.ToString(),
                    Text = rr.RRNo
                })
                .ToListAsync(cancellationToken);

            viewModel.BankAccounts = await _dbContext.BankAccounts
                .Select(ba => new SelectListItem
                {
                    Value = ba.Id.ToString(),
                    Text = ba.AccountNo + " " + ba.AccountName
                })
                .ToListAsync(cancellationToken);

            TempData["error"] = "The information provided was invalid.";
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> NonTradeInvoicing(CancellationToken cancellationToken)
        {
            var viewModel = new CheckVoucherNonTradeInvoicingViewModel();

            viewModel.ChartOfAccounts = await _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == 4 || coa.Level == 5)
                .Select(s => new SelectListItem
                {
                    Value = s.Number,
                    Text = s.Number + " " + s.Name
                })
                .ToListAsync(cancellationToken);

            viewModel.Suppliers = await _dbContext.Suppliers
                .Where(supp => supp.Category == "Non-Trade")
                .Select(sup => new SelectListItem
                {
                    Value = sup.Id.ToString(),
                    Text = sup.Name
                })
                .ToListAsync(cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> NonTradeInvoicing(CheckVoucherNonTradeInvoicingViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    #region --Validating series
                    var getLastNumber = await _checkVoucherRepo.GetLastSeriesNumberCV(cancellationToken);

                    if (getLastNumber > 9999999999)
                    {
                        TempData["error"] = "You reached the maximum Series Number";
                        return View(viewModel);
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

                    #region -- Saving the default entries -- 

                    CheckVoucherHeader checkVoucherHeader = new()
                    {
                        CVNo = await _checkVoucherRepo.GenerateCVNo(cancellationToken),
                        SeriesNumber = getLastNumber,
                        Date = viewModel.TransactionDate,
                        Payee = viewModel.SupplierName,
                        PONo = [viewModel.PoNo],
                        SINo = [viewModel.SiNo],
                        SupplierId = viewModel.SupplierId,
                        Particulars = viewModel.Particulars,
                        Total = viewModel.Total,
                        CreatedBy = _userManager.GetUserName(this.User),
                        Category = "Non-Trade",
                        CvType = "Invoicing"
                    };

                    #endregion -- Saving the default entries -- 

                    #region -- Automatic entry --

                    if (viewModel.StartDate != null && viewModel.NumberOfYears != 0)
                    {
                        checkVoucherHeader.StartDate = viewModel.StartDate;
                        checkVoucherHeader.EndDate = checkVoucherHeader.StartDate.Value.AddYears(viewModel.NumberOfYears);
                        checkVoucherHeader.NumberOfMonths = (viewModel.NumberOfYears * 12);

                        // Identify the account with a number that starts with '10201'
                        decimal? amount = null;
                        for (int i = 0; i < viewModel.AccountNumber.Length; i++)
                        {
                            if (viewModel.AccountNumber[i].StartsWith("10201") || viewModel.AccountNumber[i].StartsWith("10105"))
                            {
                                amount = viewModel.Debit[i] != 0 ? viewModel.Debit[i] : viewModel.Credit[i];
                                break;
                            }
                        }

                        if (amount.HasValue)
                        {
                            checkVoucherHeader.AmountPerMonth = (amount.Value / viewModel.NumberOfYears) / 12;
                        }
                    }

                    await _dbContext.AddAsync(checkVoucherHeader, cancellationToken);

                    #endregion -- Automatic entry --

                    #region -- cv invoiving details entry --

                    List<CheckVoucherDetail> checkVoucherDetails = new();

                    for (int i = 0; i < viewModel.AccountNumber.Length; i++)
                    {
                        if (viewModel.Debit[i] != 0 || viewModel.Credit[i] != 0)
                        {
                            checkVoucherDetails.Add(new CheckVoucherDetail
                            {
                                AccountNo = viewModel.AccountNumber[i],
                                AccountName = viewModel.AccountTitle[i],
                                TransactionNo = checkVoucherHeader.CVNo,
                                Debit = viewModel.Debit[i],
                                Credit = viewModel.Credit[i]
                            });
                        }
                    }

                    await _dbContext.CheckVoucherDetails.AddRangeAsync(checkVoucherDetails, cancellationToken);

                    #endregion -- cv invoiving details entry --

                    #region -- Uploading file --
                    if (file != null && file.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Supporting CV Files", checkVoucherHeader.CVNo);

                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        string fileName = Path.GetFileName(file.FileName);
                        string fileSavePath = Path.Combine(uploadsFolder, fileName);

                        using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream, cancellationToken);
                        }

                        //if necessary add field to store location path
                        // model.Header.SupportingFilePath = fileSavePath
                    }
                    #endregion -- Uploading file --

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(checkVoucherHeader.CreatedBy, $"Create new check voucher# {checkVoucherHeader.CVNo}", "Check Voucher");
                    await _dbContext.AddAsync(auditTrail, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    viewModel.ChartOfAccounts = await _dbContext.ChartOfAccounts
                        .Where(coa => coa.Level == 4 || coa.Level == 5)
                        .Select(s => new SelectListItem
                        {
                            Value = s.Number,
                            Text = s.Number + " " + s.Name
                        })
                        .ToListAsync(cancellationToken);

                    viewModel.Suppliers = await _dbContext.Suppliers
                        .Where(supp => supp.Category == "Non-Trade")
                        .Select(sup => new SelectListItem
                        {
                            Value = sup.Id.ToString(),
                            Text = sup.Name
                        })
                        .ToListAsync(cancellationToken);

                    TempData["error"] = ex.Message;
                    return View(viewModel);
                }
            }

            viewModel.ChartOfAccounts = await _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == 4 || coa.Level == 5)
                .Select(s => new SelectListItem
                {
                    Value = s.Number,
                    Text = s.Number + " " + s.Name
                })
                .ToListAsync(cancellationToken);

            viewModel.Suppliers = await _dbContext.Suppliers
                .Where(supp => supp.Category == "Non-Trade")
                .Select(sup => new SelectListItem
                {
                    Value = sup.Id.ToString(),
                    Text = sup.Name
                })
                .ToListAsync(cancellationToken);

            TempData["error"] = "The information provided was invalid.";
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> NonTradePayment(CancellationToken cancellationToken)
        {
            var viewModel = new CheckVoucherNonTradePaymentViewModel();

            viewModel.ChartOfAccounts = await _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == 4 || coa.Level == 5)
                .Select(s => new SelectListItem
                {
                    Value = s.Number,
                    Text = s.Number + " " + s.Name
                })
                .ToListAsync(cancellationToken);

            viewModel.CheckVouchers = await _dbContext.CheckVoucherHeaders
                .Where(cvh => cvh.CvType == "Invoicing" && !cvh.IsPaid && cvh.IsPosted)
                .Select(cvh => new SelectListItem
                {
                    Value = cvh.Id.ToString(),
                    Text = cvh.CVNo
                })
                .ToListAsync(cancellationToken);

            viewModel.Banks = await _dbContext.BankAccounts
                .Select(ba => new SelectListItem
                {
                    Value = ba.Id.ToString(),
                    Text = ba.AccountNo + " " + ba.AccountName
                })
                .ToListAsync(cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> NonTradePayment(CheckVoucherNonTradePaymentViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    #region --Validating series
                    var getLastNumber = await _checkVoucherRepo.GetLastSeriesNumberCV(cancellationToken);

                    if (getLastNumber > 9999999999)
                    {
                        TempData["error"] = "You reached the maximum Series Number";
                        return View(viewModel);
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

                    #region--Get Check Voucher Invoicing

                    var invoicingVoucher = await _dbContext.CheckVoucherHeaders
                        .FindAsync(viewModel.CvId, cancellationToken);

                    #endregion

                    #region--Saving the default entries

                    CheckVoucherHeader checkVoucherHeader = new()
                    {
                        CVNo = await _checkVoucherRepo.GenerateCVNo(cancellationToken),
                        SeriesNumber = getLastNumber,
                        Date = viewModel.TransactionDate,
                        PONo = invoicingVoucher.PONo,
                        SINo = invoicingVoucher.SINo,
                        SupplierId = invoicingVoucher.SupplierId,
                        Particulars = viewModel.Particulars,
                        Total = viewModel.Total,
                        CreatedBy = _userManager.GetUserName(this.User),
                        Category = "Non-Trade",
                        CvType = "Payment",
                        Reference = invoicingVoucher.CVNo,
                        BankId = viewModel.BankId,
                        Payee = viewModel.Payee,
                        CheckNo = viewModel.CheckNo,
                        CheckDate = viewModel.CheckDate,
                        CheckAmount = viewModel.Total
                    };

                    await _dbContext.AddAsync(checkVoucherHeader, cancellationToken);

                    List<CheckVoucherDetail> checkVoucherDetails = new();

                    for (int i = 0; i < viewModel.AccountNumber.Length; i++)
                    {
                        if (viewModel.Debit[i] != 0 || viewModel.Credit[i] != 0)
                        {
                            checkVoucherDetails.Add(new CheckVoucherDetail
                            {
                                AccountNo = viewModel.AccountNumber[i],
                                AccountName = viewModel.AccountTitle[i],
                                TransactionNo = checkVoucherHeader.CVNo,
                                Debit = viewModel.Debit[i],
                                Credit = viewModel.Credit[i]
                            });
                        }
                    }

                    await _dbContext.CheckVoucherDetails.AddRangeAsync(checkVoucherDetails, cancellationToken);

                    #endregion

                    #region -- Uploading file --
                    if (file != null && file.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Supporting CV Files", checkVoucherHeader.CVNo);

                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        string fileName = Path.GetFileName(file.FileName);
                        string fileSavePath = Path.Combine(uploadsFolder, fileName);

                        using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream, cancellationToken);
                        }

                        //if necessary add field to store location path
                        // model.Header.SupportingFilePath = fileSavePath
                    }
                    #endregion -- Uploading file --

                    #region--Update invoicing voucher

                    await _checkVoucherRepo.UpdateInvoicingVoucher(checkVoucherHeader.Total, viewModel.CvId, cancellationToken);

                    #endregion

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(checkVoucherHeader.CreatedBy, $"Create new check voucher# {checkVoucherHeader.CVNo}", "Check Voucher");
                    await _dbContext.AddAsync(auditTrail, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    viewModel.ChartOfAccounts = await _dbContext.ChartOfAccounts
                        .Where(coa => coa.Level == 4 || coa.Level == 5)
                        .Select(s => new SelectListItem
                        {
                            Value = s.Number,
                            Text = s.Number + " " + s.Name
                        })
                        .ToListAsync(cancellationToken);

                    viewModel.CheckVouchers = await _dbContext.CheckVoucherHeaders
                        .Where(cvh => cvh.CvType == "Invoicing" && !cvh.IsPaid && cvh.IsPosted)
                        .Select(cvh => new SelectListItem
                        {
                            Value = cvh.Id.ToString(),
                            Text = cvh.CVNo
                        })
                        .ToListAsync(cancellationToken);

                    viewModel.Banks = await _dbContext.BankAccounts
                        .Select(ba => new SelectListItem
                        {
                            Value = ba.Id.ToString(),
                            Text = ba.AccountNo + " " + ba.AccountName
                        })
                        .ToListAsync(cancellationToken);

                    TempData["error"] = ex.Message;
                    return View(viewModel);
                }
            }

            viewModel.ChartOfAccounts = await _dbContext.ChartOfAccounts
                        .Where(coa => coa.Level == 4 || coa.Level == 5)
                        .Select(s => new SelectListItem
                        {
                            Value = s.Number,
                            Text = s.Number + " " + s.Name
                        })
                        .ToListAsync(cancellationToken);

            viewModel.CheckVouchers = await _dbContext.CheckVoucherHeaders
                .Where(cvh => cvh.CvType == "Invoicing" && !cvh.IsPaid && cvh.IsPosted)
                .Select(cvh => new SelectListItem
                {
                    Value = cvh.Id.ToString(),
                    Text = cvh.CVNo
                })
                .ToListAsync(cancellationToken);

            viewModel.Banks = await _dbContext.BankAccounts
                .Select(ba => new SelectListItem
                {
                    Value = ba.Id.ToString(),
                    Text = ba.AccountNo + " " + ba.AccountName
                })
                .ToListAsync(cancellationToken);

            TempData["error"] = "The information provided was invalid.";
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetCheckVoucherDetails(int? cvId, CancellationToken cancellationToken)
        {
            if (cvId != null)
            {
                var cv = await _dbContext.CheckVoucherHeaders
                    .Include(c => c.Supplier)
                    .FirstOrDefaultAsync(c => c.Id == cvId, cancellationToken);

                if (cv != null)
                {
                    return Json(new
                    {
                        Payee = cv.Supplier.Name,
                        PayeeAddress = cv.Supplier.Address,
                        PayeeTin = cv.Supplier.TinNo,
                        Total = cv.Total
                    });
                }
                return Json(null);
            }
            return Json(null);
        }

        [HttpGet]
        public async Task<IActionResult> EditNonTradeInvoicing(int id, CancellationToken cancellationToken)
        {
            var existingModel = await _dbContext.CheckVoucherHeaders
                .Include(c => c.Supplier)
                .FirstOrDefaultAsync(cv => cv.Id == id, cancellationToken);

            var existingDetailsModel = await _dbContext.CheckVoucherDetails.Where(d => d.TransactionNo == existingModel.CVNo).ToListAsync(cancellationToken);

            existingModel.Suppliers = await _dbContext.Suppliers
                .Select(sup => new SelectListItem
                {
                    Value = sup.Id.ToString(),
                    Text = sup.Name
                })
                .ToListAsync(cancellationToken);
            existingModel.COA = await _dbContext.ChartOfAccounts
                        .Where(coa => coa.Level == 4 || coa.Level == 5)
                        .Select(s => new SelectListItem
                        {
                            Value = s.Number,
                            Text = s.Number + " " + s.Name
                        })
                        .ToListAsync(cancellationToken);

            var accountNumbers = existingDetailsModel.Select(model => model.AccountNo).ToArray();
            var accountTitles = existingDetailsModel.Select(model => model.AccountName).ToArray();
            var debit = existingDetailsModel.Select(model => model.Debit).ToArray();
            var credit = existingDetailsModel.Select(model => model.Credit).ToArray();

            CheckVoucherNonTradeInvoicingViewModel viewModel = new()
            {
                CVId = existingModel.Id,
                Suppliers = existingModel.Suppliers,
                SupplierName = existingModel.Supplier.Name,
                ChartOfAccounts = existingModel.COA,
                TransactionDate = existingModel.Date,
                SupplierId = existingModel.SupplierId ?? 0,
                SupplierAddress = existingModel.Supplier.Address,
                SupplierTinNo = existingModel.Supplier.TinNo,
                PoNo = existingModel.PONo?.FirstOrDefault(),
                SiNo = existingModel.SINo?.FirstOrDefault(),
                Total = existingModel.Total,
                Particulars = existingModel.Particulars,
                AccountNumber = accountNumbers,
                AccountTitle = accountTitles,
                Debit = debit,
                Credit = credit
            };

            return View(viewModel);
        }
        [HttpPost]
        public async Task<IActionResult> EditNonTradeInvoicing(CheckVoucherNonTradeInvoicingViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    #region --Saving the default entries

                    var existingModel = await _dbContext.CheckVoucherHeaders
                        .Include(cv => cv.Supplier)
                        .FirstOrDefaultAsync(cv => cv.Id == viewModel.CVId, cancellationToken);

                    if (existingModel != null)
                    {
                        existingModel.Date = viewModel.TransactionDate;
                        existingModel.SupplierId = viewModel.SupplierId;
                        existingModel.PONo = [viewModel.PoNo];
                        existingModel.SINo = [viewModel.SiNo];
                        existingModel.Total = viewModel.Total;
                        existingModel.Particulars = viewModel.Particulars;
                    }

                    //For automation purposes
                    if (viewModel.StartDate != null && viewModel.NumberOfYears != 0)
                    {
                        existingModel.StartDate = viewModel.StartDate;
                        existingModel.EndDate = existingModel.StartDate.Value.AddYears(viewModel.NumberOfYears);
                        existingModel.NumberOfMonths = (viewModel.NumberOfYears * 12);

                        // Identify the account with a number that starts with '10201'
                        decimal? amount = null;
                        for (int i = 0; i < viewModel.AccountNumber.Length; i++)
                        {
                            if (viewModel.AccountNumber[i].StartsWith("10201") || viewModel.AccountNumber[i].StartsWith("10105"))
                            {
                                amount = viewModel.Debit[i] != 0 ? viewModel.Debit[i] : viewModel.Credit[i];
                                break;
                            }
                        }

                        if (amount.HasValue)
                        {
                            existingModel.AmountPerMonth = (amount.Value / viewModel.NumberOfYears) / 12;
                        }
                    }
                    else
                    {
                        existingModel.StartDate = null;
                        existingModel.EndDate = null;
                        existingModel.NumberOfMonths = 0;
                        existingModel.AmountPerMonth = 0;
                    }

                    #endregion --Saving the default entries

                    #region --CV Details Entry

                    var existingDetailsModel = await _dbContext.CheckVoucherDetails.Where(d => d.TransactionNo == existingModel.CVNo).ToListAsync(cancellationToken);

                    // Dictionary to keep track of AccountNo and their ids for comparison
                    var accountTitleDict = new Dictionary<string, List<int>>();
                    foreach (var details in existingDetailsModel)
                    {
                        if (!accountTitleDict.ContainsKey(details.AccountNo))
                        {
                            accountTitleDict[details.AccountNo] = new List<int>();
                        }
                        accountTitleDict[details.AccountNo].Add(details.Id);
                    }

                    // Add or update records
                    for (int i = 0; i < viewModel.AccountTitle.Length; i++)
                    {

                        if (accountTitleDict.TryGetValue(viewModel.AccountNumber[i], out var ids))
                        {
                            // Update the first matching record and remove it from the list
                            var detailsId = ids.First();
                            ids.RemoveAt(0);
                            var details = existingDetailsModel.First(o => o.Id == detailsId);

                            details.AccountNo = viewModel.AccountNumber[i];
                            details.AccountName = viewModel.AccountTitle[i];
                            details.Debit = viewModel.Debit[i];
                            details.Credit = viewModel.Credit[i];
                            details.TransactionNo = existingModel.CVNo;

                            if (ids.Count == 0)
                            {
                                accountTitleDict.Remove(viewModel.AccountNumber[i]);
                            }
                        }
                        else
                        {
                            // Add new record
                            var newDetails = new CheckVoucherDetail
                            {
                                AccountNo = viewModel.AccountNumber[i],
                                AccountName = viewModel.AccountTitle[i],
                                Debit = viewModel.Debit[i],
                                Credit = viewModel.Credit[i],
                                TransactionNo = existingModel.CVNo
                            };
                            await _dbContext.CheckVoucherDetails.AddAsync(newDetails, cancellationToken);
                        }
                    }

                    // Remove remaining records that were duplicates
                    foreach (var ids in accountTitleDict.Values)
                    {
                        foreach (var id in ids)
                        {
                            var details = existingDetailsModel.First(o => o.Id == id);
                            _dbContext.CheckVoucherDetails.Remove(details);
                        }
                    }

                    #endregion --CV Details Entry

                    #region -- Uploading file --

                    if (file != null && file.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Supporting CV Files", existingModel.CVNo);

                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        string fileName = Path.GetFileName(file.FileName);
                        string fileSavePath = Path.Combine(uploadsFolder, fileName);

                        using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream, cancellationToken);
                        }

                        //if necessary add field to store location path
                        // model.Header.SupportingFilePath = fileSavePath
                    }

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(_userManager.GetUserName(this.User), $"Create new check voucher# {existingModel.CVNo}", "Check Voucher");
                    await _dbContext.AddAsync(auditTrail, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);  // await the SaveChangesAsync method
                    TempData["success"] = "Non-trade invoicing edited successfully";
                    return RedirectToAction(nameof(Index));
                    #endregion -- Uploading file --
                }
                catch (Exception ex)
                {
                    viewModel.Suppliers = await _dbContext.Suppliers
                    .Select(sup => new SelectListItem
                    {
                        Value = sup.Id.ToString(),
                        Text = sup.Name
                    })
                    .ToListAsync(cancellationToken);

                    TempData["error"] = ex.Message;
                    return View(viewModel);
                }
            }
            viewModel.Suppliers = await _dbContext.Suppliers
                    .Select(sup => new SelectListItem
                    {
                        Value = sup.Id.ToString(),
                        Text = sup.Name
                    })
                    .ToListAsync(cancellationToken);

            TempData["error"] = "The information provided was invalid.";
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> EditNonTradePayment(int id, CancellationToken cancellationToken)
        {
            var existingModel = await _dbContext.CheckVoucherHeaders
                .Include(c => c.Supplier)
                .FirstOrDefaultAsync(cv => cv.Id == id, cancellationToken);

            var existingDetailsModel = await _dbContext.CheckVoucherDetails.Where(d => d.TransactionNo == existingModel.CVNo).ToListAsync(cancellationToken);
            var invoicing = await _dbContext.CheckVoucherHeaders.FirstOrDefaultAsync(cvh => cvh.CVNo == existingModel.Reference, cancellationToken);

            var accountNumbers = existingDetailsModel.Select(model => model.AccountNo).ToArray();
            var accountTitles = existingDetailsModel.Select(model => model.AccountName).ToArray();
            var debit = existingDetailsModel.Select(model => model.Debit).ToArray();
            var credit = existingDetailsModel.Select(model => model.Credit).ToArray();

            #region -- insert fetch data into viewModel --

            CheckVoucherNonTradePaymentViewModel viewModel = new()
            {
                CvId = invoicing.Id,
                CVId = existingModel.Id,
                TransactionDate = existingModel.Date,
                Payee = existingModel.Payee,
                PayeeAddress = existingModel.Supplier.Address,
                PayeeTin = existingModel.Supplier.TinNo,
                Total = existingModel.Total,
                BankId = existingModel.BankId ?? 0,
                CheckNo = existingModel.CheckNo,
                CheckDate = existingModel.CheckDate ?? default,
                Particulars = existingModel.Particulars,
                AccountNumber = accountNumbers,
                AccountTitle = accountTitles,
                Debit = debit,
                Credit = credit,

                CheckVouchers = await _dbContext.CheckVoucherHeaders
                .Where(cvh => cvh.CvType == "Invoicing" && cvh.IsPosted)
                .Select(cvh => new SelectListItem
                {
                    Value = cvh.Id.ToString(),
                    Text = cvh.CVNo
                })
                .ToListAsync(cancellationToken),

                Banks = await _dbContext.BankAccounts
                .Select(ba => new SelectListItem
                {
                    Value = ba.Id.ToString(),
                    Text = ba.AccountNo + " " + ba.AccountName
                })
                .ToListAsync(cancellationToken),

                ChartOfAccounts = await _dbContext.ChartOfAccounts
                        .Where(coa => coa.Level == 4 || coa.Level == 5)
                        .Select(s => new SelectListItem
                        {
                            Value = s.Number,
                            Text = s.Number + " " + s.Name
                        })
                        .ToListAsync(cancellationToken)
            };

            #endregion -- insert fetch data into viewModel --

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> EditNonTradePayment(CheckVoucherNonTradePaymentViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    #region --Check if duplicate CheckNo
                    var existingHeaderModel = await _dbContext.CheckVoucherHeaders
                        .Include(cv => cv.Supplier)
                        .FirstOrDefaultAsync(cv => cv.Id == viewModel.CVId, cancellationToken);
                    var invoicing = await _dbContext.CheckVoucherHeaders.FirstOrDefaultAsync(cvh => cvh.CVNo == existingHeaderModel.Reference, cancellationToken);

                    if (viewModel.CheckNo != null && !viewModel.CheckNo.Contains("DM"))
                    {
                        var cv = await _dbContext
                        .CheckVoucherHeaders
                        .Where(cv => cv.BankId == viewModel.BankId && cv.CheckNo == viewModel.CheckNo && !cv.CheckNo.Equals(existingHeaderModel.CheckNo))
                        .ToListAsync(cancellationToken);
                        if (cv.Any())
                        {
                            TempData["error"] = "Check No. Is already exist";
                            return View(viewModel);
                        }
                    }
                    #endregion --Check if duplicate CheckNo

                    #region --CV Details Entry

                    var existingDetailsModel = await _dbContext.CheckVoucherDetails.Where(d => d.TransactionNo == existingHeaderModel.CVNo).ToListAsync(cancellationToken);

                    // Dictionary to keep track of AccountNo and their ids for comparison
                    var accountTitleDict = new Dictionary<string, List<int>>();
                    foreach (var details in existingDetailsModel)
                    {
                        if (!accountTitleDict.ContainsKey(details.AccountNo))
                        {
                            accountTitleDict[details.AccountNo] = new List<int>();
                        }
                        accountTitleDict[details.AccountNo].Add(details.Id);
                    }

                    var cashInBank = 0m;
                    // Add or update records
                    for (int i = 0; i < viewModel.AccountTitle.Length; i++)
                    {
                        cashInBank = viewModel.Credit[1];

                        if (accountTitleDict.TryGetValue(viewModel.AccountNumber[i], out var ids))
                        {
                            // Update the first matching record and remove it from the list
                            var detailsId = ids.First();
                            ids.RemoveAt(0);
                            var details = existingDetailsModel.First(o => o.Id == detailsId);

                            details.AccountNo = viewModel.AccountNumber[i];
                            details.AccountName = viewModel.AccountTitle[i];
                            details.Debit = viewModel.Debit[i];
                            details.Credit = viewModel.Credit[i];
                            details.TransactionNo = existingHeaderModel.CVNo;

                            if (ids.Count == 0)
                            {
                                accountTitleDict.Remove(viewModel.AccountNumber[i]);
                            }
                        }
                        else
                        {
                            // Add new record
                            var newDetails = new CheckVoucherDetail
                            {
                                AccountNo = viewModel.AccountNumber[i],
                                AccountName = viewModel.AccountTitle[i],
                                Debit = viewModel.Debit[i],
                                Credit = viewModel.Credit[i],
                                TransactionNo = existingHeaderModel.CVNo
                            };
                            await _dbContext.CheckVoucherDetails.AddAsync(newDetails, cancellationToken);
                        }
                    }

                    // Remove remaining records that were duplicates
                    foreach (var ids in accountTitleDict.Values)
                    {
                        foreach (var id in ids)
                        {
                            var details = existingDetailsModel.First(o => o.Id == id);
                            _dbContext.CheckVoucherDetails.Remove(details);
                        }
                    }

                    #endregion --CV Details Entry

                    #region --Saving the default entries

                    existingHeaderModel.Reference = invoicing.CVNo;
                    existingHeaderModel.Id = existingHeaderModel.Id;
                    existingHeaderModel.CVNo = existingHeaderModel.CVNo;
                    existingHeaderModel.Date = viewModel.TransactionDate;
                    existingHeaderModel.SupplierId = existingHeaderModel.Supplier.Id;
                    existingHeaderModel.Supplier.Address = viewModel.PayeeAddress;
                    existingHeaderModel.Supplier.TinNo = viewModel.PayeeTin;
                    existingHeaderModel.Particulars = viewModel.Particulars;
                    existingHeaderModel.BankId = viewModel.BankId;
                    existingHeaderModel.CheckNo = viewModel.CheckNo;
                    existingHeaderModel.Category = "Non-Trade";
                    existingHeaderModel.Payee = viewModel.Payee;
                    existingHeaderModel.CheckDate = viewModel.CheckDate;
                    existingHeaderModel.Total = cashInBank;
                    existingHeaderModel.CreatedBy = _userManager.GetUserName(this.User);

                    #endregion --Saving the default entries

                    #region -- Partial payment of RR's
                    //if (viewModel.Amount != null)
                    //{
                    //    var receivingReport = new ReceivingReport();
                    //    for (int i = 0; i < viewModel.RRSeries.Length; i++)
                    //    {
                    //        var rrValue = viewModel.RRSeries[i];
                    //        receivingReport = await _dbContext.ReceivingReports
                    //                    .FirstOrDefaultAsync(p => p.RRNo == rrValue);

                    //        receivingReport.AmountPaid += viewModel.Amount[i];

                    //        if (receivingReport.Amount <= receivingReport.AmountPaid)
                    //        {
                    //            receivingReport.IsPaid = true;
                    //            receivingReport.PaidDate = DateTime.Now;
                    //        }
                    //    }
                    //}

                    #endregion -- Partial payment of RR's

                    #region -- Uploading file --

                    if (file != null && file.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Supporting CV Files", existingHeaderModel.CVNo);

                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        string fileName = Path.GetFileName(file.FileName);
                        string fileSavePath = Path.Combine(uploadsFolder, fileName);

                        using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream, cancellationToken);
                        }

                        //if necessary add field to store location path
                        // model.Header.SupportingFilePath = fileSavePath
                    }

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(_userManager.GetUserName(this.User), $"Create new check voucher# {existingHeaderModel.CVNo}", "Check Voucher");
                    await _dbContext.AddAsync(auditTrail, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);  // await the SaveChangesAsync method
                    TempData["success"] = "Non-trade payment edited successfully";
                    return RedirectToAction(nameof(Index));
                    #endregion -- Uploading file --
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