using Accounting_System.Data;
using Accounting_System.Models;
using Accounting_System.Repository;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Accounting_System.Controllers
{
    public class DebitMemoController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly DebitMemoRepo _debitMemoRepo;

        public DebitMemoController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, DebitMemoRepo dmcmRepo)
        {
            _dbContext = dbContext;
            this._userManager = userManager;
            _debitMemoRepo = dmcmRepo;
        }

        public async Task<IActionResult> Index()
        {
            var dm = await _dbContext.DebitMemos
                .Include(dm => dm.SalesInvoice)
                .Include(dm => dm.SOA)
                .ThenInclude(soa => soa.Customer)
                .Include(dm => dm.SOA)
                .ThenInclude(soa => soa.Service)
                .OrderBy(dm => dm.Id)
                .ToListAsync();
            return View(dm);
        }

        [HttpGet]
        public IActionResult Create()
        {
            var viewModel = new DebitMemo();
            viewModel.SalesInvoices = _dbContext.SalesInvoices
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.SINo
                })
                .ToList();
            viewModel.StatementOfAccounts = _dbContext.StatementOfAccounts
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.SOANo
                })
                .ToList();

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(DebitMemo model, DateTime[] period)
        {
            if (ModelState.IsValid)
            {
                var getLastNumber = await _debitMemoRepo.GetLastSeriesNumber();

                if (getLastNumber > 9999999999)
                {
                    TempData["error"] = "You reach the maximum Series Number";
                    return View(model);
                }
                var totalRemainingSeries = 9999999999 - getLastNumber;
                if (getLastNumber >= 9999999899)
                {
                    TempData["warning"] = $"Debit Memo created successfully, Warning {totalRemainingSeries} series number remaining";
                }
                else
                {
                    TempData["success"] = "Debit Memo created successfully";
                }

                var generateDMNo = await _debitMemoRepo.GenerateDMNo();

                model.SeriesNumber = getLastNumber;
                model.DMNo = generateDMNo;

                if (model.Source == "Sales Invoice")
                {
                    model.SOAId = null;

                    var existingSalesInvoice = _dbContext.SalesInvoices
                                               .FirstOrDefault(si => si.Id == model.SalesInvoiceId);

                    model.DebitAmount = model.AdjustedPrice * existingSalesInvoice.Quantity;

                    if (existingSalesInvoice.CustomerType == "Vatable")
                    {
                        model.VatableSales = model.DebitAmount / (decimal)1.12;
                        model.VatAmount = model.DebitAmount - model.VatableSales;
                        model.TotalSales = model.VatableSales + model.VatAmount;

                        if (existingSalesInvoice.WithHoldingTaxAmount != 0)
                        {
                            model.WithHoldingTaxAmount = model.VatableSales * 0.01m;
                        }
                        if (existingSalesInvoice.WithHoldingVatAmount != 0)
                        {
                            model.WithHoldingVatAmount = model.VatableSales * 0.05m;
                        }
                    }

                    model.TotalSales = model.DebitAmount;
                }
                else if (model.Source == "Statement Of Account")
                {
                    model.SalesInvoiceId = null;

                    var existingSoa = _dbContext.StatementOfAccounts
                        .Include(soa => soa.Customer)
                        .FirstOrDefault(soa => soa.Id == model.SOAId);

                    model.DebitAmount = model.AdjustedPrice - existingSoa.Total;

                    if (existingSoa.Customer.CustomerType == "Vatable")
                    {
                        model.VatableSales = model.DebitAmount / (decimal)1.12;
                        model.VatAmount = model.DebitAmount - model.VatableSales;
                        model.TotalSales = model.VatableSales + model.VatAmount;

                        if (existingSoa.WithholdingTaxAmount != 0)
                        {
                            model.WithHoldingTaxAmount = model.VatableSales * 0.01m;
                        }
                        if (existingSoa.WithholdingVatAmount != 0)
                        {
                            model.WithHoldingVatAmount = model.VatableSales * 0.05m;
                        }
                    }

                    model.TotalSales = model.DebitAmount;

                    #region --CM Entries function

                    for (int i = 0; i < period.Length; i++)
                    {
                        if (model.CreatedDate < period[i])
                        {
                            model.UnearnedAmount += model.Amount[i];
                        }
                        else
                        {
                            model.CurrentAndPreviousAmount += model.Amount[i];
                        }
                    }

                    #endregion ----CM Entries function
                }

                model.CreatedBy = _userManager.GetUserName(this.User);
                _dbContext.Add(model);
                await _dbContext.SaveChangesAsync();

                return RedirectToAction("Index");
            }
            else
            {
                ModelState.AddModelError("", "The information you submitted is not valid!");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Print(int? id)
        {
            if (id == null || _dbContext.DebitMemos == null)
            {
                return NotFound();
            }

            var debitMemo = await _dbContext.DebitMemos
                .Include(dm => dm.SalesInvoice)
                .Include(dm => dm.SOA)
                .ThenInclude(soa => soa.Customer)
                .Include(dm => dm.SOA)
                .ThenInclude(soa => soa.Service)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (debitMemo == null)
            {
                return NotFound();
            }
            return View(debitMemo);
        }

        public async Task<IActionResult> PrintedDM(int id)
        {
            var findIdOfDM = await _debitMemoRepo.FindDM(id);
            if (findIdOfDM != null && !findIdOfDM.IsPrinted)
            {
                findIdOfDM.IsPrinted = true;
                await _dbContext.SaveChangesAsync();
            }
            return RedirectToAction("Print", new { id = id });
        }

        public async Task<IActionResult> Post(int id)
        {
            var model = await _debitMemoRepo.FindDM(id);

            if (model != null)
            {
                if (!model.IsPosted)
                {
                    model.IsPosted = true;
                    model.PostedBy = _userManager.GetUserName(this.User);
                    model.PostedDate = DateTime.Now;

                    #region --Sales Book Recording

                    var sales = new SalesBook();

                    if (model.SalesInvoiceId != null)
                    {
                        if (model.SalesInvoice.CustomerType == "Vatable")
                        {
                            sales.TransactionDate = model.Date.ToShortDateString();
                            sales.SerialNo = model.DMNo;
                            sales.SoldTo = model.SalesInvoice.SoldTo;
                            sales.TinNo = model.SalesInvoice.TinNo;
                            sales.Address = model.SalesInvoice.Address;
                            sales.Description = model.SalesInvoice.ProductName;
                            sales.Amount = model.DebitAmount;
                            sales.VatAmount = model.VatAmount;
                            sales.VatableSales = model.VatableSales;
                            //sales.Discount = model.Discount;
                            //sales.NetSales = model.NetDiscount;
                            sales.CreatedBy = model.CreatedBy;
                            sales.CreatedDate = model.CreatedDate;
                        }
                        else if (model.SalesInvoice.CustomerType == "Exempt")
                        {
                            sales.TransactionDate = model.Date.ToShortDateString();
                            sales.SerialNo = model.DMNo;
                            sales.SoldTo = model.SalesInvoice.SoldTo;
                            sales.TinNo = model.SalesInvoice.TinNo;
                            sales.Address = model.SalesInvoice.Address;
                            sales.Description = model.SalesInvoice.ProductName;
                            sales.Amount = model.DebitAmount;
                            sales.VatExemptSales = model.DebitAmount;
                            //sales.Discount = model.Discount;
                            //sales.NetSales = model.NetDiscount;
                            sales.CreatedBy = model.CreatedBy;
                            sales.CreatedDate = model.CreatedDate;
                        }
                        else
                        {
                            sales.TransactionDate = model.Date.ToShortDateString();
                            sales.SerialNo = model.DMNo;
                            sales.SoldTo = model.SalesInvoice.SoldTo;
                            sales.TinNo = model.SalesInvoice.TinNo;
                            sales.Address = model.SalesInvoice.Address;
                            sales.Description = model.SalesInvoice.ProductName;
                            sales.Amount = model.DebitAmount;
                            sales.ZeroRated = model.DebitAmount;
                            //sales.Discount = model.Discount;
                            //sales.NetSales = model.NetDiscount;
                            sales.CreatedBy = model.CreatedBy;
                            sales.CreatedDate = model.CreatedDate;
                        }
                    }
                    if (model.SOAId != null)
                    {
                        if (model.SOA.Customer.CustomerType == "Vatable")
                        {
                            sales.TransactionDate = model.Date.ToShortDateString();
                            sales.SerialNo = model.DMNo;
                            sales.SoldTo = model.SOA.Customer.Name;
                            sales.TinNo = model.SOA.Customer.TinNo;
                            sales.Address = model.SOA.Customer.Address;
                            sales.Description = model.SOA.Service.Name;
                            sales.Amount = model.DebitAmount;
                            sales.VatAmount = model.VatAmount;
                            sales.VatableSales = model.VatableSales;
                            //sales.Discount = model.Discount;
                            //sales.NetSales = model.NetDiscount;
                            sales.CreatedBy = model.CreatedBy;
                            sales.CreatedDate = model.CreatedDate;
                        }
                        else if (model.SOA.Customer.CustomerType == "Exempt")
                        {
                            sales.TransactionDate = model.Date.ToShortDateString();
                            sales.SerialNo = model.DMNo;
                            sales.SoldTo = model.SOA.Customer.Name;
                            sales.TinNo = model.SOA.Customer.TinNo;
                            sales.Address = model.SOA.Customer.Address;
                            sales.Description = model.SOA.Service.Name;
                            sales.Amount = model.DebitAmount;
                            sales.VatExemptSales = model.DebitAmount;
                            //sales.Discount = model.Discount;
                            //sales.NetSales = model.NetDiscount;
                            sales.CreatedBy = model.CreatedBy;
                            sales.CreatedDate = model.CreatedDate;
                        }
                        else
                        {
                            sales.TransactionDate = model.Date.ToShortDateString();
                            sales.SerialNo = model.DMNo;
                            sales.SoldTo = model.SOA.Customer.Name;
                            sales.TinNo = model.SOA.Customer.TinNo;
                            sales.Address = model.SOA.Customer.Address;
                            sales.Description = model.SOA.Service.Name;
                            sales.Amount = model.DebitAmount;
                            sales.ZeroRated = model.DebitAmount;
                            //sales.Discount = model.Discount;
                            //sales.NetSales = model.NetDiscount;
                            sales.CreatedBy = model.CreatedBy;
                            sales.CreatedDate = model.CreatedDate;
                        }
                    }
                    _dbContext.Add(sales);

                    #endregion --Sales Book Recording

                    #region --General Ledger Book Recording

                    if (model.SalesInvoice.SINo != null)
                    {
                        var ledgers = new List<GeneralLedgerBook>();

                        ledgers.Add(
                            new GeneralLedgerBook
                            {
                                Date = model.Date.ToShortDateString(),
                                Reference = model.DMNo,
                                Description = model.SalesInvoice.ProductName,
                                AccountTitle = "1010201 AR-Trade Receivable",
                                Debit = model.DebitAmount - (model.WithHoldingTaxAmount + model.WithHoldingVatAmount),
                                Credit = 0,
                                CreatedBy = model.CreatedBy,
                                CreatedDate = model.CreatedDate
                            }
                        );

                        if (model.WithHoldingTaxAmount > 0)
                        {
                            ledgers.Add(
                                new GeneralLedgerBook
                                {
                                    Date = model.Date.ToShortDateString(),
                                    Reference = model.DMNo,
                                    Description = model.SalesInvoice.ProductName,
                                    AccountTitle = "1010202 Deferred Creditable Withholding Tax",
                                    Debit = model.WithHoldingTaxAmount,
                                    Credit = 0,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }
                        if (model.WithHoldingVatAmount > 0)
                        {
                            ledgers.Add(
                                new GeneralLedgerBook
                                {
                                    Date = model.Date.ToShortDateString(),
                                    Reference = model.DMNo,
                                    Description = model.SalesInvoice.ProductName,
                                    AccountTitle = "1010203 Deferred Creditable Withholding Vat",
                                    Debit = model.WithHoldingVatAmount,
                                    Credit = 0,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }
                        if (model.SalesInvoice.ProductName == "Biodiesel")
                        {
                            ledgers.Add(
                                new GeneralLedgerBook
                                {
                                    Date = model.Date.ToShortDateString(),
                                    Reference = model.DMNo,
                                    Description = model.SalesInvoice.ProductName,
                                    AccountTitle = "4010101 Sales - Biodiesel",
                                    Debit = 0,
                                    Credit = model.VatableSales > 0
                                                ? model.VatableSales
                                                : model.DebitAmount,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }
                        else if (model.SalesInvoice.ProductName == "Econogas")
                        {
                            ledgers.Add(
                                new GeneralLedgerBook
                                {
                                    Date = model.Date.ToShortDateString(),
                                    Reference = model.DMNo,
                                    Description = model.SalesInvoice.ProductName,
                                    AccountTitle = "4010102 Sales - Econogas",
                                    Debit = 0,
                                    Credit = model.VatableSales > 0
                                                ? model.VatableSales
                                                : model.DebitAmount,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }
                        else if (model.SalesInvoice.ProductName == "Envirogas")
                        {
                            ledgers.Add(
                                new GeneralLedgerBook
                                {
                                    Date = model.Date.ToShortDateString(),
                                    Reference = model.DMNo,
                                    Description = model.SalesInvoice.ProductName,
                                    AccountTitle = "4010103 Sales - Envirogas",
                                    Debit = 0,
                                    Credit = model.VatableSales > 0
                                                ? model.VatableSales
                                                : model.DebitAmount,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }

                        if (model.VatAmount > 0)
                        {
                            ledgers.Add(
                                new GeneralLedgerBook
                                {
                                    Date = model.Date.ToShortDateString(),
                                    Reference = model.DMNo,
                                    Description = model.SalesInvoice.ProductName,
                                    AccountTitle = "2010301 Vat Output",
                                    Debit = 0,
                                    Credit = model.VatAmount,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }


                        _dbContext.GeneralLedgerBooks.AddRange(ledgers);
                    }

                    if (model.SOA.SOANo != null)
                    {
                        var ledgers = new List<GeneralLedgerBook>();

                        ledgers.Add(
                                new GeneralLedgerBook
                                {
                                    Date = model.Date.ToShortDateString(),
                                    Reference = model.DMNo,
                                    Description = model.SOA.Service.Name,
                                    AccountTitle = "1010204 AR-Non Trade Receivable",
                                    Debit = model.DebitAmount - (model.WithHoldingTaxAmount + model.WithHoldingVatAmount),
                                    Credit = 0,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        if (model.WithHoldingTaxAmount < 0)
                        {
                            ledgers.Add(
                                new GeneralLedgerBook
                                {
                                    Date = model.Date.ToShortDateString(),
                                    Reference = model.DMNo,
                                    Description = model.SOA.Service.Name,
                                    AccountTitle = "1010202 Deferred Creditable Withholding Tax",
                                    Debit = model.WithHoldingTaxAmount,
                                    Credit = 0,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }
                        if (model.WithHoldingVatAmount < 0)
                        {
                            ledgers.Add(
                                new GeneralLedgerBook
                                {
                                    Date = model.Date.ToShortDateString(),
                                    Reference = model.DMNo,
                                    Description = model.SOA.Service.Name,
                                    AccountTitle = "1010203 Deferred Creditable Withholding Vat",
                                    Debit = model.WithHoldingVatAmount,
                                    Credit = 0,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }

                        //for (int i = 0; i < model.StatementOfAccount.Period.Length; i++)
                        //{
                        //    if (model.CreatedDate < model.StatementOfAccount.Period[i])
                        //    {
                        //        unearnedAmount += model.StatementOfAccount.Amount[i];
                        //    }
                        //    else
                        //    {
                        //        currentAndPreviousAmount += model.StatementOfAccount.Amount[i];
                        //    }
                        //}

                        if (model.SOA.CurrentAndPreviousAmount > 0)
                        {
                            ledgers.Add(new GeneralLedgerBook
                            {
                                Date = model.Date.ToShortDateString(),
                                Reference = model.DMNo,
                                Description = model.SOA.Service.Name,
                                AccountTitle = model.SOA.Service.CurrentAndPrevious,
                                Debit = 0,
                                Credit = model.SOA.CurrentAndPreviousAmount,
                                CreatedBy = model.CreatedBy,
                                CreatedDate = model.CreatedDate
                            });
                        }

                        if (model.SOA.UnearnedAmount > 0)
                        {
                            ledgers.Add(
                                new GeneralLedgerBook
                                {
                                    Date = model.Date.ToShortDateString(),
                                    Reference = model.DMNo,
                                    Description = model.SOA.Service.Name,
                                    AccountTitle = model.SOA.Service.Unearned,
                                    Debit = 0,
                                    Credit = model.SOA.UnearnedAmount,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }

                        if (model.VatAmount < 0)
                        {
                            ledgers.Add(
                                new GeneralLedgerBook
                                {
                                    Date = model.Date.ToShortDateString(),
                                    Reference = model.DMNo,
                                    Description = model.SOA.Service.Name,
                                    AccountTitle = "2010304 Deferred Vat Output",
                                    Debit = 0,
                                    Credit = model.VatAmount,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }

                        _dbContext.GeneralLedgerBooks.AddRange(ledgers);
                    }

                    #endregion --General Ledger Book Recording

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(model.PostedBy, $"Posted debit memo# {model.DMNo}", "Debit Memo");
                    _dbContext.Add(auditTrail);

                    #endregion --Audit Trail Recording

                    //await _receiptRepo.UpdateCreditMemo(model.SalesInvoice.Id, model.Total, offsetAmount);

                    await _dbContext.SaveChangesAsync();
                    TempData["success"] = "Debit Memo has been Posted.";
                }
                return RedirectToAction("Index");
            }

            return NotFound();
        }

        public async Task<IActionResult> Void(int id)
        {
            var model = await _dbContext.DebitMemos.FindAsync(id);

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

                    AuditTrail auditTrail = new(model.VoidedBy, $"Voided debit memo# {model.DMNo}", "Debit Memo");
                    _dbContext.Add(auditTrail);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync();
                    TempData["success"] = "Debit Memo has been Voided.";
                }
                return RedirectToAction("Index");
            }

            return NotFound();
        }

        public async Task<IActionResult> Cancel(int id)
        {
            var model = await _dbContext.DebitMemos.FindAsync(id);

            if (model != null)
            {
                if (!model.IsCanceled)
                {
                    model.IsCanceled = true;
                    model.CanceledBy = _userManager.GetUserName(this.User);
                    model.CanceledDate = DateTime.Now;

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(model.CanceledBy, $"Canceled debit memo# {model.DMNo}", "Debit Memo");
                    _dbContext.Add(auditTrail);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync();
                    TempData["success"] = "Debit Memo has been Canceled.";
                }
                return RedirectToAction("Index");
            }

            return NotFound();
        }

        public async Task<IActionResult> Preview(int id)
        {
            var dm = await _debitMemoRepo.FindDM(id);
            return PartialView("_PrevieDebit", dm);
        }

        [HttpGet]
        public JsonResult GetSOADetails(int soaId)
        {
            var model = _dbContext.StatementOfAccounts.FirstOrDefault(soa => soa.Id == soaId);
            if (model != null)
            {
                return Json(new
                {
                    Period = model?.Period,
                    Amount = model?.Amount
                });
            }

            return Json(null);
        }
    }
}