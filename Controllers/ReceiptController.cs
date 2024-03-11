using Accounting_System.Data;
using Accounting_System.Models;
using Accounting_System.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.IO;
using Microsoft.EntityFrameworkCore;

namespace Accounting_System.Controllers
{
    [Authorize]
    public class ReceiptController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly ReceiptRepo _receiptRepo;

        private readonly IWebHostEnvironment _webHostEnvironment;

        private readonly GeneralRepo _generalRepo;

        public ReceiptController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, ReceiptRepo receiptRepo, IWebHostEnvironment webHostEnvironment, GeneralRepo generalRepo)
        {
            _dbContext = dbContext;
            this._userManager = userManager;
            _receiptRepo = receiptRepo;
            _webHostEnvironment = webHostEnvironment;
            _generalRepo = generalRepo;
        }

        public async Task<IActionResult> CollectionIndex(CancellationToken cancellationToken)
        {
            var viewData = await _receiptRepo.GetCRAsync(cancellationToken);

            return View(viewData);
        }

        public async Task<IActionResult> OfficialIndex(CancellationToken cancellationToken)
        {
            var viewData = await _receiptRepo.GetORAsync(cancellationToken);

            return View(viewData);
        }

        public async Task<IActionResult> CollectionCreate(CancellationToken cancellationToken)
        {
            var viewModel = new CollectionReceipt();

            viewModel.Customers = await _dbContext.Customers
               .OrderBy(c => c.Id)
               .Select(s => new SelectListItem
               {
                   Value = s.Number.ToString(),
                   Text = s.Name
               })
               .ToListAsync(cancellationToken);

            viewModel.ChartOfAccounts = await _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == "4" || coa.Level == "5")
                .OrderBy(coa => coa.Id)
                .Select(s => new SelectListItem
                {
                    Value = s.Number,
                    Text = s.Number + " " + s.Name
                })
                .ToListAsync(cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> CollectionCreate(CollectionReceipt model, string[] accountTitleText, decimal[] accountAmount, string[] accountTitle, IFormFile? bir2306, IFormFile? bir2307, CancellationToken cancellationToken)
        {
            model.Customers = await _dbContext.Customers
               .OrderBy(c => c.Id)
               .Select(s => new SelectListItem
               {
                   Value = s.Number.ToString(),
                   Text = s.Name
               })
               .ToListAsync(cancellationToken);

            model.Invoices = await _dbContext.SalesInvoices
                .Where(si => !si.IsPaid && si.CustomerNo == model.CustomerNo)
                .OrderBy(si => si.Id)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.SINo
                })
                .ToListAsync(cancellationToken);

            model.ChartOfAccounts = await _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == "4" || coa.Level == "5")
                .OrderBy(coa => coa.Id)
                .Select(s => new SelectListItem
                {
                    Value = s.Number,
                    Text = s.Number + " " + s.Name
                })
                .ToListAsync(cancellationToken);

            if (ModelState.IsValid)
            {
                #region --Validating the series

                var getLastNumber = await _receiptRepo.GetLastSeriesNumberCR(cancellationToken);

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
                var existingSalesInvoice = await _dbContext.SalesInvoices
                                               .FirstOrDefaultAsync(si => si.Id == model.SalesInvoiceId, cancellationToken);
                var generateCRNo = await _receiptRepo.GenerateCRNo(cancellationToken);

                model.SeriesNumber = getLastNumber;
                model.SINo = existingSalesInvoice.SINo;
                model.CRNo = generateCRNo;
                model.CreatedBy = _userManager.GetUserName(this.User);
                model.Total = computeTotalInModelIfZero;

                try
                {
                    if (bir2306 != null && bir2306.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "BIR 2306");

                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        string fileName = Path.GetFileName(bir2306.FileName);
                        string fileSavePath = Path.Combine(uploadsFolder, fileName);

                        using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
                        {
                            await bir2306.CopyToAsync(stream);
                        }

                        model.F2306FilePath = fileSavePath;
                        model.IsCertificateUpload = true;
                    }

                    if (bir2307 != null && bir2307.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "BIR 2307");

                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        string fileName = Path.GetFileName(bir2307.FileName);
                        string fileSavePath = Path.Combine(uploadsFolder, fileName);

                        using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
                        {
                            await bir2307.CopyToAsync(stream);
                        }

                        model.F2307FilePath = fileSavePath;
                        model.IsCertificateUpload = true;
                    }
                }
                catch (Exception ex)
                {
                }

                await _dbContext.AddAsync(model, cancellationToken);

                decimal offsetAmount = 0;

                #endregion --Saving default value

                #region --Audit Trail Recording

                AuditTrail auditTrail = new(model.CreatedBy, $"Create new collection receipt# {model.CRNo}", "Collection Receipt");
                await _dbContext.AddAsync(auditTrail, cancellationToken);

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

                    await _dbContext.AddRangeAsync(offsettings, cancellationToken);
                }

                #endregion --Offsetting function

                await _dbContext.SaveChangesAsync(cancellationToken);
                return RedirectToAction("CollectionIndex");
            }
            else
            {
                TempData["error"] = "The information you submitted is not valid!";
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> OfficialCreate(CancellationToken cancellationToken)
        {
            var viewModel = new OfficialReceipt();

            viewModel.Customers = await _dbContext.Customers
               .OrderBy(c => c.Id)
               .Select(s => new SelectListItem
               {
                   Value = s.Number.ToString(),
                   Text = s.Name
               })
               .ToListAsync(cancellationToken);

            viewModel.ChartOfAccounts = await _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == "4" || coa.Level == "5")
                .OrderBy(coa => coa.Id)
                .Select(s => new SelectListItem
                {
                    Value = s.Number,
                    Text = s.Number + " " + s.Name
                })
                .ToListAsync(cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> OfficialCreate(OfficialReceipt model, string[] accountTitleText, decimal[] accountAmount, string[] accountTitle, IFormFile? bir2306, IFormFile? bir2307, CancellationToken cancellationToken)
        {
            model.Customers = await _dbContext.Customers
               .OrderBy(c => c.Id)
               .Select(s => new SelectListItem
               {
                   Value = s.Number.ToString(),
                   Text = s.Name
               })
               .ToListAsync(cancellationToken);

            model.StatementOfAccounts = await _dbContext.StatementOfAccounts
                .Where(s => !s.IsPaid && s.Customer.Number == model.CustomerNo)
                .OrderBy(si => si.Id)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.SOANo
                })
                .ToListAsync(cancellationToken);

            model.StatementOfAccounts = await _dbContext.StatementOfAccounts
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.SOANo
                })
                .ToListAsync(cancellationToken);

            model.ChartOfAccounts = await _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == "4" || coa.Level == "5")
                .OrderBy(coa => coa.Id)
                .Select(s => new SelectListItem
                {
                    Value = s.Number,
                    Text = s.Number + " " + s.Name
                })
                .ToListAsync(cancellationToken);

            if (ModelState.IsValid)
            {
                #region --Validating the series

                var getLastNumber = await _receiptRepo.GetLastSeriesNumberOR(cancellationToken);

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
                var existingSOA = await _dbContext.StatementOfAccounts
                                               .FirstOrDefaultAsync(s => s.Id == model.SOAId, cancellationToken);

                var generateORNo = await _receiptRepo.GenerateORNo(cancellationToken);

                model.SeriesNumber = getLastNumber;
                model.ORNo = generateORNo;
                model.SOANo = existingSOA.SOANo;
                model.Total = computeTotalInModelIfZero;
                model.CreatedBy = _userManager.GetUserName(this.User);

                try
                {
                    if (bir2306 != null && bir2306.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "BIR 2306");

                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        string fileName = Path.GetFileName(bir2306.FileName);
                        string fileSavePath = Path.Combine(uploadsFolder, fileName);

                        using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
                        {
                            await bir2306.CopyToAsync(stream);
                        }

                        model.F2306FilePath = fileSavePath;
                        model.IsCertificateUpload = true;
                    }

                    if (bir2307 != null && bir2307.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "BIR 2307");

                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        string fileName = Path.GetFileName(bir2307.FileName);
                        string fileSavePath = Path.Combine(uploadsFolder, fileName);

                        using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
                        {
                            await bir2307.CopyToAsync(stream);
                        }

                        model.F2307FilePath = fileSavePath;
                        model.IsCertificateUpload = true;
                    }
                }
                catch (Exception ex)
                {
                }

                await _dbContext.AddAsync(model, cancellationToken);

                decimal offsetAmount = 0;

                #endregion --Saving default value

                #region --Audit Trail Recording

                AuditTrail auditTrail = new(model.CreatedBy, $"Create new official receipt# {model.ORNo}", "Official Receipt");
                await _dbContext.AddAsync(auditTrail, cancellationToken);

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

                    await _dbContext.AddRangeAsync(offsettings, cancellationToken);
                }

                #endregion --Offsetting function

                await _dbContext.SaveChangesAsync(cancellationToken);
                return RedirectToAction("OfficialIndex");
            }
            else
            {
                ModelState.AddModelError("", "The information you submitted is not valid!");
                return View(model);
            }
        }

        public async Task<IActionResult> CollectionPrint(int id, CancellationToken cancellationToken)
        {
            var cr = await _receiptRepo.FindCR(id, cancellationToken);
            return View(cr);
        }

        public async Task<IActionResult> OfficialPrint(int id, CancellationToken cancellationToken)
        {
            var or = await _receiptRepo.FindOR(id, cancellationToken);
            return View(or);
        }

        public async Task<IActionResult> CollectionPreview(int id, CancellationToken cancellationToken)
        {
            var cr = await _receiptRepo.FindCR(id, cancellationToken);
            return PartialView("_CollectionPreviewPartialView", cr);
        }

        public async Task<IActionResult> OfficialPreview(int id, CancellationToken cancellationToken)
        {
            var or = await _receiptRepo.FindOR(id, cancellationToken);
            return PartialView("_OfficialPreviewPartialView", or);
        }

        public async Task<IActionResult> PrintedCR(int id, CancellationToken cancellationToken)
        {
            var findIdOfCR = await _receiptRepo.FindCR(id, cancellationToken);
            if (findIdOfCR != null && !findIdOfCR.IsPrinted)
            {
                #region --Audit Trail Recording

                var printedBy = _userManager.GetUserName(this.User);
                AuditTrail auditTrail = new(printedBy, $"Printed original copy of cr# {findIdOfCR.CRNo}", "Collection Receipt");
                await _dbContext.AddAsync(auditTrail, cancellationToken);

                #endregion --Audit Trail Recording

                findIdOfCR.IsPrinted = true;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            return RedirectToAction("CollectionPrint", new { id = id });
        }

        public async Task<IActionResult> PrintedOR(int id, CancellationToken cancellationToken)
        {
            var findIdOfOR = await _receiptRepo.FindOR(id, cancellationToken);
            if (findIdOfOR != null && !findIdOfOR.IsPrinted)
            {
                #region --Audit Trail Recording

                var printedBy = _userManager.GetUserName(this.User);
                AuditTrail auditTrail = new(printedBy, $"Printed original copy of or# {findIdOfOR.ORNo}", "Official Receipt");
                await _dbContext.AddAsync(auditTrail, cancellationToken);

                #endregion --Audit Trail Recording

                findIdOfOR.IsPrinted = true;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            return RedirectToAction("OfficialPrint", new { id = id });
        }

        [HttpGet]
        public async Task<IActionResult> GetSalesInvoices(int customerNo, CancellationToken cancellationToken)
        {
            var invoices = await _dbContext
                .SalesInvoices
                .Where(si => si.CustomerNo == customerNo && !si.IsPaid)
                .OrderBy(si => si.Id)
                .ToListAsync(cancellationToken);

            var invoiceList = invoices.Select(si => new SelectListItem
            {
                Value = si.Id.ToString(),   // Replace with your actual ID property
                Text = si.SINo              // Replace with your actual property for display text
            }).ToList();

            return Json(invoiceList);
        }

        [HttpGet]
        public async Task<IActionResult> GetInvoiceDetails(int invoiceNo, CancellationToken cancellationToken)
        {
            var invoice = await _dbContext
                .SalesInvoices
                .FirstOrDefaultAsync(si => si.Id == invoiceNo, cancellationToken);

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
        public async Task<IActionResult> CollectionEdit(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }
            var existingModel = await _dbContext.CollectionReceipts.FindAsync(id, cancellationToken);

            if (existingModel == null)
            {
                return NotFound();
            }

            existingModel.Customers = await _dbContext.Customers
               .OrderBy(c => c.Id)
               .Select(s => new SelectListItem
               {
                   Value = s.Number.ToString(),
                   Text = s.Name
               })
               .ToListAsync(cancellationToken);

            existingModel.Invoices = await _dbContext.SalesInvoices
                .Where(si => !si.IsPaid && si.CustomerNo == existingModel.CustomerNo)
                .OrderBy(si => si.Id)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.SINo
                })
                .ToListAsync(cancellationToken);

            existingModel.ChartOfAccounts = await _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == "4" || coa.Level == "5")
                .OrderBy(coa => coa.Id)
                .Select(s => new SelectListItem
                {
                    Value = s.Number,
                    Text = s.Number + " " + s.Name
                })
                .ToListAsync(cancellationToken);

            var findCustomers = await _dbContext.Customers
                .FirstOrDefaultAsync(c => c.Number == existingModel.CustomerNo, cancellationToken);

            ViewBag.CustomerName = findCustomers?.Name;

            var matchingOffsettings = await _dbContext.Offsettings
            .Where(offset => offset.Source == existingModel.CRNo)
            .ToListAsync(cancellationToken);

            ViewBag.fetchAccEntries = matchingOffsettings
                .Select(offset => new { AccountNo = offset.AccountNo, Amount = offset.Amount.ToString("N2") })
                .ToList();

            return View(existingModel);
        }

        [HttpPost]
        public async Task<IActionResult> CollectionEdit(CollectionReceipt model, string[] editAccountTitleText, decimal[] editAccountAmount, string[] editAccountTitle, IFormFile? bir2306, IFormFile? bir2307, CancellationToken cancellationToken)
        {
            var existingModel = await _receiptRepo.FindCR(model.Id, cancellationToken);

            if (ModelState.IsValid)
            {
                #region --Validating the series

                var getLastNumber = await _receiptRepo.GetLastSeriesNumberCR(cancellationToken);

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

                try
                {
                    if (bir2306 != null && bir2306.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "BIR 2306");

                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        string fileName = Path.GetFileName(bir2306.FileName);
                        string fileSavePath = Path.Combine(uploadsFolder, fileName);

                        using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
                        {
                            await bir2306.CopyToAsync(stream);
                        }

                        existingModel.F2306FilePath = fileSavePath;
                        existingModel.IsCertificateUpload = true;
                    }

                    if (bir2307 != null && bir2307.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "BIR 2307");

                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        string fileName = Path.GetFileName(bir2307.FileName);
                        string fileSavePath = Path.Combine(uploadsFolder, fileName);

                        using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
                        {
                            await bir2307.CopyToAsync(stream);
                        }

                        existingModel.F2307FilePath = fileSavePath;
                        existingModel.IsCertificateUpload = true;
                    }
                }
                catch (Exception ex)
                {
                }

                decimal offsetAmount = 0;

                #endregion --Saving default value

                #region --Offsetting function

                var offsetting = new List<Offsetting>();

                for (int i = 0; i < editAccountTitleText.Length; i++)
                {
                    var existingCRNo = existingModel.CRNo;
                    var accountTitle = editAccountTitleText[i];
                    var accountAmount = editAccountAmount[i];

                    var existingOffsetting = await _dbContext.Offsettings
                        .FirstOrDefaultAsync(offset => offset.Source == existingCRNo && offset.AccountNo == accountTitle, cancellationToken);

                    if (existingOffsetting != null)
                    {
                        // Update existing entry
                        existingOffsetting.Amount = accountAmount;
                    }
                    else
                    {
                        // Add new entry
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
                }

                // Identify removed entries
                var existingAccountNos = offsetting.Select(o => o.AccountNo).ToList();
                var entriesToRemove = await _dbContext.Offsettings
                    .Where(offset => offset.Source == existingModel.CRNo && !existingAccountNos.Contains(offset.AccountNo))
                    .ToListAsync(cancellationToken);

                // Remove entries individually
                foreach (var entryToRemove in entriesToRemove)
                {
                    _dbContext.Offsettings.Remove(entryToRemove);
                }

                // Add or update entries in the database
                await _dbContext.Offsettings.AddRangeAsync(offsetting, cancellationToken);

                #endregion --Offsetting function

                #region --Audit Trail Recording

                var modifiedBy = _userManager.GetUserName(this.User);
                AuditTrail auditTrail = new(modifiedBy, $"Edited receipt# {model.CRNo}", "Collection Receipt");
                await _dbContext.AddAsync(auditTrail, cancellationToken);

                #endregion --Audit Trail Recording

                await _dbContext.SaveChangesAsync(cancellationToken);
                return RedirectToAction("CollectionIndex");
            }
            else
            {
                TempData["error"] = "The information you submitted is not valid!";
                return View(model);
            }
        }

        public async Task<IActionResult> Post(int itemId, CancellationToken cancellationToken)
        {
            var model = await _receiptRepo.FindCR(itemId, cancellationToken);

            if (model != null)
            {
                if (!model.IsPosted)
                {
                    model.IsPosted = true;
                    model.PostedBy = _userManager.GetUserName(this.User);
                    model.PostedDate = DateTime.Now;

                    var offset = await _receiptRepo.GetOffsettingAsync(model.CRNo, model.SINo, cancellationToken);

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

                    await _dbContext.AddRangeAsync(ledgers, cancellationToken);

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

                    await _dbContext.AddRangeAsync(crb, cancellationToken);

                    #endregion --Cash Receipt Book Recording

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(model.PostedBy, $"Posted collection receipt# {model.CRNo}", "Collection Receipt");
                    await _dbContext.AddAsync(auditTrail, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _receiptRepo.UpdateInvoice(model.SalesInvoice.Id, model.Total, offsetAmount, cancellationToken);

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Collection Receipt has been Posted.";
                }
                return RedirectToAction("CollectionIndex");
            }

            return NotFound();
        }

        public async Task<IActionResult> Void(int itemId, CancellationToken cancellationToken)
        {
            var model = await _dbContext.CollectionReceipts.FindAsync(itemId, cancellationToken);

            if (model != null)
            {
                if (!model.IsVoided)
                {
                    model.IsVoided = true;
                    model.VoidedBy = _userManager.GetUserName(this.User);
                    model.VoidedDate = DateTime.Now;

                    await _generalRepo.RemoveRecords<CashReceiptBook>(crb => crb.RefNo == model.CRNo, cancellationToken);
                    await _generalRepo.RemoveRecords<GeneralLedgerBook>(gl => gl.Reference == model.CRNo, cancellationToken);

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(model.VoidedBy, $"Voided collection receipt# {model.CRNo}", "Collection Receipt");
                    await _dbContext.AddAsync(auditTrail, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Collection Receipt has been Voided.";
                }
                return RedirectToAction("CollectionIndex");
            }

            return NotFound();
        }

        public async Task<IActionResult> Cancel(int itemId, CancellationToken cancellationToken)
        {
            var model = await _dbContext.CollectionReceipts.FindAsync(itemId, cancellationToken);

            if (model != null)
            {
                if (!model.IsCanceled)
                {
                    model.IsCanceled = true;
                    model.CanceledBy = _userManager.GetUserName(this.User);
                    model.CanceledDate = DateTime.Now;

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(model.CanceledBy, $"Cancelled collection receipt# {model.CRNo}", "Collection Receipt");
                    await _dbContext.AddAsync(auditTrail, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Collection Receipt has been Cancelled.";
                }
                return RedirectToAction("CollectionIndex");
            }

            return NotFound();
        }

        [HttpGet]
        public async Task<IActionResult> GetStatementOfAccount(int customerNo, CancellationToken cancellationToken)
        {
            var soa = await _dbContext
                .StatementOfAccounts
                .Where(s => s.Customer.Number == customerNo && !s.IsPaid)
                .OrderBy(s => s.Id)
                .ToListAsync(cancellationToken);

            var soaList = soa.Select(si => new SelectListItem
            {
                Value = si.Id.ToString(),   // Replace with your actual ID property
                Text = si.SOANo              // Replace with your actual property for display text
            }).ToList();

            return Json(soaList);
        }

        [HttpGet]
        public async Task<IActionResult> GetSOADetails(int soaNo, CancellationToken cancellationToken)
        {
            var soa = await _dbContext
                .StatementOfAccounts
                .FirstOrDefaultAsync(s => s.Id == soaNo, cancellationToken);

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
        public async Task<IActionResult> OfficialEdit(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }
            var existingModel = await _dbContext.OfficialReceipts.FindAsync(id, cancellationToken);

            if (existingModel == null)
            {
                return NotFound();
            }

            existingModel.Customers = await _dbContext.Customers
               .OrderBy(c => c.Id)
               .Select(s => new SelectListItem
               {
                   Value = s.Number.ToString(),
                   Text = s.Name
               })
               .ToListAsync(cancellationToken);

            existingModel.StatementOfAccounts = await _dbContext.StatementOfAccounts
                .Where(s => !s.IsPaid && s.Customer.Number == existingModel.CustomerNo)
                .OrderBy(si => si.Id)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.SOANo
                })
                .ToListAsync(cancellationToken);

            existingModel.ChartOfAccounts = await _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == "4" || coa.Level == "5")
                .OrderBy(coa => coa.Id)
                .Select(s => new SelectListItem
                {
                    Value = s.Number,
                    Text = s.Number + " " + s.Name
                })
                .ToListAsync(cancellationToken);

            var findCustomers = await _dbContext.Customers
                .FirstOrDefaultAsync(c => c.Number == existingModel.CustomerNo, cancellationToken);

            ViewBag.CustomerName = findCustomers?.Name;

            var matchingOffsettings = await _dbContext.Offsettings
            .Where(offset => offset.Source == existingModel.ORNo)
            .ToListAsync(cancellationToken);

            ViewBag.fetchAccEntries = matchingOffsettings
                .Select(offset => new { AccountNo = offset.AccountNo, Amount = offset.Amount.ToString("N2") })
                .ToList();

            return View(existingModel);
        }

        [HttpPost]
        public async Task<IActionResult> OfficialEdit(OfficialReceipt model, string[] editAccountTitleText, decimal[] editAccountAmount, string[] editAccountTitle, IFormFile? bir2306, IFormFile? bir2307, CancellationToken cancellationToken)
        {
            var existingModel = await _receiptRepo.FindOR(model.Id, cancellationToken);

            if (ModelState.IsValid)
            {
                #region --Validating the series

                var getLastNumber = await _receiptRepo.GetLastSeriesNumberOR(cancellationToken);

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

                try
                {
                    if (bir2306 != null && bir2306.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "BIR 2306");

                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        string fileName = Path.GetFileName(bir2306.FileName);
                        string fileSavePath = Path.Combine(uploadsFolder, fileName);

                        using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
                        {
                            await bir2306.CopyToAsync(stream);
                        }

                        existingModel.F2306FilePath = fileSavePath;
                        existingModel.IsCertificateUpload = true;
                    }

                    if (bir2307 != null && bir2307.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "BIR 2307");

                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        string fileName = Path.GetFileName(bir2307.FileName);
                        string fileSavePath = Path.Combine(uploadsFolder, fileName);

                        using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
                        {
                            await bir2307.CopyToAsync(stream);
                        }

                        existingModel.F2307FilePath = fileSavePath;
                        existingModel.IsCertificateUpload = true;
                    }
                }
                catch (Exception ex)
                {
                }

                decimal offsetAmount = 0;

                #endregion --Saving default value

                #region --Offsetting function

                var offsetting = new List<Offsetting>();

                for (int i = 0; i < editAccountTitleText.Length; i++)
                {
                    var existingOffset = await _dbContext.Offsettings
                        .FirstOrDefaultAsync(offset => offset.Source == existingModel.ORNo
                                                        && offset.AccountNo == editAccountTitleText[i]
                                                        && offset.Amount == editAccountAmount[i], cancellationToken);

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
                        await _dbContext.SaveChangesAsync(cancellationToken);
                    }
                }

                if (offsetting.Any())
                {
                    await _dbContext.AddRangeAsync(offsetting, cancellationToken);
                }

                #endregion --Offsetting function

                #region --Audit Trail Recording

                var modifiedBy = _userManager.GetUserName(this.User);
                AuditTrail auditTrail = new(modifiedBy, $"Edited receipt# {model.SOANo}", "Official Receipt");
                await _dbContext.AddAsync(auditTrail, cancellationToken);

                #endregion --Audit Trail Recording

                await _dbContext.SaveChangesAsync(cancellationToken);
                return RedirectToAction("OfficialIndex");
            }
            else
            {
                TempData["error"] = "The information you submitted is not valid!";
                return View(model);
            }
        }

        public async Task<IActionResult> PostOR(int itemId, CancellationToken cancellationToken)
        {
            var model = await _receiptRepo.FindOR(itemId, cancellationToken);

            if (model != null)
            {
                if (!model.IsPosted)
                {
                    model.IsPosted = true;
                    model.PostedBy = _userManager.GetUserName(this.User);
                    model.PostedDate = DateTime.Now;

                    var offset = await _receiptRepo.GetOffsettingAsync(model.ORNo, model.SOANo, cancellationToken);

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

                    if (model.StatementOfAccount.Customer.CustomerType == "Vatable")
                    {
                        ledgers.Add(
                           new GeneralLedgerBook
                           {
                               Date = model.Date.ToShortDateString(),
                               Reference = model.ORNo,
                               Description = "Collection for Receivable",
                               AccountTitle = "2010304 Deferred Vat Output",
                               Debit = (model.Total / 1.12m) * 0.12m,
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
                                AccountTitle = "1010204 AR-Non Trade Receivable",
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

                    if (model.StatementOfAccount.Customer.CustomerType == "Vatable")
                    {
                        ledgers.Add(
                           new GeneralLedgerBook
                           {
                               Date = model.Date.ToShortDateString(),
                               Reference = model.ORNo,
                               Description = "Collection for Receivable",
                               AccountTitle = "2010301 Vat Output",
                               Debit = 0,
                               Credit = (model.Total / 1.12m) * 0.12m,
                               CreatedBy = model.CreatedBy,
                               CreatedDate = model.CreatedDate
                           }
                       );
                    }

                    await _dbContext.AddRangeAsync(ledgers, cancellationToken);

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

                    if (model.StatementOfAccount.Customer.CustomerType == "Vatable")
                    {
                        crb.Add(
                            new CashReceiptBook
                            {
                                Date = model.Date.ToShortDateString(),
                                RefNo = model.ORNo,
                                CustomerName = model.StatementOfAccount.Customer.Name,
                                Bank = "--",
                                CheckNo = "--",
                                COA = "2010304 Deferred Vat Output",
                                Particulars = model.StatementOfAccount.SOANo,
                                Debit = (model.Total / 1.12m) * 0.12m,
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
                        COA = "1010204 AR-Non Trade Receivable",
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

                    if (model.StatementOfAccount.Customer.CustomerType == "Vatable")
                    {
                        crb.Add(
                            new CashReceiptBook
                            {
                                Date = model.Date.ToShortDateString(),
                                RefNo = model.ORNo,
                                CustomerName = model.StatementOfAccount.Customer.Name,
                                Bank = "--",
                                CheckNo = "--",
                                COA = "2010301 Vat Output",
                                Particulars = model.StatementOfAccount.SOANo,
                                Debit = 0,
                                Credit = (model.Total / 1.12m) * 0.12m,
                                CreatedBy = model.CreatedBy,
                                CreatedDate = model.CreatedDate
                            }
                        );
                    }

                    await _dbContext.AddRangeAsync(crb, cancellationToken);

                    #endregion --Cash Receipt Book Recording

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(model.PostedBy, $"Posted official receipt# {model.ORNo}", "Official Receipt");
                    await _dbContext.AddAsync(auditTrail, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _receiptRepo.UpdateSoa(model.StatementOfAccount.Id, model.Total, offsetAmount, cancellationToken);

                    await _dbContext.SaveChangesAsync();
                    TempData["success"] = "Official Receipt has been Posted.";
                }
                return RedirectToAction("OfficialIndex");
            }

            return NotFound();
        }

        public async Task<IActionResult> VoidOR(int itemId, CancellationToken cancellationToken)
        {
            var model = await _dbContext.OfficialReceipts.FindAsync(itemId, cancellationToken);

            if (model != null)
            {
                if (!model.IsVoided)
                {
                    model.IsVoided = true;
                    model.VoidedBy = _userManager.GetUserName(this.User);
                    model.VoidedDate = DateTime.Now;

                    await _generalRepo.RemoveRecords<CashReceiptBook>(crb => crb.RefNo == model.ORNo, cancellationToken);
                    await _generalRepo.RemoveRecords<GeneralLedgerBook>(gl => gl.Reference == model.ORNo,cancellationToken);

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(model.VoidedBy, $"Voided official receipt# {model.ORNo}", "Official Receipt");
                    await _dbContext.AddAsync(auditTrail, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Official Receipt has been Voided.";
                }
                return RedirectToAction("OfficialIndex");
            }

            return NotFound();
        }

        public async Task<IActionResult> CancelOR(int itemId, CancellationToken cancellationToken)
        {
            var model = await _dbContext.OfficialReceipts.FindAsync(itemId, cancellationToken);

            if (model != null)
            {
                if (!model.IsCanceled)
                {
                    model.IsCanceled = true;
                    model.CanceledBy = _userManager.GetUserName(this.User);
                    model.CanceledDate = DateTime.Now;

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(model.CanceledBy, $"Cancelled official receipt# {model.ORNo}", "Official Receipt");
                    await _dbContext.AddAsync(auditTrail, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Official Receipt has been Cancelled.";
                }
                return RedirectToAction("OfficialIndex");
            }

            return NotFound();
        }
    }
}