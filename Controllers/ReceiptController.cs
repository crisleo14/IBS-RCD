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
using Microsoft.IdentityModel.Tokens;

namespace Accounting_System.Controllers
{
    [Authorize]
    public class ReceiptController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly ReceiptRepo _receiptRepo;

        private readonly SalesInvoiceRepo _salesInvoiceRepo;

        private readonly ServiceInvoiceRepo _serviceInvoiceRepo;

        private readonly IWebHostEnvironment _webHostEnvironment;

        private readonly GeneralRepo _generalRepo;

        public ReceiptController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, ReceiptRepo receiptRepo, IWebHostEnvironment webHostEnvironment, GeneralRepo generalRepo, SalesInvoiceRepo salesInvoiceRepo, ServiceInvoiceRepo serviceInvoiceRepo)
        {
            _dbContext = dbContext;
            this._userManager = userManager;
            _receiptRepo = receiptRepo;
            _webHostEnvironment = webHostEnvironment;
            _generalRepo = generalRepo;
            _salesInvoiceRepo = salesInvoiceRepo;
            _serviceInvoiceRepo = serviceInvoiceRepo;
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
                            cr.CollectionReceiptNo.ToLower().Contains(searchValue) ||
                            cr.TransactionDate.ToString("MMM dd, yyyy").ToLower().Contains(searchValue) ||
                            cr.SINo?.ToLower().Contains(searchValue) == true ||
                            cr.SVNo?.ToLower().Contains(searchValue) == true ||
                            cr.MultipleSI?.Contains(searchValue) == true ||
                            cr.Customer.CustomerName.ToLower().Contains(searchValue) ||
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
                                     .Select(cr => cr.CollectionReceiptId) // Assuming Id is the primary key
                                     .ToListAsync(cancellationToken);
            return Json(collectionReceiptIds);
        }

        [HttpGet]
        public async Task<IActionResult> SingleCollectionCreateForSales(CancellationToken cancellationToken)
        {
            var viewModel = new CollectionReceipt();

            viewModel.Customers = await _dbContext.Customers
               .OrderBy(c => c.CustomerId)
               .Select(s => new SelectListItem
               {
                   Value = s.CustomerId.ToString(),
                   Text = s.CustomerName
               })
               .ToListAsync(cancellationToken);

            viewModel.ChartOfAccounts = await _dbContext.ChartOfAccounts
                .Where(coa => !coa.HasChildren)
                .OrderBy(coa => coa.AccountId)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> SingleCollectionCreateForSales(CollectionReceipt model, string[] accountTitleText, decimal[] accountAmount, string[] accountTitle, IFormFile? bir2306, IFormFile? bir2307, CancellationToken cancellationToken)
        {
            model.Customers = await _dbContext.Customers
               .OrderBy(c => c.CustomerId)
               .Select(s => new SelectListItem
               {
                   Value = s.Number.ToString(),
                   Text = s.CustomerName
               })
               .ToListAsync(cancellationToken);

            model.SalesInvoices = await _dbContext.SalesInvoices
                .Where(si => !si.IsPaid && si.CustomerId == model.CustomerId && si.IsPosted)
                .OrderBy(si => si.SalesInvoiceId)
                .Select(s => new SelectListItem
                {
                    Value = s.SalesInvoiceId.ToString(),
                    Text = s.SalesInvoiceNo
                })
                .ToListAsync(cancellationToken);

            model.ChartOfAccounts = await _dbContext.ChartOfAccounts
                .Where(coa => !coa.HasChildren)
                .OrderBy(coa => coa.AccountId)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    #region --Validating the series

                    var generateCrNo = await _receiptRepo.GenerateCRNo(cancellationToken);
                    var getLastNumber = long.Parse(generateCrNo.Substring(2));

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
                                                   .FirstOrDefaultAsync(si => si.SalesInvoiceId == model.SalesInvoiceId, cancellationToken);

                    model.SINo = existingSalesInvoice.SalesInvoiceNo;
                    model.CollectionReceiptNo = generateCrNo;
                    model.CreatedBy = _userManager.GetUserName(this.User);
                    model.Total = computeTotalInModelIfZero;

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

                    await _dbContext.AddAsync(model, cancellationToken);

                    decimal offsetAmount = 0;

                    #endregion --Saving default value

                    #region --Audit Trail Recording

                    if (model.OriginalSeriesNumber.IsNullOrEmpty() && model.OriginalDocumentId == 0)
                    {
                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        AuditTrail auditTrailBook = new(model.CreatedBy, $"Create new collection receipt# {model.CollectionReceiptNo}", "Collection Receipt", ipAddress);
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
                                Source = model.CollectionReceiptNo,
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
                    await transaction.CommitAsync(cancellationToken);
                    return RedirectToAction(nameof(CollectionIndex));
                }
                catch (Exception ex)
                {
                 await transaction.RollbackAsync(cancellationToken);
                 TempData["error"] = ex.Message;
                 return RedirectToAction(nameof(Index));
                }
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
               .OrderBy(c => c.CustomerId)
               .Select(s => new SelectListItem
               {
                   Value = s.CustomerId.ToString(),
                   Text = s.CustomerName
               })
               .ToListAsync(cancellationToken);

            viewModel.ChartOfAccounts = await _dbContext.ChartOfAccounts
                .Where(coa => !coa.HasChildren)
                .OrderBy(coa => coa.AccountId)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> MultipleCollectionCreateForSales(CollectionReceipt model, string[] accountTitleText, decimal[] accountAmount, string[] accountTitle, IFormFile? bir2306, IFormFile? bir2307, CancellationToken cancellationToken)
        {
            model.Customers = await _dbContext.Customers
               .OrderBy(c => c.CustomerId)
               .Select(s => new SelectListItem
               {
                   Value = s.Number.ToString(),
                   Text = s.CustomerName
               })
               .ToListAsync(cancellationToken);

            model.SalesInvoices = await _dbContext.SalesInvoices
                .Where(si => !si.IsPaid && si.CustomerId == model.CustomerId && si.IsPosted)
                .OrderBy(si => si.SalesInvoiceId)
                .Select(s => new SelectListItem
                {
                    Value = s.SalesInvoiceId.ToString(),
                    Text = s.SalesInvoiceNo
                })
                .ToListAsync(cancellationToken);

            model.ChartOfAccounts = await _dbContext.ChartOfAccounts
                .Where(coa => !coa.HasChildren)
                .OrderBy(coa => coa.AccountId)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    #region --Validating the series

                    var generateCrNo = await _receiptRepo.GenerateCRNo(cancellationToken);
                    var getLastNumber = long.Parse(generateCrNo.Substring(2));

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
                                                   .Where(si => model.MultipleSIId.Contains(si.SalesInvoiceId))
                                                   .ToListAsync(cancellationToken);

                    model.MultipleSI = new string[model.MultipleSIId.Length];
                    model.MultipleTransactionDate = new DateOnly[model.MultipleSIId.Length];
                    var salesInvoice = new SalesInvoice();
                    for (int i = 0; i < model.MultipleSIId.Length; i++)
                    {
                        var siId = model.MultipleSIId[i];
                        salesInvoice = await _dbContext.SalesInvoices
                                    .FirstOrDefaultAsync(si => si.SalesInvoiceId == siId, cancellationToken);

                        if (salesInvoice != null)
                        {
                            model.MultipleSI[i] = salesInvoice.SalesInvoiceNo;
                            model.MultipleTransactionDate[i] = salesInvoice.TransactionDate;
                        }
                    }

                    model.CollectionReceiptNo = generateCrNo;
                    model.CreatedBy = _userManager.GetUserName(this.User);
                    model.Total = computeTotalInModelIfZero;

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

                    await _dbContext.AddAsync(model, cancellationToken);

                    decimal offsetAmount = 0;

                    #endregion --Saving default value

                    #region --Audit Trail Recording

                    if (model.OriginalSeriesNumber.IsNullOrEmpty() && model.OriginalDocumentId == 0)
                    {
                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        AuditTrail auditTrailBook = new(model.CreatedBy, $"Create new collection receipt# {model.CollectionReceiptNo}", "Collection Receipt", ipAddress);
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
                                Source = model.CollectionReceiptNo,
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
                    await transaction.CommitAsync(cancellationToken);
                    return RedirectToAction(nameof(CollectionIndex));
                }
                catch (Exception ex)
                {
                 await transaction.RollbackAsync(cancellationToken);
                 TempData["error"] = ex.Message;
                 return RedirectToAction(nameof(Index));
                }
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
               .OrderBy(c => c.CustomerId)
               .Select(s => new SelectListItem
               {
                   Value = s.CustomerId.ToString(),
                   Text = s.CustomerName
               })
               .ToListAsync(cancellationToken);

            viewModel.ChartOfAccounts = await _dbContext.ChartOfAccounts
                .Where(coa => !coa.HasChildren)
                .OrderBy(coa => coa.AccountId)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> CollectionCreateForService(CollectionReceipt model, string[] accountTitleText, decimal[] accountAmount, string[] accountTitle, IFormFile? bir2306, IFormFile? bir2307, CancellationToken cancellationToken)
        {
            model.Customers = await _dbContext.Customers
               .OrderBy(c => c.CustomerId)
               .Select(s => new SelectListItem
               {
                   Value = s.CustomerId.ToString(),
                   Text = s.CustomerName
               })
               .ToListAsync(cancellationToken);

            model.SalesInvoices = await _dbContext.ServiceInvoices
                .Where(si => !si.IsPaid && si.CustomerId == model.CustomerId && si.IsPosted)
                .OrderBy(si => si.ServiceInvoiceId)
                .Select(s => new SelectListItem
                {
                    Value = s.ServiceInvoiceId.ToString(),
                    Text = s.ServiceInvoiceNo
                })
                .ToListAsync(cancellationToken);

            model.ChartOfAccounts = await _dbContext.ChartOfAccounts
                .Where(coa => !coa.HasChildren)
                .OrderBy(coa => coa.AccountId)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    #region --Validating the series

                    var generateCrNo = await _receiptRepo.GenerateCRNo(cancellationToken);
                    var getLastNumber = long.Parse(generateCrNo.Substring(2));

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
                                                   .FirstOrDefaultAsync(si => si.ServiceInvoiceId == model.ServiceInvoiceId, cancellationToken);

                    model.SVNo = existingServiceInvoice.ServiceInvoiceNo;
                    model.CollectionReceiptNo = generateCrNo;
                    model.CreatedBy = _userManager.GetUserName(this.User);
                    model.Total = computeTotalInModelIfZero;

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

                    await _dbContext.AddAsync(model, cancellationToken);

                    decimal offsetAmount = 0;

                    #endregion --Saving default value

                    #region --Audit Trail Recording

                    if (model.OriginalSeriesNumber.IsNullOrEmpty() && model.OriginalDocumentId == 0)
                    {
                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        AuditTrail auditTrailBook = new(model.CreatedBy, $"Create new collection receipt# {model.CollectionReceiptNo}", "Collection Receipt", ipAddress);
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
                                Source = model.CollectionReceiptNo,
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
                    await transaction.CommitAsync(cancellationToken);
                    return RedirectToAction(nameof(CollectionIndex));
                }
                catch (Exception ex)
                {
                 await transaction.RollbackAsync(cancellationToken);
                 TempData["error"] = ex.Message;
                 return RedirectToAction(nameof(Index));
                }
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

                if (findIdOfCR.OriginalSeriesNumber.IsNullOrEmpty() && findIdOfCR.OriginalDocumentId == 0)
                {
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    var printedBy = _userManager.GetUserName(this.User);
                    AuditTrail auditTrailBook = new(printedBy, $"Printed original copy of cr# {findIdOfCR.CollectionReceiptNo}", "Collection Receipt", ipAddress);
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

                if (findIdOfCR.OriginalSeriesNumber.IsNullOrEmpty() && findIdOfCR.OriginalDocumentId == 0)
                {
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    var printedBy = _userManager.GetUserName(this.User);
                    AuditTrail auditTrailBook = new(printedBy, $"Printed original copy of cr# {findIdOfCR.CollectionReceiptNo}", "Collection Receipt", ipAddress);
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
                .OrderBy(si => si.SalesInvoiceId)
                .ToListAsync(cancellationToken);

            var invoiceList = invoices.Select(si => new SelectListItem
            {
                Value = si.SalesInvoiceId.ToString(),   // Replace with your actual ID property
                Text = si.SalesInvoiceNo              // Replace with your actual property for display text
            }).ToList();

            return Json(invoiceList);
        }

        [HttpGet]
        public async Task<IActionResult> GetServiceInvoices(int customerNo, CancellationToken cancellationToken)
        {
            var invoices = await _dbContext
                .ServiceInvoices
                .Where(si => si.CustomerId == customerNo && !si.IsPaid && si.IsPosted)
                .OrderBy(si => si.ServiceInvoiceId)
                .ToListAsync(cancellationToken);

            var invoiceList = invoices.Select(si => new SelectListItem
            {
                Value = si.ServiceInvoiceId.ToString(),   // Replace with your actual ID property
                Text = si.ServiceInvoiceNo              // Replace with your actual property for display text
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
                .FirstOrDefaultAsync(si => si.SalesInvoiceId == invoiceNo, cancellationToken);

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
                .FirstOrDefaultAsync(si => si.ServiceInvoiceId == invoiceNo, cancellationToken);

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
                .FirstOrDefaultAsync(si => si.SalesInvoiceId == siNo, cancellationToken);
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
                .FirstOrDefaultAsync(si => siNo.Contains(si.SalesInvoiceId), cancellationToken);

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
               .OrderBy(c => c.CustomerId)
               .Select(s => new SelectListItem
               {
                   Value = s.CustomerId.ToString(),
                   Text = s.CustomerName
               })
               .ToListAsync(cancellationToken);

            existingModel.SalesInvoices = await _dbContext.SalesInvoices
                .Where(si => !si.IsPaid && si.CustomerId == existingModel.CustomerId && si.IsPosted)
                .OrderBy(si => si.SalesInvoiceId)
                .Select(s => new SelectListItem
                {
                    Value = s.SalesInvoiceId.ToString(),
                    Text = s.SalesInvoiceNo
                })
                .ToListAsync(cancellationToken);

            existingModel.ServiceInvoices = await _dbContext.ServiceInvoices
                .Where(si => !si.IsPaid && si.CustomerId == existingModel.CustomerId && si.IsPosted)
                .OrderBy(si => si.ServiceInvoiceId)
                .Select(s => new SelectListItem
                {
                    Value = s.ServiceInvoiceId.ToString(),
                    Text = s.ServiceInvoiceNo
                })
                .ToListAsync(cancellationToken);

            existingModel.ChartOfAccounts = await _dbContext.ChartOfAccounts
                .Where(coa => !coa.HasChildren)
                .OrderBy(coa => coa.AccountId)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            var findCustomers = await _dbContext.Customers
                .FirstOrDefaultAsync(c => c.CustomerId == existingModel.CustomerId, cancellationToken);

            var offsettings = await _dbContext.Offsettings
                .Where(offset => offset.Source == existingModel.CollectionReceiptNo)
                .ToListAsync(cancellationToken);

            ViewBag.CustomerName = findCustomers?.CustomerName;
            ViewBag.Offsettings = offsettings;

            return View(existingModel);
        }

        [HttpPost]
        public async Task<IActionResult> CollectionEdit(CollectionReceipt model, string[] accountTitleText, decimal[] accountAmount, string[] accountTitle, IFormFile? bir2306, IFormFile? bir2307, CancellationToken cancellationToken)
        {
            var existingModel = await _receiptRepo.FindCR(model.CollectionReceiptId, cancellationToken);

            var offsettings = await _dbContext.Offsettings
                .Where(offset => offset.Source == existingModel.CollectionReceiptNo)
                .ToListAsync(cancellationToken);

            ViewBag.Offsettings = offsettings;
            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    #region --Saving default value

                    var computeTotalInModelIfZero = model.CashAmount + model.CheckAmount + model.ManagerCheckAmount + model.EWT + model.WVAT;
                    if (computeTotalInModelIfZero == 0)
                    {
                        TempData["error"] = "Please input atleast one type form of payment";
                        existingModel.Customers = await _dbContext.Customers
                            .OrderBy(c => c.CustomerId)
                            .Select(s => new SelectListItem
                            {
                                Value = s.CustomerId.ToString(),
                                Text = s.CustomerName
                            })
                            .ToListAsync(cancellationToken);

                        existingModel.SalesInvoices = await _dbContext.SalesInvoices
                            .Where(si => !si.IsPaid && si.CustomerId == existingModel.CustomerId && si.IsPosted)
                            .OrderBy(si => si.SalesInvoiceId)
                            .Select(s => new SelectListItem
                            {
                                Value = s.SalesInvoiceId.ToString(),
                                Text = s.SalesInvoiceNo
                            })
                            .ToListAsync(cancellationToken);

                        existingModel.ServiceInvoices = await _dbContext.ServiceInvoices
                            .Where(si => !si.IsPaid && si.CustomerId == existingModel.CustomerId && si.IsPosted)
                            .OrderBy(si => si.ServiceInvoiceId)
                            .Select(s => new SelectListItem
                            {
                                Value = s.ServiceInvoiceId.ToString(),
                                Text = s.ServiceInvoiceNo
                            })
                            .ToListAsync(cancellationToken);

                        existingModel.ChartOfAccounts = await _dbContext.ChartOfAccounts
                            .Where(coa => !coa.HasChildren)
                            .OrderBy(coa => coa.AccountId)
                            .Select(s => new SelectListItem
                            {
                                Value = s.AccountNumber,
                                Text = s.AccountNumber + " " + s.AccountName
                            })
                            .ToListAsync(cancellationToken);
                        return View(existingModel);
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

                    decimal offsetAmount = 0;

                    #endregion --Saving default value

                    #region --Offsetting function

                    var findOffsettings = await _dbContext.Offsettings
                    .Where(offset => offset.Source == existingModel.CollectionReceiptNo)
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
                                Source = existingModel.CollectionReceiptNo,
                                Reference = existingModel.SINo != null ? existingModel.SINo : existingModel.SVNo,
                                Amount = currentAccountAmount,
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

                    if (_dbContext.ChangeTracker.HasChanges())
                    {
                        #region --Audit Trail Recording

                        if (existingModel.OriginalSeriesNumber.IsNullOrEmpty() && existingModel.OriginalDocumentId == 0)
                        {
                            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                            var modifiedBy = _userManager.GetUserName(this.User);
                            AuditTrail auditTrailBook = new(modifiedBy, $"Edited collection receipt# {existingModel.CollectionReceiptNo}", "Collection Receipt", ipAddress);
                            await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                        }

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Collection Receipt edited successfully";
                        return RedirectToAction(nameof(CollectionIndex));
                    }
                    else
                    {
                        throw new InvalidOperationException("No data changes!");
                    }
                }
                catch (Exception ex)
                {
                 await transaction.RollbackAsync(cancellationToken);
                 existingModel.Customers = await _dbContext.Customers
                     .OrderBy(c => c.CustomerId)
                     .Select(s => new SelectListItem
                     {
                         Value = s.CustomerId.ToString(),
                         Text = s.CustomerName
                     })
                     .ToListAsync(cancellationToken);

                 existingModel.SalesInvoices = await _dbContext.SalesInvoices
                     .Where(si => !si.IsPaid && si.CustomerId == existingModel.CustomerId && si.IsPosted)
                     .OrderBy(si => si.SalesInvoiceId)
                     .Select(s => new SelectListItem
                     {
                         Value = s.SalesInvoiceId.ToString(),
                         Text = s.SalesInvoiceNo
                     })
                     .ToListAsync(cancellationToken);

                 existingModel.ServiceInvoices = await _dbContext.ServiceInvoices
                     .Where(si => !si.IsPaid && si.CustomerId == existingModel.CustomerId && si.IsPosted)
                     .OrderBy(si => si.ServiceInvoiceId)
                     .Select(s => new SelectListItem
                     {
                         Value = s.ServiceInvoiceId.ToString(),
                         Text = s.ServiceInvoiceNo
                     })
                     .ToListAsync(cancellationToken);

                 existingModel.ChartOfAccounts = await _dbContext.ChartOfAccounts
                     .Where(coa => !coa.HasChildren)
                     .OrderBy(coa => coa.AccountId)
                     .Select(s => new SelectListItem
                     {
                         Value = s.AccountNumber,
                         Text = s.AccountNumber + " " + s.AccountName
                     })
                     .ToListAsync(cancellationToken);
                 TempData["error"] = ex.Message;
                 return View(existingModel);
                }
            }
            else
            {
                TempData["error"] = "The information you submitted is not valid!";
                existingModel.Customers = await _dbContext.Customers
                    .OrderBy(c => c.CustomerId)
                    .Select(s => new SelectListItem
                    {
                        Value = s.CustomerId.ToString(),
                        Text = s.CustomerName
                    })
                    .ToListAsync(cancellationToken);

                existingModel.SalesInvoices = await _dbContext.SalesInvoices
                    .Where(si => !si.IsPaid && si.CustomerId == existingModel.CustomerId && si.IsPosted)
                    .OrderBy(si => si.SalesInvoiceId)
                    .Select(s => new SelectListItem
                    {
                        Value = s.SalesInvoiceId.ToString(),
                        Text = s.SalesInvoiceNo
                    })
                    .ToListAsync(cancellationToken);

                existingModel.ServiceInvoices = await _dbContext.ServiceInvoices
                    .Where(si => !si.IsPaid && si.CustomerId == existingModel.CustomerId && si.IsPosted)
                    .OrderBy(si => si.ServiceInvoiceId)
                    .Select(s => new SelectListItem
                    {
                        Value = s.ServiceInvoiceId.ToString(),
                        Text = s.ServiceInvoiceNo
                    })
                    .ToListAsync(cancellationToken);

                existingModel.ChartOfAccounts = await _dbContext.ChartOfAccounts
                    .Where(coa => !coa.HasChildren)
                    .OrderBy(coa => coa.AccountId)
                    .Select(s => new SelectListItem
                    {
                        Value = s.AccountNumber,
                        Text = s.AccountNumber + " " + s.AccountName
                    })
                    .ToListAsync(cancellationToken);
                return View(existingModel);
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
               .OrderBy(c => c.CustomerId)
               .Select(s => new SelectListItem
               {
                   Value = s.CustomerId.ToString(),
                   Text = s.CustomerName
               })
               .ToListAsync(cancellationToken);

            existingModel.SalesInvoices = await _dbContext.SalesInvoices
                .Where(si => !si.IsPaid && si.CustomerId == existingModel.CustomerId)
                .OrderBy(si => si.SalesInvoiceId)
                .Select(s => new SelectListItem
                {
                    Value = s.SalesInvoiceId.ToString(),
                    Text = s.SalesInvoiceNo
                })
                .ToListAsync(cancellationToken);

            existingModel.ChartOfAccounts = await _dbContext.ChartOfAccounts
                .Where(coa => !coa.HasChildren)
                .OrderBy(coa => coa.AccountId)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            var findCustomers = await _dbContext.Customers
                .FirstOrDefaultAsync(c => c.CustomerId == existingModel.CustomerId, cancellationToken);

            var offsettings = await _dbContext.Offsettings
                .Where(offset => offset.Source == existingModel.CollectionReceiptNo)
                .ToListAsync(cancellationToken);

            ViewBag.CustomerName = findCustomers?.CustomerName;
            ViewBag.Offsettings = offsettings;

            return View(existingModel);
        }

        [HttpPost]
        public async Task<IActionResult> MultipleCollectionEdit(CollectionReceipt model, string[] accountTitleText, decimal[] accountAmount, string[] accountTitle, IFormFile? bir2306, IFormFile? bir2307, CancellationToken cancellationToken)
        {
            var existingModel = await _receiptRepo.FindCR(model.CollectionReceiptId, cancellationToken);

            var offsettings = await _dbContext.Offsettings
                .Where(offset => offset.Source == existingModel.CollectionReceiptNo)
                .ToListAsync(cancellationToken);

            ViewBag.Offsettings = offsettings;
            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    #region --Saving default value

                    var computeTotalInModelIfZero = model.CashAmount + model.CheckAmount + model.ManagerCheckAmount + model.EWT + model.WVAT;
                    if (computeTotalInModelIfZero == 0)
                    {
                        TempData["error"] = "Please input atleast one type form of payment";
                        existingModel.Customers = await _dbContext.Customers
                            .OrderBy(c => c.CustomerId)
                            .Select(s => new SelectListItem
                            {
                                Value = s.CustomerId.ToString(),
                                Text = s.CustomerName
                            })
                            .ToListAsync(cancellationToken);

                        existingModel.SalesInvoices = await _dbContext.SalesInvoices
                            .Where(si => !si.IsPaid && si.CustomerId == existingModel.CustomerId)
                            .OrderBy(si => si.SalesInvoiceId)
                            .Select(s => new SelectListItem
                            {
                                Value = s.SalesInvoiceId.ToString(),
                                Text = s.SalesInvoiceNo
                            })
                            .ToListAsync(cancellationToken);

                        existingModel.ChartOfAccounts = await _dbContext.ChartOfAccounts
                            .Where(coa => !coa.HasChildren)
                            .OrderBy(coa => coa.AccountId)
                            .Select(s => new SelectListItem
                            {
                                Value = s.AccountNumber,
                                Text = s.AccountNumber + " " + s.AccountName
                            })
                            .ToListAsync(cancellationToken);
                        return View(existingModel);
                    }
                    var existingSalesInvoice = await _dbContext.SalesInvoices
                                                   .Where(si => model.MultipleSIId.Contains(si.SalesInvoiceId))
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
                                    .FirstOrDefaultAsync(si => si.SalesInvoiceId == siId, cancellationToken);

                        if (salesInvoice != null)
                        {
                            existingModel.MultipleSIId[i] = model.MultipleSIId[i];
                            existingModel.MultipleSI[i] = salesInvoice.SalesInvoiceNo;
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

                    decimal offsetAmount = 0;

                    #endregion --Saving default value

                    #region --Offsetting function

                    var findOffsettings = await _dbContext.Offsettings
                    .Where(offset => offset.Source == existingModel.CollectionReceiptNo)
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
                                Source = existingModel.CollectionReceiptNo,
                                Reference = existingModel.SINo != null ? existingModel.SINo : existingModel.SVNo,
                                Amount = currentAccountAmount,
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

                    if (_dbContext.ChangeTracker.HasChanges())
                    {
                        #region --Audit Trail Recording

                        if (existingModel.OriginalSeriesNumber.IsNullOrEmpty() && existingModel.OriginalDocumentId == 0)
                        {
                            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                            var modifiedBy = _userManager.GetUserName(this.User);
                            AuditTrail auditTrailBook = new(modifiedBy, $"Edited collection receipt# {existingModel.CollectionReceiptNo}", "Collection Receipt", ipAddress);
                            await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                        }

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Collection Receipt edited successfully";
                        return RedirectToAction(nameof(CollectionIndex));
                    }
                    else
                    {
                        throw new InvalidOperationException("No data changes!");
                    }
                }
                catch (Exception ex)
                {
                 await transaction.RollbackAsync(cancellationToken);
                 existingModel.Customers = await _dbContext.Customers
                     .OrderBy(c => c.CustomerId)
                     .Select(s => new SelectListItem
                     {
                         Value = s.CustomerId.ToString(),
                         Text = s.CustomerName
                     })
                     .ToListAsync(cancellationToken);

                 existingModel.SalesInvoices = await _dbContext.SalesInvoices
                     .Where(si => !si.IsPaid && si.CustomerId == existingModel.CustomerId)
                     .OrderBy(si => si.SalesInvoiceId)
                     .Select(s => new SelectListItem
                     {
                         Value = s.SalesInvoiceId.ToString(),
                         Text = s.SalesInvoiceNo
                     })
                     .ToListAsync(cancellationToken);

                 existingModel.ChartOfAccounts = await _dbContext.ChartOfAccounts
                     .Where(coa => !coa.HasChildren)
                     .OrderBy(coa => coa.AccountId)
                     .Select(s => new SelectListItem
                     {
                         Value = s.AccountNumber,
                         Text = s.AccountNumber + " " + s.AccountName
                     })
                     .ToListAsync(cancellationToken);
                 TempData["error"] = ex.Message;
                 return View(existingModel);
                }
            }
            else
            {
                TempData["error"] = "The information you submitted is not valid!";
                existingModel.Customers = await _dbContext.Customers
                    .OrderBy(c => c.CustomerId)
                    .Select(s => new SelectListItem
                    {
                        Value = s.CustomerId.ToString(),
                        Text = s.CustomerName
                    })
                    .ToListAsync(cancellationToken);

                existingModel.SalesInvoices = await _dbContext.SalesInvoices
                    .Where(si => !si.IsPaid && si.CustomerId == existingModel.CustomerId)
                    .OrderBy(si => si.SalesInvoiceId)
                    .Select(s => new SelectListItem
                    {
                        Value = s.SalesInvoiceId.ToString(),
                        Text = s.SalesInvoiceNo
                    })
                    .ToListAsync(cancellationToken);

                existingModel.ChartOfAccounts = await _dbContext.ChartOfAccounts
                    .Where(coa => !coa.HasChildren)
                    .OrderBy(coa => coa.AccountId)
                    .Select(s => new SelectListItem
                    {
                        Value = s.AccountNumber,
                        Text = s.AccountNumber + " " + s.AccountName
                    })
                    .ToListAsync(cancellationToken);
                return View(existingModel);
            }
        }

        public async Task<IActionResult> Post(int id, CancellationToken cancellationToken)
        {
            var model = await _receiptRepo.FindCR(id, cancellationToken);

            if (model != null)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                var collectionPrint = model.MultipleSIId != null ? nameof(MultipleCollectionPrint) : nameof(CollectionPrint);
                try
                {
                    if (!model.IsPosted)
                    {
                        model.IsPosted = true;
                        model.PostedBy = _userManager.GetUserName(this.User);
                        model.PostedDate = DateTime.Now;

                        List<Offsetting>? offset = new List<Offsetting>();
                        decimal offsetAmount = 0;

                        if (model.SalesInvoiceId != null)
                        {
                            offset = await _receiptRepo.GetOffsettingAsync(model.CollectionReceiptNo, model.SINo, cancellationToken);
                            offsetAmount = offset.Sum(o => o.Amount);
                        }
                        else
                        {
                            offset = await _receiptRepo.GetOffsettingAsync(model.CollectionReceiptNo, model.SVNo, cancellationToken);
                            offsetAmount = offset.Sum(o => o.Amount);
                        }

                        await _receiptRepo.PostAsync(model, offset, cancellationToken);

                        if (model.SalesInvoiceId != null)
                        {
                            await _receiptRepo.UpdateInvoice(model.SalesInvoice.SalesInvoiceId, model.Total, offsetAmount, cancellationToken);
                        }
                        else if (model.MultipleSIId != null)
                        {
                            await _receiptRepo.UpdateMultipleInvoice(model.MultipleSI, model.SIMultipleAmount, offsetAmount, cancellationToken);
                        }
                        else
                        {
                            await _receiptRepo.UpdateSv(model.ServiceInvoice.ServiceInvoiceId, model.Total, offsetAmount, cancellationToken);
                        }

                        #region --Audit Trail Recording

                        if (model.OriginalSeriesNumber.IsNullOrEmpty() && model.OriginalDocumentId == 0)
                        {
                            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                            AuditTrail auditTrailBook = new(model.PostedBy, $"Posted collection receipt# {model.CollectionReceiptNo}", "Collection Receipt", ipAddress);
                            await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                        }

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Collection Receipt has been Posted.";
                    }
                    return RedirectToAction(collectionPrint, new { id = id });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return RedirectToAction(collectionPrint, new { id = id });
                }
            }
            return NotFound();
        }

        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {
            var model = await _receiptRepo.FindCR(id, cancellationToken);

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

                        var findOffsetting = await _dbContext.Offsettings.Where(offset => offset.Source == model.CollectionReceiptNo && offset.Reference == series).ToListAsync(cancellationToken);

                        await _generalRepo.RemoveRecords<CashReceiptBook>(crb => crb.RefNo == model.CollectionReceiptNo, cancellationToken);
                        await _generalRepo.RemoveRecords<GeneralLedgerBook>(gl => gl.Reference == model.CollectionReceiptNo, cancellationToken);

                        if (findOffsetting.Any())
                        {
                            await _generalRepo.RemoveRecords<Offsetting>(offset => offset.Source == model.CollectionReceiptNo && offset.Reference == series, cancellationToken);
                        }
                        if (model.SINo != null)
                        {
                            await _receiptRepo.RemoveSIPayment(model.SalesInvoice.SalesInvoiceId, model.Total, findOffsetting.Sum(offset => offset.Amount), cancellationToken);
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

                        if (model.OriginalSeriesNumber.IsNullOrEmpty() && model.OriginalDocumentId == 0)
                        {
                            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                            AuditTrail auditTrailBook = new(model.VoidedBy, $"Voided collection receipt# {model.CollectionReceiptNo}", "Collection Receipt", ipAddress);
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

        public async Task<IActionResult> Cancel(int id, string cancellationRemarks, CancellationToken cancellationToken)
        {
            var model = await _dbContext.CollectionReceipts.FindAsync(id, cancellationToken);
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                if (model != null)
                {
                    if (!model.IsCanceled)
                    {
                        model.IsCanceled = true;
                        model.CanceledBy = _userManager.GetUserName(this.User);
                        model.CanceledDate = DateTime.Now;
                        model.CancellationRemarks = cancellationRemarks;

                        #region --Audit Trail Recording

                        if (model.OriginalSeriesNumber.IsNullOrEmpty() && model.OriginalDocumentId == 0)
                        {
                            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                            AuditTrail auditTrailBook = new(model.CanceledBy, $"Cancelled collection receipt# {model.CollectionReceiptNo}", "Collection Receipt", ipAddress);
                            await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                        }

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Collection Receipt has been Cancelled.";
                    }
                    return RedirectToAction(nameof(CollectionIndex));
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(CollectionIndex));
            }

            return NotFound();
        }

        //Download as .xlsx file.(Export)
        #region -- export xlsx record --

        [HttpPost]
        public async Task<IActionResult> Export(string selectedRecord, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(selectedRecord))
            {
                // Handle the case where no invoices are selected
                return RedirectToAction(nameof(CollectionIndex));
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
		    {
                var recordIds = selectedRecord.Split(',').Select(int.Parse).ToList();

                // Retrieve the selected invoices from the database
                var selectedList = await _dbContext.CollectionReceipts
                    .Where(cr => recordIds.Contains(cr.CollectionReceiptId))
                    .Include(cr => cr.SalesInvoice)
                    .Include(cr => cr.ServiceInvoice)
                    .OrderBy(cr => cr.CollectionReceiptNo)
                    .ToListAsync();

                using (var package = new ExcelPackage())
                {
                    // Add a new worksheet to the Excel package
                    #region -- Sales Invoice Table Header --

                        var worksheet3 = package.Workbook.Worksheets.Add("SalesInvoice");

                        worksheet3.Cells["A1"].Value = "OtherRefNo";
                        worksheet3.Cells["B1"].Value = "Quantity";
                        worksheet3.Cells["C1"].Value = "UnitPrice";
                        worksheet3.Cells["D1"].Value = "Amount";
                        worksheet3.Cells["E1"].Value = "Remarks";
                        worksheet3.Cells["F1"].Value = "Status";
                        worksheet3.Cells["G1"].Value = "TransactionDate";
                        worksheet3.Cells["H1"].Value = "Discount";
                        worksheet3.Cells["I1"].Value = "AmountPaid";
                        worksheet3.Cells["J1"].Value = "Balance";
                        worksheet3.Cells["K1"].Value = "IsPaid";
                        worksheet3.Cells["L1"].Value = "IsTaxAndVatPaid";
                        worksheet3.Cells["M1"].Value = "DueDate";
                        worksheet3.Cells["N1"].Value = "CreatedBy";
                        worksheet3.Cells["O1"].Value = "CreatedDate";
                        worksheet3.Cells["P1"].Value = "CancellationRemarks";
                        worksheet3.Cells["Q1"].Value = "OriginalReceivingReportId";
                        worksheet3.Cells["R1"].Value = "OriginalCustomerId";
                        worksheet3.Cells["S1"].Value = "OriginalPOId";
                        worksheet3.Cells["T1"].Value = "OriginalProductId";
                        worksheet3.Cells["U1"].Value = "OriginalSINo";
                        worksheet3.Cells["V1"].Value = "OriginalDocumentId";

                    #endregion -- Sales Invoice Table Header --

                    #region -- Service Invoice Table Header --

                        var worksheet4 = package.Workbook.Worksheets.Add("ServiceInvoice");

                        worksheet4.Cells["A1"].Value = "DueDate";
                        worksheet4.Cells["B1"].Value = "Period";
                        worksheet4.Cells["C1"].Value = "Amount";
                        worksheet4.Cells["D1"].Value = "Total";
                        worksheet4.Cells["E1"].Value = "Discount";
                        worksheet4.Cells["F1"].Value = "CurrentAndPreviousMonth";
                        worksheet4.Cells["G1"].Value = "UnearnedAmount";
                        worksheet4.Cells["H1"].Value = "Status";
                        worksheet4.Cells["I1"].Value = "AmountPaid";
                        worksheet4.Cells["J1"].Value = "Balance";
                        worksheet4.Cells["K1"].Value = "Instructions";
                        worksheet4.Cells["L1"].Value = "IsPaid";
                        worksheet4.Cells["M1"].Value = "CreatedBy";
                        worksheet4.Cells["N1"].Value = "CreatedDate";
                        worksheet4.Cells["O1"].Value = "CancellationRemarks";
                        worksheet4.Cells["P1"].Value = "OriginalCustomerId";
                        worksheet4.Cells["Q1"].Value = "OriginalSVNo";
                        worksheet4.Cells["R1"].Value = "OriginalServicesId";
                        worksheet4.Cells["S1"].Value = "OriginalDocumentId";

                    #endregion -- Service Invoice Table Header --

                    #region -- Collection Receipt Table Header --

                    var worksheet = package.Workbook.Worksheets.Add("CollectionReceipt");

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
                    worksheet.Cells["AD1"].Value = "OriginalCRNo";
                    worksheet.Cells["AE1"].Value = "OriginalServiceInvoiceId";
                    worksheet.Cells["AF1"].Value = "OriginalDocumentId";

                    #endregion -- Collection Receipt Table Header --

                    #region -- Offsetting Table Header --

                    var worksheet2 = package.Workbook.Worksheets.Add("Offsetting");

                    worksheet2.Cells["A1"].Value = "AccountNo";
                    worksheet2.Cells["B1"].Value = "Source";
                    worksheet2.Cells["C1"].Value = "Reference";
                    worksheet2.Cells["D1"].Value = "IsRemoved";
                    worksheet2.Cells["E1"].Value = "Amount";
                    worksheet2.Cells["F1"].Value = "CreatedBy";
                    worksheet2.Cells["G1"].Value = "CreatedDate";
                    worksheet2.Cells["H1"].Value = "AccountTitle";

                    #endregion -- Offsetting Table Header --

                    #region -- Collection Receipt Export --
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
                        worksheet.Cells[row, 30].Value = item.CollectionReceiptNo;
                        worksheet.Cells[row, 31].Value = item.ServiceInvoiceId;
                        worksheet.Cells[row, 32].Value = item.CollectionReceiptId;

                        row++;
                    }

                    #endregion -- Collection Receipt Export --

                    #region -- Sales Invoice Export --

                    int siRow = 2;

                    foreach (var item in selectedList)
                    {
                        if (item.SalesInvoice == null)
                        {
                            continue;
                        }
                        worksheet3.Cells[siRow, 1].Value = item.SalesInvoice.OtherRefNo;
                        worksheet3.Cells[siRow, 2].Value = item.SalesInvoice.Quantity;
                        worksheet3.Cells[siRow, 3].Value = item.SalesInvoice.UnitPrice;
                        worksheet3.Cells[siRow, 4].Value = item.SalesInvoice.Amount;
                        worksheet3.Cells[siRow, 5].Value = item.SalesInvoice.Remarks;
                        worksheet3.Cells[siRow, 6].Value = item.SalesInvoice.Status;
                        worksheet3.Cells[siRow, 7].Value = item.SalesInvoice.TransactionDate.ToString("yyyy-MM-dd");
                        worksheet3.Cells[siRow, 8].Value = item.SalesInvoice.Discount;
                        worksheet3.Cells[siRow, 9].Value = item.SalesInvoice.AmountPaid;
                        worksheet3.Cells[siRow, 10].Value = item.SalesInvoice.Balance;
                        worksheet3.Cells[siRow, 11].Value = item.SalesInvoice.IsPaid;
                        worksheet3.Cells[siRow, 12].Value = item.SalesInvoice.IsTaxAndVatPaid;
                        worksheet3.Cells[siRow, 13].Value = item.SalesInvoice.DueDate.ToString("yyyy-MM-dd");
                        worksheet3.Cells[siRow, 14].Value = item.SalesInvoice.CreatedBy;
                        worksheet3.Cells[siRow, 15].Value = item.SalesInvoice.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                        worksheet3.Cells[siRow, 16].Value = item.SalesInvoice.CancellationRemarks;
                        worksheet3.Cells[siRow, 18].Value = item.SalesInvoice.CustomerId;
                        worksheet3.Cells[siRow, 20].Value = item.SalesInvoice.ProductId;
                        worksheet3.Cells[siRow, 21].Value = item.SalesInvoice.SalesInvoiceNo;
                        worksheet3.Cells[siRow, 22].Value = item.SalesInvoice.SalesInvoiceId;

                        siRow++;
                    }

                    #endregion -- Sales Invoice Export --

                    #region -- Service Invoice Export --

                        int svRow = 2;

                        foreach (var item in selectedList)
                        {
                            if (item.ServiceInvoice == null)
                            {
                                continue;
                            }
                            worksheet4.Cells[svRow, 1].Value = item.ServiceInvoice.DueDate.ToString("yyyy-MM-dd");
                            worksheet4.Cells[svRow, 2].Value = item.ServiceInvoice.Period.ToString("yyyy-MM-dd");
                            worksheet4.Cells[svRow, 3].Value = item.ServiceInvoice.Amount;
                            worksheet4.Cells[svRow, 4].Value = item.ServiceInvoice.Total;
                            worksheet4.Cells[svRow, 5].Value = item.ServiceInvoice.Discount;
                            worksheet4.Cells[svRow, 6].Value = item.ServiceInvoice.CurrentAndPreviousAmount;
                            worksheet4.Cells[svRow, 7].Value = item.ServiceInvoice.UnearnedAmount;
                            worksheet4.Cells[svRow, 8].Value = item.ServiceInvoice.Status;
                            worksheet4.Cells[svRow, 9].Value = item.ServiceInvoice.AmountPaid;
                            worksheet4.Cells[svRow, 10].Value = item.ServiceInvoice.Balance;
                            worksheet4.Cells[svRow, 11].Value = item.ServiceInvoice.Instructions;
                            worksheet4.Cells[svRow, 12].Value = item.ServiceInvoice.IsPaid;
                            worksheet4.Cells[svRow, 13].Value = item.ServiceInvoice.CreatedBy;
                            worksheet4.Cells[svRow, 14].Value = item.ServiceInvoice.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                            worksheet4.Cells[svRow, 15].Value = item.ServiceInvoice.CancellationRemarks;
                            worksheet4.Cells[svRow, 16].Value = item.ServiceInvoice.CustomerId;
                            worksheet4.Cells[svRow, 17].Value = item.ServiceInvoice.ServiceInvoiceNo;
                            worksheet4.Cells[svRow, 18].Value = item.ServiceInvoice.ServicesId;
                            worksheet4.Cells[svRow, 19].Value = item.ServiceInvoice.ServiceInvoiceId;

                            svRow++;
                        }

                        #endregion -- Service Invoice Export --

                    #region -- Collection Receipt Export (Multiple SI) --

                        List<SalesInvoice> getSalesInvoice = new List<SalesInvoice>();

                        getSalesInvoice = _dbContext.SalesInvoices
                            .AsEnumerable()
                            .Where(s => selectedList?.Select(item => item?.MultipleSI).Any(si => si?.Contains(s.SalesInvoiceNo) == true) == true)
                            .OrderBy(si => si.SalesInvoiceNo)
                            .ToList();

                        foreach (var item in getSalesInvoice)
                        {
                            worksheet3.Cells[siRow, 1].Value = item.OtherRefNo;
                            worksheet3.Cells[siRow, 2].Value = item.Quantity;
                            worksheet3.Cells[siRow, 3].Value = item.UnitPrice;
                            worksheet3.Cells[siRow, 4].Value = item.Amount;
                            worksheet3.Cells[siRow, 5].Value = item.Remarks;
                            worksheet3.Cells[siRow, 6].Value = item.Status;
                            worksheet3.Cells[siRow, 7].Value = item.TransactionDate.ToString("yyyy-MM-dd");
                            worksheet3.Cells[siRow, 8].Value = item.Discount;
                            worksheet3.Cells[siRow, 9].Value = item.AmountPaid;
                            worksheet3.Cells[siRow, 10].Value = item.Balance;
                            worksheet3.Cells[siRow, 11].Value = item.IsPaid;
                            worksheet3.Cells[siRow, 12].Value = item.IsTaxAndVatPaid;
                            worksheet3.Cells[siRow, 13].Value = item.DueDate.ToString("yyyy-MM-dd");
                            worksheet3.Cells[siRow, 14].Value = item.CreatedBy;
                            worksheet3.Cells[siRow, 15].Value = item.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                            worksheet3.Cells[siRow, 16].Value = item.CancellationRemarks;
                            worksheet3.Cells[siRow, 18].Value = item.CustomerId;
                            worksheet3.Cells[siRow, 20].Value = item.ProductId;
                            worksheet3.Cells[siRow, 21].Value = item.SalesInvoiceNo;
                            worksheet3.Cells[siRow, 22].Value = item.SalesInvoiceId;

                            siRow++;
                        }

                    #endregion -- Collection Receipt Export (Multiple SI) --

                    #region -- Offsetting Export --

                    var crNos = selectedList.Select(item => item.CollectionReceiptNo).ToList();

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

                    #endregion -- Offsetting Export --

                    // Convert the Excel package to a byte array
                    var excelBytes = await package.GetAsByteArrayAsync();
                    await transaction.CommitAsync(cancellationToken);
                    return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "CollectionReceiptList.xlsx");
                }
		    }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index), new { view = DynamicView.BankAccount });
            }

        }

        #endregion -- export xlsx record --

        //Upload as .xlsx file.(Import)
        #region -- import xlsx record --

        [HttpPost]
        public async Task<IActionResult> Import(IFormFile file, CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0)
            {
                return RedirectToAction(nameof(CollectionIndex));
            }

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                stream.Position = 0;
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "CollectionReceipt");

                        var worksheet2 = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "Offsetting");

                        var worksheet3 = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "SalesInvoice");

                        var worksheet4 = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "ServiceInvoice");

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

                        #region -- Sales Invoice Import --

                        var siRowCount = worksheet3?.Dimension?.Rows ?? 0;
                        var siDictionary = new Dictionary<string, bool>();
                        var invoiceList = await _dbContext
                            .SalesInvoices
                            .ToListAsync(cancellationToken);

                        for (int row = 2; row <= siRowCount; row++) // Assuming the first row is the header
                        {
                            if (worksheet3 == null || siRowCount == 0)
                            {
                                continue;
                            }
                            var invoice = new SalesInvoice
                            {
                                SalesInvoiceNo = worksheet3.Cells[row, 21].Text,
                                OtherRefNo = worksheet3.Cells[row, 1].Text,
                                Quantity = decimal.TryParse(worksheet3.Cells[row, 2].Text, out decimal quantity)
                                    ? quantity
                                    : 0,
                                UnitPrice = decimal.TryParse(worksheet3.Cells[row, 3].Text, out decimal unitPrice)
                                    ? unitPrice
                                    : 0,
                                Amount =
                                    decimal.TryParse(worksheet3.Cells[row, 4].Text, out decimal amount) ? amount : 0,
                                Remarks = worksheet3.Cells[row, 5].Text,
                                Status = worksheet3.Cells[row, 6].Text,
                                TransactionDate =
                                    DateOnly.TryParse(worksheet3.Cells[row, 7].Text, out DateOnly transactionDate)
                                        ? transactionDate
                                        : default,
                                Discount = decimal.TryParse(worksheet3.Cells[row, 8].Text, out decimal discount)
                                    ? discount
                                    : 0,
                                // AmountPaid = decimal.TryParse(worksheet.Cells[row, 9].Text, out decimal amountPaid)
                                //     ? amountPaid
                                //     : 0,
                                // Balance = decimal.TryParse(worksheet.Cells[row, 10].Text, out decimal balance)
                                //     ? balance
                                //     : 0,
                                // IsPaid = bool.TryParse(worksheet.Cells[row, 11].Text, out bool isPaid) ? isPaid : false,
                                // IsTaxAndVatPaid = bool.TryParse(worksheet.Cells[row, 12].Text, out bool isTaxAndVatPaid)
                                //     ? isTaxAndVatPaid
                                //     : false,
                                DueDate = DateOnly.TryParse(worksheet3.Cells[row, 13].Text, out DateOnly dueDate)
                                    ? dueDate
                                    : default,
                                CreatedBy = worksheet3.Cells[row, 14].Text,
                                CreatedDate = DateTime.TryParse(worksheet3.Cells[row, 15].Text, out DateTime createdDate)
                                    ? createdDate
                                    : default,
                                PostedBy = worksheet3.Cells[row, 23].Text,
                                PostedDate = DateTime.TryParse(worksheet3.Cells[row, 24].Text, out DateTime postedDate)
                                    ? postedDate
                                    : default,
                                CancellationRemarks = worksheet3.Cells[row, 16].Text != ""
                                    ? worksheet3.Cells[row, 16].Text
                                    : null,
                                OriginalCustomerId = int.TryParse(worksheet3.Cells[row, 18].Text, out int customerId)
                                    ? customerId
                                    : 0,
                                OriginalProductId = int.TryParse(worksheet3.Cells[row, 20].Text, out int productId)
                                    ? productId
                                    : 0,
                                OriginalSeriesNumber = worksheet3.Cells[row, 21].Text,
                                OriginalDocumentId =
                                    int.TryParse(worksheet3.Cells[row, 22].Text, out int originalDocumentId)
                                        ? originalDocumentId
                                        : 0,
                            };

                            if (!siDictionary.TryAdd(invoice.OriginalSeriesNumber, true))
                            {
                                continue;
                            }

                            if (invoiceList.Any(si => si.OriginalDocumentId == invoice.OriginalDocumentId))
                            {
                                var siChanges = new Dictionary<string, (string OriginalValue, string NewValue)>();
                                var existingSI = await _dbContext.SalesInvoices.FirstOrDefaultAsync(si => si.OriginalDocumentId == invoice.OriginalDocumentId, cancellationToken);

                                if (existingSI.SalesInvoiceNo.TrimStart().TrimEnd() != worksheet3.Cells[row, 21].Text.TrimStart().TrimEnd())
                                {
                                    siChanges["SiNo"] = (existingSI.SalesInvoiceNo.TrimStart().TrimEnd(), worksheet3.Cells[row, 21].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingSI.OriginalCustomerId.ToString().TrimStart().TrimEnd() != worksheet3.Cells[row, 18].Text.TrimStart().TrimEnd())
                                {
                                    siChanges["OriginalCustomerId"] = (existingSI.OriginalCustomerId.ToString().TrimStart().TrimEnd(), worksheet3.Cells[row, 18].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingSI.OriginalProductId.ToString().TrimStart().TrimEnd() != worksheet3.Cells[row, 20].Text.TrimStart().TrimEnd())
                                {
                                    siChanges["OriginalProductId"] = (existingSI.OriginalProductId.ToString().TrimStart().TrimEnd(), worksheet3.Cells[row, 20].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingSI.OtherRefNo.TrimStart().TrimEnd() != worksheet3.Cells[row, 1].Text.TrimStart().TrimEnd())
                                {
                                    siChanges["OtherRefNo"] = (existingSI.OtherRefNo.TrimStart().TrimEnd(), worksheet3.Cells[row, 1].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingSI.Quantity.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet3.Cells[row, 2].Text).ToString("F2").TrimStart().TrimEnd())
                                {
                                    siChanges["Quantity"] = (existingSI.Quantity.ToString("F2").TrimStart().TrimEnd(), decimal.Parse(worksheet3.Cells[row, 2].Text.TrimStart().TrimEnd()).ToString("F2"));
                                }

                                if (existingSI.UnitPrice.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet3.Cells[row, 3].Text).ToString("F2").TrimStart().TrimEnd())
                                {
                                    siChanges["UnitPrice"] = (existingSI.UnitPrice.ToString("F2").TrimStart().TrimEnd(), decimal.Parse(worksheet3.Cells[row, 3].Text).ToString("F2").TrimStart().TrimEnd());
                                }

                                if (existingSI.Amount.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet3.Cells[row, 4].Text).ToString("F2").TrimStart().TrimEnd())
                                {
                                    siChanges["Amount"] = (existingSI.Amount.ToString("F2").TrimStart().TrimEnd(), decimal.Parse(worksheet3.Cells[row, 4].Text).ToString("F2").TrimStart().TrimEnd());
                                }

                                if (existingSI.Remarks.TrimStart().TrimEnd() != worksheet3.Cells[row, 5].Text.TrimStart().TrimEnd())
                                {
                                    siChanges["Remarks"] = (existingSI.Remarks.TrimStart().TrimEnd(), worksheet3.Cells[row, 5].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingSI.Status.TrimStart().TrimEnd() != worksheet3.Cells[row, 6].Text.TrimStart().TrimEnd())
                                {
                                    siChanges["Status"] = (existingSI.Status.TrimStart().TrimEnd(), worksheet3.Cells[row, 6].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingSI.TransactionDate.ToString("yyyy-MM-dd").TrimStart().TrimEnd() != worksheet3.Cells[row, 7].Text.TrimStart().TrimEnd())
                                {
                                    siChanges["TransactionDate"] = (existingSI.TransactionDate.ToString("yyyy-MM-dd").TrimStart().TrimEnd(), worksheet3.Cells[row, 7].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingSI.Discount.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet3.Cells[row, 8].Text).ToString("F2").TrimStart().TrimEnd())
                                {
                                    siChanges["Discount"] = (existingSI.Discount.ToString("F2").TrimStart().TrimEnd(), decimal.Parse(worksheet3.Cells[row, 8].Text).ToString("F2").TrimStart().TrimEnd());
                                }

                                if (existingSI.DueDate.ToString("yyyy-MM-dd").TrimStart().TrimEnd() != worksheet3.Cells[row, 13].Text.TrimStart().TrimEnd())
                                {
                                    siChanges["DueDate"] = (existingSI.DueDate.ToString("yyyy-MM-dd").TrimStart().TrimEnd(), worksheet3.Cells[row, 13].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingSI.CreatedBy.TrimStart().TrimEnd() != worksheet3.Cells[row, 14].Text.TrimStart().TrimEnd())
                                {
                                    siChanges["CreatedBy"] = (existingSI.CreatedBy.TrimStart().TrimEnd(), worksheet3.Cells[row, 14].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingSI.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff").TrimStart().TrimEnd() != worksheet3.Cells[row, 15].Text.TrimStart().TrimEnd())
                                {
                                    siChanges["CreatedDate"] = (existingSI.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff").TrimStart().TrimEnd(), worksheet3.Cells[row, 15].Text.TrimStart().TrimEnd())!;
                                }

                                if ((string.IsNullOrWhiteSpace(existingSI.CancellationRemarks?.TrimStart().TrimEnd()) ? "" : existingSI.CancellationRemarks.TrimStart().TrimEnd()) != worksheet3.Cells[row, 16].Text.TrimStart().TrimEnd())
                                {
                                    siChanges["CancellationRemarks"] = (existingSI.CancellationRemarks?.TrimStart().TrimEnd(), worksheet3.Cells[row, 16].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingSI.OriginalSeriesNumber.TrimStart().TrimEnd() != worksheet3.Cells[row, 21].Text.TrimStart().TrimEnd())
                                {
                                    siChanges["OriginalSeriesNumber"] = (existingSI.OriginalSeriesNumber.TrimStart().TrimEnd(), worksheet3.Cells[row, 21].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingSI.OriginalDocumentId.ToString().TrimStart().TrimEnd() != worksheet3.Cells[row, 22].Text.TrimStart().TrimEnd())
                                {
                                    siChanges["OriginalDocumentId"] = (existingSI.OriginalDocumentId.ToString().TrimStart().TrimEnd(), worksheet3.Cells[row, 22].Text.TrimStart().TrimEnd())!;
                                }

                                if (siChanges.Any())
                                {
                                    await _salesInvoiceRepo.LogChangesAsync(existingSI.OriginalDocumentId, siChanges, _userManager.GetUserName(this.User));
                                }

                                continue;
                            }
                            else
                            {
                                #region --Audit Trail Recording

                                if (!invoice.CreatedBy.IsNullOrEmpty())
                                {
                                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                                    AuditTrail auditTrailBook = new(invoice.CreatedBy, $"Create new invoice# {invoice.SalesInvoiceNo}", "Sales Invoice", ipAddress, invoice.CreatedDate);
                                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                                }
                                if (!invoice.PostedBy.IsNullOrEmpty())
                                {
                                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                                    AuditTrail auditTrailBook = new(invoice.PostedBy, $"Posted invoice# {invoice.SalesInvoiceNo}", "Sales Invoice", ipAddress, invoice.PostedDate);
                                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                                }

                                #endregion --Audit Trail Recording
                            }

                            invoice.CustomerId = await _dbContext.Customers
                                                     .Where(c => c.OriginalCustomerId == invoice.OriginalCustomerId)
                                                     .Select(c => (int?)c.CustomerId)
                                                     .FirstOrDefaultAsync(cancellationToken) ?? throw new InvalidOperationException("Please upload the Excel file for the customer master file first.");

                            invoice.ProductId = await _dbContext.Products
                                                    .Where(c => c.OriginalProductId == invoice.OriginalProductId)
                                                    .Select(c => (int?)c.ProductId)
                                                    .FirstOrDefaultAsync(cancellationToken) ?? throw new InvalidOperationException("Please upload the Excel file for the product master file first.");

                            await _dbContext.SalesInvoices.AddAsync(invoice, cancellationToken);
                        }

                        await _dbContext.SaveChangesAsync(cancellationToken);

                        #endregion -- Sales Invoice Import --

                        #region -- Service Invoice Import --

                        var svRowCount = worksheet4?.Dimension?.Rows ?? 0;
                        var svDictionary = new Dictionary<string, bool>();
                        var serviceInvoiceList = await _dbContext
                            .ServiceInvoices
                            .ToListAsync(cancellationToken);

                        for (int row = 2; row <= svRowCount; row++)  // Assuming the first row is the header
                        {
                            if (worksheet4 == null || svRowCount == 0)
                            {
                                continue;
                            }
                            var serviceInvoice = new ServiceInvoice
                            {
                                ServiceInvoiceNo = worksheet4.Cells[row, 17].Text,
                                DueDate = DateOnly.TryParse(worksheet4.Cells[row, 1].Text, out DateOnly dueDate) ? dueDate : default,
                                Period = DateOnly.TryParse(worksheet4.Cells[row, 2].Text, out DateOnly period) ? period : default,
                                Amount = decimal.TryParse(worksheet4.Cells[row, 3].Text, out decimal amount) ? amount : 0,
                                Total = decimal.TryParse(worksheet4.Cells[row, 4].Text, out decimal total) ? total : 0,
                                Discount = decimal.TryParse(worksheet4.Cells[row, 5].Text, out decimal discount) ? discount : 0,
                                CurrentAndPreviousAmount = decimal.TryParse(worksheet4.Cells[row, 6].Text, out decimal currentAndPreviousAmount) ? currentAndPreviousAmount : 0,
                                UnearnedAmount = decimal.TryParse(worksheet4.Cells[row, 7].Text, out decimal unearnedAmount) ? unearnedAmount : 0,
                                Status = worksheet4.Cells[row, 8].Text,
                                // AmountPaid = decimal.TryParse(worksheet.Cells[row, 9].Text, out decimal amountPaid) ? amountPaid : 0,
                                // Balance = decimal.TryParse(worksheet.Cells[row, 10].Text, out decimal balance) ? balance : 0,
                                Instructions = worksheet4.Cells[row, 11].Text,
                                // IsPaid = bool.TryParse(worksheet.Cells[row, 12].Text, out bool isPaid) ? isPaid : false,
                                CreatedBy = worksheet4.Cells[row, 13].Text,
                                CreatedDate = DateTime.TryParse(worksheet4.Cells[row, 14].Text, out DateTime createdDate) ? createdDate : default,
                                PostedBy = worksheet4.Cells[row, 20].Text,
                                PostedDate = DateTime.TryParse(worksheet4.Cells[row, 21].Text, out DateTime postedDate) ? postedDate : default,
                                CancellationRemarks = worksheet4.Cells[row, 15].Text,
                                OriginalCustomerId = int.TryParse(worksheet4.Cells[row, 16].Text, out int originalCustomerId) ? originalCustomerId : 0,
                                OriginalSeriesNumber = worksheet4.Cells[row, 17].Text,
                                OriginalServicesId = int.TryParse(worksheet4.Cells[row, 18].Text, out int originalServicesId) ? originalServicesId : 0,
                                OriginalDocumentId = int.TryParse(worksheet4.Cells[row, 19].Text, out int originalDocumentId) ? originalDocumentId : 0,
                            };

                            if (!svDictionary.TryAdd(serviceInvoice.OriginalSeriesNumber, true))
                            {
                                continue;
                            }

                            if (serviceInvoiceList.Any(sv => sv.OriginalDocumentId == serviceInvoice.OriginalDocumentId))
                            {
                                var svChanges = new Dictionary<string, (string OriginalValue, string NewValue)>();
                                var existingSV = await _dbContext.ServiceInvoices.FirstOrDefaultAsync(si => si.OriginalDocumentId == serviceInvoice.OriginalDocumentId, cancellationToken);

                                if (existingSV.ServiceInvoiceNo.TrimStart().TrimEnd() != worksheet4.Cells[row, 17].Text.TrimStart().TrimEnd())
                                {
                                    svChanges["SvNo"] = (existingSV.ServiceInvoiceNo.TrimStart().TrimEnd(), worksheet4.Cells[row, 17].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingSV.DueDate.ToString("yyyy-MM-dd").TrimStart().TrimEnd() != worksheet4.Cells[row, 1].Text.TrimStart().TrimEnd())
                                {
                                    svChanges["DueDate"] = (existingSV.DueDate.ToString("yyyy-MM-dd").TrimStart().TrimEnd(), worksheet4.Cells[row, 1].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingSV.Period.ToString("yyyy-MM-dd").TrimStart().TrimEnd() != worksheet4.Cells[row, 2].Text.TrimStart().TrimEnd())
                                {
                                    svChanges["Period"] = (existingSV.Period.ToString("yyyy-MM-dd").TrimStart().TrimEnd(), worksheet4.Cells[row, 2].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingSV.Amount.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet4.Cells[row, 3].Text).ToString("F2").TrimStart().TrimEnd())
                                {
                                    svChanges["Amount"] = (existingSV.Amount.ToString("F2").TrimStart().TrimEnd(), decimal.Parse(worksheet4.Cells[row, 3].Text).ToString("F2").TrimStart().TrimEnd());
                                }

                                if (existingSV.Total.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet4.Cells[row, 4].Text).ToString("F2").TrimStart().TrimEnd())
                                {
                                    svChanges["Total"] = (existingSV.Total.ToString("F2").TrimStart().TrimEnd(), decimal.Parse(worksheet4.Cells[row, 4].Text).ToString("F2").TrimStart().TrimEnd());
                                }

                                if (existingSV.Discount.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet4.Cells[row, 5].Text).ToString("F2").TrimStart().TrimEnd())
                                {
                                    svChanges["Discount"] = (existingSV.Discount.ToString("F2").TrimStart().TrimEnd(), decimal.Parse(worksheet4.Cells[row, 5].Text).ToString("F2").TrimStart().TrimEnd());
                                }

                                if (existingSV.CurrentAndPreviousAmount.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet4.Cells[row, 6].Text).ToString("F2").TrimStart().TrimEnd())
                                {
                                    svChanges["CurrentAndPreviousAmount"] = (existingSV.CurrentAndPreviousAmount.ToString("F2").TrimStart().TrimEnd(), decimal.Parse(worksheet4.Cells[row, 6].Text).ToString("F2").TrimStart().TrimEnd());
                                }

                                if (existingSV.UnearnedAmount.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet4.Cells[row, 7].Text).ToString("F2").TrimStart().TrimEnd())
                                {
                                    svChanges["UnearnedAmount"] = (existingSV.UnearnedAmount.ToString("F2").TrimStart().TrimEnd(), decimal.Parse(worksheet4.Cells[row, 7].Text).ToString("F2").TrimStart().TrimEnd());
                                }

                                if (existingSV.Status.TrimStart().TrimEnd() != worksheet4.Cells[row, 8].Text.TrimStart().TrimEnd())
                                {
                                    svChanges["Status"] = (existingSV.Status.TrimStart().TrimEnd(), worksheet4.Cells[row, 8].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingSV.Instructions.TrimStart().TrimEnd() != worksheet4.Cells[row, 11].Text.TrimStart().TrimEnd())
                                {
                                    svChanges["Instructions"] = (existingSV.Instructions.TrimStart().TrimEnd(), worksheet4.Cells[row, 11].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingSV.CreatedBy.TrimStart().TrimEnd() != worksheet4.Cells[row, 13].Text.TrimStart().TrimEnd())
                                {
                                    svChanges["CreatedBy"] = (existingSV.CreatedBy.TrimStart().TrimEnd(), worksheet4.Cells[row, 13].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingSV.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff").TrimStart().TrimEnd() != worksheet4.Cells[row, 14].Text.TrimStart().TrimEnd())
                                {
                                    svChanges["CreatedDate"] = (existingSV.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff").TrimStart().TrimEnd(), worksheet4.Cells[row, 14].Text.TrimStart().TrimEnd())!;
                                }

                                if ((string.IsNullOrWhiteSpace(existingSV.CancellationRemarks?.TrimStart().TrimEnd()) ? "" : existingSV.CancellationRemarks.TrimStart().TrimEnd()) != worksheet4.Cells[row, 15].Text.TrimStart().TrimEnd())
                                {
                                    svChanges["CancellationRemarks"] = (existingSV.CancellationRemarks?.TrimStart().TrimEnd(), worksheet4.Cells[row, 15].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingSV.OriginalCustomerId.ToString().TrimStart().TrimEnd() != worksheet4.Cells[row, 16].Text.TrimStart().TrimEnd())
                                {
                                    svChanges["OriginalCustomerId"] = (existingSV.OriginalCustomerId.ToString().TrimStart().TrimEnd(), worksheet4.Cells[row, 16].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingSV.OriginalSeriesNumber.TrimStart().TrimEnd() != worksheet4.Cells[row, 17].Text.TrimStart().TrimEnd())
                                {
                                    svChanges["OriginalSeriesNumber"] = (existingSV.OriginalSeriesNumber.TrimStart().TrimEnd(), worksheet4.Cells[row, 17].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingSV.OriginalServicesId.ToString().TrimStart().TrimEnd() != worksheet4.Cells[row, 18].Text.TrimStart().TrimEnd())
                                {
                                    svChanges["OriginalServicesId"] = (existingSV.OriginalServicesId.ToString().TrimStart().TrimEnd(), worksheet4.Cells[row, 18].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingSV.OriginalDocumentId.ToString().TrimStart().TrimEnd() != worksheet4.Cells[row, 19].Text.TrimStart().TrimEnd())
                                {
                                    svChanges["OriginalDocumentId"] = (existingSV.OriginalDocumentId.ToString().TrimStart().TrimEnd(), worksheet4.Cells[row, 19].Text.TrimStart().TrimEnd())!;
                                }

                                if (svChanges.Any())
                                {
                                    await _serviceInvoiceRepo.LogChangesAsync(existingSV.OriginalDocumentId, svChanges, _userManager.GetUserName(this.User));
                                }

                                continue;
                            }
                            else
                            {
                                #region --Audit Trail Recording

                                if (!serviceInvoice.CreatedBy.IsNullOrEmpty())
                                {
                                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                                    AuditTrail auditTrailBook = new(serviceInvoice.CreatedBy, $"Create new service invoice# {serviceInvoice.ServiceInvoiceNo}", "Service Invoice", ipAddress, serviceInvoice.CreatedDate);
                                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                                }
                                if (!serviceInvoice.PostedBy.IsNullOrEmpty())
                                {
                                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                                    AuditTrail auditTrailBook = new(serviceInvoice.PostedBy, $"Posted service invoice# {serviceInvoice.ServiceInvoiceNo}", "Service Invoice", ipAddress, serviceInvoice.PostedDate);
                                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                                }

                                #endregion --Audit Trail Recording
                            }

                            serviceInvoice.CustomerId = await _dbContext.Customers
                                .Where(sv => sv.OriginalCustomerId == serviceInvoice.OriginalCustomerId)
                                .Select(sv => (int?)sv.CustomerId)
                                .FirstOrDefaultAsync(cancellationToken) ?? throw new InvalidOperationException("Please upload the Excel file for the customer master file first.");

                            serviceInvoice.ServicesId = await _dbContext.Services
                                .Where(sv => sv.OriginalServiceId == serviceInvoice.OriginalServicesId)
                                .Select(sv => (int?)sv.ServiceId)
                                .FirstOrDefaultAsync(cancellationToken) ?? throw new InvalidOperationException("Please upload the Excel file for the service master file first.");

                            await _dbContext.ServiceInvoices.AddAsync(serviceInvoice, cancellationToken);
                        }
                        await _dbContext.SaveChangesAsync(cancellationToken);

                        #endregion -- Service Invoice Import --

                        #region -- Collection Receipt Import --

                        var rowCount = worksheet.Dimension.Rows;
                        var crDictionary = new Dictionary<string, bool>();
                        var collectionReceiptList = await _dbContext
                            .CollectionReceipts
                            .ToListAsync(cancellationToken);

                        for (int row = 2; row <= rowCount; row++)  // Assuming the first row is the header
                        {
                            var collectionReceipt = new CollectionReceipt
                            {
                                CollectionReceiptNo = worksheet.Cells[row, 30].Text,
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
                                PostedBy = worksheet.Cells[row, 33].Text,
                                PostedDate = DateTime.TryParse(worksheet.Cells[row, 34].Text, out DateTime postedDate) ? postedDate : default,
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

                            if (!crDictionary.TryAdd(collectionReceipt.OriginalSeriesNumber, true))
                            {
                                continue;
                            }

                            if (collectionReceiptList.Any(cr => cr.OriginalDocumentId == collectionReceipt.OriginalDocumentId))
                            {
                                var crChanges = new Dictionary<string, (string OriginalValue, string NewValue)>();
                                var existingCR = await _dbContext.CollectionReceipts.FirstOrDefaultAsync(si => si.OriginalDocumentId == collectionReceipt.OriginalDocumentId, cancellationToken);

                                if (existingCR.CollectionReceiptNo.TrimStart().TrimEnd() != worksheet.Cells[row, 30].Text.TrimStart().TrimEnd())
                                {
                                    crChanges["CrNo"] = (existingCR.CollectionReceiptNo.TrimStart().TrimEnd(), worksheet.Cells[row, 30].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingCR.TransactionDate.ToString("yyyy-MM-dd").TrimStart().TrimEnd() != worksheet.Cells[row, 1].Text.TrimStart().TrimEnd())
                                {
                                    crChanges["TransactionDate"] = (existingCR.TransactionDate.ToString("yyyy-MM-dd").TrimStart().TrimEnd(), worksheet.Cells[row, 1].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingCR.ReferenceNo.TrimStart().TrimEnd() != worksheet.Cells[row, 2].Text.TrimStart().TrimEnd())
                                {
                                    crChanges["ReferenceNo"] = (existingCR.ReferenceNo.TrimStart().TrimEnd(), worksheet.Cells[row, 2].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingCR.Remarks.TrimStart().TrimEnd() != worksheet.Cells[row, 3].Text.TrimStart().TrimEnd())
                                {
                                    crChanges["Remarks"] = (existingCR.Remarks.TrimStart().TrimEnd(), worksheet.Cells[row, 3].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingCR.CashAmount.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet.Cells[row, 4].Text).ToString("F2").TrimStart().TrimEnd())
                                {
                                    crChanges["CashAmount"] = (existingCR.CashAmount.ToString("F2").TrimStart().TrimEnd(), decimal.Parse(worksheet.Cells[row, 4].Text).ToString("F2").TrimStart().TrimEnd())!;
                                }

                                if (existingCR.CheckDate.TrimStart().TrimEnd() != worksheet.Cells[row, 5].Text.TrimStart().TrimEnd())
                                {
                                    crChanges["CheckDate"] = (existingCR.CheckDate.TrimStart().TrimEnd(), worksheet.Cells[row, 5].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingCR.CheckNo.TrimStart().TrimEnd() != worksheet.Cells[row, 6].Text.TrimStart().TrimEnd())
                                {
                                    crChanges["CheckNo"] = (existingCR.CheckNo.TrimStart().TrimEnd(), worksheet.Cells[row, 6].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingCR.CheckBank.TrimStart().TrimEnd() != worksheet.Cells[row, 7].Text.TrimStart().TrimEnd())
                                {
                                    crChanges["CheckBank"] = (existingCR.CheckBank.TrimStart().TrimEnd(), worksheet.Cells[row, 7].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingCR.CheckBranch.TrimStart().TrimEnd() != worksheet.Cells[row, 8].Text.TrimStart().TrimEnd())
                                {
                                    crChanges["CheckBranch"] = (existingCR.CheckBranch.TrimStart().TrimEnd(), worksheet.Cells[row, 8].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingCR.CheckAmount.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet.Cells[row, 9].Text).ToString("F2").TrimStart().TrimEnd())
                                {
                                    crChanges["CheckAmount"] = (existingCR.CheckAmount.ToString("F2").TrimStart().TrimEnd(), decimal.Parse(worksheet.Cells[row, 9].Text).ToString("F2").TrimStart().TrimEnd())!;
                                }

                                if (existingCR.ManagerCheckDate.ToString().TrimStart().TrimEnd() != worksheet.Cells[row, 10].Text.TrimStart().TrimEnd())
                                {
                                    crChanges["ManagerCheckDate"] = (existingCR.ManagerCheckDate.ToString().TrimStart().TrimEnd(), worksheet.Cells[row, 10].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingCR.ManagerCheckNo.TrimStart().TrimEnd() != worksheet.Cells[row, 11].Text.TrimStart().TrimEnd())
                                {
                                    crChanges["ManagerCheckNo"] = (existingCR.ManagerCheckNo.TrimStart().TrimEnd(), worksheet.Cells[row, 11].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingCR.ManagerCheckBank.TrimStart().TrimEnd() != worksheet.Cells[row, 12].Text.TrimStart().TrimEnd())
                                {
                                    crChanges["ManagerCheckBank"] = (existingCR.ManagerCheckBank.TrimStart().TrimEnd(), worksheet.Cells[row, 12].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingCR.ManagerCheckBranch.TrimStart().TrimEnd() != worksheet.Cells[row, 13].Text.TrimStart().TrimEnd())
                                {
                                    crChanges["ManagerCheckBranch"] = (existingCR.ManagerCheckBranch.TrimStart().TrimEnd(), worksheet.Cells[row, 13].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingCR.ManagerCheckAmount.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet.Cells[row, 14].Text).ToString("F2").TrimStart().TrimEnd())
                                {
                                    crChanges["ManagerCheckAmount"] = (existingCR.ManagerCheckAmount.ToString("F2").TrimStart().TrimEnd(), decimal.Parse(worksheet.Cells[row, 14].Text).ToString("F2").TrimStart().TrimEnd())!;
                                }

                                if (existingCR.EWT.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet.Cells[row, 15].Text).ToString("F2").TrimStart().TrimEnd())
                                {
                                    crChanges["EWT"] = (existingCR.EWT.ToString("F2").TrimStart().TrimEnd(), decimal.Parse(worksheet.Cells[row, 15].Text).ToString("F2").TrimStart().TrimEnd())!;
                                }

                                if (existingCR.WVAT.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet.Cells[row, 16].Text).ToString("F2").TrimStart().TrimEnd())
                                {
                                    crChanges["WVAT"] = (existingCR.WVAT.ToString("F2").TrimStart().TrimEnd(), decimal.Parse(worksheet.Cells[row, 16].Text).ToString("F2").TrimStart().TrimEnd())!;
                                }

                                if (existingCR.Total.ToString("F2").TrimStart().TrimEnd() != decimal.Parse(worksheet.Cells[row, 17].Text).ToString("F2").TrimStart().TrimEnd())
                                {
                                    crChanges["Total"] = (existingCR.Total.ToString("F2").TrimStart().TrimEnd(), decimal.Parse(worksheet.Cells[row, 17].Text).ToString("F2").TrimStart().TrimEnd())!;
                                }

                                if (existingCR.IsCertificateUpload.ToString().ToUpper().TrimStart().TrimEnd() != worksheet.Cells[row, 18].Text.TrimStart().TrimEnd())
                                {
                                    crChanges["IsCertificateUpload"] = (existingCR.IsCertificateUpload.ToString().TrimStart().TrimEnd(), worksheet.Cells[row, 18].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingCR.F2306FilePath.TrimStart().TrimEnd() != worksheet.Cells[row, 19].Text.TrimStart().TrimEnd())
                                {
                                    crChanges["F2306FilePath"] = (existingCR.F2306FilePath.TrimStart().TrimEnd(), worksheet.Cells[row, 19].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingCR.F2307FilePath.TrimStart().TrimEnd() != worksheet.Cells[row, 20].Text.TrimStart().TrimEnd())
                                {
                                    crChanges["F2307FilePath"] = (existingCR.F2307FilePath.TrimStart().TrimEnd(), worksheet.Cells[row, 20].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingCR.CreatedBy.TrimStart().TrimEnd() != worksheet.Cells[row, 21].Text.TrimStart().TrimEnd())
                                {
                                    crChanges["CreatedBy"] = (existingCR.CreatedBy.TrimStart().TrimEnd(), worksheet.Cells[row, 21].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingCR.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff".TrimStart().TrimEnd()) != worksheet.Cells[row, 22].Text.TrimStart().TrimEnd())
                                {
                                    crChanges["CreatedDate"] = (existingCR.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff").TrimStart().TrimEnd(), worksheet.Cells[row, 22].Text.TrimStart().TrimEnd())!;
                                }

                                if ((string.IsNullOrWhiteSpace(existingCR.CancellationRemarks) ? "" : existingCR.CancellationRemarks.TrimStart().TrimEnd()) != worksheet.Cells[row, 23].Text.TrimStart().TrimEnd())
                                {
                                    crChanges["CancellationRemarks"] = (existingCR.CancellationRemarks.TrimStart().TrimEnd(), worksheet.Cells[row, 23].Text.TrimStart().TrimEnd())!;
                                }

                                var multipleSIId = existingCR.MultipleSIId != null
                                    ? string.Join(", ", existingCR.MultipleSIId.Select(si => si.ToString()))
                                    : null;
                                if (multipleSIId != null && multipleSIId.TrimStart().TrimEnd() != worksheet.Cells[row, 25].Text.TrimStart().TrimEnd())
                                {
                                    var multipleSI = existingCR.MultipleSI != null
                                        ? string.Join(", ", existingCR.MultipleSI.Select(si => si.ToString()))
                                        : null;
                                    if (multipleSI != null && multipleSI.TrimStart().TrimEnd() != worksheet.Cells[row, 24].Text.TrimStart().TrimEnd())
                                    {
                                        crChanges["MultipleSI"] = (multipleSI.TrimStart().TrimEnd(), worksheet.Cells[row, 24].Text.TrimStart().TrimEnd())!;
                                    }

                                    if (multipleSIId.TrimStart().TrimEnd() != worksheet.Cells[row, 25].Text.TrimStart().TrimEnd())
                                    {
                                        crChanges["MultipleSIId"] = (multipleSIId.TrimStart().TrimEnd(), worksheet.Cells[row, 25].Text.TrimStart().TrimEnd())!;
                                    }

                                    var siMultipleAmount = existingCR.SIMultipleAmount != null
                                        ? string.Join(" ", existingCR.SIMultipleAmount.Select(si => si.ToString("N4")))
                                        : null;
                                    if (siMultipleAmount != null && siMultipleAmount.TrimStart().TrimEnd() != worksheet.Cells[row, 26].Text.TrimStart().TrimEnd())
                                    {
                                        crChanges["SIMultipleAmount"] = (siMultipleAmount.TrimStart().TrimEnd(), worksheet.Cells[row, 26].Text.TrimStart().TrimEnd())!;
                                    }

                                    var multipleTransactionDate = existingCR.MultipleTransactionDate != null
                                        ? string.Join(", ", existingCR.MultipleTransactionDate.Select(multipleTransactionDate => multipleTransactionDate.ToString("yyyy-MM-dd")))
                                        : null;
                                    if (multipleTransactionDate != null && multipleTransactionDate.TrimStart().TrimEnd() != worksheet.Cells[row, 27].Text.TrimStart().TrimEnd())
                                    {
                                        crChanges["MultipleTransactionDate"] = (multipleTransactionDate.TrimStart().TrimEnd(), worksheet.Cells[row, 27].Text.TrimStart().TrimEnd())!;
                                    }
                                }

                                if (existingCR.OriginalCustomerId.ToString().TrimStart().TrimEnd() != (worksheet.Cells[row, 28].Text.TrimStart().TrimEnd() == "" ? 0.ToString() : worksheet.Cells[row, 28].Text.TrimStart().TrimEnd()))
                                {
                                    crChanges["OriginalCustomerId"] = (existingCR.OriginalCustomerId.ToString().TrimStart().TrimEnd(), worksheet.Cells[row, 28].Text.TrimStart().TrimEnd() == "" ? 0.ToString() : worksheet.Cells[row, 28].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingCR.OriginalSalesInvoiceId.ToString().TrimStart().TrimEnd() != (worksheet.Cells[row, 29].Text.TrimStart().TrimEnd() == "" ? 0.ToString() : worksheet.Cells[row, 29].Text.TrimStart().TrimEnd()))
                                {
                                    crChanges["OriginalSalesInvoiceId"] = (existingCR.OriginalSalesInvoiceId.ToString(), worksheet.Cells[row, 29].Text.TrimStart().TrimEnd() == "" ? 0.ToString() : worksheet.Cells[row, 29].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingCR.OriginalSeriesNumber.TrimStart().TrimEnd() != worksheet.Cells[row, 30].Text.TrimStart().TrimEnd())
                                {
                                    crChanges["OriginalSeriesNumber"] = (existingCR.OriginalSeriesNumber.TrimStart().TrimEnd(), worksheet.Cells[row, 30].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingCR.OriginalServiceInvoiceId.ToString().TrimStart().TrimEnd() != (worksheet.Cells[row, 31].Text.TrimStart().TrimEnd() == "" ? 0.ToString() : worksheet.Cells[row, 31].Text.TrimStart().TrimEnd()))
                                {
                                    crChanges["OriginalServiceInvoiceId"] = (existingCR.OriginalServiceInvoiceId.ToString().TrimStart().TrimEnd(), worksheet.Cells[row, 31].Text.TrimStart().TrimEnd() == "" ? 0.ToString() : worksheet.Cells[row, 31].Text.TrimStart().TrimEnd())!;
                                }

                                if (existingCR.OriginalDocumentId.ToString().TrimStart().TrimEnd() != (worksheet.Cells[row, 32].Text.TrimStart().TrimEnd() == "" ? 0.ToString() : worksheet.Cells[row, 32].Text.TrimStart().TrimEnd()))
                                {
                                    crChanges["OriginalDocumentId"] = (existingCR.OriginalDocumentId.ToString().TrimStart().TrimEnd(), worksheet.Cells[row, 32].Text.TrimStart().TrimEnd() == "" ? 0.ToString() : worksheet.Cells[row, 32].Text.TrimStart().TrimEnd())!;
                                }

                                if (crChanges.Any())
                                {
                                    await _receiptRepo.LogChangesAsync(existingCR.OriginalDocumentId, crChanges, _userManager.GetUserName(this.User));
                                }

                                continue;
                            }
                            else
                            {
                                #region --Audit Trail Recording

                                if (!collectionReceipt.CreatedBy.IsNullOrEmpty())
                                {
                                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                                    AuditTrail auditTrailBook = new(collectionReceipt.CreatedBy, $"Create new collection receipt# {collectionReceipt.CollectionReceiptNo}", "Collection Receipt", ipAddress, collectionReceipt.CreatedDate);
                                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                                }
                                if (!collectionReceipt.PostedBy.IsNullOrEmpty())
                                {
                                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                                    AuditTrail auditTrailBook = new(collectionReceipt.PostedBy, $"Posted collection receipt# {collectionReceipt.CollectionReceiptNo}", "Collection Receipt", ipAddress, collectionReceipt.PostedDate);
                                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                                }

                                #endregion --Audit Trail Recording
                            }

                            collectionReceipt.CustomerId = await _dbContext.Customers
                                .Where(c => c.OriginalCustomerId == collectionReceipt.OriginalCustomerId)
                                .Select(c => (int?)c.CustomerId)
                                .FirstOrDefaultAsync(cancellationToken) ?? throw new InvalidOperationException("Please upload the Excel file for the customer master file first.");

                            var getSi = await _dbContext.SalesInvoices
                                        .Where(si => si.OriginalDocumentId == collectionReceipt.OriginalSalesInvoiceId)
                                        .Select(si => new { si.SalesInvoiceId, SINo = si.SalesInvoiceNo })
                                        .FirstOrDefaultAsync(cancellationToken);

                            collectionReceipt.SalesInvoiceId = getSi?.SalesInvoiceId;
                            collectionReceipt.SINo = getSi?.SINo;

                            var getSv = await _dbContext.ServiceInvoices
                                .Where(sv => sv.OriginalDocumentId == collectionReceipt.OriginalServiceInvoiceId)
                                .Select(sv => new { sv.ServiceInvoiceId, SVNo = sv.ServiceInvoiceNo })
                                .FirstOrDefaultAsync(cancellationToken);

                            collectionReceipt.ServiceInvoiceId = getSv?.ServiceInvoiceId;
                            collectionReceipt.SVNo = getSv?.SVNo;

                            if((getSv == null && !collectionReceipt.OriginalSalesInvoiceId.HasValue) &&
                               (getSi == null && !collectionReceipt.OriginalServiceInvoiceId.HasValue))
                            {
                                throw new InvalidOperationException("Please upload the Excel file for the sales invoice or service invoice first.");
                            }

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

                            await _dbContext.CollectionReceipts.AddAsync(collectionReceipt, cancellationToken);
                        }
                        await _dbContext.SaveChangesAsync(cancellationToken);

                        #endregion -- Collection Receipt Import --

                        #region -- Offsetting Import --

                        var offsetRowCount = worksheet2?.Dimension?.Rows ?? 0;

                        for (int offsetRow = 2; offsetRow <= offsetRowCount; offsetRow++)
                        {
                            if (worksheet2 == null || offsetRowCount == 0)
                            {
                                continue;
                            }
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
                                .Select(cr => cr.CollectionReceiptNo)
                                .FirstOrDefaultAsync(cancellationToken);

                            await _dbContext.Offsettings.AddAsync(offsetting, cancellationToken);
                        }
                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);

                        var checkChangesOfRecord = await _dbContext.ImportExportLogs
                            .Where(iel => iel.Action == string.Empty).ToListAsync(cancellationToken);
                        if (checkChangesOfRecord.Any())
                        {
                            TempData["importChanges"] = "";
                        }
                        #endregion -- Offsetting Import --
                    }
                }
                catch (OperationCanceledException oce)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = oce.Message;
                    return RedirectToAction(nameof(CollectionIndex), new { view = DynamicView.CollectionReceipt });
                }
                catch (InvalidOperationException ioe)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["warning"] = ioe.Message;
                    return RedirectToAction(nameof(CollectionIndex), new { view = DynamicView.CollectionReceipt });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
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
