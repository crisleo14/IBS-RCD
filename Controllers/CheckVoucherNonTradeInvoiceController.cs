﻿using Accounting_System.Data;
using Accounting_System.Repository;
using Accounting_System.Models;
using Accounting_System.Models.AccountsPayable;
using Accounting_System.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using Accounting_System.Models.Reports;
using Accounting_System.Utility;
using Microsoft.AspNetCore.Authorization;

namespace Accounting_System.Controllers
{
    public class CheckVoucherNonTradeInvoiceController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;

        private readonly ApplicationDbContext _dbContext;

        private readonly IWebHostEnvironment _webHostEnvironment;

        private readonly ILogger<CheckVoucherNonTradeInvoiceController> _logger;

        private readonly GeneralRepo _generalRepo;

        private readonly CheckVoucherRepo _checkVoucherRepo;

        public CheckVoucherNonTradeInvoiceController(UserManager<IdentityUser> userManager,
            ApplicationDbContext dbContext,
            IWebHostEnvironment webHostEnvironment,
            ILogger<CheckVoucherNonTradeInvoiceController> logger,
            GeneralRepo generalRepo,
            CheckVoucherRepo checkVoucherRepo)
        {
            _userManager = userManager;
            _dbContext = dbContext;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
            _generalRepo = generalRepo;
            _checkVoucherRepo = checkVoucherRepo;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetInvoiceCheckVouchers([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var checkVoucherDetails = await _checkVoucherRepo.GetCheckVouchersAsync(cancellationToken);

                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search?.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    checkVoucherDetails = checkVoucherDetails
                        .Where(cv =>
                            cv.CVNo.ToLower().Contains(searchValue) ||
                            cv.Date.ToString(CS.Date_Format).ToLower().Contains(searchValue) ||
                            cv.Supplier?.Name.ToLower().Contains(searchValue) == true ||
                            cv.Total.ToString().Contains(searchValue) ||
                            cv.Amount?.ToString()?.Contains(searchValue) == true ||
                            cv.AmountPaid.ToString().Contains(searchValue) ||
                            cv.Category.ToLower().Contains(searchValue) ||
                            cv.CvType?.ToLower().Contains(searchValue) == true ||
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

                    checkVoucherDetails = checkVoucherDetails
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}")
                        .ToList();
                }

                var totalRecords = checkVoucherDetails.Count();

                var pagedData = checkVoucherDetails
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
                _logger.LogError(ex, "Failed to get invoice check vouchers. Error: {ErrorMessage}, Stack: {StackTrace}.",
                    ex.Message, ex.StackTrace);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> GetDefaultExpense(int? supplierId)
        {
            var supplier = await _dbContext.Suppliers
                .Where(supp => supp.Id == supplierId)
                .Select(supp => supp.DefaultExpenseNumber)
                .FirstOrDefaultAsync();

            var defaultExpense = await _dbContext.ChartOfAccounts
                .Where(coa => !coa.HasChildren)
                .OrderBy(coa => coa.AccountId)
                .ToListAsync();

            if (defaultExpense.Count > 0)
            {
                var defaultExpenseList = defaultExpense.Select(coa => new
                {
                    AccountNumber = coa.AccountNumber,
                    AccountTitle = coa.AccountName,
                    IsSelected = coa.AccountNumber == supplier?.Split(' ')[0]
                }).ToList();

                return Json(defaultExpenseList);
            }

            return Json(null);
        }

        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var viewModel = new CheckVoucherNonTradeInvoicingViewModel();
            viewModel.ChartOfAccounts = await _dbContext.ChartOfAccounts
                .Where(coa => !coa.HasChildren)
                .OrderBy(coa => coa.AccountNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber + " " + s.AccountName,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            viewModel.Suppliers = await _dbContext.Suppliers
                .Where(supp => supp.Category == "Non-Trade")
                .Select(sup => new SelectListItem
                {
                    Value = sup.Id.ToString(),
                    Text = sup.Name
                })
                .ToListAsync();

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CheckVoucherNonTradeInvoicingViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    #region -- Saving the default entries --

                    CheckVoucherHeader checkVoucherHeader = new()
                    {
                        CVNo = await _checkVoucherRepo.GenerateCVNo(cancellationToken),
                        Date = viewModel.TransactionDate,
                        Payee = viewModel.SupplierName,
                        //Address = viewModel.SupplierAddress,
                        //Tin = viewModel.SupplierTinNo,
                        PONo = [viewModel.PoNo],
                        SINo = [viewModel.SiNo],
                        SupplierId = viewModel.SupplierId,
                        Particulars = viewModel.Particulars,
                        CreatedBy = _userManager.GetUserName(this.User),
                        Category = "Non-Trade",
                        CvType = nameof(CVType.Invoicing),
                        Total = viewModel.Total
                    };

                    await _dbContext.AddAsync(checkVoucherHeader, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    #endregion -- Saving the default entries --

                    #region -- cv invoiving details entry --

                    List<CheckVoucherDetail> checkVoucherDetails = new();

                    decimal apNontradeAmount = 0;
                    decimal vatAmount = 0;
                    decimal ewtOnePercentAmount = 0;
                    decimal ewtTwoPercentAmount = 0;
                    decimal ewtFivePercentAmount = 0;
                    decimal ewtTenPercentAmount = 0;

                    var accountTitlesDto = await _generalRepo.GetListOfAccountTitleDto(cancellationToken);
                    var apNonTradeTitle = accountTitlesDto.Find(c => c.AccountNumber == "202010200") ?? throw new ArgumentException("Account title '202010200' not found.");
                    var vatInputTitle = accountTitlesDto.Find(c => c.AccountNumber == "101060200") ?? throw new ArgumentException("Account title '101060200' not found.");
                    var ewtOnePercent = accountTitlesDto.Find(c => c.AccountNumber == "201030210") ?? throw new ArgumentException("Account title '201030210' not found.");
                    var ewtTwoPercent = accountTitlesDto.Find(c => c.AccountNumber == "201030220") ?? throw new ArgumentException("Account title '201030220' not found.");
                    var ewtFivePercent = accountTitlesDto.Find(c => c.AccountNumber == "201030230") ?? throw new ArgumentException("Account title '201030230' not found.");
                    var ewtTenPercent = accountTitlesDto.Find(c => c.AccountNumber == "201030240") ?? throw new ArgumentException("Account title '201030240' not found.");

                    foreach (var accountEntry in viewModel.AccountingEntries)
                    {
                        var parts = accountEntry.AccountTitle.Split(' ', 2); // Split into at most two parts
                        var accountNo = parts[0];
                        var accountName = parts[1];

                        checkVoucherDetails.Add(new CheckVoucherDetail
                        {
                            AccountNo = accountNo,
                            AccountName = accountName,
                            TransactionNo = checkVoucherHeader.CVNo,
                            CVHeaderId = checkVoucherHeader.Id,
                            Debit = accountEntry.NetOfVatAmount,
                            Credit = 0,
                            IsVatable = accountEntry.VatAmount > 0,
                            EwtPercent = accountEntry.TaxPercentage,
                            IsUserSelected = true,
                            SupplierId = accountEntry.SupplierMasterFileId,
                        });

                        if (accountEntry.VatAmount > 0)
                        {
                            vatAmount += accountEntry.VatAmount;
                        }

                        // Check EWT percentage
                        switch (accountEntry.TaxPercentage)
                        {
                            case 0.01m:
                                ewtOnePercentAmount += accountEntry.TaxAmount;
                                break;
                            case 0.02m:
                                ewtTwoPercentAmount += accountEntry.TaxAmount;
                                break;
                            case 0.05m:
                                ewtFivePercentAmount += accountEntry.TaxAmount;
                                break;
                            case 0.10m:
                                ewtTenPercentAmount += accountEntry.TaxAmount;
                                break;
                        }

                        apNontradeAmount += accountEntry.Amount - accountEntry.TaxAmount;

                    }

                    checkVoucherHeader.InvoiceAmount = apNontradeAmount;

                    if (vatAmount > 0)
                    {
                        checkVoucherDetails.Add(new CheckVoucherDetail
                        {
                            AccountNo = vatInputTitle.AccountNumber,
                            AccountName = vatInputTitle.AccountName,
                            TransactionNo = checkVoucherHeader.CVNo,
                            CVHeaderId = checkVoucherHeader.Id,
                            Debit = vatAmount,
                            Credit = 0,
                        });
                    }

                    if (apNontradeAmount > 0)
                    {
                        checkVoucherDetails.Add(new CheckVoucherDetail
                        {
                            AccountNo = apNonTradeTitle.AccountNumber,
                            AccountName = apNonTradeTitle.AccountName,
                            TransactionNo = checkVoucherHeader.CVNo,
                            CVHeaderId = checkVoucherHeader.Id,
                            Debit = 0,
                            Credit = apNontradeAmount,
                            SupplierId = checkVoucherHeader.SupplierId
                        });
                    }

                    if (ewtOnePercentAmount > 0)
                    {
                        checkVoucherDetails.Add(new CheckVoucherDetail
                        {
                            AccountNo = ewtOnePercent.AccountNumber,
                            AccountName = ewtOnePercent.AccountName,
                            TransactionNo = checkVoucherHeader.CVNo,
                            CVHeaderId = checkVoucherHeader.Id,
                            Debit = 0,
                            Credit = ewtOnePercentAmount,
                            Amount = ewtOnePercentAmount,
                            SupplierId = await _dbContext.Suppliers
                                .Where(s => s.Name.Contains("BUREAU OF INTERNAL REVENUE"))
                                .Select(s => s.Id)
                                .FirstOrDefaultAsync()
                        });
                    }

                    if (ewtTwoPercentAmount > 0)
                    {
                        checkVoucherDetails.Add(new CheckVoucherDetail
                        {
                            AccountNo = ewtTwoPercent.AccountNumber,
                            AccountName = ewtTwoPercent.AccountName,
                            TransactionNo = checkVoucherHeader.CVNo,
                            CVHeaderId = checkVoucherHeader.Id,
                            Debit = 0,
                            Credit = ewtTwoPercentAmount,
                            Amount = ewtTwoPercentAmount,
                            SupplierId = await _dbContext.Suppliers
                                .Where(s => s.Name.Contains("BUREAU OF INTERNAL REVENUE"))
                                .Select(s => s.Id)
                                .FirstOrDefaultAsync()
                        });
                    }

                    if (ewtFivePercentAmount > 0)
                    {
                        checkVoucherDetails.Add(new CheckVoucherDetail
                        {
                            AccountNo = ewtFivePercent.AccountNumber,
                            AccountName = ewtFivePercent.AccountName,
                            TransactionNo = checkVoucherHeader.CVNo,
                            CVHeaderId = checkVoucherHeader.Id,
                            Debit = 0,
                            Credit = ewtFivePercentAmount,
                            Amount = ewtFivePercentAmount,
                            SupplierId = await _dbContext.Suppliers
                                .Where(s => s.Name.Contains("BUREAU OF INTERNAL REVENUE"))
                                .Select(s => s.Id)
                                .FirstOrDefaultAsync()
                        });
                    }

                    if (ewtTenPercentAmount > 0)
                    {
                        checkVoucherDetails.Add(new CheckVoucherDetail
                        {
                            AccountNo = ewtTenPercent.AccountNumber,
                            AccountName = ewtTenPercent.AccountName,
                            TransactionNo = checkVoucherHeader.CVNo,
                            CVHeaderId = checkVoucherHeader.Id,
                            Debit = 0,
                            Credit = ewtTenPercentAmount,
                            Amount = ewtTenPercentAmount,
                            SupplierId = await _dbContext.Suppliers
                                .Where(s => s.Name.Contains("BUREAU OF INTERNAL REVENUE"))
                                .Select(s => s.Id)
                                .FirstOrDefaultAsync()
                        });
                    }

                    await _dbContext.CheckVoucherDetails.AddRangeAsync(checkVoucherDetails, cancellationToken);

                    #endregion -- cv invoiving details entry --

                    #region -- Uploading file --

                    if (file != null && file.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Supporting CV Files",
                            checkVoucherHeader.CVNo);

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
                        AuditTrail auditTrailBook = new(checkVoucherHeader.CreatedBy,
                            $"Create new check voucher# {checkVoucherHeader.CVNo}", "Check Voucher Non Trade Invoice", ipAddress);
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                    }

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    TempData["success"] = "Check voucher invoicing created successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create invoice check vouchers. Error: {ErrorMessage}, Stack: {StackTrace}. Created by: {UserName}",
                        ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                    viewModel.ChartOfAccounts = await _dbContext.ChartOfAccounts
                        .Where(coa => !coa.HasChildren)
                        .OrderBy(coa => coa.AccountNumber)
                        .Select(s => new SelectListItem
                        {
                            Value = s.AccountNumber + " " + s.AccountName,
                            Text = s.AccountNumber + " " + s.AccountName
                        })
                        .ToListAsync(cancellationToken);

                    viewModel.Suppliers = await _dbContext.Suppliers
                        .Where(supp => supp.Category == "Non-Trade")
                        .Select(sup => new SelectListItem
                        {
                            Value = sup.Id.ToString(),
                            Text = sup.Name
                        })
                        .ToListAsync();

                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(viewModel);
                }
            }

            viewModel.ChartOfAccounts = await _dbContext.ChartOfAccounts
                .Where(coa => !coa.HasChildren)
                .OrderBy(coa => coa.AccountNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber + " " + s.AccountName,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            viewModel.Suppliers = await _dbContext.Suppliers
                .Where(supp => supp.Category == "Non-Trade")
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
        public async Task<IActionResult> CreatePayrollInvoice(CancellationToken cancellationToken)
        {
            var viewModel = new CheckVoucherNonTradeInvoicingViewModel();

            viewModel.ChartOfAccounts = await _dbContext.ChartOfAccounts
                .Where(coa => !coa.HasChildren)
                .OrderBy(coa => coa.AccountNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            viewModel.Suppliers = await _dbContext.Suppliers
                .Where(supp => supp.Category == "Non-Trade")
                .Select(sup => new SelectListItem
                {
                    Value = sup.Id.ToString(),
                    Text = sup.Name
                })
                .ToListAsync();

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePayrollInvoice(CheckVoucherNonTradeInvoicingViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
        {

            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    #region -- Saving the default entries --

                    CheckVoucherHeader checkVoucherHeader = new()
                    {
                        CVNo = await _checkVoucherRepo.GenerateCVNo(cancellationToken),
                        Date = viewModel.TransactionDate,
                        Payee = null,
                        // Address = "",
                        // Tin = "",
                        PONo = [viewModel.PoNo],
                        SINo = [viewModel.SiNo],
                        SupplierId = null,
                        Particulars = viewModel.Particulars,
                        Total = viewModel.Total,
                        CreatedBy = _userManager.GetUserName(this.User),
                        Category = "Non-Trade",
                        CvType = nameof(CVType.Invoicing),
                        InvoiceAmount = viewModel.Total
                    };

                    await _dbContext.AddAsync(checkVoucherHeader, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    #endregion -- Saving the default entries -

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
                                CVHeaderId = checkVoucherHeader.Id,
                                Debit = viewModel.Debit[i],
                                Credit = viewModel.Credit[i],
                                Amount = viewModel.Credit[i],
                                SupplierId = viewModel.MultipleSupplierId[i] != 0 ? viewModel.MultipleSupplierId[i] : null,
                                IsUserSelected = true
                            });
                        }
                    }

                    await _dbContext.CheckVoucherDetails.AddRangeAsync(checkVoucherDetails, cancellationToken);

                    #endregion -- cv invoiving details entry --

                    #region -- Uploading file --

                    if (file != null && file.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Supporting CV Files",
                            checkVoucherHeader.CVNo);

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
                        AuditTrail auditTrailBook = new(checkVoucherHeader.CreatedBy,
                            $"Create new check voucher# {checkVoucherHeader.CVNo}", "Check Voucher Non Trade Payroll Invoice", ipAddress);
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                    }

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    TempData["success"] = "Check voucher invoicing created successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create payroll invoice check vouchers. Error: {ErrorMessage}, Stack: {StackTrace}. Created by: {UserName}",
                        ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                    viewModel.ChartOfAccounts = await _dbContext.ChartOfAccounts
                        .Where(coa => !coa.HasChildren)
                        .OrderBy(coa => coa.AccountNumber)
                        .Select(s => new SelectListItem
                        {
                            Value = s.AccountNumber,
                            Text = s.AccountNumber + " " + s.AccountName
                        })
                        .ToListAsync(cancellationToken);

                    viewModel.Suppliers = await _dbContext.Suppliers
                        .Where(supp => supp.Category == "Non-Trade")
                        .Select(sup => new SelectListItem
                        {
                            Value = sup.Id.ToString(),
                            Text = sup.Name
                        })
                        .ToListAsync();

                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(viewModel);
                }
            }

            viewModel.ChartOfAccounts = await _dbContext.ChartOfAccounts
                .Where(coa => !coa.HasChildren)
                .OrderBy(coa => coa.AccountNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            viewModel.Suppliers = await _dbContext.Suppliers
                .Where(supp => supp.Category == "Non-Trade")
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
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            var existingModel = await _dbContext.CheckVoucherHeaders
                .Include(c => c.Supplier)
                .FirstOrDefaultAsync(cv => cv.Id == id, cancellationToken);

            var existingDetailsModel = await _dbContext.CheckVoucherDetails
                .Where(d => d.IsUserSelected && d.CVHeaderId == existingModel.Id )
                .ToListAsync(cancellationToken);

            existingModel.Suppliers = await _dbContext.Suppliers
                .Select(sup => new SelectListItem
                {
                    Value = sup.Id.ToString(),
                    Text = sup.Name
                })
                .ToListAsync();

            existingModel.COA = await _dbContext.ChartOfAccounts
                .Where(coa => !coa.HasChildren)
                .OrderBy(coa => coa.AccountNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber + " " + s.AccountName,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

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
                AccountingEntries = []
            };

            foreach (var details in existingDetailsModel)
            {
                viewModel.AccountingEntries.Add(new AccountingEntryViewModel
                {
                    AccountTitle = $"{details.AccountNo} {details.AccountName}",
                    Amount = details.IsVatable ? Math.Round(details.Debit * 1.12m, 2) : Math.Round(details.Debit, 2),
                    Vatable = details.IsVatable,
                    TaxPercentage = details.EwtPercent,
                });
            }

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(CheckVoucherNonTradeInvoicingViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    #region --Saving the default entries

                    var existingModel = await _dbContext.CheckVoucherHeaders
                        .Include(cv => cv.Supplier)
                        .FirstOrDefaultAsync(cv => cv.Id == viewModel.CVId, cancellationToken);

                    var supplier = await _dbContext.Suppliers
                        .FirstOrDefaultAsync(s => s.Id == viewModel.SupplierId, cancellationToken);

                    if (existingModel != null)
                    {
                        // existingModel.EditedBy = _userManager.GetUserName(User);
                        // existingModel.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();
                        existingModel.Date = viewModel.TransactionDate;
                        existingModel.SupplierId = supplier.Id;
                        existingModel.Payee = supplier.Name;
                        existingModel.PONo = [viewModel.PoNo];
                        existingModel.SINo = [viewModel.SiNo];
                        existingModel.Particulars = viewModel.Particulars;
                        existingModel.Total = viewModel.Total;
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

                    var existingDetailsModel = await _dbContext.CheckVoucherDetails
                        .Where(d => d.CVHeaderId == existingModel.Id).
                        ToListAsync(cancellationToken);

                    _dbContext.RemoveRange(existingDetailsModel);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    var checkVoucherDetails = new List<CheckVoucherDetail>();

                    decimal apNontradeAmount = 0;
                    decimal vatAmount = 0;
                    decimal ewtOnePercentAmount = 0;
                    decimal ewtTwoPercentAmount = 0;
                    decimal ewtFivePercentAmount = 0;
                    decimal ewtTenPercentAmount = 0;

                    var accountTitlesDto = await _generalRepo.GetListOfAccountTitleDto(cancellationToken);
                    var apNonTradeTitle = accountTitlesDto.Find(c => c.AccountNumber == "202010200") ?? throw new ArgumentException("Account title '202010200' not found.");
                    var vatInputTitle = accountTitlesDto.Find(c => c.AccountNumber == "101060200") ?? throw new ArgumentException("Account title '101060200' not found.");
                    var ewtOnePercent = accountTitlesDto.Find(c => c.AccountNumber == "201030210") ?? throw new ArgumentException("Account title '201030210' not found.");
                    var ewtTwoPercent = accountTitlesDto.Find(c => c.AccountNumber == "201030220") ?? throw new ArgumentException("Account title '201030220' not found.");
                    var ewtFivePercent = accountTitlesDto.Find(c => c.AccountNumber == "201030230") ?? throw new ArgumentException("Account title '201030230' not found.");
                    var ewtTenPercent = accountTitlesDto.Find(c => c.AccountNumber == "201030240") ?? throw new ArgumentException("Account title '201030240' not found.");

                    foreach (var accountEntry in viewModel.AccountingEntries)
                    {
                        var parts = accountEntry.AccountTitle.Split(' ', 2); // Split into at most two parts
                        var accountNo = parts[0];
                        var accountName = parts[1];

                        checkVoucherDetails.Add(new CheckVoucherDetail
                        {
                            AccountNo = accountNo,
                            AccountName = accountName,
                            TransactionNo = existingModel.CVNo,
                            CVHeaderId = existingModel.Id,
                            Debit = accountEntry.NetOfVatAmount,
                            Credit = 0,
                            IsVatable = accountEntry.Vatable,
                            EwtPercent = accountEntry.TaxPercentage,
                            IsUserSelected = true,
                        });

                        if (accountEntry.Vatable)
                        {
                            vatAmount += accountEntry.VatAmount;
                        }

                        // Check EWT percentage
                        switch (accountEntry.TaxPercentage)
                        {
                            case 0.01m:
                                ewtOnePercentAmount += accountEntry.TaxAmount;
                                break;
                            case 0.02m:
                                ewtTwoPercentAmount += accountEntry.TaxAmount;
                                break;
                            case 0.05m:
                                ewtFivePercentAmount += accountEntry.TaxAmount;
                                break;
                            case 0.10m:
                                ewtTenPercentAmount += accountEntry.TaxAmount;
                                break;
                        }

                        apNontradeAmount += accountEntry.Amount - accountEntry.TaxAmount;

                    }

                    existingModel.InvoiceAmount = apNontradeAmount;

                    if (vatAmount > 0)
                    {
                        checkVoucherDetails.Add(new CheckVoucherDetail
                        {
                            AccountNo = vatInputTitle.AccountNumber,
                            AccountName = vatInputTitle.AccountName,
                            TransactionNo = existingModel.CVNo,
                            CVHeaderId = existingModel.Id,
                            Debit = vatAmount,
                            Credit = 0,
                        });
                    }

                    if (apNontradeAmount > 0)
                    {
                        checkVoucherDetails.Add(new CheckVoucherDetail
                        {
                            AccountNo = apNonTradeTitle.AccountNumber,
                            AccountName = apNonTradeTitle.AccountName,
                            TransactionNo = existingModel.CVNo,
                            CVHeaderId = existingModel.Id,
                            Debit = 0,
                            Credit = apNontradeAmount,
                            SupplierId = existingModel.SupplierId
                        });
                    }

                    if (ewtOnePercentAmount > 0)
                    {
                        checkVoucherDetails.Add(new CheckVoucherDetail
                        {
                            AccountNo = ewtOnePercent.AccountNumber,
                            AccountName = ewtOnePercent.AccountName,
                            TransactionNo = existingModel.CVNo,
                            CVHeaderId = existingModel.Id,
                            Debit = 0,
                            Credit = ewtOnePercentAmount,
                            Amount = ewtOnePercentAmount,
                            SupplierId = await _dbContext.Suppliers
                                .Where(s => s.Name.Contains("BUREAU OF INTERNAL REVENUE"))
                                .Select(s => s.Id)
                                .FirstOrDefaultAsync()
                        });
                    }

                    if (ewtTwoPercentAmount > 0)
                    {
                        checkVoucherDetails.Add(new CheckVoucherDetail
                        {
                            AccountNo = ewtTwoPercent.AccountNumber,
                            AccountName = ewtTwoPercent.AccountName,
                            TransactionNo = existingModel.CVNo,
                            CVHeaderId = existingModel.Id,
                            Debit = 0,
                            Credit = ewtTwoPercentAmount,
                            Amount = ewtTwoPercentAmount,
                            SupplierId = await _dbContext.Suppliers
                                .Where(s => s.Name.Contains("BUREAU OF INTERNAL REVENUE"))
                                .Select(s => s.Id)
                                .FirstOrDefaultAsync()
                        });
                    }

                    if (ewtFivePercentAmount > 0)
                    {
                        checkVoucherDetails.Add(new CheckVoucherDetail
                        {
                            AccountNo = ewtFivePercent.AccountNumber,
                            AccountName = ewtFivePercent.AccountName,
                            TransactionNo = existingModel.CVNo,
                            CVHeaderId = existingModel.Id,
                            Debit = 0,
                            Credit = ewtFivePercentAmount,
                            Amount = ewtFivePercentAmount,
                            SupplierId = await _dbContext.Suppliers
                                .Where(s => s.Name.Contains("BUREAU OF INTERNAL REVENUE"))
                                .Select(s => s.Id)
                                .FirstOrDefaultAsync()
                        });
                    }

                    if (ewtTenPercentAmount > 0)
                    {
                        checkVoucherDetails.Add(new CheckVoucherDetail
                        {
                            AccountNo = ewtTenPercent.AccountNumber,
                            AccountName = ewtTenPercent.AccountName,
                            TransactionNo = existingModel.CVNo,
                            CVHeaderId = existingModel.Id,
                            Debit = 0,
                            Credit = ewtTenPercentAmount,
                            Amount = ewtTenPercentAmount,
                            SupplierId = await _dbContext.Suppliers
                                .Where(s => s.Name.Contains("BUREAU OF INTERNAL REVENUE"))
                                .Select(s => s.Id)
                                .FirstOrDefaultAsync()
                        });
                    }

                    await _dbContext.CheckVoucherDetails.AddRangeAsync(checkVoucherDetails, cancellationToken);

                    #endregion --CV Details Entry

                    #region -- Uploading file --

                    if (file != null && file.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Supporting CV Files",
                            existingModel.CVNo);

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

                    // if (existingModel.OriginalSeriesNumber == null && existingModel.OriginalDocumentId == 0)
                    // {
                    //     var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    //     AuditTrail auditTrailBook = new(existingModel.CreatedBy,
                    //         $"Edited check voucher# {existingModel.CVNo}", "Check Voucher Non Trade Invoice", ipAddress);
                    //     await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                    // }

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    TempData["success"] = "Non-trade invoicing edited successfully";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to edit invoice check vouchers. Error: {ErrorMessage}, Stack: {StackTrace}. Edited by: {UserName}",
                        ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                    viewModel.Suppliers = await _dbContext.Suppliers
                    .Select(sup => new SelectListItem
                    {
                        Value = sup.Id.ToString(),
                        Text = sup.Name
                    })
                    .ToListAsync(cancellationToken);

                    viewModel.ChartOfAccounts = await _dbContext.ChartOfAccounts
                        .Where(coa => !coa.HasChildren)
                        .OrderBy(coa => coa.AccountNumber)
                        .Select(s => new SelectListItem
                        {
                            Value = s.AccountNumber + " " + s.AccountName,
                            Text = s.AccountNumber + " " + s.AccountName
                        })
                        .ToListAsync(cancellationToken);

                    await transaction.RollbackAsync(cancellationToken);
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

            viewModel.ChartOfAccounts = await _dbContext.ChartOfAccounts
                .Where(coa => !coa.HasChildren)
                .OrderBy(coa => coa.AccountNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber + " " + s.AccountName,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            TempData["error"] = "The information provided was invalid.";
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Print(int? id, int? supplierId, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            var header = await _dbContext.CheckVoucherHeaders
                .Include(cvh => cvh.Supplier)
                .FirstOrDefaultAsync(cvh => cvh.Id == id.Value, cancellationToken);

            if (header == null)
            {
                return NotFound();
            }

            var details = await _dbContext.CheckVoucherDetails
                .Include(cvd => cvd.Supplier)
                .Where(cvd => cvd.CVHeaderId == header.Id)
                .ToListAsync(cancellationToken);

            var getSupplier = await _dbContext.Suppliers
                .FindAsync(supplierId, cancellationToken);

            var viewModel = new CheckVoucherVM
            {
                Header = header,
                Details = details,
                Supplier = getSupplier
            };

            return View(viewModel);
        }

        public IActionResult GetAutomaticEntry(DateTime startDate, DateTime? endDate)
        {
            if (startDate != default && endDate != default)
            {
                return Json(true);
            }

            return Json(null);
        }

        public async Task<IActionResult> Post(int id, int? supplierId, CancellationToken cancellationToken)
        {
            var modelHeader = await _dbContext.CheckVoucherHeaders.FirstOrDefaultAsync(cv => cv.Id == id, cancellationToken);
            var modelDetails = await _dbContext.CheckVoucherDetails.Where(cvd => cvd.CVHeaderId == modelHeader.Id).ToListAsync();
            var supplierName = await _dbContext.Suppliers.Where(s => s.Id == supplierId).Select(s => s.Name).FirstOrDefaultAsync(cancellationToken);

            if (modelHeader != null)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    if (modelHeader.PostedBy == null)
                    {
                        modelHeader.PostedBy = _userManager.GetUserName(this.User);
                        modelHeader.PostedDate = DateTime.UtcNow.AddHours(8);
                        modelHeader.IsPosted = true;
                        //modelHeader.Status = nameof(CheckVoucherInvoiceStatus.ForPayment);

                        #region --General Ledger Book Recording(CV)--

                        var accountTitlesDto = await _generalRepo.GetListOfAccountTitleDto(cancellationToken);
                        var ledgers = new List<GeneralLedgerBook>();
                        foreach (var details in modelDetails)
                        {
                            var account = accountTitlesDto.Find(c => c.AccountNumber == details.AccountNo) ?? throw new ArgumentException($"Account title '{details.AccountNo}' not found.");
                            ledgers.Add(
                                    new GeneralLedgerBook
                                    {
                                        Date = modelHeader.Date,
                                        Reference = modelHeader.CVNo,
                                        Description = modelHeader.Particulars,
                                        AccountNo = account.AccountNumber,
                                        AccountTitle = account.AccountName,
                                        Debit = details.Debit,
                                        Credit = details.Credit,
                                        CreatedBy = modelHeader.CreatedBy,
                                        CreatedDate = modelHeader.CreatedDate
                                    }
                                );
                        }

                        if (!_generalRepo.IsJournalEntriesBalanced(ledgers))
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
                                        Payee = modelHeader.Payee != null ? modelHeader.Payee : supplierName,
                                        Amount = modelHeader.Total,
                                        Particulars = modelHeader.Particulars,
                                        Bank = bank != null ? bank.BankCode : "N/A",
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
                            AuditTrail auditTrailBook = new(modelHeader.CreatedBy,
                                $"Posted check voucher# {modelHeader.CVNo}", "Check Voucher Non Trade Invoice", ipAddress);
                            await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                        }

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Check Voucher has been Posted.";
                    }
                    return RedirectToAction(nameof(Print), new { id, supplierId });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to post invoice check vouchers. Error: {ErrorMessage}, Stack: {StackTrace}. Posted by: {UserName}",
                        ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                    await transaction.RollbackAsync(cancellationToken);

                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(Index));
                }
            }

            return NotFound();
        }

        public async Task<IActionResult> Cancel(int id, string? cancellationRemarks, CancellationToken cancellationToken)
        {
            var model = await _dbContext.CheckVoucherHeaders.FindAsync(id, cancellationToken);

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                if (model != null)
                {
                    if (model.CanceledBy == null)
                    {
                        model.CanceledBy = _userManager.GetUserName(this.User);
                        model.CanceledDate = DateTime.UtcNow.AddHours(8);
                        model.IsCanceled = true;
                        //model.Status = nameof(CheckVoucherInvoiceStatus.Canceled);
                        model.CancellationRemarks = cancellationRemarks;

                        #region --Audit Trail Recording

                        if (model.OriginalSeriesNumber == null && model.OriginalDocumentId == 0)
                        {
                            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                            AuditTrail auditTrailBook = new(model.CreatedBy,
                                $"Canceled check voucher# {model.CVNo}", "Check Voucher Non Trade Invoice", ipAddress);
                            await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                        }

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Check Voucher has been Cancelled.";
                    }

                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to cancel invoice check vouchers. Error: {ErrorMessage}, Stack: {StackTrace}. Canceled by: {UserName}",
                    ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                TempData["error"] = $"Error: '{ex.Message}'";
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {
            var model = await _dbContext.CheckVoucherHeaders.FindAsync(id, cancellationToken);

            if (model != null)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    if (model.VoidedBy == null)
                    {
                        if (model.PostedBy != null)
                        {
                            model.PostedBy = null;
                        }

                        model.VoidedBy = _userManager.GetUserName(this.User);
                        model.VoidedDate = DateTime.UtcNow.AddHours(8);
                        model.IsVoided = true;
                        //model.Status = nameof(CheckVoucherInvoiceStatus.Voided);

                        await _generalRepo.RemoveRecords<DisbursementBook>(db => db.CVNo == model.CVNo, cancellationToken);
                        await _generalRepo.RemoveRecords<GeneralLedgerBook>(gl => gl.Reference == model.CVNo, cancellationToken);

                        //re-compute amount paid in trade and payment voucher

                        #region --Audit Trail Recording

                        if (model.OriginalSeriesNumber == null && model.OriginalDocumentId == 0)
                        {
                            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                            AuditTrail auditTrailBook = new(model.CreatedBy,
                                $"Voided check voucher# {model.CVNo}", "Check Voucher Non Trade Invoice", ipAddress);
                            await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                        }

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Check Voucher has been Voided.";

                        return RedirectToAction(nameof(Index));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to void invoice check vouchers. Error: {ErrorMessage}, Stack: {StackTrace}. Voided by: {UserName}",
                        ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(Index));
                }
            }

            return NotFound();
        }

        public async Task<IActionResult> Printed(int id, int? supplierId, CancellationToken cancellationToken)
        {
            var cv = await _dbContext.CheckVoucherHeaders.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (!cv.IsPrinted)
            {
                #region --Audit Trail Recording

                if (cv.OriginalSeriesNumber == null && cv.OriginalDocumentId == 0)
                {
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    AuditTrail auditTrailBook = new(cv.CreatedBy,
                        $"Printed original copy of check voucher# {cv.CVNo}", "Check Voucher Non Trade Invoice", ipAddress);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                }

                #endregion --Audit Trail Recording

                cv.IsPrinted = true;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            return RedirectToAction(nameof(Print), new { id, supplierId });
        }

        [HttpGet]
        public async Task<IActionResult> EditPayrollInvoice(int id, CancellationToken cancellationToken)
        {
            if (id == 0)
            {
                return NotFound();
            }

            var existingHeaderModel = await _dbContext.CheckVoucherHeaders.FindAsync(id, cancellationToken);

            var existingDetailsModel = await _dbContext.CheckVoucherDetails
                .Where(cvd => cvd.CVHeaderId == existingHeaderModel.Id)
                .ToListAsync();

            if (existingHeaderModel == null || existingDetailsModel == null)
            {
                return NotFound();
            }

            var accountNumbers = existingDetailsModel.Select(model => model.AccountNo).ToArray();
            var accountTitles = existingDetailsModel.Select(model => model.AccountName).ToArray();
            var debit = existingDetailsModel.Select(model => model.Debit).ToArray();
            var credit = existingDetailsModel.Select(model => model.Credit).ToArray();

            var coa = await _dbContext.ChartOfAccounts
                .Where(coa => !coa.HasChildren)
                .OrderBy(coa => coa.AccountNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            var suppliers = await _dbContext.Suppliers
                .Where(supp => supp.Category == "Non-Trade")
                .Select(sup => new SelectListItem
                {
                    Value = sup.Id.ToString(),
                    Text = sup.Name
                })
                .ToListAsync();

            var details = await _dbContext.CheckVoucherDetails
                .Where(cvd => cvd.CVHeaderId == existingHeaderModel.Id)
                .Include(s => s.Supplier)
                .FirstOrDefaultAsync();

            var getSupplierId = await _dbContext.CheckVoucherDetails
                .Where(cvd => cvd.CVHeaderId == existingHeaderModel.Id)
                .OrderBy(s => s.Id)
                .Select(s => s.SupplierId)
                .ToArrayAsync();

            CheckVoucherNonTradeInvoicingViewModel model = new()
            {
                MultipleSupplierId = getSupplierId,
                SupplierAddress = details?.Supplier?.Address,
                SupplierTinNo = details?.Supplier?.TinNo,
                Suppliers = suppliers,
                TransactionDate = existingHeaderModel.Date,
                Particulars = existingHeaderModel.Particulars,
                Total = existingHeaderModel.Total,
                AccountNumber = accountNumbers,
                AccountTitle = accountTitles,
                Debit = debit,
                Credit = credit,
                ChartOfAccounts = coa,
                CVId = existingHeaderModel.Id,
                PoNo = existingHeaderModel.PONo.First(),
                SiNo = existingHeaderModel.SINo.First()
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditPayrollInvoice(CheckVoucherNonTradeInvoicingViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    #region -- Saving the default entries --

                    var existingHeaderModel = await _dbContext.CheckVoucherHeaders
                        .Include(cv => cv.Supplier)
                        .FirstOrDefaultAsync(cv => cv.Id == viewModel.CVId, cancellationToken);

                    if (existingHeaderModel != null)
                    {
                        // existingHeaderModel.EditedBy = _userManager.GetUserName(User);
                        // existingHeaderModel.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();
                        existingHeaderModel.Date = viewModel.TransactionDate;
                        existingHeaderModel.PONo = [viewModel.PoNo];
                        existingHeaderModel.SINo = [viewModel.SiNo];
                        existingHeaderModel.Particulars = viewModel.Particulars;
                        existingHeaderModel.Total = viewModel.Total;
                    }

                    #endregion -- Saving the default entries --

                    #region -- Get Supplier --

                    var supplier = await _dbContext.Suppliers
                        .Where(s => s.Id == viewModel.SupplierId)
                        .FirstOrDefaultAsync(cancellationToken);

                    #endregion -- Get Supplier --

                    #region -- Automatic entry --

                    if (viewModel.StartDate != null && viewModel.NumberOfYears != 0)
                    {
                        existingHeaderModel.StartDate = viewModel.StartDate;
                        existingHeaderModel.EndDate = existingHeaderModel.StartDate.Value.AddYears(viewModel.NumberOfYears);
                        existingHeaderModel.NumberOfMonths = (viewModel.NumberOfYears * 12);

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
                            existingHeaderModel.AmountPerMonth = (amount.Value / viewModel.NumberOfYears) / 12;
                        }
                    }

                    await _dbContext.SaveChangesAsync(cancellationToken);

                    #endregion -- Automatic entry --

                    #region -- cv invoiving details entry --

                    var existingDetailsModel = await _dbContext.CheckVoucherDetails.Where(d => d.CVHeaderId == existingHeaderModel.Id).ToListAsync();

                    _dbContext.RemoveRange(existingDetailsModel);
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
                                TransactionNo = existingHeaderModel.CVNo,
                                CVHeaderId = viewModel.CVId,
                                Debit = viewModel.Debit[i],
                                Credit = viewModel.Credit[i],
                                Amount = viewModel.Credit[i],
                                SupplierId = viewModel.MultipleSupplierId[i] != 0 ? viewModel.MultipleSupplierId[i] : null
                            });
                        }
                    }

                    await _dbContext.CheckVoucherDetails.AddRangeAsync(checkVoucherDetails, cancellationToken);

                    #endregion -- cv invoiving details entry --

                    #region -- Uploading file --

                    if (file != null && file.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Supporting CV Files",
                            existingHeaderModel.CVNo);

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

                    // if (existingHeaderModel.OriginalSeriesNumber == null && existingHeaderModel.OriginalDocumentId == 0)
                    // {
                    //     var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    //     AuditTrail auditTrailBook = new(existingHeaderModel.CreatedBy,
                    //         $"Edited check voucher# {existingHeaderModel.CVNo}", "Check Voucher Non Trade Invoice", ipAddress);
                    //     await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                    // }

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    TempData["success"] = "Check voucher invoicing edited successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to edit payroll invoice check vouchers. Error: {ErrorMessage}, Stack: {StackTrace}. Edited by: {UserName}",
                        ex.Message, ex.StackTrace, _userManager.GetUserName(User));
                    viewModel.ChartOfAccounts = await _dbContext.ChartOfAccounts
                        .Where(coa => !coa.HasChildren)
                        .OrderBy(coa => coa.AccountNumber)
                        .Select(s => new SelectListItem
                        {
                            Value = s.AccountNumber,
                            Text = s.AccountNumber + " " + s.AccountName
                        })
                        .ToListAsync(cancellationToken);

                    viewModel.Suppliers = await _dbContext.Suppliers
                        .Where(supp => supp.Category == "Non-Trade")
                        .Select(sup => new SelectListItem
                        {
                            Value = sup.Id.ToString(),
                            Text = sup.Name
                        })
                        .ToListAsync();

                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(viewModel);
                }
            }

            viewModel.ChartOfAccounts = await _dbContext.ChartOfAccounts
                .Where(coa => !coa.HasChildren)
                .OrderBy(coa => coa.AccountNumber)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            viewModel.Suppliers = await _dbContext.Suppliers
                .Where(supp => supp.Category == "Non-Trade")
                .Select(sup => new SelectListItem
                {
                    Value = sup.Id.ToString(),
                    Text = sup.Name
                })
                .ToListAsync();

            TempData["error"] = "The information provided was invalid.";
            return View(viewModel);
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
                        Name = supplier.Name,
                        Address = supplier.Address,
                        TinNo = supplier.TinNo,
                        supplier.TaxType,
                        supplier.Category,
                        TaxPercent = supplier.WithholdingTaxPercent,
                        supplier.VatType,
                        DefaultExpense = supplier.DefaultExpenseNumber,
                        WithholdingTax = supplier.WithholdingTaxtitle,
                        Vatable = supplier.VatType == CS.VatType_Vatable
                    });
                }
                return Json(null);
            }
            return Json(null);
        }

        [HttpGet]
        public async Task<IActionResult> GetBankAccounts(CancellationToken cancellationToken)
        {
            // Replace this with your actual repository/service call
            var bankAccounts = await _dbContext.BankAccounts.ToListAsync(cancellationToken);

            return Json(bankAccounts.Select(b => new {
                id = b.Id,
                accountName = b.AccountName,
                accountNumber = b.BankCode
            }));
        }

        [HttpGet]
        public async Task<IActionResult> GetBankAccountById(int bankId)
        {
            var bankAccount = await _dbContext.BankAccounts.FirstOrDefaultAsync(b => b.Id == bankId);
            return Json(new
            {
                id = bankAccount.Id,
                accountName = bankAccount.AccountName,
                accountNumber = bankAccount.BankCode
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomers(CancellationToken cancellationToken)
        {
            var employees = await _dbContext.Customers.ToListAsync(cancellationToken);

            return Json(employees.OrderBy(c => c.Number).Select(c => new {
                id = c.Id,
                accountName = c.Name,
                accountNumber = c.Number
            }));
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomerById(int customerId)
        {
            var customer = await _dbContext.Customers.FirstOrDefaultAsync(e => e.Id == customerId);
            return Json(new
            {
                id = customer.Id,
                accountName = customer.Name,
                accountNumber = customer.Number
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetSuppliers(CancellationToken cancellationToken)
        {
            var suppliers = await _dbContext.Suppliers.ToListAsync(cancellationToken);

            return Json(suppliers.OrderBy(c => c.Number).Select(c => new {
                id = c.Id,
                accountName = c.Name,
                accountNumber = c.Number
            }));
        }

        [HttpGet]
        public async Task<IActionResult> GetSupplierById(int supplierId)
        {
            var supplier = await _dbContext.Suppliers.FirstOrDefaultAsync(e => e.Id == supplierId);
            return Json(new
            {
                id = supplier.Id,
                accountName = supplier.Name,
                accountNumber = supplier.Number
            });
        }
    }
}
