using Accounting_System.Data;
using Accounting_System.Models;
using Accounting_System.Repository;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Accounting_System.Controllers
{
    public class CreditMemoController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly CreditMemoRepo _creditMemoRepo;

        public CreditMemoController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, CreditMemoRepo creditMemoRepo)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _creditMemoRepo = creditMemoRepo;
        }

        public async Task<IActionResult> Index()
        {
            var cm = await _dbContext.CreditMemos
                .Include(cm => cm.SalesInvoice)
                .Include(cm => cm.StatementOfAccount)
                .ThenInclude(soa => soa.Customer)
                .Include(cm => cm.StatementOfAccount)
                .ThenInclude(soa => soa.Service)
                .OrderBy(cm => cm.Id) 
                .ToListAsync();

            return View(cm);
        }

        public async Task<IActionResult> Create()
        {
            var viewModel = new CreditMemo();
            viewModel.Invoices = await _dbContext.SalesInvoices
                .Select(si => new SelectListItem
                {
                    Value = si.Id.ToString(),
                    Text = si.SINo
                })
                .ToListAsync();
            viewModel.Soa = await _dbContext.StatementOfAccounts
                .Select(soa => new SelectListItem
                {
                    Value = soa.Id.ToString(),
                    Text = soa.SOANo
                })
                .ToListAsync();

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreditMemo model, DateTime[] period)
        {
            model.Invoices = await _dbContext.SalesInvoices
                .Select(si => new SelectListItem
                {
                    Value = si.Id.ToString(),
                    Text = si.SINo
                })
                .ToListAsync();
            model.Soa = await _dbContext.StatementOfAccounts
                .Select(soa => new SelectListItem
                {
                    Value = soa.Id.ToString(),
                    Text = soa.SOANo
                })
                .ToListAsync();
            if (ModelState.IsValid)
            {
                var getLastNumber = await _creditMemoRepo.GetLastSeriesNumber();

                if (getLastNumber > 9999999999)
                {
                    TempData["error"] = "You reach the maximum Series Number";
                    return View(model);
                }
                var totalRemainingSeries = 9999999999 - getLastNumber;
                if (getLastNumber >= 9999999899)
                {
                    TempData["warning"] = $"Credit Memo created successfully, Warning {totalRemainingSeries} series number remaining";
                }
                else
                {
                    TempData["success"] = "Credit Memo created successfully";
                }

                var generatedCM = await _creditMemoRepo.GenerateCMNo();

                model.SeriesNumber = getLastNumber;
                model.CMNo = generatedCM;
                model.CreatedBy = _userManager.GetUserName(this.User);

                if (model.Source == "Sales Invoice")
                {
                    model.SOAId = null;
                    model.SINo = await _creditMemoRepo.GetSINoAsync(model.SIId);

                    var existingSalesInvoice = _dbContext.SalesInvoices
                                               .FirstOrDefault(si => si.Id == model.SIId);

                    model.CreditAmount = existingSalesInvoice.Quantity * model.AdjustedPrice;

                    if (existingSalesInvoice.CustomerType == "Vatable")
                    {
                        model.VatableSales = model.CreditAmount / 1.12m;
                        model.VatAmount = model.CreditAmount - model.VatableSales;
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

                    model.TotalSales = model.CreditAmount;
                }
                else if (model.Source == "Statement Of Account")
                {
                    model.SIId = null;
                    model.SOANo = await _creditMemoRepo.GetSOANoAsync(model.SOAId);

                    var existingSoa = _dbContext.StatementOfAccounts
                        .Include(soa => soa.Customer)
                        .FirstOrDefault(soa => soa.Id == model.SOAId);

                    model.CreditAmount = model.AdjustedPrice - existingSoa.Total;

                    if (existingSoa.Customer.CustomerType == "Vatable")
                    {
                        model.VatableSales = model.CreditAmount / 1.12m;
                        model.VatAmount = model.CreditAmount - model.VatableSales;
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

                    model.TotalSales = model.CreditAmount;

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



                _dbContext.Add(model);
                await _dbContext.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            ModelState.AddModelError("", "The information you submitted is not valid!");
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _dbContext.CreditMemos == null)
            {
                return NotFound();
            }

            var creditMemo = await _dbContext.CreditMemos.FindAsync(id);
            if (creditMemo == null)
            {
                return NotFound();
            }

            creditMemo.Invoices = await _dbContext.SalesInvoices
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.SINo
                })
                .ToListAsync();

            creditMemo.Soa = await _dbContext.StatementOfAccounts
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.SOANo
                })
                .ToListAsync();

            return View(creditMemo);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(CreditMemo model)
        {
            if (ModelState.IsValid)
            {
                var existingModel = await _dbContext.CreditMemos.FindAsync(model.Id);

                if (existingModel == null)
                {
                    return NotFound();
                }

                existingModel.Date = model.Date;
                existingModel.Source = model.Source;
                existingModel.Description = model.Description;
                existingModel.AdjustedPrice = model.AdjustedPrice;

                if (existingModel.Source == "Sales Invoice")
                {
                    existingModel.SOAId = null;
                    existingModel.SIId = model.SIId;

                    var existingSalesInvoice = _dbContext.SalesInvoices
                                               .FirstOrDefault(si => si.Id == existingModel.SIId);

                    existingModel.CreditAmount = existingSalesInvoice.Quantity * model.AdjustedPrice - existingSalesInvoice.Amount;

                    if (existingSalesInvoice.CustomerType == "Vatable")
                    {
                        existingModel.VatableSales = existingModel.CreditAmount / (decimal)1.12;
                        existingModel.VatAmount = existingModel.CreditAmount - existingModel.VatableSales;
                        existingModel.TotalSales = existingModel.VatableSales + existingModel.VatAmount;
                    }

                    existingModel.TotalSales = existingModel.CreditAmount;
                }
                else if (model.Source == "Statement Of Account")
                {
                    existingModel.SIId = null;
                    existingModel.SOAId = model.SOAId;

                    var existingSoa = _dbContext.StatementOfAccounts
                        .Include(soa => soa.Customer)
                        .FirstOrDefault(soa => soa.Id == existingModel.SOAId);

                    existingModel.CreditAmount = model.AdjustedPrice - existingSoa.Total;

                    if (existingSoa.Customer.CustomerType == "Vatable")
                    {
                        existingModel.VatableSales = existingModel.CreditAmount / (decimal)1.12;
                        existingModel.VatAmount = existingModel.CreditAmount - existingModel.VatableSales;
                        existingModel.TotalSales = existingModel.VatableSales + existingModel.VatAmount;
                    }

                    existingModel.TotalSales = existingModel.CreditAmount;
                }

                await _dbContext.SaveChangesAsync();

                TempData["success"] = "Credit Memo updated successfully";
                return RedirectToAction("Index");
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Print(int? id)
        {
            if (id == null || _dbContext.CreditMemos == null)
            {
                return NotFound();
            }

            var creditMemo = await _dbContext.CreditMemos
                .Include(cm => cm.SalesInvoice)
                .Include(cm => cm.StatementOfAccount)
                .ThenInclude(soa => soa.Customer)
                .Include(cm => cm.StatementOfAccount)
                .ThenInclude(soa => soa.Service)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (creditMemo == null)
            {
                return NotFound();
            }

            return View(creditMemo);
        }

        public async Task<IActionResult> Printed(int id)
        {
            var cm = await _dbContext.CreditMemos.FindAsync(id);
            if (cm != null && !cm.IsPrinted)
            {
                cm.IsPrinted = true;
                await _dbContext.SaveChangesAsync();
            }
            return RedirectToAction("Print", new { id = id });
        }

        public async Task<IActionResult> Post(int id)
        {
            var model = await _creditMemoRepo.FindCM(id);

            if (model != null)
            {
                if (!model.IsPosted)
                {
                    model.IsPosted = true;
                    model.PostedBy = _userManager.GetUserName(this.User);
                    model.PostedDate = DateTime.Now;

                    
                    #region --Sales Book Recording

                var sales = new SalesBook();

                if (model.SINo != null)
                {
                    if (model.SalesInvoice.CustomerType == "Vatable")
                    {
                        sales.TransactionDate = model.Date.ToShortDateString();
                        sales.SerialNo = model.CMNo;
                        sales.SoldTo = model.SalesInvoice.SoldTo;
                        sales.TinNo = model.SalesInvoice.TinNo;
                        sales.Address = model.SalesInvoice.Address;
                        sales.Description = model.SalesInvoice.ProductName;
                        sales.Amount = model.CreditAmount;
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
                        sales.SerialNo = model.CMNo;
                        sales.SoldTo = model.SalesInvoice.SoldTo;
                        sales.TinNo = model.SalesInvoice.TinNo;
                        sales.Address = model.SalesInvoice.Address;
                        sales.Description = model.SalesInvoice.ProductName;
                        sales.Amount = model.CreditAmount;
                        sales.VatExemptSales = model.CreditAmount;
                        //sales.Discount = model.Discount;
                        //sales.NetSales = model.NetDiscount;
                        sales.CreatedBy = model.CreatedBy;
                        sales.CreatedDate = model.CreatedDate;
                    }
                    else
                    {
                        sales.TransactionDate = model.Date.ToShortDateString();
                        sales.SerialNo = model.CMNo;
                        sales.SoldTo = model.SalesInvoice.SoldTo;
                        sales.TinNo = model.SalesInvoice.TinNo;
                        sales.Address = model.SalesInvoice.Address;
                        sales.Description = model.SalesInvoice.ProductName;
                        sales.Amount = model.CreditAmount;
                        sales.ZeroRated = model.CreditAmount;
                        //sales.Discount = model.Discount;
                        //sales.NetSales = model.NetDiscount;
                        sales.CreatedBy = model.CreatedBy;
                        sales.CreatedDate = model.CreatedDate;
                    }
                }
                if (model.SOANo != null)
                {
                    if (model.StatementOfAccount.Customer.CustomerType == "Vatable")
                    {
                        sales.TransactionDate = model.Date.ToShortDateString();
                        sales.SerialNo = model.CMNo;
                        sales.SoldTo = model.StatementOfAccount.Customer.Name;
                        sales.TinNo = model.StatementOfAccount.Customer.TinNo;
                        sales.Address = model.StatementOfAccount.Customer.Address;
                        sales.Description = model.StatementOfAccount.Service.Name;
                        sales.Amount = model.CreditAmount;
                        sales.VatAmount = model.VatAmount;
                        sales.VatableSales = model.VatableSales;
                        //sales.Discount = model.Discount;
                        //sales.NetSales = model.NetDiscount;
                        sales.CreatedBy = model.CreatedBy;
                        sales.CreatedDate = model.CreatedDate;
                    }
                    else if (model.StatementOfAccount.Customer.CustomerType == "Exempt")
                    {
                        sales.TransactionDate = model.Date.ToShortDateString();
                        sales.SerialNo = model.CMNo;
                        sales.SoldTo = model.StatementOfAccount.Customer.Name;
                        sales.TinNo = model.StatementOfAccount.Customer.TinNo;
                        sales.Address = model.StatementOfAccount.Customer.Address;
                        sales.Description = model.StatementOfAccount.Service.Name;
                        sales.Amount = model.CreditAmount;
                        sales.VatExemptSales = model.CreditAmount;
                        //sales.Discount = model.Discount;
                        //sales.NetSales = model.NetDiscount;
                        sales.CreatedBy = model.CreatedBy;
                        sales.CreatedDate = model.CreatedDate;
                    }
                    else
                    {
                        sales.TransactionDate = model.Date.ToShortDateString();
                        sales.SerialNo = model.CMNo;
                        sales.SoldTo = model.StatementOfAccount.Customer.Name;
                        sales.TinNo = model.StatementOfAccount.Customer.TinNo;
                        sales.Address = model.StatementOfAccount.Customer.Address;
                        sales.Description = model.StatementOfAccount.Service.Name;
                        sales.Amount = model.CreditAmount;
                        sales.ZeroRated = model.CreditAmount;
                        //sales.Discount = model.Discount;
                        //sales.NetSales = model.NetDiscount;
                        sales.CreatedBy = model.CreatedBy;
                        sales.CreatedDate = model.CreatedDate;
                    }
                }
                _dbContext.Add(sales);

                #endregion --Sales Book Recording

                    #region --General Ledger Book Recording

                    if (model.SINo != null)
                    {
                        var ledgers = new List<GeneralLedgerBook>();

                        ledgers.Add(
                            new GeneralLedgerBook
                            {
                                Date = model.Date.ToShortDateString(),
                                Reference = model.CMNo,
                                Description = model.SalesInvoice.ProductName,
                                AccountTitle = "1010201 AR-Trade Receivable",
                                Debit = model.CreditAmount - (model.WithHoldingTaxAmount + model.WithHoldingVatAmount),
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
                                    Reference = model.CMNo,
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
                                    Reference = model.CMNo,
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
                                    Reference = model.CMNo,
                                    Description = model.SalesInvoice.ProductName,
                                    AccountTitle = "4010101 Sales - Biodiesel",
                                    Debit = 0,
                                    Credit = model.VatableSales > 0
                                                ? model.VatableSales
                                                : model.CreditAmount,
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
                                    Reference = model.CMNo,
                                    Description = model.SalesInvoice.ProductName,
                                    AccountTitle = "4010102 Sales - Econogas",
                                    Debit = 0,
                                    Credit = model.VatableSales > 0
                                                ? model.VatableSales
                                                : model.CreditAmount,
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
                                    Reference = model.CMNo,
                                    Description = model.SalesInvoice.ProductName,
                                    AccountTitle = "4010103 Sales - Envirogas",
                                    Debit = 0,
                                    Credit = model.VatableSales > 0
                                                ? model.VatableSales
                                                : model.CreditAmount,
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
                                    Reference = model.CMNo,
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

                    if (model.SOANo != null)
                    {
                        var ledgers = new List<GeneralLedgerBook>();

                        ledgers.Add(
                                new GeneralLedgerBook
                                {
                                    Date = model.Date.ToShortDateString(),
                                    Reference = model.CMNo,
                                    Description = model.StatementOfAccount.Service.Name,
                                    AccountTitle = "1010204 AR-Non Trade Receivable",
                                    Debit = model.CreditAmount - (model.WithHoldingTaxAmount + model.WithHoldingVatAmount),
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
                                    Reference = model.CMNo,
                                    Description = model.StatementOfAccount.Service.Name,
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
                                    Reference = model.CMNo,
                                    Description = model.StatementOfAccount.Service.Name,
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

                        if (model.StatementOfAccount.CurrentAndPreviousAmount > 0)
                        {
                            ledgers.Add(new GeneralLedgerBook
                            {
                                Date = model.Date.ToShortDateString(),
                                Reference = model.CMNo,
                                Description = model.StatementOfAccount.Service.Name,
                                AccountTitle = model.StatementOfAccount.Service.CurrentAndPrevious,
                                Debit = 0,
                                Credit = model.StatementOfAccount.CurrentAndPreviousAmount,
                                CreatedBy = model.CreatedBy,
                                CreatedDate = model.CreatedDate
                            });
                        }

                        if (model.StatementOfAccount.UnearnedAmount > 0)
                        {
                            ledgers.Add(
                                new GeneralLedgerBook
                                {
                                    Date = model.Date.ToShortDateString(),
                                    Reference = model.CMNo,
                                    Description = model.StatementOfAccount.Service.Name,
                                    AccountTitle = model.StatementOfAccount.Service.Unearned,
                                    Debit = 0,
                                    Credit = model.StatementOfAccount.UnearnedAmount,
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
                                    Reference = model.CMNo,
                                    Description = model.StatementOfAccount.Service.Name,
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

                    AuditTrail auditTrail = new(model.PostedBy, $"Posted credit memo# {model.CMNo}", "Credit Memo");
                    _dbContext.Add(auditTrail);

                    #endregion --Audit Trail Recording

                    //await _receiptRepo.UpdateCreditMemo(model.SalesInvoice.Id, model.Total, offsetAmount);

                    await _dbContext.SaveChangesAsync();
                    TempData["success"] = "Credit Memo has been Posted.";
                }
                return RedirectToAction("Index");
            }

            return NotFound();
        }

        public async Task<IActionResult> Void(int id)
        {
            var model = await _dbContext.CreditMemos.FindAsync(id);

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

                    AuditTrail auditTrail = new(model.VoidedBy, $"Voided credit memo# {model.CMNo}", "Credit Memo");
                    _dbContext.Add(auditTrail);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync();
                    TempData["success"] = "Credit Memo has been Voided.";
                }
                return RedirectToAction("Index");
            }

            return NotFound();
        }

        public async Task<IActionResult> Cancel(int id)
        {
            var model = await _dbContext.CreditMemos.FindAsync(id);

            if (model != null)
            {
                if (!model.IsCanceled)
                {
                    model.IsCanceled = true;
                    model.CanceledBy = _userManager.GetUserName(this.User);
                    model.CanceledDate = DateTime.Now;

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(model.CanceledBy, $"Canceled credit memo# {model.CMNo}", "Credit Memo");
                    _dbContext.Add(auditTrail);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync();
                    TempData["success"] = "Credit Memo has been Canceled.";
                }
                return RedirectToAction("Index");
            }

            return NotFound();
        }

        public async Task<IActionResult> Preview(int id)
        {
            var cm = await _creditMemoRepo.FindCM(id);
            return PartialView("_PreviewCredit", cm);
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