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
        public async Task<IActionResult> GetPOs(int supplierId)
        {
            var purchaseOrders = await _dbContext.PurchaseOrders
                .Where(po => po.SupplierId == supplierId && po.IsPosted)
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
            .Where(rr => poNumber.Contains(rr.PONo) && !rr.IsPaid && rr.IsPosted)
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
                if (cvId != null || supplierId != null)
                {
                    name = supplier.Name;
                }
                return Json(new { TaxType = si, TinNo = tinNo, Address = address, Name = name });
            }

            return Json(null);
        }

        public async Task<IActionResult> GetSupplierDetails(int? supplierId)
        {
            if (supplierId != null)
            {
                var supplier = await _dbContext.Suppliers
                    .FindAsync(supplierId);

                if (supplier != null)
                {
                    return Json(new
                    {
                        SupplierAddress = supplier.Address,
                        SupplierTinNo = supplier.TinNo
                    });
                }
                return Json(null);
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
                                .FirstOrDefaultAsync(p => p.RRNo == rrValue);

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
                                    Amount = modelHeader.Total,
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
                var getLastNumber = await _checkVoucherRepo.GetLastSeriesNumberCV(cancellationToken);

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

        [HttpGet]
        public async Task<IActionResult> Trade(CheckVoucherTradeViewModel model, CancellationToken cancellationToken)
        {
            model.COA = await _dbContext.ChartOfAccounts
                .Where(coa => !new[] { "2010102", "2010101", "1010101" }.Any(excludedNumber => coa.Number.Contains(excludedNumber)) && coa.Level == 4 || coa.Level == 5)
                .Select(s => new SelectListItem
                {
                    Value = s.Number,
                    Text = s.Number + " " + s.Name
                })
                .ToListAsync(cancellationToken);

            model.Suppliers = await _dbContext.Suppliers
                .Select(sup => new SelectListItem
                {
                    Value = sup.Id.ToString(),
                    Text = sup.Name
                })
                .ToListAsync();
            model.BankAccounts = await _dbContext.BankAccounts
                .Select(ba => new SelectListItem
                {
                    Value = ba.Id.ToString(),
                    Text = ba.AccountNo + " " + ba.AccountName
                })
                .ToListAsync();

            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> Trade(CheckVoucherTradeViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
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
                .Select(sup => new SelectListItem
                {
                    Value = sup.Id.ToString(),
                    Text = sup.Name
                })
                .ToListAsync();
            viewModel.BankAccounts = await _dbContext.BankAccounts
                .Select(ba => new SelectListItem
                {
                    Value = ba.Id.ToString(),
                    Text = ba.AccountNo + " " + ba.AccountName
                })
                .ToListAsync();
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
                                .FirstOrDefaultAsync(p => p.RRNo == rrValue);

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
                        await file.CopyToAsync(stream);
                    }

                    //if necessary add field to store location path
                    // model.Header.SupportingFilePath = fileSavePath
                }

                #region --Audit Trail Recording

                AuditTrail auditTrail = new(item.CreatedBy, $"Create new check voucher# {item.CVNo}", "Check Voucher");
                _dbContext.Add(auditTrail);

                #endregion --Audit Trail Recording
            }
            await _dbContext.SaveChangesAsync(cancellationToken);  // await the SaveChangesAsync method
            return RedirectToAction("Index");
            #endregion -- Uploading file --
        }

        [HttpGet]
        public async Task<IActionResult> NonTradeInvoicing(CancellationToken cancellationToken)
        {
            var viewModel = new CheckVoucherNonTradeInvoicing();

            viewModel.ChartOfAccounts = await _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == 4 || coa.Level == 5)
                .Select(s => new SelectListItem
                {
                    Value = s.Number,
                    Text = s.Number + " " + s.Name
                })
                .ToListAsync(cancellationToken);

            viewModel.Suppliers = await _dbContext.Suppliers
                .Select(sup => new SelectListItem
                {
                    Value = sup.Id.ToString(),
                    Text = sup.Name
                })
                .ToListAsync();

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> NonTradeInvoicing(CheckVoucherNonTradeInvoicing viewModel, IFormFile? file, CancellationToken cancellationToken)
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

                    #region--Saving the default entries

                    CheckVoucherHeader checkVoucherHeader = new()
                    {
                        CVNo = await _checkVoucherRepo.GenerateCVNo(cancellationToken),
                        SeriesNumber = getLastNumber,
                        Date = viewModel.TransactionDate,
                        PONo = [viewModel.PoNo],
                        SINo = [viewModel.SiNo],
                        SupplierId = viewModel.SupplierId,
                        Particulars = viewModel.Particulars,
                        Total = viewModel.Total,
                        CreatedBy = _userManager.GetUserName(this.User),
                        Category = "Non-Trade",
                        CvType = "Invoicing"

                    };

                    await _dbContext.AddAsync(checkVoucherHeader, cancellationToken);

                    List<CheckVoucherDetail> checkVoucherDetails = new();

                    for (int i = 0; i < viewModel.AccountNumber.Length; i++)
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
                            await file.CopyToAsync(stream);
                        }

                        //if necessary add field to store location path
                        // model.Header.SupportingFilePath = fileSavePath
                    }
                    #endregion -- Uploading file --

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(checkVoucherHeader.CreatedBy, $"Create new check voucher# {checkVoucherHeader.CVNo}", "Check Voucher");
                    _dbContext.Add(auditTrail);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    return RedirectToAction("Index");
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
                        .Select(sup => new SelectListItem
                        {
                            Value = sup.Id.ToString(),
                            Text = sup.Name
                        })
                        .ToListAsync();

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
                .Select(sup => new SelectListItem
                {
                    Value = sup.Id.ToString(),
                    Text = sup.Name
                })
                .ToListAsync();

            TempData["error"] = "The information provided was invalid.";
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> NonTradePayment(CancellationToken cancellationToken)
        {
            var viewModel = new CheckVoucherNonTradePayment();

            viewModel.ChartOfAccounts = await _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == 4 || coa.Level == 5)
                .Select(s => new SelectListItem
                {
                    Value = s.Number,
                    Text = s.Number + " " + s.Name
                })
                .ToListAsync(cancellationToken);

            viewModel.CheckVouchers = await _dbContext.CheckVoucherHeaders
                .Where(cvh => cvh.CvType == "Invoicing" && !cvh.IsPaid)
                .Select(cvh => new SelectListItem
                {
                    Value = cvh.Id.ToString(),
                    Text = cvh.CVNo
                })
                .ToListAsync();

            viewModel.Banks = await _dbContext.BankAccounts
                .Select(ba => new SelectListItem
                {
                    Value = ba.Id.ToString(),
                    Text = ba.AccountNo + " " + ba.AccountName
                })
                .ToListAsync();



            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> NonTradePayment(CheckVoucherNonTradePayment viewModel, IFormFile? file, CancellationToken cancellationToken)
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
                        CheckNo = viewModel.CheckNo,
                        CheckDate = viewModel.CheckDate,
                        CheckAmount = viewModel.Total

                    };

                    await _dbContext.AddAsync(checkVoucherHeader, cancellationToken);

                    List<CheckVoucherDetail> checkVoucherDetails = new();

                    for (int i = 0; i < viewModel.AccountNumber.Length; i++)
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
                            await file.CopyToAsync(stream);
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
                    _dbContext.Add(auditTrail);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    return RedirectToAction("Index");
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
                        .Where(cvh => cvh.CvType == "Invoicing" && !cvh.IsPaid)
                        .Select(cvh => new SelectListItem
                        {
                            Value = cvh.Id.ToString(),
                            Text = cvh.CVNo
                        })
                        .ToListAsync();

                    viewModel.Banks = await _dbContext.BankAccounts
                        .Select(ba => new SelectListItem
                        {
                            Value = ba.Id.ToString(),
                            Text = ba.AccountNo + " " + ba.AccountName
                        })
                        .ToListAsync();

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
                .Where(cvh => cvh.CvType == "Invoicing" && !cvh.IsPaid)
                .Select(cvh => new SelectListItem
                {
                    Value = cvh.Id.ToString(),
                    Text = cvh.CVNo
                })
                .ToListAsync();

            viewModel.Banks = await _dbContext.BankAccounts
                .Select(ba => new SelectListItem
                {
                    Value = ba.Id.ToString(),
                    Text = ba.AccountNo + " " + ba.AccountName
                })
                .ToListAsync();

            TempData["error"] = "The information provided was invalid.";
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetCheckVoucherDetails(int? cvId)
        {
            if (cvId != null)
            {
                var cv = await _dbContext.CheckVoucherHeaders
                    .Include(c => c.Supplier)
                    .FirstOrDefaultAsync(c => c.Id == cvId);

                if (cv != null)
                {
                    return Json(new
                    {
                        Payee = cv.Supplier.Name,
                        PayeeAddress = cv.Supplier.Address,
                        PayeeTin = cv.Supplier.TinNo
                    });
                }
                return Json(null);
            }
            return Json(null);
        }
    }
}