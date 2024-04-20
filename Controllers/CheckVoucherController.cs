using Accounting_System.Data;
using Accounting_System.Models;
using Accounting_System.Repository;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Accounting_System.Controllers
{
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
                .OrderByDescending(cv => cv.Id)
                .ToListAsync(cancellationToken);

            // Create a list to store CheckVoucherVM objectssw
            var checkVoucherVMs = new List<CheckVoucherVM>();

            // Retrieve details for each header
            foreach (var header in headers)
            {
                var headerCVNo = header.CVNo;
                var headerDetails = await _dbContext.CheckVoucherDetails.Where(d => d.TransactionNo == headerCVNo).ToListAsync(cancellationToken);

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
                .Where(coa => !new[] { "2010102", "2010101", "1010101" }.Any(excludedNumber => coa.Number.Contains(excludedNumber)))
                .Select(s => new SelectListItem
                {
                    Value = s.Number,
                    Text = s.Name
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
                    Text = ba.AccountName
                })
                .ToListAsync();

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CheckVoucherVM? model, CancellationToken cancellationToken, string[] accountNumberText, string[] accountNumber, decimal[]? debit, decimal[]? credit, string? siNo, string? poNo, decimal[] amount, decimal netOfEWT, decimal expandedWTaxDebitAmount, decimal cashInBankDebitAmount, IFormFile? file)
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

            if (ModelState.IsValid)
            {
                #region --Validating series
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
                    .Where(cv => cv.CheckNo == model.Header.CheckNo)
                    .ToListAsync(cancellationToken);
                    if (cv.Any())
                    {
                        TempData["error"] = "Check No. Is already exist";
                        return View(model);
                    }
                }
                #endregion --Check if duplicate record

                #region --Retrieve Supplier

                var supplier = await _dbContext
                            .Suppliers
                            .FirstOrDefaultAsync(po => po.Id == model.Header.SupplierId, cancellationToken);

                #endregion --Retrieve Supplier

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
                            TransactionNo = generateCVNo,
                            Debit = supplier.TaxType == "Withholding Tax" ? netOfEWT : totalAmount,
                            Credit = 0
                        }
                    );
                }
                else if (model.Header.Category == "Non-Trade")
                {
                    cvDetails.Add(
                        new CheckVoucherDetail
                        {
                            AccountNo = "2010102",
                            AccountName = "AP-Non Trade Payable",
                            TransactionNo = generateCVNo,
                            Debit = supplier.TaxType == "Withholding Tax" ? netOfEWT : totalAmount,
                            Credit = 0
                        }
                    );
                }
                if (supplier.TaxType == "Withholding Tax")
                {
                    cvDetails.Add(
                        new CheckVoucherDetail
                        {
                            AccountNo = "2010302",
                            AccountName = "Expanded Witholding Tax 1%",
                            TransactionNo = generateCVNo,
                            Debit = expandedWTaxDebitAmount,
                            Credit = 0
                        }
                    );
                    cvDetails.Add(
                        new CheckVoucherDetail
                        {
                            AccountNo = "2010302",
                            AccountName = "Expanded Witholding Tax 1%",
                            TransactionNo = generateCVNo,
                            Debit = 0,
                            Credit = expandedWTaxDebitAmount
                        }
                    );
                }

                for (int i = 0; i < accountNumber.Length; i++)
                    {
                        var currentAccountNumber = accountNumber[i];
                        var currentAccountNumberText = accountNumberText[i];
                        var currentDebit = debit[i];
                        var currentCredit = credit[i];

                        cvDetails.Add(
                            new CheckVoucherDetail
                            {
                                AccountNo = currentAccountNumber,
                                AccountName = currentAccountNumberText,
                                TransactionNo = generateCVNo,
                                Debit = currentDebit,
                                Credit = currentCredit
                            }
                        );
                    }
                        cvDetails.Add(
                            new CheckVoucherDetail
                            {
                                AccountNo = "1010101",
                                AccountName = "Cash in Bank",
                                TransactionNo = generateCVNo,
                                Debit = 0,
                                Credit = supplier.TaxType == "Withholding Tax" ? cashInBankDebitAmount : totalAmount
                            }
                        );

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
                    var list = cvDetails.Where(cv => cv.TransactionNo == generateCVNo);

                    model.Header.SeriesNumber = getLastNumber;
                    model.Header.CVNo = generateCVNo;
                    model.Header.CreatedBy = _userManager.GetUserName(this.User);
                    model.Header.TotalDebit = list.Sum(cvd => cvd.Debit);
                    model.Header.TotalCredit = list.Sum(cvd => cvd.Credit);

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

                await _dbContext.AddAsync(model.Header, cancellationToken);  // Add CheckVoucherHeader to the context
                    await _dbContext.SaveChangesAsync(cancellationToken);  // await the SaveChangesAsync method
                    return RedirectToAction("Index");
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
        public IActionResult GetSI(int supplierId)
        {
            var supplier = _dbContext.Suppliers
                .FirstOrDefault(po => po.Id == supplierId);

            if (supplier != null)
            {
                var si = supplier.TaxType;
                var address = supplier.Address;
                var tinNo = supplier.TinNo;
                return Json(new { TaxType = si, TinNo = tinNo, Address = address });
            }

            return Json(null);
        }
        public IActionResult RRBalance(string rrNo)
        {
            var receivingReport = _dbContext.ReceivingReports
                .FirstOrDefault(rr => rr.RRNo == rrNo);
            if (receivingReport != null)
            {
                var amount = receivingReport.Amount;
                var amountPaid = receivingReport.AmountPaid;
                var netAmount = receivingReport.NetAmount;
                var vatAmount = receivingReport.VatAmount;
                var ewtAmount = receivingReport.EwtAmount;
                var balance = amount - amountPaid;

                return Json(new { Amount = amount,
                                AmountPaid = amountPaid,
                                NetAmount = netAmount,
                                VatAmount = vatAmount,
                                EwtAmount = ewtAmount,
                                Balance = balance });
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

                    #region --General Ledger Book Recording(CV)--

                    var ledgers = new List<GeneralLedgerBook>();
                    foreach (var details in modelDetails)
                    {
                        ledgers.Add(
                                new GeneralLedgerBook
                                {
                                    Date = modelHeader.Date.ToShortDateString(),
                                    Reference = modelHeader.CVNo,
                                    Description = modelHeader.Particulars,
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

                    AuditTrail auditTrail = new(model.VoidedBy, $"Voided debit memo# {model.CVNo}", "Debit Memo");
                    await _dbContext.AddAsync(auditTrail, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Debit Memo has been Voided.";
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

                    AuditTrail auditTrail = new(model.CanceledBy, $"Cancelled credit memo# {model.CVNo}", "Credit Memo");
                    await _dbContext.AddAsync(auditTrail, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Credit Memo has been Cancelled.";
                }
                return RedirectToAction("Index");
            }

            return NotFound();
        }
    }
}