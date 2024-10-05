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

        public async Task<IActionResult> Index(string? view, CancellationToken cancellationToken)
        {
            if (view == nameof(DynamicView.CheckVoucher))
            {
                var checkVouchers = await _checkVoucherRepo.GetCheckVouchersAsync(cancellationToken);

                return View("ImportExportIndex", checkVouchers);
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetCheckVouchers([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var checkVouchers = await _checkVoucherRepo.GetCheckVouchersAsync(cancellationToken);
                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search?.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();
                    checkVouchers = checkVouchers
                        .Where(cv =>
                            cv.CVNo.ToLower().Contains(searchValue) ||
                            cv.Date.ToString("MMM dd, yyyy").ToLower().Contains(searchValue) ||
                            cv.Supplier.Name.ToLower().Contains(searchValue) ||
                            cv.Category.ToLower().Contains(searchValue) ||
                            cv.Category.ToLower().Contains(searchValue) ||
                            cv.CvType.ToLower().Contains(searchValue) ||
                            cv.CreatedBy.ToLower().Contains(searchValue)
                            )
                        .ToList();
                }
                // Sorting
                if (parameters.Order != null && parameters.Order.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Data;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";
                    checkVouchers = checkVouchers
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}")
                        .ToList();
                }
                var totalRecords = checkVouchers.Count();
                var pagedData = checkVouchers
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
        public async Task<IActionResult> GetAllCheckVoucherIds(CancellationToken cancellationToken)
        {
            var checkVoucherIds = await _dbContext.CheckVoucherHeaders 
                                     .Select(cv => cv.Id) // Assuming Id is the primary key
                                     .ToListAsync(cancellationToken);
            return Json(checkVoucherIds);
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

                if (cv.OriginalSeriesNumber == null && cv.OriginalDocumentId == 0)
                {
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    var printedBy = _userManager.GetUserName(this.User);
                    AuditTrail auditTrailBook = new(printedBy, $"Printed original copy of cv# {cv.CVNo}", "Check Vouchers", ipAddress);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                }

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

                        if (modelHeader.OriginalSeriesNumber == null && modelHeader.OriginalDocumentId == 0)
                        {
                            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                            AuditTrail auditTrailBook = new(modelHeader.PostedBy, $"Posted check voucher# {modelHeader.CVNo}", "Check Voucher", ipAddress);
                            await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                        }

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        TempData["success"] = "Check Voucher has been Posted.";
                    }
                    return RedirectToAction(nameof(Print), new { id = cvId });
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(Print), new { id = cvId });
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
                    var cashInBank = viewModel.Credit[2];

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

                    if (existingHeaderModel.OriginalSeriesNumber == null && existingHeaderModel.OriginalDocumentId == 0)
                    {
                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        AuditTrail auditTrailBook = new(viewModel.CreatedBy, $"Create new check voucher# {viewModel.CVNo}", "Check Voucher", ipAddress);
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                    }

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

                    if (model.OriginalSeriesNumber == null && model.OriginalDocumentId == 0)
                    {
                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        AuditTrail auditTrailBook = new(model.VoidedBy, $"Voided check voucher# {model.CVNo}", "Check Voucher", ipAddress);
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                    }

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

                    if (model.OriginalSeriesNumber == null && model.OriginalDocumentId == 0)
                    {
                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        AuditTrail auditTrailBook = new(model.CanceledBy, $"Cancelled check voucher# {model.CVNo}", "Check Voucher", ipAddress);
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                    }

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

                    #region --Saving the default entries

                    var generateCVNo = await _checkVoucherRepo.GenerateCVNo(cancellationToken);
                    var cashInBank = viewModel.Credit[2];
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
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    #endregion --Saving the default entries

                    #region --CV Details Entry

                    var cvDetails = new List<CheckVoucherDetail>();
                    for (int i = 0; i < viewModel.AccountNumber.Length; i++)
                    {
                        if (viewModel.Debit[i] != 0 || viewModel.Credit[i] != 0)
                        {
                            cvDetails.Add(
                            new CheckVoucherDetail
                            {
                                AccountNo = viewModel.AccountNumber[i],
                                AccountName = viewModel.AccountTitle[i],
                                Debit = viewModel.Debit[i],
                                Credit = viewModel.Credit[i],
                                TransactionNo = generateCVNo,
                                CVHeaderId = cvh.Select(cvh => cvh.Id).FirstOrDefault()
                            });
                        }
                    }

                    await _dbContext.CheckVoucherDetails.AddRangeAsync(cvDetails, cancellationToken);
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

                        if (item.OriginalSeriesNumber == null && item.OriginalDocumentId == 0)
                        {
                            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                            AuditTrail auditTrailBook = new(item.CreatedBy, $"Create new check voucher# {item.CVNo}", "Check Voucher", ipAddress);
                            await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                        }

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

            viewModel.DefaultExpenses = await _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == 4 || coa.Level == 5)
                .Select(s => new SelectListItem
                {
                    Value = s.Number,
                    Text = s.Name
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
                        PONo = viewModel.PoNo != null ? [viewModel.PoNo] : null,
                        SINo = viewModel.SiNo != null ? [viewModel.SiNo] : null,
                        SupplierId = viewModel.SupplierId,
                        Particulars = viewModel.Particulars,
                        Total = viewModel.Total,
                        CreatedBy = _userManager.GetUserName(this.User),
                        Category = "Non-Trade",
                        CvType = "Invoicing"
                    };
                    #endregion -- Saving the default entries -- 

                    #region -- Get Supplier --

                    var supplier = await _dbContext.Suppliers
                        .Where(s => s.Id == viewModel.SupplierId)
                        .FirstOrDefaultAsync(cancellationToken);

                    #endregion -- Get Supplier --

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
                            if (supplier.TaxType == "Exempt" && (i == 2 || i == 3))
                            {
                                continue;
                            }

                            if (viewModel.AccountNumber[i].StartsWith("10201") || viewModel.AccountNumber[i].StartsWith("10105"))
                            {
                                amount = viewModel.Debit[i] != 0 ? viewModel.Debit[i] : viewModel.Credit[i];
                            }
                        }

                        if (amount.HasValue)
                        {
                            checkVoucherHeader.AmountPerMonth = (amount.Value / viewModel.NumberOfYears) / 12;
                        }
                    }

                    await _dbContext.CheckVoucherHeaders.AddAsync(checkVoucherHeader, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);

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
                                Credit = viewModel.Credit[i],
                                CVHeaderId = checkVoucherHeader.Id
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

                    if (checkVoucherHeader.OriginalSeriesNumber == null && checkVoucherHeader.OriginalDocumentId == 0)
                    {
                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        AuditTrail auditTrailBook = new(checkVoucherHeader.CreatedBy, $"Create new check voucher# {checkVoucherHeader.CVNo}", "Check Voucher", ipAddress);
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                    }

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

                    await _dbContext.CheckVoucherHeaders.AddAsync(checkVoucherHeader, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);

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
                                Credit = viewModel.Credit[i],
                                CVHeaderId = checkVoucherHeader.Id
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

                    if (checkVoucherHeader.OriginalSeriesNumber == null && checkVoucherHeader.OriginalDocumentId == 0)
                    {
                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        AuditTrail auditTrailBook = new(checkVoucherHeader.CreatedBy, $"Create new check voucher# {checkVoucherHeader.CVNo}", "Check Voucher", ipAddress);
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                    }

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

            var accountNumbers = existingDetailsModel.OrderBy(x => x.Id).Select(model => model.AccountNo).ToArray();
            var accountTitles = existingDetailsModel.OrderBy(x => x.Id).Select(model => model.AccountName).ToArray();
            var debit = existingDetailsModel.OrderBy(x => x.Id).Select(model => model.Debit).ToArray();
            var credit = existingDetailsModel.OrderBy(x => x.Id).Select(model => model.Credit).ToArray();

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
                DefaultExpenses = await _dbContext.ChartOfAccounts
                        .Where(coa => coa.Level == 4 || coa.Level == 5)
                        .Select(s => new SelectListItem
                        {
                            Value = s.Number,
                            Text = s.Name
                        })
                        .ToListAsync(cancellationToken),
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

                            var acctNo = await _dbContext.ChartOfAccounts
                                .FirstOrDefaultAsync(x => x.Name == viewModel.AccountTitle[i]);

                            details.AccountNo = acctNo.Number ?? throw new ArgumentNullException("Account title not found!");
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

                    if (existingModel.OriginalSeriesNumber == null && existingModel.OriginalDocumentId == 0)
                    {
                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        AuditTrail auditTrailBook = new(_userManager.GetUserName(this.User), $"Create new check voucher# {existingModel.CVNo}", "Check Voucher", ipAddress);
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                    }

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

                    #region --Saving the default entries

                    var cashInBank = viewModel.Credit[1];
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
                            details.TransactionNo = existingHeaderModel.CVNo;
                            details.CVHeaderId = existingHeaderModel.Id;

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
                                TransactionNo = existingHeaderModel.CVNo,
                                CVHeaderId = existingHeaderModel.Id
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

                    if (existingHeaderModel.OriginalSeriesNumber == null && existingHeaderModel.OriginalDocumentId == 0)
                    {
                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        AuditTrail auditTrailBook = new(_userManager.GetUserName(this.User), $"Create new check voucher# {existingHeaderModel.CVNo}", "Check Voucher", ipAddress);
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                    }

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

            try
            {
                var recordIds = selectedRecord.Split(',').Select(int.Parse).ToList();

                // Retrieve the selected invoices from the database
                var selectedList = await _dbContext.CheckVoucherHeaders
                    .Where(cvh => recordIds.Contains(cvh.Id))
                    .OrderBy(cvh => cvh.CVNo)
                    .ToListAsync();

                // Create the Excel package
                using (var package = new ExcelPackage())
                {
                    // Add a new worksheet to the Excel package
                    var worksheet = package.Workbook.Worksheets.Add("CheckVoucherHeader");
                    var worksheet2 = package.Workbook.Worksheets.Add("CheckVoucherDetails");

                    worksheet.Cells["A1"].Value = "TransactionDate";
                    worksheet.Cells["B1"].Value = "ReceivingReportNo";
                    worksheet.Cells["C1"].Value = "SalesInvoiceNo";
                    worksheet.Cells["D1"].Value = "PurchaseOrderNo";
                    worksheet.Cells["E1"].Value = "Particulars";
                    worksheet.Cells["F1"].Value = "CheckNo";
                    worksheet.Cells["G1"].Value = "Category";
                    worksheet.Cells["H1"].Value = "Payee";
                    worksheet.Cells["I1"].Value = "CheckDate";
                    worksheet.Cells["J1"].Value = "StartDate";
                    worksheet.Cells["K1"].Value = "EndDate";
                    worksheet.Cells["L1"].Value = "NumberOfMonths";
                    worksheet.Cells["M1"].Value = "NumberOfMonthsCreated";
                    worksheet.Cells["N1"].Value = "LastCreatedDate";
                    worksheet.Cells["O1"].Value = "AmountPerMonth";
                    worksheet.Cells["P1"].Value = "IsComplete";
                    worksheet.Cells["Q1"].Value = "AccruedType";
                    worksheet.Cells["R1"].Value = "Reference";
                    worksheet.Cells["S1"].Value = "CreatedBy";
                    worksheet.Cells["T1"].Value = "CreatedDate";
                    worksheet.Cells["U1"].Value = "Total";
                    worksheet.Cells["V1"].Value = "Amount";
                    worksheet.Cells["W1"].Value = "CheckAmount";
                    worksheet.Cells["X1"].Value = "CVType";
                    worksheet.Cells["Y1"].Value = "AmountPaid";
                    worksheet.Cells["Z1"].Value = "IsPaid";
                    worksheet.Cells["AA1"].Value = "CancellationRemarks";
                    worksheet.Cells["AB1"].Value = "OriginalBankId";
                    worksheet.Cells["AC1"].Value = "OriginalSeriesNumber";
                    worksheet.Cells["AD1"].Value = "OriginalSupplierId";
                    worksheet.Cells["AE1"].Value = "OriginalDocumentId";

                    worksheet2.Cells["A1"].Value = "AccountNo";
                    worksheet2.Cells["B1"].Value = "AccountName";
                    worksheet2.Cells["C1"].Value = "TransactionNo";
                    worksheet2.Cells["D1"].Value = "Debit";
                    worksheet2.Cells["E1"].Value = "Credit";

                    int row = 2;

                    List<CheckVoucherDetail> getCVDetails = new List<CheckVoucherDetail>();

                    foreach (var item in selectedList)
                    {
                        worksheet.Cells[row, 1].Value = item.Date.ToString("yyyy-MM-dd");
                        if (item.RRNo != null && !item.RRNo.Contains(null))
                        {
                            worksheet.Cells[row, 2].Value = string.Join(", ", item.RRNo.Select(rrNo => rrNo.ToString()));
                        }
                        if (item.SINo != null && !item.SINo.Contains(null))
                        {
                            worksheet.Cells[row, 3].Value = string.Join(", ", item.SINo.Select(siNo => siNo.ToString()));
                        }
                        if (item.PONo != null && !item.PONo.Contains(null))
                        {
                            worksheet.Cells[row, 4].Value = string.Join(", ", item.PONo.Select(poNo => poNo.ToString()));
                        }

                        worksheet.Cells[row, 5].Value = item.Particulars;
                        worksheet.Cells[row, 6].Value = item.CheckNo;
                        worksheet.Cells[row, 7].Value = item.Category;
                        worksheet.Cells[row, 8].Value = item.Payee;
                        worksheet.Cells[row, 9].Value = item.CheckDate?.ToString("yyyy-MM-dd");
                        worksheet.Cells[row, 10].Value = item.StartDate?.ToString("yyyy-MM-dd");
                        worksheet.Cells[row, 11].Value = item.EndDate?.ToString("yyyy-MM-dd");
                        worksheet.Cells[row, 12].Value = item.NumberOfMonths;
                        worksheet.Cells[row, 13].Value = item.NumberOfMonthsCreated;
                        worksheet.Cells[row, 14].Value = item.LastCreatedDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                        worksheet.Cells[row, 15].Value = item.AmountPerMonth;
                        worksheet.Cells[row, 16].Value = item.IsComplete;
                        worksheet.Cells[row, 17].Value = item.AccruedType;
                        worksheet.Cells[row, 18].Value = item.Reference;
                        worksheet.Cells[row, 19].Value = item.CreatedBy;
                        worksheet.Cells[row, 20].Value = item.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                        worksheet.Cells[row, 21].Value = item.Total;
                        if (item.Amount != null)
                        {
                            worksheet.Cells[row, 22].Value = string.Join(" ", item.Amount.Select(amount => amount.ToString("N2")));
                        }
                        worksheet.Cells[row, 23].Value = item.CheckAmount;
                        worksheet.Cells[row, 24].Value = item.CvType;
                        worksheet.Cells[row, 25].Value = item.AmountPaid;
                        worksheet.Cells[row, 26].Value = item.IsPaid;
                        worksheet.Cells[row, 27].Value = item.CancellationRemarks;
                        worksheet.Cells[row, 28].Value = item.BankId;
                        worksheet.Cells[row, 29].Value = item.CVNo;
                        worksheet.Cells[row, 30].Value = item.SupplierId;
                        worksheet.Cells[row, 31].Value = item.Id;

                        row++;
                    }

                    var cvNos = selectedList.Select(item => item.CVNo).ToList();

                    getCVDetails = await _dbContext.CheckVoucherDetails
                        .Where(cvd => cvNos.Contains(cvd.TransactionNo))
                        .OrderBy(cvd => cvd.Id)
                        .ToListAsync();

                    int cvdRow = 2;

                    foreach (var item in getCVDetails)
                    {
                        worksheet2.Cells[cvdRow, 1].Value = item.AccountNo;
                        worksheet2.Cells[cvdRow, 2].Value = item.AccountName;
                        worksheet2.Cells[cvdRow, 3].Value = item.TransactionNo;
                        worksheet2.Cells[cvdRow, 4].Value = item.Debit;
                        worksheet2.Cells[cvdRow, 5].Value = item.Credit;

                        cvdRow++;
                    }

                    // Convert the Excel package to a byte array
                    var excelBytes = await package.GetAsByteArrayAsync();

                    return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "CheckVoucherList.xlsx");
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }

        }

        #endregion -- export xlsx record --

        //Upload as .xlsx file.(Import)
        #region -- import xlsx record --

        [HttpPost]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return RedirectToAction(nameof(Index));
            }

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                stream.Position = 0;

                try
                {
                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "CheckVoucherHeader");

                        var worksheet2 = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "CheckVoucherDetails");

                        if (worksheet == null)
                        {
                            TempData["error"] = "The Excel file contains no worksheets of check voucher header.";
                            return RedirectToAction(nameof(Index), new { view = DynamicView.CheckVoucher });
                        }
                        if (worksheet2 == null)
                        {
                            TempData["error"] = "The Excel file contains no worksheets of check voucher details.";
                            return RedirectToAction(nameof(Index), new { view = DynamicView.CheckVoucher });
                        }
                        if (worksheet.ToString() != "CheckVoucherHeader")
                        {
                            TempData["error"] = "The Excel file is not related to check voucher.";
                            return RedirectToAction(nameof(Index), new { view = DynamicView.CheckVoucher });
                        }

                        var rowCount = worksheet.Dimension.Rows;

                        for (int row = 2; row <= rowCount; row++)  // Assuming the first row is the header
                        {
                            var checkVoucherHeader = new CheckVoucherHeader
                            {
                                CVNo = await _checkVoucherRepo.GenerateCVNo(),
                                SeriesNumber = await _checkVoucherRepo.GetLastSeriesNumberCV(),
                                Date = DateOnly.TryParse(worksheet.Cells[row, 1].Text, out DateOnly date) ? date : default,
                                RRNo = worksheet.Cells[row, 2].Text.Split(',').Select(rrNo => rrNo.Trim()).ToArray(),
                                SINo = worksheet.Cells[row, 3].Text.Split(',').Select(siNo => siNo.Trim()).ToArray(),
                                PONo = worksheet.Cells[row, 4].Text.Split(',').Select(poNo => poNo.Trim()).ToArray(),
                                Particulars = worksheet.Cells[row, 5].Text,
                                CheckNo = worksheet.Cells[row, 6].Text,
                                Category = worksheet.Cells[row, 7].Text,
                                Payee = worksheet.Cells[row, 8].Text,
                                CheckDate = DateOnly.TryParse(worksheet.Cells[row, 9].Text, out DateOnly checkDate) ? checkDate : default,
                                StartDate = DateOnly.TryParse(worksheet.Cells[row, 10].Text, out DateOnly startDate) ? startDate : default,
                                EndDate = DateOnly.TryParse(worksheet.Cells[row, 11].Text, out DateOnly endDate) ? endDate : default,
                                NumberOfMonths = int.TryParse(worksheet.Cells[row, 12].Text, out int numberOfMonths) ? numberOfMonths : 0,
                                NumberOfMonthsCreated = int.TryParse(worksheet.Cells[row, 13].Text, out int numberOfMonthsCreated) ? numberOfMonthsCreated : 0,
                                LastCreatedDate = DateTime.TryParse(worksheet.Cells[row, 14].Text, out DateTime lastCreatedDate) ? lastCreatedDate : default,
                                AmountPerMonth = decimal.TryParse(worksheet.Cells[row, 15].Text, out decimal amountPerMonth) ? amountPerMonth : 0,
                                IsComplete = bool.TryParse(worksheet.Cells[row, 16].Text, out bool isComplete) ? isComplete : false,
                                AccruedType = worksheet.Cells[row, 17].Text,
                                Reference = worksheet.Cells[row, 18].Text,
                                CreatedBy = worksheet.Cells[row, 19].Text,
                                CreatedDate = DateTime.TryParse(worksheet.Cells[row, 20].Text, out DateTime createdDate) ? createdDate : default,
                                Total = decimal.TryParse(worksheet.Cells[row, 21].Text, out decimal total) ? total : 0,
                                Amount = worksheet.Cells[row, 22].Text.Split(' ').Select(arrayAmount => decimal.TryParse(arrayAmount.Trim(), out decimal amount) ? amount : 0).ToArray(),
                                CheckAmount = decimal.TryParse(worksheet.Cells[row, 23].Text, out decimal checkAmount) ? checkAmount : 0,
                                CvType = worksheet.Cells[row, 24].Text,
                                AmountPaid = decimal.TryParse(worksheet.Cells[row, 25].Text, out decimal amountPaid) ? amountPaid : 0,
                                IsPaid = bool.TryParse(worksheet.Cells[row, 26].Text, out bool isPaid) ? isPaid : false,
                                CancellationRemarks = worksheet.Cells[row, 27].Text,
                                OriginalBankId = int.TryParse(worksheet.Cells[row, 28].Text, out int originalBankId) ? originalBankId : 0,
                                OriginalSeriesNumber = worksheet.Cells[row, 29].Text,
                                OriginalSupplierId = int.TryParse(worksheet.Cells[row, 30].Text, out int originalSupplierId) ? originalSupplierId : 0,
                                OriginalDocumentId = int.TryParse(worksheet.Cells[row, 31].Text, out int originalDocumentId) ? originalDocumentId : 0,
                            };
                            checkVoucherHeader.SupplierId = await _dbContext.Suppliers
                                .Where(supp => supp.OriginalSupplierId == checkVoucherHeader.OriginalSupplierId)
                                .Select(supp => supp.Id)
                                .FirstOrDefaultAsync();

                            if (checkVoucherHeader.CvType != "Invoicing")
                            {
                                checkVoucherHeader.BankId = await _dbContext.BankAccounts
                                .Where(bank => bank.OriginalBankId == checkVoucherHeader.OriginalBankId)
                                .Select(bank => bank.Id)
                                .FirstOrDefaultAsync();
                            }

                            await _dbContext.CheckVoucherHeaders.AddAsync(checkVoucherHeader);
                            await _dbContext.SaveChangesAsync();
                        }

                        var cvdRowCount = worksheet2.Dimension.Rows;
                        for (int cvdRow = 2; cvdRow <= cvdRowCount; cvdRow++)
                        {
                            var checkVoucherDetails = new CheckVoucherDetail
                            {
                                AccountNo = worksheet2.Cells[cvdRow, 1].Text,
                                AccountName = worksheet2.Cells[cvdRow, 2].Text,
                                Debit = decimal.TryParse(worksheet2.Cells[cvdRow, 4].Text, out decimal debit) ? debit : 0,
                                Credit = decimal.TryParse(worksheet2.Cells[cvdRow, 5].Text, out decimal credit) ? credit : 0,
                            };

                            checkVoucherDetails.TransactionNo = await _dbContext.CheckVoucherHeaders
                                .Where(cvh => cvh.OriginalSeriesNumber == worksheet2.Cells[cvdRow, 3].Text)
                                .Select(cvh => cvh.CVNo)
                                .FirstOrDefaultAsync();

                            await _dbContext.CheckVoucherDetails.AddAsync(checkVoucherDetails);
                            await _dbContext.SaveChangesAsync();
                        }
                    }
                }
                catch (OperationCanceledException oce)
                {
                    TempData["error"] = oce.Message;
                    return RedirectToAction(nameof(Index), new { view = DynamicView.CheckVoucher });
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(Index), new { view = DynamicView.CheckVoucher });
                }
            }
            TempData["success"] = "Uploading Success!";
            return RedirectToAction(nameof(Index), new { view = DynamicView.CheckVoucher });
        }

        #endregion -- import xlsx record --
    }
}