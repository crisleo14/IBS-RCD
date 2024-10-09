using Accounting_System.Data;
using Accounting_System.Models;
using Accounting_System.Models.AccountsReceivable;
using Accounting_System.Models.Reports;
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

        public async Task<IActionResult> CollectionIndex(string? view, CancellationToken cancellationToken)
        {
            var collectionReceipts = await _receiptRepo.GetCollectionReceiptsAsync(cancellationToken);

            if (view == nameof(DynamicView.CollectionReceipt))
            {
                return View("ImportExportIndex", collectionReceipts);
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetCollectionReceipts([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var collectionReceipts = await _receiptRepo.GetCollectionReceiptsAsync(cancellationToken);
                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search?.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();
                    collectionReceipts = collectionReceipts
                        .Where(cr =>
                            cr.CRNo.ToLower().Contains(searchValue) ||
                            cr.TransactionDate.ToString("MMM dd, yyyy").ToLower().Contains(searchValue) ||
                            cr.SINo?.ToLower().Contains(searchValue) == true ||
                            cr.SVNo?.ToLower().Contains(searchValue) == true ||
                            cr.MultipleSI?.Contains(searchValue) == true ||
                            cr.Customer.Name.ToLower().Contains(searchValue) ||
                            cr.Total.ToString().ToLower().Contains(searchValue) ||
                            cr.CreatedBy.ToLower().Contains(searchValue)
                            )
                        .ToList();
                }
                // Sorting
                if (parameters.Order != null && parameters.Order.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Data;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";
                    collectionReceipts = collectionReceipts
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}")
                        .ToList();
                }
                var totalRecords = collectionReceipts.Count();
                var pagedData = collectionReceipts
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
        public async Task<IActionResult> GetAllCollectionReceiptIds(CancellationToken cancellationToken)
        {
            var collectionReceiptIds = await _dbContext.CollectionReceipts
                                     .Select(cr => cr.Id) // Assuming Id is the primary key
                                     .ToListAsync(cancellationToken);
            return Json(collectionReceiptIds);
        }

        [HttpGet]
        public async Task<IActionResult> SingleCollectionCreateForSales(CancellationToken cancellationToken)
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
        public async Task<IActionResult> SingleCollectionCreateForSales(CollectionReceipt model, string[] accountTitleText, decimal[] accountAmount, string[] accountTitle, IFormFile? bir2306, IFormFile? bir2307, CancellationToken cancellationToken)
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
                            await bir2306.CopyToAsync(stream, cancellationToken);
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
                            await bir2307.CopyToAsync(stream, cancellationToken);
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

                if (model.OriginalSeriesNumber == null && model.OriginalDocumentId == 0)
                {
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    AuditTrail auditTrailBook = new(model.CreatedBy, $"Create new collection receipt# {model.CRNo}", "Collection Receipt", ipAddress);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                }

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
                return RedirectToAction(nameof(CollectionIndex));
            }
            else
            {
                TempData["error"] = "The information you submitted is not valid!";
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> MultipleCollectionCreateForSales(CancellationToken cancellationToken)
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
        public async Task<IActionResult> MultipleCollectionCreateForSales(CollectionReceipt model, string[] accountTitleText, decimal[] accountAmount, string[] accountTitle, IFormFile? bir2306, IFormFile? bir2307, CancellationToken cancellationToken)
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
                                               .Where(si => model.MultipleSIId.Contains(si.Id))
                                               .ToListAsync(cancellationToken);

                model.MultipleSI = new string[model.MultipleSIId.Length];
                model.MultipleTransactionDate = new DateOnly[model.MultipleSIId.Length];
                var salesInvoice = new SalesInvoice();
                for (int i = 0; i < model.MultipleSIId.Length; i++)
                {
                    var siId = model.MultipleSIId[i];
                    salesInvoice = await _dbContext.SalesInvoices
                                .FirstOrDefaultAsync(si => si.Id == siId, cancellationToken);

                    if (salesInvoice != null)
                    {
                        model.MultipleSI[i] = salesInvoice.SINo;
                        model.MultipleTransactionDate[i] = salesInvoice.TransactionDate;
                    }
                }

                var generateCRNo = await _receiptRepo.GenerateCRNo(cancellationToken);

                model.SeriesNumber = getLastNumber;
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
                            await bir2306.CopyToAsync(stream, cancellationToken);
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
                            await bir2307.CopyToAsync(stream, cancellationToken);
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

                if (model.OriginalSeriesNumber == null && model.OriginalDocumentId == 0)
                {
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    AuditTrail auditTrailBook = new(model.CreatedBy, $"Create new collection receipt# {model.CRNo}", "Collection Receipt", ipAddress);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                }

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
                return RedirectToAction(nameof(CollectionIndex));
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
                            await bir2306.CopyToAsync(stream, cancellationToken);
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
                            await bir2307.CopyToAsync(stream, cancellationToken);
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

                if (model.OriginalSeriesNumber == null && model.OriginalDocumentId == 0)
                {
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    AuditTrail auditTrailBook = new(model.CreatedBy, $"Create new collection receipt# {model.CRNo}", "Collection Receipt", ipAddress);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                }

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
                return RedirectToAction(nameof(CollectionIndex));
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
        public async Task<IActionResult> MultipleCollectionPrint(int id, CancellationToken cancellationToken)
        {
            var cr = await _receiptRepo.FindCR(id, cancellationToken);
            return View(cr);
        }

        public async Task<IActionResult> PrintedCollectionReceipt(int id, CancellationToken cancellationToken)
        {
            var findIdOfCR = await _receiptRepo.FindCR(id, cancellationToken);
            if (findIdOfCR != null && !findIdOfCR.IsPrinted)
            {

                #region --Audit Trail Recording

                if (findIdOfCR.OriginalSeriesNumber == null && findIdOfCR.OriginalDocumentId == 0)
                {
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    var printedBy = _userManager.GetUserName(this.User);
                    AuditTrail auditTrailBook = new(printedBy, $"Printed original copy of cr# {findIdOfCR.CRNo}", "Collection Receipt", ipAddress);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                }

                #endregion --Audit Trail Recording

                findIdOfCR.IsPrinted = true;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            return RedirectToAction(nameof(CollectionPrint), new { id });
        }
        public async Task<IActionResult> PrintedMultipleCR(int id, CancellationToken cancellationToken)
        {
            var findIdOfCR = await _receiptRepo.FindCR(id, cancellationToken);
            if (findIdOfCR != null && !findIdOfCR.IsPrinted)
            {

                #region --Audit Trail Recording

                if (findIdOfCR.OriginalSeriesNumber == null && findIdOfCR.OriginalDocumentId == 0)
                {
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    var printedBy = _userManager.GetUserName(this.User);
                    AuditTrail auditTrailBook = new(printedBy, $"Printed original copy of cr# {findIdOfCR.CRNo}", "Collection Receipt", ipAddress);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                }

                #endregion --Audit Trail Recording

                findIdOfCR.IsPrinted = true;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            return RedirectToAction(nameof(MultipleCollectionPrint), new { id });
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
                .Include(c => c.Customer)
                .FirstOrDefaultAsync(si => si.Id == invoiceNo, cancellationToken);

                decimal netDiscount = si.Amount - si.Discount;
                decimal netOfVatAmount = si.Customer.CustomerType == CS.VatType_Vatable ? _generalRepo.ComputeNetOfVat(netDiscount) : netDiscount;
                decimal withHoldingTaxAmount = si.Customer.WithHoldingTax ? _generalRepo.ComputeEwtAmount(netOfVatAmount, 0.01m) : 0;
                decimal withHoldingVatAmount = si.Customer.WithHoldingVat ? _generalRepo.ComputeEwtAmount(netOfVatAmount, 0.05m) : 0;

                return Json(new
                {
                    Amount = netDiscount.ToString("N2"),
                    AmountPaid = si.AmountPaid.ToString("N2"),
                    Balance = si.Balance.ToString("N2"),
                    Ewt = withHoldingTaxAmount.ToString("N2"),
                    Wvat = withHoldingVatAmount.ToString("N2"),
                    Total = (netDiscount - (withHoldingTaxAmount + withHoldingVatAmount)).ToString("N2")
                });
            }
            else if (isServices && !isSales)
            {
                var sv = await _dbContext
                .ServiceInvoices
                .Include(c => c.Customer)
                .FirstOrDefaultAsync(si => si.Id == invoiceNo, cancellationToken);

                decimal netOfVatAmount = sv.Customer.CustomerType == CS.VatType_Vatable ? _generalRepo.ComputeNetOfVat(sv.Amount) - sv.Discount : sv.Amount - sv.Discount;
                decimal withHoldingTaxAmount = sv.Customer.WithHoldingTax ? _generalRepo.ComputeEwtAmount(netOfVatAmount, 0.01m) : 0;
                decimal withHoldingVatAmount = sv.Customer.WithHoldingVat ? _generalRepo.ComputeEwtAmount(netOfVatAmount, 0.05m) : 0;

                return Json(new
                {
                    Amount = sv.Total.ToString("N2"),
                    AmountPaid = sv.AmountPaid.ToString("N2"),
                    Balance = sv.Balance.ToString("N2"),
                    Ewt = withHoldingTaxAmount.ToString("N2"),
                    Wvat = withHoldingVatAmount.ToString("N2"),
                    Total = (sv.Total - (withHoldingTaxAmount + withHoldingVatAmount)).ToString("N2")
                });
            }
            return Json(null);
        }

        public async Task<IActionResult> MultipleInvoiceBalance(int siNo, CancellationToken cancellationToken)
        {
            var salesInvoice = await _dbContext.SalesInvoices
                .Include(c => c.Customer)
                .FirstOrDefaultAsync(si => si.Id == siNo, cancellationToken);
            if (salesInvoice != null)
            {
                var amount = salesInvoice.Amount;
                var amountPaid = salesInvoice.AmountPaid;
                var netAmount = salesInvoice.Amount - salesInvoice.Discount;
                var vatAmount = salesInvoice.Customer.CustomerType == CS.VatType_Vatable ? _generalRepo.ComputeVatAmount((netAmount / 1.12m) * 0.12m) : 0;
                var ewtAmount = salesInvoice.Customer.WithHoldingTax ? _generalRepo.ComputeEwtAmount((netAmount / 1.12m), 0.01m) : 0;
                var wvatAmount = salesInvoice.Customer.WithHoldingVat ? _generalRepo.ComputeEwtAmount((netAmount / 1.12m), 0.05m) : 0;
                var balance = amount - amountPaid;

                return Json(new
                {
                    Amount = amount,
                    AmountPaid = amountPaid,
                    NetAmount = netAmount,
                    VatAmount = vatAmount,
                    EwtAmount = ewtAmount,
                    WvatAmount = wvatAmount,
                    Balance = balance
                });
            }
            return Json(null);
        }

        [HttpGet]
        public async Task<IActionResult> GetMultipleInvoiceDetails(int[] siNo, bool isSales, CancellationToken cancellationToken)
        {
            if (isSales)
            {
                var si = await _dbContext
                .SalesInvoices
                .FirstOrDefaultAsync(si => siNo.Contains(si.Id), cancellationToken);

                decimal netDiscount = si.Amount - si.Discount;
                decimal netOfVatAmount = si.Customer.CustomerType == CS.VatType_Vatable ? _generalRepo.ComputeNetOfVat(netDiscount) : netDiscount;
                decimal withHoldingTaxAmount = si.Customer.WithHoldingTax ? _generalRepo.ComputeEwtAmount(netOfVatAmount, 0.01m) : 0;
                decimal withHoldingVatAmount = si.Customer.WithHoldingVat ? _generalRepo.ComputeEwtAmount(netOfVatAmount, 0.05m) : 0;

                return Json(new
                {
                    Amount = netDiscount,
                    AmountPaid = si.AmountPaid,
                    Balance = si.Balance,
                    WithholdingTax = withHoldingTaxAmount,
                    WithholdingVat = withHoldingVatAmount,
                    Total = netDiscount - (withHoldingTaxAmount + withHoldingVatAmount)
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
                            await bir2306.CopyToAsync(stream, cancellationToken);
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
                            await bir2307.CopyToAsync(stream, cancellationToken);
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
                        await _dbContext.Offsettings.AddAsync(newOffsetting, cancellationToken);
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

                if (existingModel.OriginalSeriesNumber == null && existingModel.OriginalDocumentId == 0)
                {
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    var modifiedBy = _userManager.GetUserName(this.User);
                    AuditTrail auditTrailBook = new(modifiedBy, $"Edited collection receipt# {existingModel.CRNo}", "Collection Receipt", ipAddress);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                }

                #endregion --Audit Trail Recording

                await _dbContext.SaveChangesAsync(cancellationToken);
                return RedirectToAction(nameof(CollectionIndex));
            }
            else
            {
                TempData["error"] = "The information you submitted is not valid!";
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> MultipleCollectionEdit(int? id, CancellationToken cancellationToken)
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
        public async Task<IActionResult> MultipleCollectionEdit(CollectionReceipt model, string[] accountTitleText, decimal[] accountAmount, string[] accountTitle, IFormFile? bir2306, IFormFile? bir2307, CancellationToken cancellationToken)
        {
            var existingModel = await _receiptRepo.FindCR(model.Id, cancellationToken);

            if (ModelState.IsValid)
            {
                #region --Saving default value

                var computeTotalInModelIfZero = model.CashAmount + model.CheckAmount + model.ManagerCheckAmount + model.EWT + model.WVAT;
                if (computeTotalInModelIfZero == 0)
                {
                    TempData["error"] = "Please input atleast one type form of payment";
                    return View(model);
                }
                var existingSalesInvoice = await _dbContext.SalesInvoices
                                               .Where(si => model.MultipleSIId.Contains(si.Id))
                                               .ToListAsync(cancellationToken);

                existingModel.MultipleSIId = new int[model.MultipleSIId.Length];
                existingModel.MultipleSI = new string[model.MultipleSIId.Length];
                existingModel.SIMultipleAmount = new decimal[model.MultipleSIId.Length];
                existingModel.MultipleTransactionDate = new DateOnly[model.MultipleSIId.Length];
                var salesInvoice = new SalesInvoice();
                for (int i = 0; i < model.MultipleSIId.Length; i++)
                {
                    var siId = model.MultipleSIId[i];
                    salesInvoice = await _dbContext.SalesInvoices
                                .FirstOrDefaultAsync(si => si.Id == siId, cancellationToken);

                    if (salesInvoice != null)
                    {
                        existingModel.MultipleSIId[i] = model.MultipleSIId[i];
                        existingModel.MultipleSI[i] = salesInvoice.SINo;
                        existingModel.MultipleTransactionDate[i] = salesInvoice.TransactionDate;
                        existingModel.SIMultipleAmount[i] = model.SIMultipleAmount[i];
                    }
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
                            await bir2306.CopyToAsync(stream, cancellationToken);
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
                            await bir2307.CopyToAsync(stream, cancellationToken);
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
                        await _dbContext.Offsettings.AddAsync(newOffsetting, cancellationToken);
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

                if (model.OriginalSeriesNumber == null && model.OriginalDocumentId == 0)
                {
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    var modifiedBy = _userManager.GetUserName(this.User);
                    AuditTrail auditTrailBook = new(modifiedBy, $"Edited collection receipt# {existingModel.CRNo}", "Collection Receipt", ipAddress);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                }

                #endregion --Audit Trail Recording

                await _dbContext.SaveChangesAsync(cancellationToken);
                TempData["success"] = "Collection Receipt edited successfully";
                return RedirectToAction(nameof(CollectionIndex));
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
                var collectionPrint = model.MultipleSIId != null ? nameof(MultipleCollectionPrint) : nameof(CollectionPrint);
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

                        if (model.CashAmount > 0 || model.CheckAmount > 0 || model.ManagerCheckAmount > 0)
                        {
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
                        }

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

                        if (model.CashAmount > 0 || model.CheckAmount > 0 || model.ManagerCheckAmount > 0 || offsetAmount > 0)
                        {
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
                        }

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
                                CustomerName = model.SalesInvoiceId != null ? model.SalesInvoice.Customer.Name : model.MultipleSIId != null ? model.Customer.Name : model.ServiceInvoice.Customer.Name,
                                Bank = model.CheckBank ?? (model.ManagerCheckBank != null ? model.ManagerCheckBank : "--"),
                                CheckNo = model.CheckNo ?? (model.ManagerCheckNo != null ? model.ManagerCheckNo : "--"),
                                COA = "1010101 Cash in Bank",
                                Particulars = model.SalesInvoiceId != null ? model.SalesInvoice.SINo : model.MultipleSIId != null ? string.Join(", ", model.MultipleSI.Select(si => si.ToString())) : model.ServiceInvoice.SVNo,
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
                                    CustomerName = model.SalesInvoiceId != null ? model.SalesInvoice.Customer.Name : model.MultipleSIId != null ? model.Customer.Name : model.ServiceInvoice.Customer.Name,
                                    Bank = model.CheckBank ?? (model.ManagerCheckBank != null ? model.ManagerCheckBank : "--"),
                                    CheckNo = model.CheckNo ?? (model.ManagerCheckNo != null ? model.ManagerCheckNo : "--"),
                                    COA = "1010604 Creditable Withholding Tax",
                                    Particulars = model.SalesInvoiceId != null ? model.SalesInvoice.SINo : model.MultipleSIId != null ? string.Join(", ", model.MultipleSI.Select(si => si.ToString())) : model.ServiceInvoice.SVNo,
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
                                    CustomerName = model.SalesInvoiceId != null ? model.SalesInvoice.Customer.Name : model.MultipleSIId != null ? model.Customer.Name : model.ServiceInvoice.Customer.Name,
                                    Bank = model.CheckBank ?? (model.ManagerCheckBank != null ? model.ManagerCheckBank : "--"),
                                    CheckNo = model.CheckNo ?? (model.ManagerCheckNo != null ? model.ManagerCheckNo : "--"),
                                    COA = "1010605 Creditable Withholding Vat",
                                    Particulars = model.SalesInvoiceId != null ? model.SalesInvoice.SINo : model.MultipleSIId != null ? string.Join(", ", model.MultipleSI.Select(si => si.ToString())) : model.ServiceInvoice.SVNo,
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
                                        CustomerName = model.SalesInvoiceId != null ? model.SalesInvoice.Customer.Name : model.MultipleSIId != null ? model.Customer.Name : model.ServiceInvoice.Customer.Name,
                                        Bank = model.CheckBank ?? (model.ManagerCheckBank != null ? model.ManagerCheckBank : "--"),
                                        CheckNo = model.CheckNo ?? (model.ManagerCheckNo != null ? model.ManagerCheckNo : "--"),
                                        COA = item.AccountNo,
                                        Particulars = model.SalesInvoiceId != null ? model.SalesInvoice.SINo : model.MultipleSIId != null ? string.Join(", ", model.MultipleSI.Select(si => si.ToString())) : model.ServiceInvoice.SVNo,
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
                            CustomerName = model.SalesInvoiceId != null ? model.SalesInvoice.Customer.Name : model.MultipleSIId != null ? model.Customer.Name : model.ServiceInvoice.Customer.Name,
                            Bank = model.CheckBank ?? (model.ManagerCheckBank != null ? model.ManagerCheckBank : "--"),
                            CheckNo = model.CheckNo ?? (model.ManagerCheckNo != null ? model.ManagerCheckNo : "--"),
                            COA = "1010201 AR-Trade Receivable",
                            Particulars = model.SalesInvoiceId != null ? model.SalesInvoice.SINo : model.MultipleSIId != null ? string.Join(", ", model.MultipleSI.Select(si => si.ToString())) : model.ServiceInvoice.SVNo,
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
                                    CustomerName = model.SalesInvoiceId != null ? model.SalesInvoice.Customer.Name : model.MultipleSIId != null ? model.Customer.Name : model.ServiceInvoice.Customer.Name,
                                    Bank = model.CheckBank ?? (model.ManagerCheckBank != null ? model.ManagerCheckBank : "--"),
                                    CheckNo = model.CheckNo ?? (model.ManagerCheckNo != null ? model.ManagerCheckNo : "--"),
                                    COA = "1010202 Deferred Creditable Withholding Tax",
                                    Particulars = model.SalesInvoiceId != null ? model.SalesInvoice.SINo : model.MultipleSIId != null ? string.Join(", ", model.MultipleSI.Select(si => si.ToString())) : model.ServiceInvoice.SVNo,
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
                                    CustomerName = model.SalesInvoiceId != null ? model.SalesInvoice.Customer.Name : model.MultipleSIId != null ? model.Customer.Name : model.ServiceInvoice.Customer.Name,
                                    Bank = model.CheckBank ?? (model.ManagerCheckBank != null ? model.ManagerCheckBank : "--"),
                                    CheckNo = model.CheckNo ?? (model.ManagerCheckNo != null ? model.ManagerCheckNo : "--"),
                                    COA = "1010203 Deferred Creditable Withholding Vat",
                                    Particulars = model.SalesInvoiceId != null ? model.SalesInvoice.SINo : model.MultipleSIId != null ? string.Join(", ", model.MultipleSI.Select(si => si.ToString())) : model.ServiceInvoice.SVNo,
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

                        if (model.OriginalSeriesNumber == null && model.OriginalDocumentId == 0)
                        {
                            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                            AuditTrail auditTrailBook = new(model.PostedBy, $"Posted collection receipt# {model.CRNo}", "Collection Receipt", ipAddress);
                            await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                        }

                        #endregion --Audit Trail Recording

                        if (model.SalesInvoiceId != null)
                        {
                            await _receiptRepo.UpdateInvoice(model.SalesInvoice.Id, model.Total, offsetAmount, cancellationToken);
                        }
                        else if (model.MultipleSIId != null)
                        {
                            await _receiptRepo.UpdateMultipleInvoice(model.MultipleSI, model.SIMultipleAmount, offsetAmount, cancellationToken);
                        }
                        else
                        {
                            await _receiptRepo.UpdateSv(model.ServiceInvoice.Id, model.Total, offsetAmount, cancellationToken);
                        }
                        await _dbContext.SaveChangesAsync(cancellationToken);
                        TempData["success"] = "Collection Receipt has been Posted.";
                    }
                    return RedirectToAction(collectionPrint, new { id = itemId });
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(collectionPrint, new { id = itemId });
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
                        if (model.SINo != null)
                        {
                            await _receiptRepo.RemoveSIPayment(model.SalesInvoice.Id, model.Total, findOffsetting.Sum(offset => offset.Amount), cancellationToken);
                        }
                        else if (model.SVNo != null)
                        {
                            await _receiptRepo.RemoveSVPayment(model.ServiceInvoiceId, model.Total, findOffsetting.Sum(offset => offset.Amount), cancellationToken);
                        }
                        else if (model.MultipleSI != null)
                        {
                            await _receiptRepo.RemoveMultipleSIPayment(model.MultipleSIId, model.SIMultipleAmount, findOffsetting.Sum(offset => offset.Amount), cancellationToken);
                        }
                        else
                        {
                            TempData["error"] = "No series number found";
                            return RedirectToAction(nameof(Index));
                        }

                        #region --Audit Trail Recording

                        if (model.OriginalSeriesNumber == null && model.OriginalDocumentId == 0)
                        {
                            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                            AuditTrail auditTrailBook = new(model.VoidedBy, $"Voided collection receipt# {model.CRNo}", "Collection Receipt", ipAddress);
                            await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                        }

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Collection Receipt has been Voided.";
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        TempData["error"] = ex.Message;
                    }
                }
                return RedirectToAction(nameof(CollectionIndex));
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

                    if (model.OriginalSeriesNumber == null && model.OriginalDocumentId == 0)
                    {
                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        AuditTrail auditTrailBook = new(model.CanceledBy, $"Cancelled collection receipt# {model.CRNo}", "Collection Receipt", ipAddress);
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                    }

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Collection Receipt has been Cancelled.";
                }
                return RedirectToAction(nameof(CollectionIndex));
            }

            return NotFound();
        }

        //Download as .xlsx file.(Export)
        #region -- export xlsx record --

        [HttpPost]
        public async Task<IActionResult> Export(string selectedRecord)
        {
            if (string.IsNullOrEmpty(selectedRecord))
            {
                // Handle the case where no invoices are selected
                return RedirectToAction(nameof(CollectionIndex));
            }

            var recordIds = selectedRecord.Split(',').Select(int.Parse).ToList();

            // Retrieve the selected invoices from the database
            var selectedList = await _dbContext.CollectionReceipts
                .Where(cr => recordIds.Contains(cr.Id))
                .OrderBy(cr => cr.CRNo)
                .ToListAsync();

            using (var package = new ExcelPackage())
            {
                // Add a new worksheet to the Excel package
                var worksheet = package.Workbook.Worksheets.Add("CollectionReceipt");

                var worksheet2 = package.Workbook.Worksheets.Add("Offsetting");

                worksheet.Cells["A1"].Value = "TransactionDate";
                worksheet.Cells["B1"].Value = "ReferenceNo";
                worksheet.Cells["C1"].Value = "Remarks";
                worksheet.Cells["D1"].Value = "CashAmount";
                worksheet.Cells["E1"].Value = "CheckDate";
                worksheet.Cells["F1"].Value = "CheckNo";
                worksheet.Cells["G1"].Value = "CheckBank";
                worksheet.Cells["H1"].Value = "CheckBranch";
                worksheet.Cells["I1"].Value = "CheckAmount";
                worksheet.Cells["J1"].Value = "ManagerCheckDate";
                worksheet.Cells["K1"].Value = "ManagerCheckNo";
                worksheet.Cells["L1"].Value = "ManagerCheckBank";
                worksheet.Cells["M1"].Value = "ManagerCheckBranch";
                worksheet.Cells["N1"].Value = "ManagerCheckAmount";
                worksheet.Cells["O1"].Value = "EWT";
                worksheet.Cells["P1"].Value = "WVAT";
                worksheet.Cells["Q1"].Value = "Total";
                worksheet.Cells["R1"].Value = "IsCertificateUpload";
                worksheet.Cells["S1"].Value = "f2306FilePath";
                worksheet.Cells["T1"].Value = "f2307FilePath";
                worksheet.Cells["U1"].Value = "CreatedBy";
                worksheet.Cells["V1"].Value = "CreatedDate";
                worksheet.Cells["W1"].Value = "CancellationRemarks";
                worksheet.Cells["X1"].Value = "MultipleSI";
                worksheet.Cells["Y1"].Value = "MultipleSIId";
                worksheet.Cells["Z1"].Value = "SIMultipleAmount";
                worksheet.Cells["AA1"].Value = "MultipleTransactionDate";
                worksheet.Cells["AB1"].Value = "OriginalCustomerId";
                worksheet.Cells["AC1"].Value = "OriginalSalesInvoiceId";
                worksheet.Cells["AD1"].Value = "OriginalSeriesNumber";
                worksheet.Cells["AE1"].Value = "OriginalServiceInvoiceId";
                worksheet.Cells["AF1"].Value = "OriginalDocumentId";

                worksheet2.Cells["A1"].Value = "AccountNo";
                worksheet2.Cells["B1"].Value = "Source";
                worksheet2.Cells["C1"].Value = "Reference";
                worksheet2.Cells["D1"].Value = "IsRemoved";
                worksheet2.Cells["E1"].Value = "Amount";
                worksheet2.Cells["F1"].Value = "CreatedBy";
                worksheet2.Cells["G1"].Value = "CreatedDate";
                worksheet2.Cells["H1"].Value = "AccountTitle";

                int row = 2;

                foreach (var item in selectedList)
                {
                    worksheet.Cells[row, 1].Value = item.TransactionDate.ToString("yyyy-MM-dd");
                    worksheet.Cells[row, 2].Value = item.ReferenceNo;
                    worksheet.Cells[row, 3].Value = item.Remarks;
                    worksheet.Cells[row, 4].Value = item.CashAmount;
                    worksheet.Cells[row, 5].Value = item.CheckDate;
                    worksheet.Cells[row, 6].Value = item.CheckNo;
                    worksheet.Cells[row, 7].Value = item.CheckBank;
                    worksheet.Cells[row, 8].Value = item.CheckBranch;
                    worksheet.Cells[row, 9].Value = item.CheckAmount;
                    worksheet.Cells[row, 10].Value = item.ManagerCheckDate?.ToString("yyyy-MM-dd");
                    worksheet.Cells[row, 11].Value = item.ManagerCheckNo;
                    worksheet.Cells[row, 12].Value = item.ManagerCheckBank;
                    worksheet.Cells[row, 13].Value = item.ManagerCheckBranch;
                    worksheet.Cells[row, 14].Value = item.ManagerCheckAmount;
                    worksheet.Cells[row, 15].Value = item.EWT;
                    worksheet.Cells[row, 16].Value = item.WVAT;
                    worksheet.Cells[row, 17].Value = item.Total;
                    worksheet.Cells[row, 18].Value = item.IsCertificateUpload;
                    worksheet.Cells[row, 19].Value = item.F2306FilePath;
                    worksheet.Cells[row, 20].Value = item.F2307FilePath;
                    worksheet.Cells[row, 21].Value = item.CreatedBy;
                    worksheet.Cells[row, 22].Value = item.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                    worksheet.Cells[row, 23].Value = item.CancellationRemarks;
                    if (item.MultipleSIId != null)
                    {
                        worksheet.Cells[row, 24].Value = string.Join(", ", item.MultipleSI.Select(si => si.ToString()));
                        worksheet.Cells[row, 25].Value = string.Join(", ", item.MultipleSIId.Select(siId => siId.ToString()));
                        worksheet.Cells[row, 26].Value = string.Join(" ", item.SIMultipleAmount.Select(multipleSI => multipleSI.ToString("N2")));
                        worksheet.Cells[row, 27].Value = string.Join(", ", item.MultipleTransactionDate.Select(multipleTransactionDate => multipleTransactionDate.ToString("yyyy-MM-dd")));
                    }
                    worksheet.Cells[row, 28].Value = item.CustomerId;
                    worksheet.Cells[row, 29].Value = item.SalesInvoiceId;
                    worksheet.Cells[row, 30].Value = item.CRNo;
                    worksheet.Cells[row, 31].Value = item.ServiceInvoiceId;
                    worksheet.Cells[row, 32].Value = item.Id;

                    row++;
                }

                var crNos = selectedList.Select(item => item.CRNo).ToList();

                var getOffsetting = await _dbContext.Offsettings
                    .Where(offset => crNos.Contains(offset.Source))
                    .OrderBy(offset => offset.Id)
                    .ToListAsync();

                int offsetRow = 2;

                foreach (var item in getOffsetting)
                {
                    worksheet2.Cells[offsetRow, 1].Value = item.AccountNo;
                    worksheet2.Cells[offsetRow, 2].Value = item.Source;
                    worksheet2.Cells[offsetRow, 3].Value = item.Reference;
                    worksheet2.Cells[offsetRow, 4].Value = item.IsRemoved;
                    worksheet2.Cells[offsetRow, 5].Value = item.Amount;
                    worksheet2.Cells[offsetRow, 6].Value = item.CreatedBy;
                    worksheet2.Cells[offsetRow, 7].Value = item.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                    worksheet2.Cells[offsetRow, 8].Value = item.AccountTitle;

                    offsetRow++;
                }

                // Convert the Excel package to a byte array
                var excelBytes = await package.GetAsByteArrayAsync();

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "CollectionReceiptList.xlsx");
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
                return RedirectToAction(nameof(CollectionIndex));
            }

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                stream.Position = 0;

                try
                {
                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "CollectionReceipt");

                        var worksheet2 = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "Offsetting");

                        if (worksheet == null)
                        {
                            TempData["error"] = "The Excel file contains no worksheets.";
                            return RedirectToAction(nameof(CollectionIndex), new { view = DynamicView.CollectionReceipt });
                        }
                        if (worksheet.ToString() != "CollectionReceipt")
                        {
                            TempData["error"] = "The Excel file is not related to collection receipt.";
                            return RedirectToAction(nameof(Index), new { view = DynamicView.CollectionReceipt });
                        }

                        var rowCount = worksheet.Dimension.Rows;

                        for (int row = 2; row <= rowCount; row++)  // Assuming the first row is the header
                        {
                            var collectionReceipt = new CollectionReceipt
                            {
                                CRNo = await _receiptRepo.GenerateCRNo(),
                                SeriesNumber = await _receiptRepo.GetLastSeriesNumberCR(),
                                TransactionDate = DateOnly.TryParse(worksheet.Cells[row, 1].Text, out DateOnly transactionDate) ? transactionDate : default,
                                ReferenceNo = worksheet.Cells[row, 2].Text,
                                Remarks = worksheet.Cells[row, 3].Text,
                                CashAmount = decimal.TryParse(worksheet.Cells[row, 4].Text, out decimal cashAmount) ? cashAmount : 0,
                                CheckDate = worksheet.Cells[row, 5].Text,
                                CheckNo = worksheet.Cells[row, 6].Text,
                                CheckBank = worksheet.Cells[row, 7].Text,
                                CheckBranch = worksheet.Cells[row, 8].Text,
                                CheckAmount = decimal.TryParse(worksheet.Cells[row, 9].Text, out decimal checkAmount) ? checkAmount : 0,
                                ManagerCheckDate = DateOnly.TryParse(worksheet.Cells[row, 10].Text, out DateOnly managerCheckDate) ? managerCheckDate : null,
                                ManagerCheckNo = worksheet.Cells[row, 11].Text,
                                ManagerCheckBank = worksheet.Cells[row, 12].Text,
                                ManagerCheckBranch = worksheet.Cells[row, 13].Text,
                                ManagerCheckAmount = decimal.TryParse(worksheet.Cells[row, 14].Text, out decimal managerCheckAmount) ? managerCheckAmount : 0,
                                EWT = decimal.TryParse(worksheet.Cells[row, 15].Text, out decimal ewt) ? ewt : 0,
                                WVAT = decimal.TryParse(worksheet.Cells[row, 16].Text, out decimal wvat) ? wvat : 0,
                                Total = decimal.TryParse(worksheet.Cells[row, 17].Text, out decimal total) ? total : 0,
                                IsCertificateUpload = bool.TryParse(worksheet.Cells[row, 18].Text, out bool isCertificateUpload) ? isCertificateUpload : false,
                                F2306FilePath = worksheet.Cells[row, 19].Text,
                                F2307FilePath = worksheet.Cells[row, 20].Text,
                                CreatedBy = worksheet.Cells[row, 21].Text,
                                CreatedDate = DateTime.TryParse(worksheet.Cells[row, 22].Text, out DateTime createdDate) ? createdDate : default,
                                CancellationRemarks = worksheet.Cells[row, 23].Text,
                                MultipleSI = worksheet.Cells[row, 24].Text.Split(',').Select(si => si.Trim()).ToArray(),
                                MultipleSIId = worksheet.Cells[row, 25].Text.Split(',').Select(multipleId => int.TryParse(multipleId.Trim(), out int multipleSIId) ? multipleSIId : 0).ToArray(),
                                SIMultipleAmount = worksheet.Cells[row, 26].Text.Split(' ').Select(multipleAmount => decimal.TryParse(multipleAmount.Trim(), out decimal siMultipleAmount) ? siMultipleAmount : 0).ToArray(),
                                MultipleTransactionDate = worksheet.Cells[row, 27].Text.Split(',').Select(date => DateOnly.TryParse(date.Trim(), out DateOnly parsedDate) ? parsedDate : default).ToArray(),
                                OriginalCustomerId = int.TryParse(worksheet.Cells[row, 28].Text, out int originalCustomerId) ? originalCustomerId : 0,
                                OriginalSalesInvoiceId = int.TryParse(worksheet.Cells[row, 29].Text, out int originalSalesInvoiceId) ? originalSalesInvoiceId : 0,
                                OriginalSeriesNumber = worksheet.Cells[row, 30].Text,
                                OriginalServiceInvoiceId = int.TryParse(worksheet.Cells[row, 31].Text, out int originalServiceInvoiceId) ? originalServiceInvoiceId : 0,
                                OriginalDocumentId = int.TryParse(worksheet.Cells[row, 32].Text, out int originalDocumentId) ? originalDocumentId : 0,
                            };

                            collectionReceipt.CustomerId = await _dbContext.Customers
                                .Where(c => c.OriginalCustomerId == collectionReceipt.OriginalCustomerId)
                                .Select(c => (int?)c.Id)
                                .FirstOrDefaultAsync();

                            var getSI = await _dbContext.SalesInvoices
                                        .Where(si => si.OriginalDocumentId == collectionReceipt.OriginalSalesInvoiceId)
                                        .Select(si => new { si.Id, si.SINo })
                                        .FirstOrDefaultAsync();

                            collectionReceipt.SalesInvoiceId = getSI?.Id;
                            collectionReceipt.SINo = getSI?.SINo;

                            var getSV = await _dbContext.ServiceInvoices
                                .Where(sv => sv.OriginalDocumentId == collectionReceipt.OriginalServiceInvoiceId)
                                .Select(sv => new { sv.Id, sv.SVNo })
                                .FirstOrDefaultAsync();

                            collectionReceipt.ServiceInvoiceId = getSV?.Id;
                            collectionReceipt.SVNo = getSV?.SVNo;

                            foreach (var item in collectionReceipt.MultipleSIId)
                            {
                                if (item == 0)
                                {
                                    collectionReceipt.MultipleSIId = null;
                                }
                            }
                            foreach (var item in collectionReceipt.SIMultipleAmount)
                            {
                                if (item == 0)
                                {
                                    collectionReceipt.SIMultipleAmount = null;
                                }
                            }

                            await _dbContext.CollectionReceipts.AddAsync(collectionReceipt);
                            await _dbContext.SaveChangesAsync();
                        }

                        var offsetRowCount = worksheet2.Dimension.Rows;
                        for (int offsetRow = 2; offsetRow <= offsetRowCount; offsetRow++)
                        {
                            var offsetting = new Offsetting
                            {
                                AccountNo = worksheet2.Cells[offsetRow, 1].Text,
                                Reference = worksheet2.Cells[offsetRow, 3].Text,
                                IsRemoved = bool.TryParse(worksheet2.Cells[offsetRow, 4].Text, out bool isRemoved) ? isRemoved : false,
                                Amount = decimal.TryParse(worksheet2.Cells[offsetRow, 5].Text, out decimal amount) ? amount : 0,
                                CreatedBy = worksheet2.Cells[offsetRow, 6].Text,
                                CreatedDate = DateTime.TryParse(worksheet2.Cells[offsetRow, 7].Text, out DateTime createdDate) ? createdDate : default,
                                AccountTitle = worksheet2.Cells[offsetRow, 8].Text,
                            };

                            offsetting.Source = await _dbContext.CollectionReceipts
                                .Where(cr => cr.OriginalSeriesNumber == worksheet2.Cells[offsetRow, 2].Text)
                                .Select(cr => cr.CRNo)
                                .FirstOrDefaultAsync();

                            await _dbContext.Offsettings.AddAsync(offsetting);
                            await _dbContext.SaveChangesAsync();
                        }

                    }
                }
                catch (OperationCanceledException oce)
                {
                    TempData["error"] = oce.Message;
                    return RedirectToAction(nameof(CollectionIndex), new { view = DynamicView.CollectionReceipt });
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(CollectionIndex), new { view = DynamicView.CollectionReceipt });
                }
            }
            TempData["success"] = "Uploading Success!";
            return RedirectToAction(nameof(CollectionIndex), new { view = DynamicView.CollectionReceipt });
        }

        #endregion -- import xlsx record --
    }
}