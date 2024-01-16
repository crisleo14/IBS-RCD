using Accounting_System.Data;
using Accounting_System.Models;
using Accounting_System.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Accounting_System.Controllers
{
    [Authorize]
    public class ReceiptController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly ReceiptRepo _receiptRepo;

        public ReceiptController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, ReceiptRepo receiptRepo)
        {
            _dbContext = dbContext;
            this._userManager = userManager;
            _receiptRepo = receiptRepo;
        }

        public async Task<IActionResult> CollectionIndex()
        {
            var viewData = await _receiptRepo.GetCRAsync();

            return View(viewData);
        }

        public async Task<IActionResult> OfficialIndex()
        {
            var viewData = await _receiptRepo.GetORAsync();

            return View(viewData);
        }

        public IActionResult CollectionCreate()
        {
            var viewModel = new CollectionReceipt();

            viewModel.Customers = _dbContext.Customers
               .OrderBy(c => c.Id)
               .Select(s => new SelectListItem
               {
                   Value = s.Number.ToString(),
                   Text = s.Name
               })
               .ToList();

            viewModel.ChartOfAccounts = _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == "4" || coa.Level == "5")
                .OrderBy(coa => coa.Id)
                .Select(s => new SelectListItem
                {
                    Value = s.Number,
                    Text = s.Number + " " + s.Name
                })
                .ToList();

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> CollectionCreate(CollectionReceipt model, string[] accountTitleText, decimal[] accountAmount, string[] accountTitle)
        {
            model.Customers = _dbContext.Customers
               .OrderBy(c => c.Id)
               .Select(s => new SelectListItem
               {
                   Value = s.Number.ToString(),
                   Text = s.Name
               })
               .ToList();

            model.Invoices = _dbContext.SalesInvoices
                .Where(si => !si.IsPaid && si.CustomerNo == model.CustomerNo)
                .OrderBy(si => si.Id)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.SINo
                })
                .ToList();

            model.ChartOfAccounts = _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == "4" || coa.Level == "5")
                .OrderBy(coa => coa.Id)
                .Select(s => new SelectListItem
                {
                    Value = s.Number,
                    Text = s.Number + " " + s.Name
                })
                .ToList();

            if (ModelState.IsValid)
            {
                #region --Validating the series

                var getLastNumber = await _receiptRepo.GetLastSeriesNumberCR();

                if (getLastNumber > 9999999999)
                {
                    TempData["error"] = "You reach the maximum Series Number";
                    return View(model);
                }
                var totalRemainingSeries = 9999999999 - getLastNumber;
                if (getLastNumber >= 9999999899)
                {
                    TempData["warning"] = $"Collection Receipt created successfully, Warning {totalRemainingSeries} series number remaining";
                }
                else
                {
                    TempData["success"] = "Collection Receipt created successfully";
                }

                #endregion --Validating the series

                #region --Saving default value

                var computeTotalInModelIfZero = model.CashAmount + model.CheckAmount + model.ManagerCheckAmount + model.EWT + model.WVAT;
                if (computeTotalInModelIfZero == 0)
                {
                    TempData["error"] = "Please input atleast one type form of payment";
                    return View(model);
                }
                var existingSalesInvoice = _dbContext.SalesInvoices
                                               .FirstOrDefault(si => si.Id == model.SalesInvoiceId);
                var generateCRNo = await _receiptRepo.GenerateCRNo();

                model.SeriesNumber = getLastNumber;
                model.SINo = existingSalesInvoice.SINo;
                model.CRNo = generateCRNo;
                model.CreatedBy = _userManager.GetUserName(this.User);
                model.Total = computeTotalInModelIfZero;

                decimal offsetAmount = 0;

                #endregion --Saving default value

                _dbContext.Add(model);

                #region --Audit Trail Recording

                AuditTrail auditTrail = new(model.CreatedBy, $"Create new collection receipt# {model.CRNo}", "Collection Receipt");
                _dbContext.Add(auditTrail);

                #endregion --Audit Trail Recording

                #region --Offsetting function

                var offsettings = new List<Offsetting>();

                for (int i = 0; i < accountTitle.Length; i++)
                {
                    var currentAccountTitle = accountTitleText[i];
                    var currentAccountAmount = accountAmount[i];
                    offsetAmount += accountAmount[i];

                    offsettings.Add(
                        new Offsetting
                        {
                            AccountNo = currentAccountTitle,
                            Source = model.CRNo,
                            Reference = model.SINo,
                            Amount = currentAccountAmount,
                            CreatedBy = model.CreatedBy,
                            CreatedDate = model.CreatedDate
                        }
                    );

                    _dbContext.AddRange(offsettings);
                }

                #endregion --Offsetting function

                await _dbContext.SaveChangesAsync();
                return RedirectToAction("CollectionIndex");
            }
            else
            {
                TempData["error"] = "The information you submitted is not valid!";
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult OfficialCreate()
        {
            var viewModel = new OfficialReceipt();

            viewModel.Customers = _dbContext.Customers
               .OrderBy(c => c.Id)
               .Select(s => new SelectListItem
               {
                   Value = s.Number.ToString(),
                   Text = s.Name
               })
               .ToList();

            viewModel.ChartOfAccounts = _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == "4" || coa.Level == "5")
                .OrderBy(coa => coa.Id)
                .Select(s => new SelectListItem
                {
                    Value = s.Number,
                    Text = s.Number + " " + s.Name
                })
                .ToList();

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> OfficialCreate(OfficialReceipt model, string[] accountTitleText, decimal[] accountAmount, string[] accountTitle)
        {
            model.Customers = _dbContext.Customers
               .OrderBy(c => c.Id)
               .Select(s => new SelectListItem
               {
                   Value = s.Number.ToString(),
                   Text = s.Name
               })
               .ToList();

            model.StatementOfAccounts = _dbContext.StatementOfAccounts
                .Where(s => !s.IsPaid && s.Customer.Number == model.CustomerNo)
                .OrderBy(si => si.Id)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.SOANo
                })
                .ToList();

            model.StatementOfAccounts = _dbContext.StatementOfAccounts
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.SOANo
                })
                .ToList();

            model.ChartOfAccounts = _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == "4" || coa.Level == "5")
                .OrderBy(coa => coa.Id)
                .Select(s => new SelectListItem
                {
                    Value = s.Number,
                    Text = s.Number + " " + s.Name
                })
                .ToList();

            if (ModelState.IsValid)
            {
                #region --Validating the series

                var getLastNumber = await _receiptRepo.GetLastSeriesNumberOR();

                if (getLastNumber > 9999999999)
                {
                    TempData["error"] = "You reach the maximum Series Number";
                    return View(model);
                }
                var totalRemainingSeries = 9999999999 - getLastNumber;
                if (getLastNumber >= 9999999899)
                {
                    TempData["warning"] = $"Official Receipt created successfully, Warning {totalRemainingSeries} series number remaining";
                }
                else
                {
                    TempData["success"] = "Official Receipt created successfully";
                }

                #endregion --Validating the series

                #region --Saving default value

                var computeTotalInModelIfZero = model.CashAmount + model.CheckAmount + model.EWT + model.WVAT;
                if (computeTotalInModelIfZero == 0)
                {
                    TempData["error"] = "Please input atleast one type form of payment";
                    return View(model);
                }
                var existingSOA = _dbContext.StatementOfAccounts
                                               .FirstOrDefault(si => si.Id == model.SOAId);

                var generateORNo = await _receiptRepo.GenerateORNo();

                model.SeriesNumber = getLastNumber;
                model.ORNo = generateORNo;
                model.SOANo = existingSOA.SOANo;
                model.Total = computeTotalInModelIfZero;
                model.CreatedBy = _userManager.GetUserName(this.User);

                decimal offsetAmount = 0;

                #endregion --Saving default value

                _dbContext.Add(model);

                #region --Audit Trail Recording

                AuditTrail auditTrail = new(model.CreatedBy, $"Create new official receipt# {model.ORNo}", "Official Receipt");
                _dbContext.Add(auditTrail);

                #endregion --Audit Trail Recording

                #region --Offsetting function

                var offsettings = new List<Offsetting>();

                for (int i = 0; i < accountTitle.Length; i++)
                {
                    var currentAccountTitle = accountTitleText[i];
                    var currentAccountAmount = accountAmount[i];
                    offsetAmount += accountAmount[i];

                    offsettings.Add(
                        new Offsetting
                        {
                            AccountNo = currentAccountTitle,
                            Source = model.ORNo,
                            Reference = existingSOA.SOANo,
                            Amount = currentAccountAmount,
                            CreatedBy = model.CreatedBy,
                            CreatedDate = model.CreatedDate
                        }
                    );

                    _dbContext.AddRange(offsettings);
                }

                #endregion --Offsetting function

                await _dbContext.SaveChangesAsync();
                return RedirectToAction("OfficialIndex");
            }
            else
            {
                ModelState.AddModelError("", "The information you submitted is not valid!");
                return View(model);
            }
        }

        public async Task<IActionResult> CollectionPrint(int id)
        {
            var cr = await _receiptRepo.FindCR(id);
            return View(cr);
        }

        public async Task<IActionResult> OfficialPrint(int id)
        {
            var or = await _receiptRepo.FindOR(id);
            return View(or);
        }

        public async Task<IActionResult> PrintedCR(int id)
        {
            var findIdOfCR = await _receiptRepo.FindCR(id);
            if (findIdOfCR != null && !findIdOfCR.IsPrinted)
            {
                findIdOfCR.IsPrinted = true;
                await _dbContext.SaveChangesAsync();
            }
            return RedirectToAction("CollectionPrint", new { id = id });
        }

        public async Task<IActionResult> PrintedOR(int id)
        {
            var findIdOfOR = await _receiptRepo.FindOR(id);
            if (findIdOfOR != null && !findIdOfOR.IsPrinted)
            {
                findIdOfOR.IsPrinted = true;
                await _dbContext.SaveChangesAsync();
            }
            return RedirectToAction("OfficialPrint", new { id = id });
        }

        [HttpGet]
        public async Task<IActionResult> GetSalesInvoices(int customerNo)
        {
            var invoices = await _dbContext
                .SalesInvoices
                .Where(si => si.CustomerNo == customerNo && !si.IsPaid)
                .OrderBy(si => si.Id)
                .ToListAsync();

            var invoiceList = invoices.Select(si => new SelectListItem
            {
                Value = si.Id.ToString(),   // Replace with your actual ID property
                Text = si.SINo              // Replace with your actual property for display text
            }).ToList();

            return Json(invoiceList);
        }

        [HttpGet]
        public async Task<IActionResult> GetInvoiceDetails(int invoiceNo)
        {
            var invoice = await _dbContext
                .SalesInvoices
                .FirstOrDefaultAsync(si => si.Id == invoiceNo);

            if (invoice != null)
            {
                return Json(new
                {
                    Amount = invoice.NetDiscount.ToString("0.00"),
                    AmountPaid = invoice.AmountPaid.ToString("0.00"),
                    Balance = invoice.Balance.ToString("0.00"),
                    Ewt = invoice.WithHoldingTaxAmount.ToString("0.00"),
                    Wvat = invoice.WithHoldingVatAmount.ToString("0.00"),
                    Total = (invoice.NetDiscount - (invoice.WithHoldingTaxAmount + invoice.WithHoldingVatAmount)).ToString("0.00")
                });
            }
            return Json(null);
        }

        [HttpGet]
        public async Task<IActionResult> CollectionEdit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var existingModel = await _dbContext.CollectionReceipts.FindAsync(id);

            if (existingModel == null)
            {
                return NotFound();
            }

            existingModel.Customers = _dbContext.Customers
               .OrderBy(c => c.Id)
               .Select(s => new SelectListItem
               {
                   Value = s.Number.ToString(),
                   Text = s.Name
               })
               .ToList();

            existingModel.Invoices = _dbContext.SalesInvoices
                .Where(si => !si.IsPaid && si.CustomerNo == existingModel.CustomerNo)
                .OrderBy(si => si.Id)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.SINo
                })
                .ToList();

            existingModel.ChartOfAccounts = _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == "4" || coa.Level == "5")
                .OrderBy(coa => coa.Id)
                .Select(s => new SelectListItem
                {
                    Value = s.Number,
                    Text = s.Number + " " + s.Name
                })
                .ToList();

            var findCustomers = await _dbContext.Customers
                .FirstOrDefaultAsync(c => c.Number == existingModel.CustomerNo);

            ViewBag.CustomerName = findCustomers?.Name;

            var matchingOffsettings = await _dbContext.Offsettings
            .Where(offset => offset.Source == existingModel.CRNo)
            .ToListAsync();

            ViewBag.fetchAccEntries = matchingOffsettings
                .Select(offset => new { AccountNo = offset.AccountNo, Amount = offset.Amount.ToString("N2") })
                .ToList();

            return View(existingModel);
        }

        [HttpPost]
        public async Task<IActionResult> CollectionEdit(CollectionReceipt model, string[] editAccountTitleText, decimal[] editAccountAmount, string[] editAccountTitle)
        {
            var existingModel = await _receiptRepo.FindCR(model.Id);

            if (ModelState.IsValid)
            {
                #region --Validating the series

                var getLastNumber = await _receiptRepo.GetLastSeriesNumberCR();

                if (getLastNumber > 9999999999)
                {
                    TempData["error"] = "You reach the maximum Series Number";
                    return View(model);
                }
                var totalRemainingSeries = 9999999999 - getLastNumber;
                if (getLastNumber >= 9999999899)
                {
                    TempData["warning"] = $"Collection Receipt created successfully, Warning {totalRemainingSeries} series number remaining";
                }
                else
                {
                    TempData["success"] = "Collection Receipt created successfully";
                }

                #endregion --Validating the series

                #region --Saving default value

                var computeTotalInModelIfZero = model.CashAmount + model.CheckAmount + model.ManagerCheckAmount + model.EWT + model.WVAT;
                if (computeTotalInModelIfZero == 0)
                {
                    TempData["error"] = "Please input atleast one type form of payment";
                    return View(model);
                }

                existingModel.Date = model.Date;
                existingModel.ReferenceNo = model.ReferenceNo;
                existingModel.Remarks = model.Remarks;
                existingModel.CheckDate = model.CheckDate;
                existingModel.CheckNo = model.CheckNo;
                existingModel.CheckBank = model.CheckBank;
                existingModel.CheckBranch = model.CheckBranch;
                existingModel.CashAmount = model.CashAmount;
                existingModel.CheckAmount = model.CheckAmount;
                existingModel.ManagerCheckAmount = model.ManagerCheckAmount;
                existingModel.EWT = model.EWT;
                existingModel.WVAT = model.WVAT;
                existingModel.Total = computeTotalInModelIfZero;

                decimal offsetAmount = 0;

                #endregion --Saving default value

                #region --Offsetting function

                var offsetting = new List<Offsetting>();

                for (int i = 0; i < editAccountTitleText.Length; i++)
                {
                    var existingOffset = await _dbContext.Offsettings
                        .FirstOrDefaultAsync(offset => offset.Source == existingModel.CRNo
                                                        && offset.AccountNo == editAccountTitleText[i]
                                                        && offset.Amount == editAccountAmount[i]);

                    if (existingOffset == null)
                    {
                        var accountTitle = editAccountTitleText[i];
                        var accountAmount = editAccountAmount[i];
                        offsetAmount += editAccountAmount[i];

                        offsetting.Add(
                            new Offsetting
                            {
                                AccountNo = accountTitle,
                                Source = existingModel.CRNo,
                                Amount = accountAmount,
                                CreatedBy = existingModel.CreatedBy,
                                CreatedDate = existingModel.CreatedDate
                            }
                        );
                    }

                    if (existingOffset != null && existingOffset.IsRemoved)
                    {
                        _dbContext.Offsettings.Remove(existingOffset);
                        await _dbContext.SaveChangesAsync();
                    }
                }

                if (offsetting.Any())
                {
                    _dbContext.AddRange(offsetting);
                    await _dbContext.SaveChangesAsync();
                }

                #endregion --Offsetting function

                #region --Audit Trail Recording

                var modifiedBy = _userManager.GetUserName(this.User);
                AuditTrail auditTrail = new(modifiedBy, $"Edited receipt# {model.SINo}", "Collection Receipt");
                _dbContext.Add(auditTrail);

                #endregion --Audit Trail Recording

                await _dbContext.SaveChangesAsync();
                return RedirectToAction("CollectionIndex");
            }
            else
            {
                TempData["error"] = "The information you submitted is not valid!";
                return View(model);
            }
        }

        public async Task<IActionResult> Post(int itemId)
        {
            var model = await _receiptRepo.FindCR(itemId);

            if (model != null)
            {
                if (!model.IsPosted)
                {
                    model.IsPosted = true;
                    model.PostedBy = _userManager.GetUserName(this.User);
                    model.PostedDate = DateTime.Now;

                    var offset = await _receiptRepo.GetOffsettingAsync(model.CRNo, model.SINo);

                    decimal offsetAmount = 0;

                    #region --General Ledger Book Recording

                    var ledgers = new List<GeneralLedgerBook>();

                    ledgers.Add(
                                new GeneralLedgerBook
                                {
                                    Date = model.Date.ToShortDateString(),
                                    Reference = model.CRNo,
                                    Description = "Collection for Receivable",
                                    AccountTitle = "1010101 Cash in Bank",
                                    Debit = model.CashAmount + model.CheckAmount + model.ManagerCheckAmount,
                                    Credit = 0,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );

                    if (model.EWT > 0)
                    {
                        ledgers.Add(
                            new GeneralLedgerBook
                            {
                                Date = model.Date.ToShortDateString(),
                                Reference = model.CRNo,
                                Description = "Collection for Receivable",
                                AccountTitle = "1010604 Creditable Withholding Tax",
                                Debit = model.EWT,
                                Credit = 0,
                                CreatedBy = model.CreatedBy,
                                CreatedDate = model.CreatedDate
                            }
                        );
                    }

                    if (model.WVAT > 0)
                    {
                        ledgers.Add(
                            new GeneralLedgerBook
                            {
                                Date = model.Date.ToShortDateString(),
                                Reference = model.CRNo,
                                Description = "Collection for Receivable",
                                AccountTitle = "1010605 Creditable Withholding Vat",
                                Debit = model.WVAT,
                                Credit = 0,
                                CreatedBy = model.CreatedBy,
                                CreatedDate = model.CreatedDate
                            }
                        );
                    }

                    if (offset != null)
                    {
                        foreach (var item in offset)
                        {
                            ledgers.Add(
                            new GeneralLedgerBook
                            {
                                Date = model.Date.ToShortDateString(),
                                Reference = model.CRNo,
                                Description = "Collection for Receivable",
                                AccountTitle = item.AccountNo,
                                Debit = item.Amount,
                                Credit = 0,
                                CreatedBy = model.CreatedBy,
                                CreatedDate = model.CreatedDate
                            }
                            );

                            offsetAmount += item.Amount;
                        }
                    }

                    ledgers.Add(
                            new GeneralLedgerBook
                            {
                                Date = model.Date.ToShortDateString(),
                                Reference = model.CRNo,
                                Description = "Collection for Receivable",
                                AccountTitle = "1010201 AR-Trade Receivable",
                                Debit = 0,
                                Credit = model.CashAmount + model.CheckAmount + model.ManagerCheckAmount + offsetAmount,
                                CreatedBy = model.CreatedBy,
                                CreatedDate = model.CreatedDate
                            }
                        );

                    if (model.EWT > 0)
                    {
                        ledgers.Add(
                            new GeneralLedgerBook
                            {
                                Date = model.Date.ToShortDateString(),
                                Reference = model.CRNo,
                                Description = "Collection for Receivable",
                                AccountTitle = "1010202 Deferred Creditable Withholding Tax",
                                Debit = 0,
                                Credit = model.EWT,
                                CreatedBy = model.CreatedBy,
                                CreatedDate = model.CreatedDate
                            }
                        );
                    }

                    if (model.WVAT > 0)
                    {
                        ledgers.Add(
                            new GeneralLedgerBook
                            {
                                Date = model.Date.ToShortDateString(),
                                Reference = model.CRNo,
                                Description = "Collection for Receivable",
                                AccountTitle = "1010203 Deferred Creditable Withholding Vat",
                                Debit = 0,
                                Credit = model.WVAT,
                                CreatedBy = model.CreatedBy,
                                CreatedDate = model.CreatedDate
                            }
                        );
                    }

                    _dbContext.AddRange(ledgers);

                    #endregion --General Ledger Book Recording

                    #region --Cash Receipt Book Recording

                    var crb = new List<CashReceiptBook>();

                    crb.Add(
                        new CashReceiptBook
                        {
                            Date = model.Date.ToShortDateString(),
                            RefNo = model.CRNo,
                            CustomerName = model.SalesInvoice.SoldTo,
                            Bank = model.CheckBank ?? (model.ManagerCheckBank != null ? model.ManagerCheckBank : "--"),
                            CheckNo = model.CheckNo ?? (model.ManagerCheckNo != null ? model.ManagerCheckNo : "--"),
                            COA = "1010101 Cash in Bank",
                            Particulars = model.SalesInvoice.SINo,
                            Debit = model.CashAmount + model.CheckAmount + model.ManagerCheckAmount,
                            Credit = 0,
                            CreatedBy = model.CreatedBy,
                            CreatedDate = model.CreatedDate
                        }

                    );

                    if (model.EWT > 0)
                    {
                        crb.Add(
                            new CashReceiptBook
                            {
                                Date = model.Date.ToShortDateString(),
                                RefNo = model.CRNo,
                                CustomerName = model.SalesInvoice.SoldTo,
                                Bank = model.CheckBank ?? (model.ManagerCheckBank != null ? model.ManagerCheckBank : "--"),
                                CheckNo = model.CheckNo ?? (model.ManagerCheckNo != null ? model.ManagerCheckNo : "--"),
                                COA = "1010604 Creditable Withholding Tax",
                                Particulars = model.SalesInvoice.SINo,
                                Debit = model.EWT,
                                Credit = 0,
                                CreatedBy = model.CreatedBy,
                                CreatedDate = model.CreatedDate
                            }
                        );
                    }

                    if (model.WVAT > 0)
                    {
                        crb.Add(
                            new CashReceiptBook
                            {
                                Date = model.Date.ToShortDateString(),
                                RefNo = model.CRNo,
                                CustomerName = model.SalesInvoice.SoldTo,
                                Bank = model.CheckBank ?? (model.ManagerCheckBank != null ? model.ManagerCheckBank : "--"),
                                CheckNo = model.CheckNo ?? (model.ManagerCheckNo != null ? model.ManagerCheckNo : "--"),
                                COA = "1010605 Creditable Withholding Vat",
                                Particulars = model.SalesInvoice.SINo,
                                Debit = model.WVAT,
                                Credit = 0,
                                CreatedBy = model.CreatedBy,
                                CreatedDate = model.CreatedDate
                            }
                        );
                    }

                    if (offset != null)
                    {
                        foreach (var item in offset)
                        {
                            crb.Add(
                                new CashReceiptBook
                                {
                                    Date = model.Date.ToShortDateString(),
                                    RefNo = model.CRNo,
                                    CustomerName = model.SalesInvoice.SoldTo,
                                    Bank = model.CheckBank ?? (model.ManagerCheckBank != null ? model.ManagerCheckBank : "--"),
                                    CheckNo = model.CheckNo ?? (model.ManagerCheckNo != null ? model.ManagerCheckNo : "--"),
                                    COA = item.AccountNo,
                                    Particulars = model.SalesInvoice.SINo,
                                    Debit = item.Amount,
                                    Credit = 0,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }
                    }

                    crb.Add(
                    new CashReceiptBook
                    {
                        Date = model.Date.ToShortDateString(),
                        RefNo = model.CRNo,
                        CustomerName = model.SalesInvoice.SoldTo,
                        Bank = model.CheckBank ?? (model.ManagerCheckBank != null ? model.ManagerCheckBank : "--"),
                        CheckNo = model.CheckNo ?? (model.ManagerCheckNo != null ? model.ManagerCheckNo : "--"),
                        COA = "1010201 AR-Trade Receivable",
                        Particulars = model.SalesInvoice.SINo,
                        Debit = 0,
                        Credit = model.CashAmount + model.CheckAmount + model.ManagerCheckAmount + offsetAmount,
                        CreatedBy = model.CreatedBy,
                        CreatedDate = model.CreatedDate
                    }
                    );

                    if (model.EWT > 0)
                    {
                        crb.Add(
                            new CashReceiptBook
                            {
                                Date = model.Date.ToShortDateString(),
                                RefNo = model.CRNo,
                                CustomerName = model.SalesInvoice.SoldTo,
                                Bank = model.CheckBank ?? (model.ManagerCheckBank != null ? model.ManagerCheckBank : "--"),
                                CheckNo = model.CheckNo ?? (model.ManagerCheckNo != null ? model.ManagerCheckNo : "--"),
                                COA = "1010202 Deferred Creditable Withholding Tax",
                                Particulars = model.SalesInvoice.SINo,
                                Debit = 0,
                                Credit = model.EWT,
                                CreatedBy = model.CreatedBy,
                                CreatedDate = model.CreatedDate
                            }
                        );
                    }

                    if (model.WVAT > 0)
                    {
                        crb.Add(
                            new CashReceiptBook
                            {
                                Date = model.Date.ToShortDateString(),
                                RefNo = model.CRNo,
                                CustomerName = model.SalesInvoice.SoldTo,
                                Bank = model.CheckBank ?? (model.ManagerCheckBank != null ? model.ManagerCheckBank : "--"),
                                CheckNo = model.CheckNo ?? (model.ManagerCheckNo != null ? model.ManagerCheckNo : "--"),
                                COA = "1010203 Deferred Creditable Withholding Vat",
                                Particulars = model.SalesInvoice.SINo,
                                Debit = 0,
                                Credit = model.WVAT,
                                CreatedBy = model.CreatedBy,
                                CreatedDate = model.CreatedDate
                            }
                        );
                    }

                    _dbContext.AddRange(crb);

                    #endregion --Cash Receipt Book Recording

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(model.PostedBy, $"Posted collection# {model.CRNo}", "Collection Receipt");
                    _dbContext.Add(auditTrail);

                    #endregion --Audit Trail Recording

                    await _receiptRepo.UpdateInvoice(model.SalesInvoice.Id, model.Total, offsetAmount);

                    await _dbContext.SaveChangesAsync();
                    TempData["success"] = "Collection Receipt has been Posted.";
                }
                return RedirectToAction("CollectionIndex");
            }

            return NotFound();
        }

        public async Task<IActionResult> Void(int itemId)
        {
            var model = await _dbContext.CollectionReceipts.FindAsync(itemId);

            if (model != null)
            {
                if (!model.IsVoided)
                {
                    model.IsVoided = true;
                    model.VoidedBy = _userManager.GetUserName(this.User);
                    model.VoidedDate = DateTime.Now;

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(model.VoidedBy, $"Voided collection receipt# {model.CRNo}", "Collection Receipt");
                    _dbContext.Add(auditTrail);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync();
                    TempData["success"] = "Collection Receipt has been Voided.";
                }
                return RedirectToAction("CollectionIndex");
            }

            return NotFound();
        }

        public async Task<IActionResult> Cancel(int itemId)
        {
            var model = await _dbContext.CollectionReceipts.FindAsync(itemId);

            if (model != null)
            {
                if (!model.IsCanceled)
                {
                    model.IsCanceled = true;
                    model.CanceledBy = _userManager.GetUserName(this.User);
                    model.CanceledDate = DateTime.Now;

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(model.CanceledBy, $"Canceled collection receipt# {model.CRNo}", "Collection Receipt");
                    _dbContext.Add(auditTrail);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync();
                    TempData["success"] = "Collection Receipt has been Canceled.";
                }
                return RedirectToAction("CollectionIndex");
            }

            return NotFound();
        }

        [HttpGet]
        public async Task<IActionResult> GetStatementOfAccount(int customerNo)
        {
            var soa = await _dbContext
                .StatementOfAccounts
                .Where(s => s.Customer.Number == customerNo && !s.IsPaid)
                .OrderBy(s => s.Id)
                .ToListAsync();

            var soaList = soa.Select(si => new SelectListItem
            {
                Value = si.Id.ToString(),   // Replace with your actual ID property
                Text = si.SOANo              // Replace with your actual property for display text
            }).ToList();

            return Json(soaList);
        }

        [HttpGet]
        public async Task<IActionResult> GetSOADetails(int soaNo)
        {
            var soa = await _dbContext
                .StatementOfAccounts
                .FirstOrDefaultAsync(s => s.Id == soaNo);

            if (soa != null)
            {
                return Json(new
                {
                    Amount = (soa.Total - soa.Discount).ToString("0.00"),
                    AmountPaid = soa.AmountPaid.ToString("0.00"),
                    Balance = soa.Balance.ToString("0.00"),
                    Ewt = soa.WithholdingTaxAmount.ToString("0.00"),
                    Wvat = soa.WithholdingVatAmount.ToString("0.00"),
                    Total = (soa.Total - soa.Discount - (soa.WithholdingTaxAmount + soa.WithholdingVatAmount)).ToString("0.00")
                });
            }
            return Json(null);
        }

        [HttpGet]
        public async Task<IActionResult> OfficialEdit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var existingModel = await _dbContext.OfficialReceipts.FindAsync(id);

            if (existingModel == null)
            {
                return NotFound();
            }

            existingModel.Customers = _dbContext.Customers
               .OrderBy(c => c.Id)
               .Select(s => new SelectListItem
               {
                   Value = s.Number.ToString(),
                   Text = s.Name
               })
               .ToList();

            existingModel.StatementOfAccounts = _dbContext.StatementOfAccounts
                .Where(s => !s.IsPaid && s.Customer.Number == existingModel.CustomerNo)
                .OrderBy(si => si.Id)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.SOANo
                })
                .ToList();

            existingModel.ChartOfAccounts = _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == "4" || coa.Level == "5")
                .OrderBy(coa => coa.Id)
                .Select(s => new SelectListItem
                {
                    Value = s.Number,
                    Text = s.Number + " " + s.Name
                })
                .ToList();

            var findCustomers = await _dbContext.Customers
                .FirstOrDefaultAsync(c => c.Number == existingModel.CustomerNo);

            ViewBag.CustomerName = findCustomers?.Name;

            var matchingOffsettings = await _dbContext.Offsettings
            .Where(offset => offset.Source == existingModel.ORNo)
            .ToListAsync();

            ViewBag.fetchAccEntries = matchingOffsettings
                .Select(offset => new { AccountNo = offset.AccountNo, Amount = offset.Amount.ToString("N2") })
                .ToList();


            return View(existingModel);
        }

        [HttpPost]
        public async Task<IActionResult> OfficialEdit(OfficialReceipt model, string[] editAccountTitleText, decimal[] editAccountAmount, string[] editAccountTitle)
        {
            var existingModel = await _receiptRepo.FindOR(model.Id);

            if (ModelState.IsValid)
            {
                #region --Validating the series

                var getLastNumber = await _receiptRepo.GetLastSeriesNumberOR();

                if (getLastNumber > 9999999999)
                {
                    TempData["error"] = "You reach the maximum Series Number";
                    return View(model);
                }
                var totalRemainingSeries = 9999999999 - getLastNumber;
                if (getLastNumber >= 9999999899)
                {
                    TempData["warning"] = $"Official Receipt created successfully, Warning {totalRemainingSeries} series number remaining";
                }
                else
                {
                    TempData["success"] = "Official Receipt created successfully";
                }

                #endregion --Validating the series

                #region --Saving default value

                var computeTotalInModelIfZero = model.CashAmount + model.CheckAmount + model.EWT + model.WVAT;
                if (computeTotalInModelIfZero == 0)
                {
                    TempData["error"] = "Please input atleast one type form of payment";
                    return View(model);
                }

                existingModel.Date = model.Date;
                existingModel.ReferenceNo = model.ReferenceNo;
                existingModel.Remarks = model.Remarks;
                existingModel.CheckNo = model.CheckNo;
                existingModel.CashAmount = model.CashAmount;
                existingModel.CheckAmount = model.CheckAmount;
                existingModel.EWT = model.EWT;
                existingModel.WVAT = model.WVAT;
                existingModel.Total = computeTotalInModelIfZero;

                decimal offsetAmount = 0;

                #endregion --Saving default value

                #region --Offsetting function
                var offsetting = new List<Offsetting>();

                for (int i = 0; i < editAccountTitleText.Length; i++)
                {
                    var existingOffset = await _dbContext.Offsettings
                        .FirstOrDefaultAsync(offset => offset.Source == existingModel.ORNo
                                                        && offset.AccountNo == editAccountTitleText[i]
                                                        && offset.Amount == editAccountAmount[i]);

                    if (existingOffset == null)
                    {
                        var accountTitle = editAccountTitleText[i];
                        var accountAmount = editAccountAmount[i];
                        offsetAmount += editAccountAmount[i];

                        offsetting.Add(
                            new Offsetting
                            {
                                AccountNo = accountTitle,
                                Source = existingModel.ORNo,
                                Amount = accountAmount,
                                CreatedBy = existingModel.CreatedBy,
                                CreatedDate = existingModel.CreatedDate
                            }
                        );
                    }

                    if (existingOffset != null && existingOffset.IsRemoved)
                    {
                        _dbContext.Offsettings.Remove(existingOffset);
                        await _dbContext.SaveChangesAsync();
                    }
                }

                if (offsetting.Any())
                {
                    _dbContext.AddRange(offsetting);
                    await _dbContext.SaveChangesAsync();
                }

                #endregion

                #region --Audit Trail Recording

                var modifiedBy = _userManager.GetUserName(this.User);
                AuditTrail auditTrail = new(modifiedBy, $"Edited receipt# {model.SOANo}", "Official Receipt");
                _dbContext.Add(auditTrail);

                #endregion --Audit Trail Recording

                await _dbContext.SaveChangesAsync();
                return RedirectToAction("OfficialIndex");
            }
            else
            {
                TempData["error"] = "The information you submitted is not valid!";
                return View(model);
            }
        }

        public async Task<IActionResult> PostOR(int itemId)
        {
            var model = await _receiptRepo.FindOR(itemId);

            if (model != null)
            {
                if (!model.IsPosted)
                {
                    model.IsPosted = true;
                    model.PostedBy = _userManager.GetUserName(this.User);
                    model.PostedDate = DateTime.Now;

                    var offset = await _receiptRepo.GetOffsettingAsync(model.ORNo, model.SOANo);

                    decimal offsetAmount = 0;

                    #region --General Ledger Book Recording

                    var ledgers = new List<GeneralLedgerBook>();

                    ledgers.Add(
                                new GeneralLedgerBook
                                {
                                    Date = model.Date.ToShortDateString(),
                                    Reference = model.ORNo,
                                    Description = "Collection for Receivable",
                                    AccountTitle = "1010101 Cash in Bank",
                                    Debit = model.CashAmount + model.CheckAmount,
                                    Credit = 0,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );

                    if (model.EWT > 0)
                    {
                        ledgers.Add(
                            new GeneralLedgerBook
                            {
                                Date = model.Date.ToShortDateString(),
                                Reference = model.ORNo,
                                Description = "Collection for Receivable",
                                AccountTitle = "1010604 Creditable Withholding Tax",
                                Debit = model.EWT,
                                Credit = 0,
                                CreatedBy = model.CreatedBy,
                                CreatedDate = model.CreatedDate
                            }
                        );
                    }

                    if (model.WVAT > 0)
                    {
                        ledgers.Add(
                            new GeneralLedgerBook
                            {
                                Date = model.Date.ToShortDateString(),
                                Reference = model.ORNo,
                                Description = "Collection for Receivable",
                                AccountTitle = "1010605 Creditable Withholding Vat",
                                Debit = model.WVAT,
                                Credit = 0,
                                CreatedBy = model.CreatedBy,
                                CreatedDate = model.CreatedDate
                            }
                        );
                    }

                    if (offset != null)
                    {
                        foreach (var item in offset)
                        {
                            ledgers.Add(
                            new GeneralLedgerBook
                            {
                                Date = model.Date.ToShortDateString(),
                                Reference = model.ORNo,
                                Description = "Collection for Receivable",
                                AccountTitle = item.AccountNo,
                                Debit = item.Amount,
                                Credit = 0,
                                CreatedBy = model.CreatedBy,
                                CreatedDate = model.CreatedDate
                            }
                            );

                            offsetAmount += item.Amount;
                        }
                    }

                    ledgers.Add(
                            new GeneralLedgerBook
                            {
                                Date = model.Date.ToShortDateString(),
                                Reference = model.ORNo,
                                Description = "Collection for Receivable",
                                AccountTitle = "1010201 AR-Trade Receivable",
                                Debit = 0,
                                Credit = model.CashAmount + model.CheckAmount + offsetAmount,
                                CreatedBy = model.CreatedBy,
                                CreatedDate = model.CreatedDate
                            }
                        );

                    if (model.EWT > 0)
                    {
                        ledgers.Add(
                            new GeneralLedgerBook
                            {
                                Date = model.Date.ToShortDateString(),
                                Reference = model.ORNo,
                                Description = "Collection for Receivable",
                                AccountTitle = "1010202 Deferred Creditable Withholding Tax",
                                Debit = 0,
                                Credit = model.EWT,
                                CreatedBy = model.CreatedBy,
                                CreatedDate = model.CreatedDate
                            }
                        );
                    }

                    if (model.WVAT > 0)
                    {
                        ledgers.Add(
                            new GeneralLedgerBook
                            {
                                Date = model.Date.ToShortDateString(),
                                Reference = model.ORNo,
                                Description = "Collection for Receivable",
                                AccountTitle = "1010203 Deferred Creditable Withholding Vat",
                                Debit = 0,
                                Credit = model.WVAT,
                                CreatedBy = model.CreatedBy,
                                CreatedDate = model.CreatedDate
                            }
                        );
                    }

                    _dbContext.AddRange(ledgers);

                    #endregion --General Ledger Book Recording

                    #region --Cash Receipt Book Recording

                    var crb = new List<CashReceiptBook>();

                    crb.Add(
                        new CashReceiptBook
                        {
                            Date = model.Date.ToShortDateString(),
                            RefNo = model.ORNo,
                            CustomerName = model.StatementOfAccount.Customer.Name,
                            Bank = "--",
                            CheckNo = "--",
                            COA = "1010101 Cash in Bank",
                            Particulars = model.StatementOfAccount.SOANo,
                            Debit = model.CashAmount + model.CheckAmount,
                            Credit = 0,
                            CreatedBy = model.CreatedBy,
                            CreatedDate = model.CreatedDate
                        }

                    );

                    if (model.EWT > 0)
                    {
                        crb.Add(
                            new CashReceiptBook
                            {
                                Date = model.Date.ToShortDateString(),
                                RefNo = model.ORNo,
                                CustomerName = model.StatementOfAccount.Customer.Name,
                                Bank = "--",
                                CheckNo = "--",
                                COA = "1010604 Creditable Withholding Tax",
                                Particulars = model.StatementOfAccount.SOANo,
                                Debit = model.EWT,
                                Credit = 0,
                                CreatedBy = model.CreatedBy,
                                CreatedDate = model.CreatedDate
                            }
                        );
                    }

                    if (model.WVAT > 0)
                    {
                        crb.Add(
                            new CashReceiptBook
                            {
                                Date = model.Date.ToShortDateString(),
                                RefNo = model.ORNo,
                                CustomerName = model.StatementOfAccount.Customer.Name,
                                Bank = "--",
                                CheckNo = "--",
                                COA = "1010605 Creditable Withholding Vat",
                                Particulars = model.StatementOfAccount.SOANo,
                                Debit = model.WVAT,
                                Credit = 0,
                                CreatedBy = model.CreatedBy,
                                CreatedDate = model.CreatedDate
                            }
                        );
                    }

                    if (offset != null)
                    {
                        foreach (var item in offset)
                        {
                            crb.Add(
                                new CashReceiptBook
                                {
                                    Date = model.Date.ToShortDateString(),
                                    RefNo = model.ORNo,
                                    CustomerName = model.StatementOfAccount.Customer.Name,
                                    Bank = "--",
                                    CheckNo = "--",
                                    COA = item.AccountNo,
                                    Particulars = model.StatementOfAccount.SOANo,
                                    Debit = item.Amount,
                                    Credit = 0,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }
                    }

                    crb.Add(
                    new CashReceiptBook
                    {
                        Date = model.Date.ToShortDateString(),
                        RefNo = model.ORNo,
                        CustomerName = model.StatementOfAccount.Customer.Name,
                        Bank = "--",
                        CheckNo = "--",
                        COA = "1010201 AR-Trade Receivable",
                        Particulars = model.StatementOfAccount.SOANo,
                        Debit = 0,
                        Credit = model.CashAmount + model.CheckAmount + offsetAmount,
                        CreatedBy = model.CreatedBy,
                        CreatedDate = model.CreatedDate
                    }
                    );

                    if (model.EWT > 0)
                    {
                        crb.Add(
                            new CashReceiptBook
                            {
                                Date = model.Date.ToShortDateString(),
                                RefNo = model.ORNo,
                                CustomerName = model.StatementOfAccount.Customer.Name,
                                Bank = "--",
                                CheckNo = "--",
                                COA = "1010202 Deferred Creditable Withholding Tax",
                                Particulars = model.StatementOfAccount.SOANo,
                                Debit = 0,
                                Credit = model.EWT,
                                CreatedBy = model.CreatedBy,
                                CreatedDate = model.CreatedDate
                            }
                        );
                    }

                    if (model.WVAT > 0)
                    {
                        crb.Add(
                            new CashReceiptBook
                            {
                                Date = model.Date.ToShortDateString(),
                                RefNo = model.ORNo,
                                CustomerName = model.StatementOfAccount.Customer.Name,
                                Bank = "--",
                                CheckNo = "--",
                                COA = "1010203 Deferred Creditable Withholding Vat",
                                Particulars = model.StatementOfAccount.SOANo,
                                Debit = 0,
                                Credit = model.WVAT,
                                CreatedBy = model.CreatedBy,
                                CreatedDate = model.CreatedDate
                            }
                        );
                    }

                    _dbContext.AddRange(crb);

                    #endregion --Cash Receipt Book Recording

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(model.PostedBy, $"Posted official# {model.ORNo}", "Official Receipt");
                    _dbContext.Add(auditTrail);

                    #endregion --Audit Trail Recording

                    await _receiptRepo.UpdateSoa(model.StatementOfAccount.Id, model.Total, offsetAmount);

                    await _dbContext.SaveChangesAsync();
                    TempData["success"] = "Official Receipt has been Posted.";
                }
                return RedirectToAction("OfficialIndex");
            }

            return NotFound();
        }

        public async Task<IActionResult> VoidOR(int itemId)
        {
            var model = await _dbContext.OfficialReceipts.FindAsync(itemId);

            if (model != null)
            {
                if (!model.IsVoided)
                {
                    model.IsVoided = true;
                    model.VoidedBy = _userManager.GetUserName(this.User);
                    model.VoidedDate = DateTime.Now;

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(model.VoidedBy, $"Voided official receipt# {model.ORNo}", "Official Receipt");
                    _dbContext.Add(auditTrail);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync();
                    TempData["success"] = "Official Receipt has been Voided.";
                }
                return RedirectToAction("OfficialIndex");
            }

            return NotFound();
        }

        public async Task<IActionResult> CancelOR(int itemId)
        {
            var model = await _dbContext.OfficialReceipts.FindAsync(itemId);

            if (model != null)
            {
                if (!model.IsCanceled)
                {
                    model.IsCanceled = true;
                    model.CanceledBy = _userManager.GetUserName(this.User);
                    model.CanceledDate = DateTime.Now;

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(model.CanceledBy, $"Canceled official receipt# {model.ORNo}", "Official Receipt");
                    _dbContext.Add(auditTrail);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync();
                    TempData["success"] = "Official Receipt has been Canceled.";
                }
                return RedirectToAction("OfficialIndex");
            }

            return NotFound();
        }
    }
}