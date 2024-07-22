using Accounting_System.Data;
using Accounting_System.Models;
using Accounting_System.Models.AccountsReceivable;
using Accounting_System.Models.Reports;
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

        [HttpGet]
        public async Task<IActionResult> CollectionCreateForSales(CancellationToken cancellationToken)
        {
            var viewModel = new CollectionReceipt();

            viewModel.Customers = await _dbContext.Customers
               .OrderBy(c => c.Id)
               .Select(s => new SelectListItem
               {
                   Value = s.Id.ToString(),
                   Text = s.Name
               })
               .ToListAsync(cancellationToken);

            viewModel.ChartOfAccounts = await _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == 4 || coa.Level == 5)
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
        public async Task<IActionResult> CollectionCreateForSales(CollectionReceipt model, string[] accountTitleText, decimal[] accountAmount, string[] accountTitle, IFormFile? bir2306, IFormFile? bir2307, CancellationToken cancellationToken)
        {
            model.Customers = await _dbContext.Customers
               .OrderBy(c => c.Id)
               .Select(s => new SelectListItem
               {
                   Value = s.Number.ToString(),
                   Text = s.Name
               })
               .ToListAsync(cancellationToken);

            model.SalesInvoices = await _dbContext.SalesInvoices
                .Where(si => !si.IsPaid && si.CustomerId == model.CustomerId && si.IsPosted)
                .OrderBy(si => si.Id)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.SINo
                })
                .ToListAsync(cancellationToken);

            model.ChartOfAccounts = await _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == 4 || coa.Level == 5)
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

                    var splitAccountTitle = currentAccountTitle.Split(new[] { ' ' }, 2);

                    offsettings.Add(
                        new Offsetting
                        {
                            AccountNo = accountTitle[i],
                            AccountTitle = splitAccountTitle.Length > 1 ? splitAccountTitle[1] : splitAccountTitle[0],
                            Source = model.CRNo,
                            Reference = model.SINo,
                            Amount = currentAccountAmount,
                            CreatedBy = model.CreatedBy,
                            CreatedDate = model.CreatedDate
                        }
                    );
                }

                await _dbContext.AddRangeAsync(offsettings, cancellationToken);

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
        public async Task<IActionResult> CollectionCreateForService(CancellationToken cancellationToken)
        {
            var viewModel = new CollectionReceipt();

            viewModel.Customers = await _dbContext.Customers
               .OrderBy(c => c.Id)
               .Select(s => new SelectListItem
               {
                   Value = s.Id.ToString(),
                   Text = s.Name
               })
               .ToListAsync(cancellationToken);

            viewModel.ChartOfAccounts = await _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == 4 || coa.Level == 5)
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
        public async Task<IActionResult> CollectionCreateForService(CollectionReceipt model, string[] accountTitleText, decimal[] accountAmount, string[] accountTitle, IFormFile? bir2306, IFormFile? bir2307, CancellationToken cancellationToken)
        {
            model.Customers = await _dbContext.Customers
               .OrderBy(c => c.Id)
               .Select(s => new SelectListItem
               {
                   Value = s.Id.ToString(),
                   Text = s.Name
               })
               .ToListAsync(cancellationToken);

            model.SalesInvoices = await _dbContext.ServiceInvoices
                .Where(si => !si.IsPaid && si.CustomerId == model.CustomerId && si.IsPosted)
                .OrderBy(si => si.Id)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.SVNo
                })
                .ToListAsync(cancellationToken);

            model.ChartOfAccounts = await _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == 4 || coa.Level == 5)
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
                var existingServiceInvoice = await _dbContext.ServiceInvoices
                                               .FirstOrDefaultAsync(si => si.Id == model.ServiceInvoiceId, cancellationToken);
                var generateCRNo = await _receiptRepo.GenerateCRNo(cancellationToken);

                model.SeriesNumber = getLastNumber;
                model.SVNo = existingServiceInvoice.SVNo;
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

                    var splitAccountTitle = currentAccountTitle.Split(new[] { ' ' }, 2);

                    offsettings.Add(
                        new Offsetting
                        {
                            AccountNo = accountTitle[i],
                            AccountTitle = splitAccountTitle.Length > 1 ? splitAccountTitle[1] : splitAccountTitle[0],
                            Source = model.CRNo,
                            Reference = model.SVNo,
                            Amount = currentAccountAmount,
                            CreatedBy = model.CreatedBy,
                            CreatedDate = model.CreatedDate
                        }
                    );
                }

                await _dbContext.AddRangeAsync(offsettings, cancellationToken);

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

        public async Task<IActionResult> CollectionPrint(int id, CancellationToken cancellationToken)
        {
            var cr = await _receiptRepo.FindCR(id, cancellationToken);
            return View(cr);
        }

        public async Task<IActionResult> CollectionPreview(int id, CancellationToken cancellationToken)
        {
            var cr = await _receiptRepo.FindCR(id, cancellationToken);
            return PartialView("_CollectionPreviewPartialView", cr);
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

        [HttpGet]
        public async Task<IActionResult> GetSalesInvoices(int customerNo, CancellationToken cancellationToken)
        {
            var invoices = await _dbContext
                .SalesInvoices
                .Where(si => si.CustomerId == customerNo && !si.IsPaid && si.IsPosted)
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
        public async Task<IActionResult> GetServiceInvoices(int customerNo, CancellationToken cancellationToken)
        {
            var invoices = await _dbContext
                .ServiceInvoices
                .Where(si => si.CustomerId == customerNo && !si.IsPaid && si.IsPosted)
                .OrderBy(si => si.Id)
                .ToListAsync(cancellationToken);

            var invoiceList = invoices.Select(si => new SelectListItem
            {
                Value = si.Id.ToString(),   // Replace with your actual ID property
                Text = si.SVNo              // Replace with your actual property for display text
            }).ToList();

            return Json(invoiceList);
        }

        [HttpGet]
        public async Task<IActionResult> GetInvoiceDetails(int invoiceNo, bool isSales, bool isServices, CancellationToken cancellationToken)
        {
            if (isSales && !isServices)
            {
                var si = await _dbContext
                .SalesInvoices
                .FirstOrDefaultAsync(si => si.Id == invoiceNo, cancellationToken);

                return Json(new
                {
                    Amount = si.NetDiscount.ToString("N2"),
                    AmountPaid = si.AmountPaid.ToString("N2"),
                    Balance = si.Balance.ToString("N2"),
                    Ewt = si.WithHoldingTaxAmount.ToString("N2"),
                    Wvat = si.WithHoldingVatAmount.ToString("N2"),
                    Total = (si.NetDiscount - (si.WithHoldingTaxAmount + si.WithHoldingVatAmount)).ToString("N2")
                });
            }
            else if (isServices && !isSales)
            {
                var sv = await _dbContext
                .ServiceInvoices
                .FirstOrDefaultAsync(si => si.Id == invoiceNo, cancellationToken);

                return Json(new
                {
                    Amount = sv.Total.ToString("N2"),
                    AmountPaid = sv.AmountPaid.ToString("N2"),
                    Balance = sv.Balance.ToString("N2"),
                    Ewt = sv.WithholdingTaxAmount.ToString("N2"),
                    Wvat = sv.WithholdingVatAmount.ToString("N2"),
                    Total = (sv.Total - (sv.WithholdingTaxAmount + sv.WithholdingVatAmount)).ToString("N2")
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
                   Value = s.Id.ToString(),
                   Text = s.Name
               })
               .ToListAsync(cancellationToken);

            existingModel.SalesInvoices = await _dbContext.SalesInvoices
                .Where(si => !si.IsPaid && si.CustomerId == existingModel.CustomerId)
                .OrderBy(si => si.Id)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.SINo
                })
                .ToListAsync(cancellationToken);

            existingModel.ServiceInvoices = await _dbContext.ServiceInvoices
                .Where(si => !si.IsPaid && si.CustomerId == existingModel.CustomerId)
                .OrderBy(si => si.Id)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.SVNo
                })
                .ToListAsync(cancellationToken);

            existingModel.ChartOfAccounts = await _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == 4 || coa.Level == 5)
                .OrderBy(coa => coa.Id)
                .Select(s => new SelectListItem
                {
                    Value = s.Number,
                    Text = s.Number + " " + s.Name
                })
                .ToListAsync(cancellationToken);

            var findCustomers = await _dbContext.Customers
                .FirstOrDefaultAsync(c => c.Id == existingModel.CustomerId, cancellationToken);

            var offsettings = await _dbContext.Offsettings
                .Where(offset => offset.Source == existingModel.CRNo)
                .ToListAsync(cancellationToken);

            ViewBag.CustomerName = findCustomers?.Name;
            ViewBag.Offsettings = offsettings;

            return View(existingModel);
        }

        [HttpPost]
        public async Task<IActionResult> CollectionEdit(CollectionReceipt model, string[] accountTitleText, decimal[] accountAmount, string[] accountTitle, IFormFile? bir2306, IFormFile? bir2307, CancellationToken cancellationToken)
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

                existingModel.TransactionDate = model.TransactionDate;
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

                var findOffsettings = await _dbContext.Offsettings
                .Where(offset => offset.Source == existingModel.CRNo)
                .ToListAsync(cancellationToken);

                var accountTitleSet = new HashSet<string>(accountTitle);

                // Remove records not in accountTitle
                foreach (var offsetting in findOffsettings)
                {
                    if (!accountTitleSet.Contains(offsetting.AccountNo))
                    {
                        _dbContext.Offsettings.Remove(offsetting);
                    }
                }

                // Dictionary to keep track of AccountNo and their ids for comparison
                var accountTitleDict = new Dictionary<string, List<int>>();
                foreach (var offsetting in findOffsettings)
                {
                    if (!accountTitleDict.ContainsKey(offsetting.AccountNo))
                    {
                        accountTitleDict[offsetting.AccountNo] = new List<int>();
                    }
                    accountTitleDict[offsetting.AccountNo].Add(offsetting.Id);
                }

                // Add or update records
                for (int i = 0; i < accountTitle.Length; i++)
                {
                    var accountNo = accountTitle[i];
                    var currentAccountTitle = accountTitleText[i];
                    var currentAccountAmount = accountAmount[i];
                    offsetAmount += accountAmount[i];

                    var splitAccountTitle = currentAccountTitle.Split(new[] { ' ' }, 2);

                    if (accountTitleDict.TryGetValue(accountNo, out var ids))
                    {
                        // Update the first matching record and remove it from the list
                        var offsettingId = ids.First();
                        ids.RemoveAt(0);
                        var offsetting = findOffsettings.First(o => o.Id == offsettingId);

                        offsetting.AccountTitle = splitAccountTitle.Length > 1 ? splitAccountTitle[1] : splitAccountTitle[0];
                        offsetting.Amount = currentAccountAmount;
                        offsetting.CreatedBy = _userManager.GetUserName(this.User);
                        offsetting.CreatedDate = DateTime.Now;

                        if (ids.Count == 0)
                        {
                            accountTitleDict.Remove(accountNo);
                        }
                    }
                    else
                    {
                        // Add new record
                        var newOffsetting = new Offsetting
                        {
                            AccountNo = accountNo,
                            AccountTitle = splitAccountTitle.Length > 1 ? splitAccountTitle[1] : splitAccountTitle[0],
                            Source = existingModel.CRNo,
                            Reference = existingModel.SINo != null ? existingModel.SINo : existingModel.SVNo,
                            Amount = currentAccountAmount,
                            CreatedBy = _userManager.GetUserName(this.User),
                            CreatedDate = DateTime.Now
                        };
                        _dbContext.Offsettings.Add(newOffsetting);
                    }
                }

                // Remove remaining records that were duplicates
                foreach (var ids in accountTitleDict.Values)
                {
                    foreach (var id in ids)
                    {
                        var offsetting = findOffsettings.First(o => o.Id == id);
                        _dbContext.Offsettings.Remove(offsetting);
                    }
                }

                #endregion --Offsetting function

                #region --Audit Trail Recording

                var modifiedBy = _userManager.GetUserName(this.User);
                AuditTrail auditTrail = new(modifiedBy, $"Edited receipt# {existingModel.CRNo}", "Collection Receipt");
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
                try
                {
                    if (!model.IsPosted)
                    {
                        model.IsPosted = true;
                        model.PostedBy = _userManager.GetUserName(this.User);
                        model.PostedDate = DateTime.Now;

                        List<Offsetting>? offset = new List<Offsetting>();

                        if (model.SalesInvoiceId != null)
                        {
                            offset = await _receiptRepo.GetOffsettingAsync(model.CRNo, model.SINo, cancellationToken);
                        }
                        else
                        {
                            offset = await _receiptRepo.GetOffsettingAsync(model.CRNo, model.SVNo, cancellationToken);
                        }

                        decimal offsetAmount = 0;

                        #region --General Ledger Book Recording

                        var ledgers = new List<GeneralLedgerBook>();

                        ledgers.Add(
                                    new GeneralLedgerBook
                                    {
                                        Date = model.TransactionDate,
                                        Reference = model.CRNo,
                                        Description = "Collection for Receivable",
                                        AccountNo = "1010101",
                                        AccountTitle = "Cash in Bank",
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
                                    Date = model.TransactionDate,
                                    Reference = model.CRNo,
                                    Description = "Collection for Receivable",
                                    AccountNo = "1010604",
                                    AccountTitle = "Creditable Withholding Tax",
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
                                    Date = model.TransactionDate,
                                    Reference = model.CRNo,
                                    Description = "Collection for Receivable",
                                    AccountNo = "1010605",
                                    AccountTitle = "Creditable Withholding Vat",
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
                                    Date = model.TransactionDate,
                                    Reference = model.CRNo,
                                    Description = "Collection for Receivable",
                                    AccountNo = item.AccountNo,
                                    AccountTitle = item.AccountTitle,
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
                                    Date = model.TransactionDate,
                                    Reference = model.CRNo,
                                    Description = "Collection for Receivable",
                                    AccountNo = "1010201",
                                    AccountTitle = "AR-Trade Receivable",
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
                                    Date = model.TransactionDate,
                                    Reference = model.CRNo,
                                    Description = "Collection for Receivable",
                                    AccountNo = "1010202",
                                    AccountTitle = "Deferred Creditable Withholding Tax",
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
                                    Date = model.TransactionDate,
                                    Reference = model.CRNo,
                                    Description = "Collection for Receivable",
                                    AccountNo = "1010203",
                                    AccountTitle = "Deferred Creditable Withholding Vat",
                                    Debit = 0,
                                    Credit = model.WVAT,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }

                        if (!_generalRepo.IsDebitCreditBalanced(ledgers))
                        {
                            throw new ArgumentException("Debit and Credit is not equal, check your entries.");
                        }

                        await _dbContext.AddRangeAsync(ledgers, cancellationToken);

                        #endregion --General Ledger Book Recording

                        #region --Cash Receipt Book Recording

                        var crb = new List<CashReceiptBook>();

                        crb.Add(
                            new CashReceiptBook
                            {
                                Date = model.TransactionDate,
                                RefNo = model.CRNo,
                                CustomerName = model.SalesInvoiceId != null ? model.SalesInvoice.Customer.Name : model.ServiceInvoice.Customer.Name,
                                Bank = model.CheckBank ?? (model.ManagerCheckBank != null ? model.ManagerCheckBank : "--"),
                                CheckNo = model.CheckNo ?? (model.ManagerCheckNo != null ? model.ManagerCheckNo : "--"),
                                COA = "1010101 Cash in Bank",
                                Particulars = model.SalesInvoiceId != null ? model.SalesInvoice.SINo : model.ServiceInvoice.SVNo,
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
                                    Date = model.TransactionDate,
                                    RefNo = model.CRNo,
                                    CustomerName = model.SalesInvoiceId != null ? model.SalesInvoice.Customer.Name : model.ServiceInvoice.Customer.Name,
                                    Bank = model.CheckBank ?? (model.ManagerCheckBank != null ? model.ManagerCheckBank : "--"),
                                    CheckNo = model.CheckNo ?? (model.ManagerCheckNo != null ? model.ManagerCheckNo : "--"),
                                    COA = "1010604 Creditable Withholding Tax",
                                    Particulars = model.SalesInvoiceId != null ? model.SalesInvoice.SINo : model.ServiceInvoice.SVNo,
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
                                    Date = model.TransactionDate,
                                    RefNo = model.CRNo,
                                    CustomerName = model.SalesInvoiceId != null ? model.SalesInvoice.Customer.Name : model.ServiceInvoice.Customer.Name,
                                    Bank = model.CheckBank ?? (model.ManagerCheckBank != null ? model.ManagerCheckBank : "--"),
                                    CheckNo = model.CheckNo ?? (model.ManagerCheckNo != null ? model.ManagerCheckNo : "--"),
                                    COA = "1010605 Creditable Withholding Vat",
                                    Particulars = model.SalesInvoiceId != null ? model.SalesInvoice.SINo : model.ServiceInvoice.SVNo,
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
                                        Date = model.TransactionDate,
                                        RefNo = model.CRNo,
                                        CustomerName = model.SalesInvoiceId != null ? model.SalesInvoice.Customer.Name : model.ServiceInvoice.Customer.Name,
                                        Bank = model.CheckBank ?? (model.ManagerCheckBank != null ? model.ManagerCheckBank : "--"),
                                        CheckNo = model.CheckNo ?? (model.ManagerCheckNo != null ? model.ManagerCheckNo : "--"),
                                        COA = item.AccountNo,
                                        Particulars = model.SalesInvoiceId != null ? model.SalesInvoice.SINo : model.ServiceInvoice.SVNo,
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
                            Date = model.TransactionDate,
                            RefNo = model.CRNo,
                            CustomerName = model.SalesInvoiceId != null ? model.SalesInvoice.Customer.Name : model.ServiceInvoice.Customer.Name,
                            Bank = model.CheckBank ?? (model.ManagerCheckBank != null ? model.ManagerCheckBank : "--"),
                            CheckNo = model.CheckNo ?? (model.ManagerCheckNo != null ? model.ManagerCheckNo : "--"),
                            COA = "1010201 AR-Trade Receivable",
                            Particulars = model.SalesInvoiceId != null ? model.SalesInvoice.SINo : model.ServiceInvoice.SVNo,
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
                                    Date = model.TransactionDate,
                                    RefNo = model.CRNo,
                                    CustomerName = model.SalesInvoiceId != null ? model.SalesInvoice.Customer.Name : model.ServiceInvoice.Customer.Name,
                                    Bank = model.CheckBank ?? (model.ManagerCheckBank != null ? model.ManagerCheckBank : "--"),
                                    CheckNo = model.CheckNo ?? (model.ManagerCheckNo != null ? model.ManagerCheckNo : "--"),
                                    COA = "1010202 Deferred Creditable Withholding Tax",
                                    Particulars = model.SalesInvoiceId != null ? model.SalesInvoice.SINo : model.ServiceInvoice.SVNo,
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
                                    Date = model.TransactionDate,
                                    RefNo = model.CRNo,
                                    CustomerName = model.SalesInvoiceId != null ? model.SalesInvoice.Customer.Name : model.ServiceInvoice.Customer.Name,
                                    Bank = model.CheckBank ?? (model.ManagerCheckBank != null ? model.ManagerCheckBank : "--"),
                                    CheckNo = model.CheckNo ?? (model.ManagerCheckNo != null ? model.ManagerCheckNo : "--"),
                                    COA = "1010203 Deferred Creditable Withholding Vat",
                                    Particulars = model.SalesInvoiceId != null ? model.SalesInvoice.SINo : model.ServiceInvoice.SVNo,
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

                        if (model.SalesInvoiceId != null)
                        {
                            await _receiptRepo.UpdateInvoice(model.SalesInvoice.Id, model.Total, offsetAmount, cancellationToken);
                        }
                        else
                        {
                            await _receiptRepo.UpdateSv(model.ServiceInvoice.Id, model.Total, offsetAmount, cancellationToken);
                        }

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        TempData["success"] = "Collection Receipt has been Posted.";
                    }

                    return RedirectToAction("CollectionIndex");
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction("CollectionIndex");
                }
            }

            return NotFound();
        }

        public async Task<IActionResult> Void(int itemId, CancellationToken cancellationToken)
        {
            var model = await _receiptRepo.FindCR(itemId, cancellationToken);

            if (model != null)
            {
                if (!model.IsVoided)
                {
                    await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                    try
                    {
                        if (model.IsPosted)
                        {
                            model.IsPosted = false;
                        }

                        model.IsVoided = true;
                        model.VoidedBy = _userManager.GetUserName(this.User);
                        model.VoidedDate = DateTime.Now;
                        var series = model.SINo != null ? model.SINo : model.SVNo;

                        var findOffsetting = await _dbContext.Offsettings.Where(offset => offset.Source == model.CRNo && offset.Reference == series).ToListAsync(cancellationToken);

                        await _generalRepo.RemoveRecords<CashReceiptBook>(crb => crb.RefNo == model.CRNo, cancellationToken);
                        await _generalRepo.RemoveRecords<GeneralLedgerBook>(gl => gl.Reference == model.CRNo, cancellationToken);

                        if (findOffsetting.Any())
                        {
                            await _generalRepo.RemoveRecords<Offsetting>(offset => offset.Source == model.CRNo && offset.Reference == series, cancellationToken);
                        }
                        if (series.Contains("SI"))
                        {
                            await _receiptRepo.RemoveSIPayment(model.SalesInvoice.Id, model.Total, findOffsetting.Sum(offset => offset.Amount), cancellationToken);
                        }
                        else
                        {
                            await _receiptRepo.RemoveSVPayment(model.ServiceInvoiceId, model.Total, findOffsetting.Sum(offset => offset.Amount), cancellationToken);
                        }

                        #region --Audit Trail Recording

                        AuditTrail auditTrail = new(model.VoidedBy, $"Voided collection receipt# {model.CRNo}", "Collection Receipt");
                        await _dbContext.AddAsync(auditTrail, cancellationToken);

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync();
                        TempData["success"] = "Collection Receipt has been Voided.";
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        TempData["error"] = ex.Message;
                    }
                }
                return RedirectToAction("CollectionIndex");
            }

            return NotFound();
        }

        public async Task<IActionResult> Cancel(int itemId, string cancellationRemarks, CancellationToken cancellationToken)
        {
            var model = await _dbContext.CollectionReceipts.FindAsync(itemId, cancellationToken);

            if (model != null)
            {
                if (!model.IsCanceled)
                {
                    model.IsCanceled = true;
                    model.CanceledBy = _userManager.GetUserName(this.User);
                    model.CanceledDate = DateTime.Now;
                    model.CancellationRemarks = cancellationRemarks;

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
    }
}