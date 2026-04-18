using System.Globalization;
using Accounting_System.Data;
using Accounting_System.Repository;
using Accounting_System.Models;
using Accounting_System.Models.AccountsPayable;
using Accounting_System.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Linq.Dynamic.Core;
using Accounting_System.Models.Reports;
using Accounting_System.Utility;
using Microsoft.IdentityModel.Tokens;

namespace Accounting_System.Controllers
{
    public class CheckVoucherTradeController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly AasDbContext _aasDbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly IWebHostEnvironment _webHostEnvironment;

        private readonly ILogger<CheckVoucherTradeController> _logger;

        private readonly GeneralRepo _generalRepo;

        private readonly CheckVoucherRepo _checkVoucherRepo;

        private readonly ReceivingReportRepo _receivingReportRepo;

        private readonly PurchaseOrderRepo _purchaseOrderRepo;

        public CheckVoucherTradeController(UserManager<IdentityUser> userManager,
            ApplicationDbContext dbContext,
            IWebHostEnvironment webHostEnvironment,
            ILogger<CheckVoucherTradeController> logger,
            GeneralRepo generalRepo,
            CheckVoucherRepo checkVoucherRepo,
            PurchaseOrderRepo purchaseOrderRepo,
            ReceivingReportRepo receivingReportRepo,
            AasDbContext aasDbContext)
        {
            _userManager = userManager;
            _dbContext = dbContext;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
            _generalRepo = generalRepo;
            _checkVoucherRepo = checkVoucherRepo;
            _purchaseOrderRepo = purchaseOrderRepo;
            _receivingReportRepo = receivingReportRepo;
            _aasDbContext = aasDbContext;
        }

        public IActionResult Index(string? view)
        {
            if (view == nameof(DynamicView.CheckVoucher))
            {
                return View("ImportExportIndex");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetCheckVouchers([FromForm] DataTablesParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var checkVoucherHeaders = await _checkVoucherRepo.GetCheckVouchersAsync(cancellationToken);

                // Search filter
                if (!string.IsNullOrEmpty(parameters.Search.Value))
                {
                    var searchValue = parameters.Search.Value.ToLower();

                    checkVoucherHeaders = checkVoucherHeaders
                        .Where(cv =>
                            cv.CheckVoucherHeaderNo!.ToLower().Contains(searchValue) ||
                            cv.Date.ToString(CS.Date_Format).ToLower().Contains(searchValue) ||
                            cv.Supplier?.SupplierName.ToLower().Contains(searchValue) == true ||
                            cv.Total.ToString(CultureInfo.InvariantCulture).Contains(searchValue) ||
                            cv.Amount?.ToString()?.Contains(searchValue) == true ||
                            cv.AmountPaid.ToString(CultureInfo.InvariantCulture).Contains(searchValue) ||
                            cv.Category.ToLower().Contains(searchValue) ||
                            cv.CvType?.ToLower().Contains(searchValue) == true ||
                            cv.CreatedBy!.ToLower().Contains(searchValue)
                        )
                    .ToList();
                }

                // Sorting
                if (parameters.Order?.Count > 0)
                {
                    var orderColumn = parameters.Order[0];
                    var columnName = parameters.Columns[orderColumn.Column].Data;
                    var sortDirection = orderColumn.Dir.ToLower() == "asc" ? "ascending" : "descending";

                    checkVoucherHeaders = checkVoucherHeaders
                        .AsQueryable()
                        .OrderBy($"{columnName} {sortDirection}")
                        .ToList();
                }

                var totalRecords = checkVoucherHeaders.Count();

                var pagedData = checkVoucherHeaders
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
                _logger.LogError(ex, "Failed to get check vouchers. Error: {ErrorMessage}, Stack: {StackTrace}.",
                    ex.Message, ex.StackTrace);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            CheckVoucherTradeViewModel model = new()
            {
                COA = await _dbContext.ChartOfAccounts
                    .Where(coa => !new[] { "202010200", "202010100", "101010100" }.Any(excludedNumber => coa.AccountNumber != null && coa.AccountNumber.Contains(excludedNumber)) && !coa.HasChildren)
                    .Select(s => new SelectListItem
                    {
                        Value = s.AccountNumber,
                        Text = s.AccountNumber + " " + s.AccountName
                    })
                    .ToListAsync(cancellationToken),
                Suppliers = await _dbContext.Suppliers
                    .Where(supp => supp.Category == "Trade")
                    .OrderBy(supp => supp.Number)
                    .Select(sup => new SelectListItem
                    {
                        Value = sup.SupplierId.ToString(),
                        Text = sup.SupplierName
                    })
                    .ToListAsync(cancellationToken: cancellationToken),
                BankAccounts = await _dbContext.BankAccounts
                    .Select(ba => new SelectListItem
                    {
                        Value = ba.BankAccountId.ToString(),
                        Text = ba.Bank + " " + ba.AccountName
                    })
                    .ToListAsync(cancellationToken: cancellationToken)
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CheckVoucherTradeViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                var createdBy = await _generalRepo.GetUserFullNameAsync(User.Identity!.Name!);

                try
                {
                    #region --Check if duplicate record

                    if (!viewModel.CheckNo.Any() && !viewModel.CheckNo.Contains("DM"))
                    {
                        var cv = await _dbContext
                        .CheckVoucherHeaders
                        .Where(cv => cv.CheckNo == viewModel.CheckNo && cv.BankId == viewModel.BankId)
                        .ToListAsync(cancellationToken);
                        if (cv.Any())
                        {
                            viewModel.COA = await _dbContext.ChartOfAccounts
                                .Where(coa => !new[] { "202010200", "202010100", "101010100" }.Any(excludedNumber => coa.AccountNumber != null && coa.AccountNumber.Contains(excludedNumber)) && !coa.HasChildren)
                                .Select(s => new SelectListItem
                                {
                                    Value = s.AccountNumber,
                                    Text = s.AccountNumber + " " + s.AccountName
                                })
                                .ToListAsync(cancellationToken);

                            viewModel.Suppliers = await _dbContext.Suppliers
                                .Where(supp => supp.Category == "Trade")
                                .Select(sup => new SelectListItem
                                {
                                    Value = sup.SupplierId.ToString(),
                                    Text = sup.SupplierName
                                })
                                .ToListAsync(cancellationToken: cancellationToken);

                            viewModel.PONo = await _dbContext.PurchaseOrders
                                .Where(po => po.SupplierId == viewModel.SupplierId && po.IsPosted)
                                .Select(po => new SelectListItem
                                {
                                    Value = po.PurchaseOrderNo!.ToString(),
                                    Text = po.PurchaseOrderNo
                                })
                                .ToListAsync(cancellationToken);

                            viewModel.BankAccounts = await _dbContext.BankAccounts
                                .Select(ba => new SelectListItem
                                {
                                    Value = ba.BankAccountId.ToString(),
                                    Text = ba.Bank + " " + ba.AccountName
                                })
                                .ToListAsync(cancellationToken: cancellationToken);

                            TempData["error"] = "Check No. Is already exist";
                            return View(viewModel);
                        }
                    }

                    #endregion --Check if duplicate record

                    #region --Retrieve Supplier

                    await _dbContext
                        .Suppliers
                        .FirstOrDefaultAsync(po => po.SupplierId == viewModel.SupplierId, cancellationToken);

                    #endregion --Retrieve Supplier

                    #region -- Get PO --

                    await _dbContext.PurchaseOrders
                        .Where(po => viewModel.POSeries != null && viewModel.POSeries.Contains(po.PurchaseOrderNo))
                        .FirstOrDefaultAsync(cancellationToken: cancellationToken);

                    #endregion -- Get PO --

                    #region --Saving the default entries

                    var generateCvNo = await _checkVoucherRepo.GenerateCVNo(cancellationToken);
                    var cashInBank = viewModel.Credit[1];
                    var cvh = new CheckVoucherHeader
                    {
                        CheckVoucherHeaderNo = generateCvNo,
                        Date = viewModel.TransactionDate,
                        PONo = viewModel.POSeries,
                        SupplierId = viewModel.SupplierId,
                        Particulars = viewModel.Particulars,
                        BankId = viewModel.BankId,
                        CheckNo = viewModel.CheckNo,
                        Category = "Trade",
                        Payee = viewModel.Payee,
                        CheckDate = viewModel.CheckDate,
                        Total = cashInBank,
                        CreatedBy = createdBy,
                        CvType = "Supplier",
                        // Address = supplier.SupplierAddress,
                        // Tin = supplier.SupplierTin,
                    };

                    await _dbContext.CheckVoucherHeaders.AddAsync(cvh, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    #endregion --Saving the default entries

                    #region --CV Details Entry

                    var cvDetails = new List<CheckVoucherDetail>();
                    for (int i = 0; i < viewModel.AccountNumber.Length; i++)
                    {
                        if (viewModel.Debit[i] != 0 || viewModel.Credit[i] != 0)
                        {
                            cvDetails.Add(
                            new CheckVoucherDetail
                            {
                                AccountNo = viewModel.AccountNumber[i],
                                AccountName = viewModel.AccountTitle[i],
                                Debit = viewModel.Debit[i],
                                Credit = viewModel.Credit[i],
                                TransactionNo = cvh.CheckVoucherHeaderNo,
                                CheckVoucherHeaderId = cvh.CheckVoucherHeaderId,
                                SupplierId = i == 0 ? viewModel.SupplierId : null
                            });
                        }
                    }

                    await _dbContext.CheckVoucherDetails.AddRangeAsync(cvDetails, cancellationToken);

                    #endregion --CV Details Entry

                    #region -- Partial payment of RR's

                    var cvTradePaymentModel = new List<CVTradePayment>();
                    foreach (var item in viewModel.RRs)
                    {
                        var getReceivingReport = await _dbContext.ReceivingReports.FirstOrDefaultAsync(x => x.ReceivingReportId == item.Id, cancellationToken);
                        getReceivingReport!.AmountPaid += item.Amount;

                        cvTradePaymentModel.Add(
                            new CVTradePayment
                            {
                                DocumentId = getReceivingReport.ReceivingReportId,
                                DocumentType = "RR",
                                CheckVoucherId = cvh.CheckVoucherHeaderId,
                                AmountPaid = item.Amount
                            });
                    }

                    await _dbContext.AddRangeAsync(cvTradePaymentModel, cancellationToken);

                    #endregion -- Partial payment of RR's

                    #region -- Uploading file --

                    if (file?.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Supporting CV Files",
                            cvh.CheckVoucherHeaderNo);

                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        var fileName = Path.GetFileName(file.FileName);
                        var fileSavePath = Path.Combine(uploadsFolder, fileName);

                        await using FileStream stream = new FileStream(fileSavePath, FileMode.Create);
                        await file.CopyToAsync(stream, cancellationToken);

                        //if necessary add field to store location path
                        // model.Header.SupportingFilePath = fileSavePath
                    }

                    #region --Audit Trail Recording

                    if (cvh.OriginalSeriesNumber.IsNullOrEmpty() && cvh.OriginalDocumentId == 0)
                    {
                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        AuditTrail auditTrailBook = new(createdBy,
                            $"Create new check voucher# {cvh.CheckVoucherHeaderNo}", "Check Voucher Trade", ipAddress!);
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                    }

                    #endregion --Audit Trail Recording

                    TempData["success"] = "Check voucher trade created successfully";
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return RedirectToAction(nameof(Index));

                    #endregion -- Uploading file --
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create check voucher. Error: {ErrorMessage}, Stack: {StackTrace}. Created by: {UserName}",
                        ex.Message, ex.StackTrace, createdBy);
                    viewModel.COA = await _dbContext.ChartOfAccounts
                        .Where(coa => !new[] { "202010200", "202010100", "101010100" }.Any(excludedNumber => coa.AccountNumber != null && coa.AccountNumber.Contains(excludedNumber)) && !coa.HasChildren)
                        .Select(s => new SelectListItem
                        {
                            Value = s.AccountNumber,
                            Text = s.AccountNumber + " " + s.AccountName
                        })
                        .ToListAsync(cancellationToken);

                    viewModel.Suppliers = await _dbContext.Suppliers
                            .Where(supp => supp.Category == "Trade")
                            .Select(sup => new SelectListItem
                            {
                                Value = sup.SupplierId.ToString(),
                                Text = sup.SupplierName
                            })
                            .ToListAsync(cancellationToken: cancellationToken);

                    viewModel.PONo = await _dbContext.PurchaseOrders
                                .Where(po => po.SupplierId == viewModel.SupplierId && po.IsPosted)
                                .Select(po => new SelectListItem
                                {
                                    Value = po.PurchaseOrderNo!.ToString(),
                                    Text = po.PurchaseOrderNo
                                })
                                .ToListAsync(cancellationToken);

                    viewModel.BankAccounts = await _dbContext.BankAccounts
                        .Select(ba => new SelectListItem
                        {
                            Value = ba.BankAccountId.ToString(),
                            Text = ba.Bank + " " + ba.AccountName
                        })
                        .ToListAsync(cancellationToken: cancellationToken);

                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(viewModel);
                }
            }
            viewModel.COA = await _dbContext.ChartOfAccounts
                .Where(coa => !new[] { "202010200", "202010100", "101010100" }.Any(excludedNumber => coa.AccountNumber != null && coa.AccountNumber.Contains(excludedNumber)) && !coa.HasChildren)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            viewModel.Suppliers = await _dbContext.Suppliers
                .Where(supp => supp.Category == "Trade")
                .Select(sup => new SelectListItem
                {
                    Value = sup.SupplierId.ToString(),
                    Text = sup.SupplierName
                })
                .ToListAsync(cancellationToken: cancellationToken);

            viewModel.PONo = await _dbContext.PurchaseOrders
                .Where(po => po.SupplierId == viewModel.SupplierId && po.IsPosted)
                .Select(po => new SelectListItem
                {
                    Value = po.PurchaseOrderNo!.ToString(),
                    Text = po.PurchaseOrderNo
                })
                .ToListAsync(cancellationToken);

            viewModel.BankAccounts = await _dbContext.BankAccounts
                .Select(ba => new SelectListItem
                {
                    Value = ba.BankAccountId.ToString(),
                    Text = ba.Bank + " " + ba.AccountName
                })
                .ToListAsync(cancellationToken: cancellationToken);

            TempData["error"] = "The information provided was invalid.";
            return View(viewModel);
        }

        public async Task<IActionResult> GetPOs(int supplierId)
        {
            var purchaseOrders = await _dbContext.PurchaseOrders
                .Where(po => po.SupplierId == supplierId && po.IsPosted)
                .ToListAsync();

            if (purchaseOrders.Any())
            {
                var poList = purchaseOrders.OrderBy(po => po.PurchaseOrderNo)
                                        .Select(po => new { Id = po.PurchaseOrderId, PONumber = po.PurchaseOrderNo })
                                        .ToList();
                return Json(poList);
            }

            return Json(null);
        }

        public async Task<IActionResult> GetRRs(string[] poNumber, int? cvId, CancellationToken cancellationToken)
        {
            var query = _dbContext.ReceivingReports
                .Where(rr => !rr.IsPaid && rr.AmountPaid == 0 && poNumber.Contains(rr.PONo) && rr.PostedBy != null);

            if (cvId != null)
            {
                var rrIds = await _dbContext.CVTradePayments
                    .Where(cvp => cvp.CheckVoucherId == cvId && cvp.DocumentType == "RR")
                    .Select(cvp => cvp.DocumentId)
                    .ToListAsync(cancellationToken);

                query = query.Union(_dbContext.ReceivingReports
                    .Where(rr => poNumber.Contains(rr.PONo) && rrIds.Contains(rr.ReceivingReportId)));
            }

            var receivingReports = await query
                .Include(rr => rr.PurchaseOrder)
                .ThenInclude(rr => rr!.Supplier)
                .OrderBy(rr => rr.PurchaseOrder!.PurchaseOrderNo)
                .ToListAsync(cancellationToken);

            if (!receivingReports.Any())
            {
                return Json(null);
            }

            var rrList = receivingReports
                .Select(rr =>
                {
                    var netOfVatAmount = rr.PurchaseOrder?.Supplier?.VatType == CS.VatType_Vatable
                        ? _generalRepo.ComputeNetOfVat(rr.Amount)
                        : rr.Amount;

                    var ewtAmount = rr.PurchaseOrder?.Supplier?.TaxType == CS.TaxType_WithTax
                        ? _generalRepo.ComputeEwtAmount(netOfVatAmount, 0.01m)
                        : 0.0000m;

                    var netOfEwtAmount = rr.PurchaseOrder?.Supplier?.TaxType == CS.TaxType_WithTax
                        ? _generalRepo.ComputeNetOfEwt(rr.Amount, ewtAmount)
                        : rr.Amount;

                    return new
                    {
                        Id = rr.ReceivingReportId,
                        rr.ReceivingReportNo,
                        rr.PurchaseOrder?.PurchaseOrderNo,
                        AmountPaid = rr.AmountPaid.ToString(CS.Four_Decimal_Format),
                        NetOfEwtAmount = netOfEwtAmount.ToString(CS.Four_Decimal_Format)
                    };
                }).ToList();

            return Json(rrList);
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
                        Name = supplier.SupplierName,
                        Address = supplier.SupplierAddress,
                        TinNo = supplier.SupplierTin,
                        supplier.TaxType,
                        supplier.Category,
                        TaxPercent = supplier.WithholdingTaxPercent,
                        supplier.VatType,
                        DefaultExpense = supplier.DefaultExpenseNumber,
                        WithholdingTax = supplier.WithholdingTaxtitle
                    });
                }
            }
            return Json(null);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            var createdBy = await _generalRepo.GetUserFullNameAsync(User.Identity!.Name!);

            var existingHeaderModel = await _dbContext.CheckVoucherHeaders
                .FirstOrDefaultAsync(cvh => cvh.CheckVoucherHeaderId == id, cancellationToken);

            var existingDetailsModel = await _dbContext.CheckVoucherDetails
                .Where(cvd => cvd.CheckVoucherHeaderId == existingHeaderModel!.CheckVoucherHeaderId)
                .ToListAsync(cancellationToken);

            if (existingHeaderModel == null || !existingDetailsModel.Any())
            {
                return NotFound();
            }

            CheckVoucherTradeViewModel model = new()
            {
                SupplierId = existingHeaderModel.SupplierId ?? 0,
                Payee = existingHeaderModel.Payee!,
                // SupplierAddress = existingHeaderModel.Address,
                // SupplierTinNo = existingHeaderModel.Tin,
                POSeries = existingHeaderModel.PONo,
                TransactionDate = existingHeaderModel.Date,
                BankId = existingHeaderModel.BankId,
                CheckNo = existingHeaderModel.CheckNo!,
                CheckDate = existingHeaderModel.CheckDate ?? DateOnly.MinValue,
                Particulars = existingHeaderModel.Particulars!,
                CVId = existingHeaderModel.CheckVoucherHeaderId,
                CVNo = existingHeaderModel.CheckVoucherHeaderNo,
                CreatedBy = createdBy,
                RRs = new List<ReceivingReportList>(),
                Suppliers = await _dbContext.Suppliers
                    .Where(supp => supp.Category == "Trade")
                    .OrderBy(supp => supp.Number)
                    .Select(sup => new SelectListItem
                    {
                        Value = sup.SupplierId.ToString(),
                        Text = sup.SupplierName
                    })
                    .ToListAsync(cancellationToken: cancellationToken)
            };

            var getCheckVoucherTradePayment = await _dbContext.CVTradePayments
                .Where(cv => cv.CheckVoucherId == id && cv.DocumentType == "RR")
                .ToListAsync(cancellationToken);

            foreach (var item in getCheckVoucherTradePayment)
            {
                model.RRs.Add(new ReceivingReportList
                {
                    Id = item.DocumentId,
                    Amount = item.AmountPaid
                });
            }

            model.COA = await _dbContext.ChartOfAccounts
                .Where(coa => !new[] { "202010200", "202010100", "101010100" }.Any(excludedNumber => coa.AccountNumber != null && coa.AccountNumber.Contains(excludedNumber)) && !coa.HasChildren)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            model.PONo = await _dbContext.PurchaseOrders
                .OrderBy(s => s.PurchaseOrderNo)
                .Select(s => new SelectListItem
                {
                    Value = s.PurchaseOrderNo,
                    Text = s.PurchaseOrderNo
                })
                .ToListAsync(cancellationToken);

            model.BankAccounts = await _dbContext.BankAccounts
                .Select(ba => new SelectListItem
                {
                    Value = ba.BankAccountId.ToString(),
                    Text = ba.Bank + " " + ba.AccountName
                })
                .ToListAsync(cancellationToken: cancellationToken);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(CheckVoucherTradeViewModel viewModel, IFormFile? file, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                var existingHeaderModel = await _dbContext.CheckVoucherHeaders.FirstOrDefaultAsync(cv => cv.CheckVoucherHeaderId == viewModel.CVId, cancellationToken);
                var createdBy = await _generalRepo.GetUserFullNameAsync(User.Identity!.Name!);

                try
                {
                    #region --CV Details Entry

                    var existingDetailsModel = await _dbContext.CheckVoucherDetails
                        .Where(d => d.CheckVoucherHeaderId == existingHeaderModel!.CheckVoucherHeaderId)
                        .ToListAsync(cancellationToken: cancellationToken);

                    _dbContext.RemoveRange(existingDetailsModel);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    var details = new List<CheckVoucherDetail>();

                    var cashInBank = 0m;
                    for (int i = 0; i < viewModel.AccountTitle.Length; i++)
                    {
                        cashInBank = viewModel.Credit[1];
                        var getOriginalDocumentId =
                            existingDetailsModel.FirstOrDefault(x => x.AccountName == viewModel.AccountTitle[i]);

                        details.Add(new CheckVoucherDetail
                        {
                            AccountNo = viewModel.AccountNumber[i],
                            AccountName = viewModel.AccountTitle[i],
                            Debit = viewModel.Debit[i],
                            Credit = viewModel.Credit[i],
                            TransactionNo = existingHeaderModel!.CheckVoucherHeaderNo!,
                            CheckVoucherHeaderId = viewModel.CVId,
                            SupplierId = i == 0 ? viewModel.SupplierId : null,
                            OriginalDocumentId = getOriginalDocumentId?.OriginalDocumentId
                        });
                    }

                    await _dbContext.CheckVoucherDetails.AddRangeAsync(details, cancellationToken);

                    #endregion --CV Details Entry

                    #region --Saving the default entries

                    existingHeaderModel!.Date = viewModel.TransactionDate;
                    existingHeaderModel.PONo = viewModel.POSeries;
                    existingHeaderModel.SupplierId = viewModel.SupplierId;
                    // existingHeaderModel.Address = viewModel.SupplierAddress;
                    // existingHeaderModel.Tin = viewModel.SupplierTinNo;
                    existingHeaderModel.Particulars = viewModel.Particulars;
                    existingHeaderModel.BankId = viewModel.BankId;
                    existingHeaderModel.CheckNo = viewModel.CheckNo;
                    existingHeaderModel.Category = "Trade";
                    existingHeaderModel.Payee = viewModel.Payee;
                    existingHeaderModel.CheckDate = viewModel.CheckDate;
                    existingHeaderModel.Total = cashInBank;
                    // existingHeaderModel.EditedBy = _userManager.GetUserName(User);
                    // existingHeaderModel.EditedDate = DateTimeHelper.GetCurrentPhilippineTime();

                    #endregion --Saving the default entries

                    #region -- Partial payment of RR's

                    var getCheckVoucherTradePayment = await _dbContext.CVTradePayments
                        .Where(cv => cv.CheckVoucherId == existingHeaderModel.CheckVoucherHeaderId && cv.DocumentType == "RR")
                        .ToListAsync(cancellationToken);

                    foreach (var item in getCheckVoucherTradePayment)
                    {
                        var recevingReport = await _dbContext.ReceivingReports.FirstOrDefaultAsync(x => x.ReceivingReportId == item.DocumentId, cancellationToken);

                        recevingReport!.AmountPaid -= item.AmountPaid;
                    }

                    _dbContext.RemoveRange(getCheckVoucherTradePayment);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    var cvTradePaymentModel = new List<CVTradePayment>();
                    foreach (var item in viewModel.RRs)
                    {
                        var getReceivingReport = await _dbContext.ReceivingReports.FirstOrDefaultAsync(x => x.ReceivingReportId == item.Id, cancellationToken);
                        getReceivingReport!.AmountPaid += item.Amount;

                        cvTradePaymentModel.Add(
                            new CVTradePayment
                            {
                                DocumentId = getReceivingReport.ReceivingReportId,
                                DocumentType = "RR",
                                CheckVoucherId = existingHeaderModel.CheckVoucherHeaderId,
                                AmountPaid = item.Amount
                            });
                    }

                    await _dbContext.AddRangeAsync(cvTradePaymentModel, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    #endregion -- Partial payment of RR's

                    #region -- Uploading file --

                    if (file?.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Supporting CV Files",
                            existingHeaderModel.CheckVoucherHeaderNo!);

                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        var fileName = Path.GetFileName(file.FileName);
                        var fileSavePath = Path.Combine(uploadsFolder, fileName);

                        await using FileStream stream = new FileStream(fileSavePath, FileMode.Create);
                        await file.CopyToAsync(stream, cancellationToken);

                        //if necessary add field to store location path
                        // model.Header.SupportingFilePath = fileSavePath
                    }

                    #endregion -- Uploading file --

                    #region --Audit Trail Recording

                    if (existingHeaderModel.OriginalSeriesNumber.IsNullOrEmpty() && existingHeaderModel.OriginalDocumentId == 0)
                    {
                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        AuditTrail auditTrailBook = new(createdBy,
                            $"Edited check voucher# {existingHeaderModel.CheckVoucherHeaderNo}", "Check Voucher Trade", ipAddress!);
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                    }

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);  // await the SaveChangesAsync method
                    await transaction.CommitAsync(cancellationToken);
                    TempData["success"] = "Trade edited successfully";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to edit check voucher. Error: {ErrorMessage}, Stack: {StackTrace}. Edited by: {UserName}",
                        ex.Message, ex.StackTrace, createdBy);
                    viewModel.COA = await _dbContext.ChartOfAccounts
                        .Where(coa => !new[] { "202010200", "202010100", "101010100" }.Any(excludedNumber => coa.AccountNumber != null && coa.AccountNumber.Contains(excludedNumber)) && !coa.HasChildren)
                        .Select(s => new SelectListItem
                        {
                            Value = s.AccountNumber,
                            Text = s.AccountNumber + " " + s.AccountName
                        })
                        .ToListAsync(cancellationToken);

                    viewModel.PONo = await _dbContext.PurchaseOrders
                        .OrderBy(s => s.PurchaseOrderNo)
                        .Select(s => new SelectListItem
                        {
                            Value = s.PurchaseOrderNo,
                            Text = s.PurchaseOrderNo
                        })
                        .ToListAsync(cancellationToken);

                    viewModel.BankAccounts = await _dbContext.BankAccounts
                        .Select(ba => new SelectListItem
                        {
                            Value = ba.BankAccountId.ToString(),
                            Text = ba.Bank + " " + ba.AccountName
                        })
                        .ToListAsync(cancellationToken: cancellationToken);

                    viewModel.Suppliers = await _dbContext.Suppliers
                            .OrderBy(s => s.Number)
                            .Select(s => new SelectListItem
                            {
                                Value = s.SupplierId.ToString(),
                                Text = s.Number + " " + s.SupplierName
                            })
                            .ToListAsync(cancellationToken);

                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(viewModel);
                }
            }

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
                .FirstOrDefaultAsync(cvh => cvh.CheckVoucherHeaderId == id.Value, cancellationToken);

            if (header == null)
            {
                return NotFound();
            }

            var details = await _dbContext.CheckVoucherDetails
                .Include(cvd => cvd.Supplier)
                .Where(cvd => cvd.CheckVoucherHeaderId == header.CheckVoucherHeaderId)
                .ToListAsync(cancellationToken);

            var getSupplier = await _dbContext.Suppliers
                .FirstOrDefaultAsync(x => x.SupplierId == supplierId, cancellationToken);

            if (header.Category == "Trade" && header.RRNo != null)
            {
                var siArray = new string[header.RRNo.Length];
                for (int i = 0; i < header.RRNo.Length; i++)
                {
                    var rrValue = header.RRNo[i];

                    var rr = await _dbContext.ReceivingReports
                                .FirstOrDefaultAsync(p => p.ReceivingReportNo == rrValue, cancellationToken: cancellationToken);

                    if (rr != null)
                    {
                        siArray[i] = rr.SupplierInvoiceNumber!;
                    }
                }

                ViewBag.SINoArray = siArray;
            }

            var viewModel = new CheckVoucherVM
            {
                Header = header,
                Details = details,
                Supplier = getSupplier
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Printed(int id, int? supplierId, CancellationToken cancellationToken)
        {
            var cv = await _dbContext.CheckVoucherHeaders.FirstOrDefaultAsync(x => x.CheckVoucherHeaderId == id, cancellationToken);
            if (!cv!.IsPrinted)
            {
                #region --Audit Trail Recording

                if (cv.OriginalSeriesNumber.IsNullOrEmpty() && cv.OriginalDocumentId == 0)
                {
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    AuditTrail auditTrailBook = new(cv.CreatedBy!,
                        $"Printed original copy of check voucher# {cv.CheckVoucherHeaderNo}", "Check Voucher Trade", ipAddress!);
                    await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                }

                #endregion --Audit Trail Recording

                cv.IsPrinted = true;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            return RedirectToAction(nameof(Print), new { id, supplierId });
        }

        public async Task<IActionResult> Post(int id, int? supplierId, CancellationToken cancellationToken)
        {
            var modelHeader = await _dbContext.CheckVoucherHeaders.FirstOrDefaultAsync(cv => cv.CheckVoucherHeaderId == id, cancellationToken);
            var modelDetails = await _dbContext.CheckVoucherDetails.Where(cvd => cvd.CheckVoucherHeaderId == modelHeader!.CheckVoucherHeaderId).ToListAsync(cancellationToken: cancellationToken);
            var supplierName = await _dbContext.Suppliers.Where(s => s.SupplierId == supplierId).Select(s => s.SupplierName).FirstOrDefaultAsync(cancellationToken);
            var createdBy = await _generalRepo.GetUserFullNameAsync(User.Identity!.Name!);

            if (modelHeader != null)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    if (!modelHeader.IsPosted)
                    {
                        modelHeader.IsPosted = true;
                        //modelHeader.Status = nameof(Status.Posted);

                        #region -- Recalculate payment of RR's or DR's

                        var getCheckVoucherTradePayment = await _dbContext.CVTradePayments
                            .Where(cv => cv.CheckVoucherId == id)
                            .Include(cv => cv.CV)
                            .ToListAsync(cancellationToken);

                        foreach (var item in getCheckVoucherTradePayment)
                        {
                            if (item.DocumentType == "RR")
                            {
                                var receivingReport = await _dbContext.ReceivingReports.FirstOrDefaultAsync(x => x.ReceivingReportId == item.DocumentId, cancellationToken);

                                receivingReport!.IsPaid = true;
                                receivingReport.PaidDate = DateTime.Now;
                            }
                        }

                        #endregion -- Recalculate payment of RR's or DR's

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
                                        Reference = modelHeader.CheckVoucherHeaderNo!,
                                        Description = modelHeader.Particulars!,
                                        AccountNo = account.AccountNumber,
                                        AccountTitle = account.AccountName,
                                        Debit = details.Debit,
                                        Credit = details.Credit,
                                        CreatedBy = modelHeader.CreatedBy,
                                        CreatedDate = modelHeader.CreatedDate,
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
                            var bank = _dbContext.BankAccounts.FirstOrDefault(model => model.BankAccountId == modelHeader.BankId);
                            disbursement.Add(
                                    new DisbursementBook
                                    {
                                        Date = modelHeader.Date,
                                        CVNo = modelHeader.CheckVoucherHeaderNo!,
                                        Payee = modelHeader.Payee ?? supplierName!,
                                        Amount = modelHeader.Total,
                                        Particulars = modelHeader.Particulars!,
                                        Bank = bank != null ? bank.Bank : "N/A",
                                        CheckNo = !string.IsNullOrEmpty(modelHeader.CheckNo) ? modelHeader.CheckNo : "N/A",
                                        CheckDate = modelHeader.CheckDate != null ? modelHeader.CheckDate?.ToString("MM/dd/yyyy")! : "N/A",
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

                        if (modelHeader.OriginalSeriesNumber.IsNullOrEmpty() && modelHeader.OriginalDocumentId == 0)
                        {
                            modelHeader.PostedBy = createdBy;
                            modelHeader.PostedDate = DateTime.Now;

                            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                            AuditTrail auditTrailBook = new(createdBy,
                                $"Posted check voucher# {modelHeader.CheckVoucherHeaderNo}", "Check Voucher Trade", ipAddress!);
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
                    _logger.LogError(ex, "Failed to post check voucher. Error: {ErrorMessage}, Stack: {StackTrace}. Posted by: {UserName}",
                        ex.Message, ex.StackTrace, createdBy);
                    await transaction.RollbackAsync(cancellationToken);

                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(Index));
                }
            }

            return NotFound();
        }

        public async Task<IActionResult> Cancel(int id, string? cancellationRemarks, CancellationToken cancellationToken)
        {
            var model = await _dbContext.CheckVoucherHeaders.FirstOrDefaultAsync(x => x.CheckVoucherHeaderId == id, cancellationToken);

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            var createdBy = await _generalRepo.GetUserFullNameAsync(User.Identity!.Name!);

            try
            {
                if (model != null)
                {
                    if (!model.IsCanceled)
                    {
                        model.CanceledBy = createdBy;
                        model.CanceledDate = DateTime.Now;
                        model.IsCanceled = true;
                        //model.Status = nameof(Status.Canceled);
                        model.CancellationRemarks = cancellationRemarks;

                        #region -- Recalculate payment of RR's or DR's

                        var getCheckVoucherTradePayment = await _dbContext.CVTradePayments
                            .Where(cv => cv.CheckVoucherId == id)
                            .Include(cv => cv.CV)
                            .ToListAsync(cancellationToken);

                        foreach (var item in getCheckVoucherTradePayment)
                        {
                            if (item.DocumentType == "RR")
                            {
                                var receivingReport = await _dbContext.ReceivingReports.FirstOrDefaultAsync(x => x.ReceivingReportId == item.DocumentId, cancellationToken);

                                receivingReport!.IsPaid = false;
                                receivingReport.AmountPaid -= item.AmountPaid;
                            }
                        }

                        #endregion -- Recalculate payment of RR's or DR's

                        #region --Audit Trail Recording

                        if (model.OriginalSeriesNumber.IsNullOrEmpty() && model.OriginalDocumentId == 0)
                        {
                            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                            AuditTrail auditTrailBook = new(createdBy,
                                $"Canceled check voucher# {model.CheckVoucherHeaderNo}", "Check Voucher Trade", ipAddress!);
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
                _logger.LogError(ex, "Failed to cancel check voucher. Error: {ErrorMessage}, Stack: {StackTrace}. Canceled by: {UserName}",
                    ex.Message, ex.StackTrace, createdBy);
                TempData["error"] = $"Error: '{ex.Message}'";
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {
            var model = await _dbContext.CheckVoucherHeaders.FirstOrDefaultAsync(x => x.CheckVoucherHeaderId == id, cancellationToken);
            var createdBy = await _generalRepo.GetUserFullNameAsync(User.Identity!.Name!);

            if (model != null)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    if (!model.IsVoided)
                    {
                        if (model.IsPosted)
                        {
                            model.IsPosted = false;
                        }

                        model.VoidedBy = createdBy;
                        model.VoidedDate = DateTime.Now;
                        model.IsVoided = true;
                        //model.Status = nameof(Status.Voided);

                        await _generalRepo.RemoveRecords<DisbursementBook>(db => db.CVNo == model.CheckVoucherHeaderNo, cancellationToken);
                        await _generalRepo.RemoveRecords<GeneralLedgerBook>(gl => gl.Reference == model.CheckVoucherHeaderNo, cancellationToken);

                        //re-compute amount paid in trade and payment voucher
                        #region -- Recalculate payment of RR's or DR's

                        var getCheckVoucherTradePayment = await _dbContext.CVTradePayments
                            .Where(cv => cv.CheckVoucherId == id)
                            .Include(cv => cv.CV)
                            .ToListAsync(cancellationToken);

                        foreach (var item in getCheckVoucherTradePayment)
                        {
                            if (item.DocumentType == "RR")
                            {
                                var receivingReport = await _dbContext.ReceivingReports.FirstOrDefaultAsync(x => x.ReceivingReportId == item.DocumentId, cancellationToken);

                                receivingReport!.IsPaid = false;
                                receivingReport.AmountPaid -= item.AmountPaid;
                            }
                        }

                        #endregion -- Recalculate payment of RR's or DR's

                        #region --Audit Trail Recording

                        if (model.OriginalSeriesNumber.IsNullOrEmpty() && model.OriginalDocumentId == 0)
                        {
                            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                            AuditTrail auditTrailBook = new(createdBy,
                                $"Voided check voucher# {model.CheckVoucherHeaderNo}", "Check Voucher Trade", ipAddress!);
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
                    _logger.LogError(ex, "Failed to void check voucher. Error: {ErrorMessage}, Stack: {StackTrace}. Voided by: {UserName}",
                        ex.Message, ex.StackTrace, createdBy);
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(Index));
                }
            }

            return NotFound();
        }

        [HttpGet]
        public IActionResult GetAllCheckVoucherIds()
        {
            var cvIds = _dbContext.CheckVoucherHeaders
                                     .Select(cv => cv.CheckVoucherHeaderId) // Assuming Id is the primary key
                                     .ToList();

            return Json(cvIds);
        }

        [HttpPost]
        public async Task<IActionResult> GetCheckVoucherHeaderList(CancellationToken cancellationToken)
        {
            try
            {
                var checkVoucherHeaders = (await _checkVoucherRepo.GetCheckVouchersAsync(cancellationToken))
                    .Where(x => x.CvType != "Payment");

                return Json(new
                {
                    data = checkVoucherHeaders
                });
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        //Download as .xlsx file.(Export)

        #region -- export xlsx record --

        [HttpPost]
        public async Task<IActionResult> Export(string selectedRecord, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(selectedRecord))
            {
                // Handle the case where no invoices are selected
                return RedirectToAction(nameof(Index));
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var recordIds = selectedRecord.Split(',').Select(int.Parse).ToList();

                // Retrieve the selected invoices from the database
                var selectedList = await _dbContext.CheckVoucherHeaders
                    .Where(cvh => recordIds.Contains(cvh.CheckVoucherHeaderId))
                    .OrderBy(cvh => cvh.CheckVoucherHeaderNo)
                    .ToListAsync(cancellationToken);

                // Create the Excel package
                using var package = new ExcelPackage();
                // Add a new worksheet to the Excel package
                #region -- Purchase Order Table Header --

                var worksheet4 = package.Workbook.Worksheets.Add("PurchaseOrder");

                worksheet4.Cells["A1"].Value = "Date";
                worksheet4.Cells["B1"].Value = "Terms";
                worksheet4.Cells["C1"].Value = "Quantity";
                worksheet4.Cells["D1"].Value = "Price";
                worksheet4.Cells["E1"].Value = "Amount";
                worksheet4.Cells["F1"].Value = "FinalPrice";
                worksheet4.Cells["G1"].Value = "QuantityReceived";
                worksheet4.Cells["H1"].Value = "IsReceived";
                worksheet4.Cells["I1"].Value = "ReceivedDate";
                worksheet4.Cells["J1"].Value = "Remarks";
                worksheet4.Cells["K1"].Value = "CreatedBy";
                worksheet4.Cells["L1"].Value = "CreatedDate";
                worksheet4.Cells["M1"].Value = "IsClosed";
                worksheet4.Cells["N1"].Value = "CancellationRemarks";
                worksheet4.Cells["O1"].Value = "OriginalProductId";
                worksheet4.Cells["P1"].Value = "OriginalJVNo";
                worksheet4.Cells["Q1"].Value = "OriginalSupplierId";
                worksheet4.Cells["R1"].Value = "OriginalDocumentId";

                #endregion -- Purchase Order Table Header --

                #region -- Receiving Report Table Header --

                var worksheet3 = package.Workbook.Worksheets.Add("ReceivingReport");

                worksheet3.Cells["A1"].Value = "Date";
                worksheet3.Cells["B1"].Value = "DueDate";
                worksheet3.Cells["C1"].Value = "SupplierInvoiceNumber";
                worksheet3.Cells["D1"].Value = "SupplierInvoiceDate";
                worksheet3.Cells["E1"].Value = "TruckOrVessels";
                worksheet3.Cells["F1"].Value = "QuantityDelivered";
                worksheet3.Cells["G1"].Value = "QuantityReceived";
                worksheet3.Cells["H1"].Value = "GainOrLoss";
                worksheet3.Cells["I1"].Value = "Amount";
                worksheet3.Cells["J1"].Value = "OtherRef";
                worksheet3.Cells["K1"].Value = "Remarks";
                worksheet3.Cells["L1"].Value = "AmountPaid";
                worksheet3.Cells["M1"].Value = "IsPaid";
                worksheet3.Cells["N1"].Value = "PaidDate";
                worksheet3.Cells["O1"].Value = "CanceledQuantity";
                worksheet3.Cells["P1"].Value = "CreatedBy";
                worksheet3.Cells["Q1"].Value = "CreatedDate";
                worksheet3.Cells["R1"].Value = "CancellationRemarks";
                worksheet3.Cells["S1"].Value = "ReceivedDate";
                worksheet3.Cells["T1"].Value = "OriginalPOId";
                worksheet3.Cells["U1"].Value = "OriginalRRNo";
                worksheet3.Cells["V1"].Value = "OriginalDocumentId";

                #endregion -- Receiving Report Table Header --

                #region -- Check Voucher Header Table Header --

                var worksheet = package.Workbook.Worksheets.Add("CheckVoucherHeader");

                worksheet.Cells["A1"].Value = "TransactionDate";
                worksheet.Cells["B1"].Value = "ReceivingReportNo";
                worksheet.Cells["C1"].Value = "SalesInvoiceNo";
                worksheet.Cells["D1"].Value = "PurchaseOrderNo";
                worksheet.Cells["E1"].Value = "Particulars";
                worksheet.Cells["F1"].Value = "CheckNo";
                worksheet.Cells["G1"].Value = "Category";
                worksheet.Cells["H1"].Value = "Payee";
                worksheet.Cells["I1"].Value = "CheckDate";
                worksheet.Cells["J1"].Value = "StartDate";
                worksheet.Cells["K1"].Value = "EndDate";
                worksheet.Cells["L1"].Value = "NumberOfMonths";
                worksheet.Cells["M1"].Value = "NumberOfMonthsCreated";
                worksheet.Cells["N1"].Value = "LastCreatedDate";
                worksheet.Cells["O1"].Value = "AmountPerMonth";
                worksheet.Cells["P1"].Value = "IsComplete";
                worksheet.Cells["Q1"].Value = "AccruedType";
                worksheet.Cells["R1"].Value = "Reference";
                worksheet.Cells["S1"].Value = "CreatedBy";
                worksheet.Cells["T1"].Value = "CreatedDate";
                worksheet.Cells["U1"].Value = "Total";
                worksheet.Cells["V1"].Value = "Amount";
                worksheet.Cells["W1"].Value = "CheckAmount";
                worksheet.Cells["X1"].Value = "CVType";
                worksheet.Cells["Y1"].Value = "AmountPaid";
                worksheet.Cells["Z1"].Value = "IsPaid";
                worksheet.Cells["AA1"].Value = "CancellationRemarks";
                worksheet.Cells["AB1"].Value = "OriginalBankId";
                worksheet.Cells["AC1"].Value = "OriginalCVNo";
                worksheet.Cells["AD1"].Value = "OriginalSupplierId";
                worksheet.Cells["AE1"].Value = "OriginalDocumentId";

                #endregion -- Check Voucher Header Table Header --

                #region -- Check Voucher Details Table Header--

                var worksheet2 = package.Workbook.Worksheets.Add("CheckVoucherDetails");

                worksheet2.Cells["A1"].Value = "AccountNo";
                worksheet2.Cells["B1"].Value = "AccountName";
                worksheet2.Cells["C1"].Value = "TransactionNo";
                worksheet2.Cells["D1"].Value = "Debit";
                worksheet2.Cells["E1"].Value = "Credit";
                worksheet2.Cells["F1"].Value = "CVHeaderId";
                worksheet2.Cells["G1"].Value = "OriginalDocumentId";

                #endregion -- Check Voucher Details Table Header --

                #region -- Check Voucher Trade Payments Table Header --

                var worksheet5 = package.Workbook.Worksheets.Add("CheckVoucherTradePayments");

                worksheet5.Cells["A1"].Value = "Id";
                worksheet5.Cells["B1"].Value = "DocumentId";
                worksheet5.Cells["C1"].Value = "DocumentType";
                worksheet5.Cells["D1"].Value = "CheckVoucherId";
                worksheet5.Cells["E1"].Value = "AmountPaid";

                #endregion -- Check Voucher Header Table Header --

                #region -- Check Voucher Multiple Payment Table Header --

                var worksheet6 = package.Workbook.Worksheets.Add("MultipleCheckVoucherPayments");

                worksheet6.Cells["A1"].Value = "Id";
                worksheet6.Cells["B1"].Value = "CheckVoucherHeaderPaymentId";
                worksheet6.Cells["C1"].Value = "CheckVoucherHeaderInvoiceId";
                worksheet6.Cells["D1"].Value = "AmountPaid";

                #endregion

                #region -- Check Voucher Header Export (Trade and Invoicing)--

                int row = 2;

                foreach (var item in selectedList)
                {
                    worksheet.Cells[row, 1].Value = item.Date.ToString("yyyy-MM-dd");
                    if (item.RRNo != null && !item.RRNo.Contains(null))
                    {
                        worksheet.Cells[row, 2].Value = string.Join(", ", item.RRNo.Select(rrNo => rrNo.ToString()));
                    }
                    if (item.SINo != null && !item.SINo.Contains(null))
                    {
                        worksheet.Cells[row, 3].Value = string.Join(", ", item.SINo.Select(siNo => siNo.ToString()));
                    }
                    if (item.PONo != null && !item.PONo.Contains(null))
                    {
                        worksheet.Cells[row, 4].Value = string.Join(", ", item.PONo.Select(poNo => poNo.ToString()));
                    }

                    worksheet.Cells[row, 5].Value = item.Particulars;
                    worksheet.Cells[row, 6].Value = item.CheckNo;
                    worksheet.Cells[row, 7].Value = item.Category;
                    worksheet.Cells[row, 8].Value = item.Payee;
                    worksheet.Cells[row, 9].Value = item.CheckDate?.ToString("yyyy-MM-dd");
                    worksheet.Cells[row, 10].Value = item.StartDate?.ToString("yyyy-MM-dd");
                    worksheet.Cells[row, 11].Value = item.EndDate?.ToString("yyyy-MM-dd");
                    worksheet.Cells[row, 12].Value = item.NumberOfMonths;
                    worksheet.Cells[row, 13].Value = item.NumberOfMonthsCreated;
                    worksheet.Cells[row, 14].Value = item.LastCreatedDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                    worksheet.Cells[row, 15].Value = item.AmountPerMonth;
                    worksheet.Cells[row, 16].Value = item.IsComplete;
                    worksheet.Cells[row, 17].Value = item.AccruedType;
                    worksheet.Cells[row, 18].Value = item.Reference;
                    worksheet.Cells[row, 19].Value = item.CreatedBy;
                    worksheet.Cells[row, 20].Value = item.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                    worksheet.Cells[row, 21].Value = item.Total;
                    if (item.Amount != null)
                    {
                        worksheet.Cells[row, 22].Value = string.Join(" ", item.Amount.Select(amount => amount.ToString("N4")));
                    }
                    worksheet.Cells[row, 23].Value = item.CheckAmount;
                    worksheet.Cells[row, 24].Value = item.CvType;
                    worksheet.Cells[row, 25].Value = item.AmountPaid;
                    worksheet.Cells[row, 26].Value = item.IsPaid;
                    worksheet.Cells[row, 27].Value = item.CancellationRemarks;
                    worksheet.Cells[row, 28].Value = item.BankId;
                    worksheet.Cells[row, 29].Value = item.CheckVoucherHeaderNo;
                    worksheet.Cells[row, 30].Value = item.SupplierId;
                    worksheet.Cells[row, 31].Value = item.CheckVoucherHeaderId;
                    worksheet.Cells[row, 32].Value = item.PostedBy;
                    worksheet.Cells[row, 33].Value = item.PostedDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff") ?? null;

                    row++;
                }

                var getCheckVoucherTradePayment = await _dbContext.CVTradePayments
                    .Where(cv => recordIds.Contains(cv.CheckVoucherId) && cv.DocumentType == "RR")
                    .ToListAsync();

                int cvRow = 2;
                foreach (var payment in getCheckVoucherTradePayment)
                {
                    worksheet5.Cells[cvRow, 1].Value = payment.Id;
                    worksheet5.Cells[cvRow, 2].Value = payment.DocumentId;
                    worksheet5.Cells[cvRow, 3].Value = payment.DocumentType;
                    worksheet5.Cells[cvRow, 4].Value = payment.CheckVoucherId;
                    worksheet5.Cells[cvRow, 5].Value = payment.AmountPaid;

                    cvRow++;
                }

                #endregion -- Check Voucher Header Export (Trade and Invoicing) --

                #region -- Check Voucher Header Export (Payment) --

                var cvNos = selectedList.Select(item => item.CheckVoucherHeaderNo).ToList();

                var checkVoucherPayment = await _dbContext.CheckVoucherHeaders
                    .Where(cvh => cvh.Reference != null
                                  && cvNos.Any(cvNo =>
                                      EF.Functions.Like("," + cvh.Reference + ",", "%," + cvNo + ",%")))
                    .ToListAsync(cancellationToken);

                foreach (var item in checkVoucherPayment)
                {
                    worksheet.Cells[row, 1].Value = item.Date.ToString("yyyy-MM-dd");
                    if (item.RRNo != null && !item.RRNo.Contains(null))
                    {
                        worksheet.Cells[row, 2].Value = string.Join(", ", item.RRNo.Select(rrNo => rrNo.ToString()));
                    }
                    if (item.SINo != null && !item.SINo.Contains(null))
                    {
                        worksheet.Cells[row, 3].Value = string.Join(", ", item.SINo.Select(siNo => siNo.ToString()));
                    }
                    if (item.PONo != null && !item.PONo.Contains(null))
                    {
                        worksheet.Cells[row, 4].Value = string.Join(", ", item.PONo.Select(poNo => poNo.ToString()));
                    }

                    worksheet.Cells[row, 5].Value = item.Particulars;
                    worksheet.Cells[row, 6].Value = item.CheckNo;
                    worksheet.Cells[row, 7].Value = item.Category;
                    worksheet.Cells[row, 8].Value = item.Payee;
                    worksheet.Cells[row, 9].Value = item.CheckDate?.ToString("yyyy-MM-dd");
                    worksheet.Cells[row, 10].Value = item.StartDate?.ToString("yyyy-MM-dd");
                    worksheet.Cells[row, 11].Value = item.EndDate?.ToString("yyyy-MM-dd");
                    worksheet.Cells[row, 12].Value = item.NumberOfMonths;
                    worksheet.Cells[row, 13].Value = item.NumberOfMonthsCreated;
                    worksheet.Cells[row, 14].Value = item.LastCreatedDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                    worksheet.Cells[row, 15].Value = item.AmountPerMonth;
                    worksheet.Cells[row, 16].Value = item.IsComplete;
                    worksheet.Cells[row, 17].Value = item.AccruedType;
                    worksheet.Cells[row, 18].Value = item.Reference;
                    worksheet.Cells[row, 19].Value = item.CreatedBy;
                    worksheet.Cells[row, 20].Value = item.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                    worksheet.Cells[row, 21].Value = item.Total;
                    if (item.Amount != null)
                    {
                        worksheet.Cells[row, 22].Value = string.Join(" ", item.Amount.Select(amount => amount.ToString("N4")));
                    }
                    worksheet.Cells[row, 23].Value = item.CheckAmount;
                    worksheet.Cells[row, 24].Value = item.CvType;
                    worksheet.Cells[row, 25].Value = item.AmountPaid;
                    worksheet.Cells[row, 26].Value = item.IsPaid;
                    worksheet.Cells[row, 27].Value = item.CancellationRemarks;
                    worksheet.Cells[row, 28].Value = item.BankId;
                    worksheet.Cells[row, 29].Value = item.CheckVoucherHeaderNo;
                    worksheet.Cells[row, 30].Value = item.SupplierId;
                    worksheet.Cells[row, 31].Value = item.CheckVoucherHeaderId;
                    worksheet.Cells[row, 32].Value = item.PostedBy;
                    worksheet.Cells[row, 33].Value = item.PostedDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff") ?? null;

                    row++;
                }

                var cvPaymentId = checkVoucherPayment.Select(cvn => cvn.CheckVoucherHeaderId).ToList();
                var getCheckVoucherMultiplePayment = await _dbContext.MultipleCheckVoucherPayments
                    .Where(cv => cvPaymentId.Contains(cv.CheckVoucherHeaderPaymentId))
                    .ToListAsync();

                int cvn = 2;
                foreach (var payment in getCheckVoucherMultiplePayment)
                {
                    worksheet6.Cells[cvn, 1].Value = payment.Id;
                    worksheet6.Cells[cvn, 2].Value = payment.CheckVoucherHeaderPaymentId;
                    worksheet6.Cells[cvn, 3].Value = payment.CheckVoucherHeaderInvoiceId;
                    worksheet6.Cells[cvn, 4].Value = payment.AmountPaid;

                    cvn++;
                }

                #endregion -- Check Voucher Header Export (Payment) --

                #region -- Check Voucher Details Export (Trade and Invoicing) --

                var getCvDetails = await _dbContext.CheckVoucherDetails
                    .Where(cvd => cvNos.Contains(cvd.TransactionNo))
                    .OrderBy(cvd => cvd.CheckVoucherHeaderId)
                    .ToListAsync();

                var cvdRow = 2;

                foreach (var item in getCvDetails)
                {
                    worksheet2.Cells[cvdRow, 1].Value = item.AccountNo;
                    worksheet2.Cells[cvdRow, 2].Value = item.AccountName;
                    worksheet2.Cells[cvdRow, 3].Value = item.TransactionNo;
                    worksheet2.Cells[cvdRow, 4].Value = item.Debit;
                    worksheet2.Cells[cvdRow, 5].Value = item.Credit;
                    worksheet2.Cells[cvdRow, 6].Value = item.CheckVoucherHeaderId;
                    worksheet2.Cells[cvdRow, 7].Value = item.CheckVoucherDetailId;
                    worksheet2.Cells[cvdRow, 8].Value = item.Amount;
                    worksheet2.Cells[cvdRow, 9].Value = item.AmountPaid;
                    worksheet2.Cells[cvdRow, 10].Value = item.SupplierId;
                    worksheet2.Cells[cvdRow, 11].Value = item.EwtPercent;
                    worksheet2.Cells[cvdRow, 12].Value = item.IsUserSelected;
                    worksheet2.Cells[cvdRow, 13].Value = item.IsVatable;

                    cvdRow++;
                }

                #endregion -- Check Voucher Details Export (Trade and Invoicing) --

                #region -- Check Voucher Details Export (Payment) --

                var getCvPaymentDetails = await _dbContext.CheckVoucherDetails
                    .Where(cvd => checkVoucherPayment.Select(cvh => cvh.CheckVoucherHeaderNo).Contains(cvd.TransactionNo))
                    .OrderBy(cvd => cvd.CheckVoucherHeaderId)
                    .ToListAsync();

                foreach (var item in getCvPaymentDetails)
                {
                    worksheet2.Cells[cvdRow, 1].Value = item.AccountNo;
                    worksheet2.Cells[cvdRow, 2].Value = item.AccountName;
                    worksheet2.Cells[cvdRow, 3].Value = item.TransactionNo;
                    worksheet2.Cells[cvdRow, 4].Value = item.Debit;
                    worksheet2.Cells[cvdRow, 5].Value = item.Credit;
                    worksheet2.Cells[cvdRow, 6].Value = item.CheckVoucherHeaderId;
                    worksheet2.Cells[cvdRow, 7].Value = item.CheckVoucherDetailId;
                    worksheet2.Cells[cvdRow, 8].Value = item.Amount;
                    worksheet2.Cells[cvdRow, 9].Value = item.AmountPaid;
                    worksheet2.Cells[cvdRow, 10].Value = item.SupplierId;
                    worksheet2.Cells[cvdRow, 11].Value = item.EwtPercent;
                    worksheet2.Cells[cvdRow, 12].Value = item.IsUserSelected;
                    worksheet2.Cells[cvdRow, 13].Value = item.IsVatable;

                    cvdRow++;
                }

                #endregion -- Check Voucher Details Export (Payment) --

                #region -- Receiving Report Export --

                var selectedIds = selectedList.Select(item => item.CheckVoucherHeaderId).ToList();

                var cvTradePaymentList = await _dbContext.CVTradePayments
                    .Where(p => selectedIds.Contains(p.CheckVoucherId))
                    .ToListAsync();

                var rrIds = cvTradePaymentList.Select(item => item.DocumentId).ToList();

                var getReceivingReport = await _dbContext.ReceivingReports
                    .Where(rr => rrIds.Contains(rr.ReceivingReportId))
                    .ToListAsync(cancellationToken);

                var rrRow = 2;

                foreach (var item in getReceivingReport)
                {
                    worksheet3.Cells[rrRow, 1].Value = item.Date.ToString("yyyy-MM-dd");
                    worksheet3.Cells[rrRow, 2].Value = item.DueDate.ToString("yyyy-MM-dd");
                    worksheet3.Cells[rrRow, 3].Value = item.SupplierInvoiceNumber;
                    worksheet3.Cells[rrRow, 4].Value = item.SupplierInvoiceDate;
                    worksheet3.Cells[rrRow, 5].Value = item.TruckOrVessels;
                    worksheet3.Cells[rrRow, 6].Value = item.QuantityDelivered;
                    worksheet3.Cells[rrRow, 7].Value = item.QuantityReceived;
                    worksheet3.Cells[rrRow, 8].Value = item.GainOrLoss;
                    worksheet3.Cells[rrRow, 9].Value = item.Amount;
                    worksheet3.Cells[rrRow, 10].Value = item.OtherRef;
                    worksheet3.Cells[rrRow, 11].Value = item.Remarks;
                    worksheet3.Cells[rrRow, 12].Value = item.AmountPaid;
                    worksheet3.Cells[rrRow, 13].Value = item.IsPaid;
                    worksheet3.Cells[rrRow, 14].Value = item.PaidDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                    worksheet3.Cells[rrRow, 15].Value = item.CanceledQuantity;
                    worksheet3.Cells[rrRow, 16].Value = item.CreatedBy;
                    worksheet3.Cells[rrRow, 17].Value = item.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                    worksheet3.Cells[rrRow, 18].Value = item.CancellationRemarks;
                    worksheet3.Cells[rrRow, 19].Value = item.ReceivedDate?.ToString("yyyy-MM-dd");
                    worksheet3.Cells[rrRow, 20].Value = item.POId;
                    worksheet3.Cells[rrRow, 21].Value = item.ReceivingReportNo;
                    worksheet3.Cells[rrRow, 22].Value = item.ReceivingReportId;

                    rrRow++;
                }

                #endregion -- Receiving Report Export --

                #region -- Purchase Order Export --

                var getPurchaseOrder = await _dbContext.PurchaseOrders
                    .Where(po => getReceivingReport.Select(item => item.POId).Contains(po.PurchaseOrderId))
                    .OrderBy(po => po.PurchaseOrderNo)
                    .ToListAsync(cancellationToken);

                int poRow = 2;

                foreach (var item in getPurchaseOrder)
                {
                    worksheet4.Cells[poRow, 1].Value = item.Date.ToString("yyyy-MM-dd");
                    worksheet4.Cells[poRow, 2].Value = item.Terms;
                    worksheet4.Cells[poRow, 3].Value = item.Quantity;
                    worksheet4.Cells[poRow, 4].Value = item.Price;
                    worksheet4.Cells[poRow, 5].Value = item.Amount;
                    worksheet4.Cells[poRow, 6].Value = item.FinalPrice;
                    worksheet4.Cells[poRow, 7].Value = item.QuantityReceived;
                    worksheet4.Cells[poRow, 8].Value = item.IsReceived;
                    worksheet4.Cells[poRow, 9].Value = item.ReceivedDate != default ? item.ReceivedDate.ToString("yyyy-MM-dd HH:mm:ss.ffffff zzz") : null;
                    worksheet4.Cells[poRow, 10].Value = item.Remarks;
                    worksheet4.Cells[poRow, 11].Value = item.CreatedBy;
                    worksheet4.Cells[poRow, 12].Value = item.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                    worksheet4.Cells[poRow, 13].Value = item.IsClosed;
                    worksheet4.Cells[poRow, 14].Value = item.CancellationRemarks;
                    worksheet4.Cells[poRow, 15].Value = item.ProductId;
                    worksheet4.Cells[poRow, 16].Value = item.PurchaseOrderNo;
                    worksheet4.Cells[poRow, 17].Value = item.SupplierId;
                    worksheet4.Cells[poRow, 18].Value = item.PurchaseOrderId;

                    poRow++;
                }

                #endregion -- Purchase Order Export --

                // Convert the Excel package to a byte array
                var excelBytes = await package.GetAsByteArrayAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"CheckVoucherList_IBS-RCD_{DateTime.Now:yyyyddMMHHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        #endregion -- export xlsx record --

        //Upload as .xlsx file.(Import)
        #region -- import xlsx record from IBS --

        [HttpPost]
        public async Task<IActionResult> Import(IFormFile file, CancellationToken cancellationToken)
        {
            if (file.Length == 0)
            {
                TempData["error"] = "The Excel file length is zero!.";
                return RedirectToAction(nameof(Index), new { view = DynamicView.CheckVoucher });
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                await using var stream = file.OpenReadStream();
                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "CheckVoucherHeader");

                var worksheet2 = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "CheckVoucherDetails");

                var worksheet3 = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "PurchaseOrder");

                var worksheet4 = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "ReceivingReport");

                var worksheet5 = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "CheckVoucherTradePayments");

                var worksheet6 = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "MultipleCheckVoucherPayments");

                if (worksheet == null)
                {
                    TempData["error"] = "The Excel file contains no worksheets of check voucher header.";
                    return RedirectToAction(nameof(Index), new { view = DynamicView.CheckVoucher });
                }

                if (worksheet2 == null)
                {
                    TempData["error"] = "The Excel file contains no worksheets of check voucher details.";
                    return RedirectToAction(nameof(Index), new { view = DynamicView.CheckVoucher });
                }

                if (worksheet.ToString() != "CheckVoucherHeader")
                {
                    TempData["error"] = "The Excel file is not related to check voucher.";
                    return RedirectToAction(nameof(Index), new { view = DynamicView.CheckVoucher });
                }

                #region Purchase Order Import

                if (worksheet3 != null)
                {
                    var rows = _purchaseOrderRepo.ParseWorksheet(worksheet3);
                    var lookup = await _purchaseOrderRepo.BuildLookupPurchaseOrderContextAsync(rows, cancellationToken);

                    var purchaseOrders = new List<PurchaseOrder>();
                    var auditTrails = new List<AuditTrail>();
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    var checkingDuplicateSeriesNo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var row in rows)
                    {
                        if (!lookup.ExistingPurchaseOrder.TryGetValue(row.OriginalSeriesNumber, out var existing))
                        {
                            if (!checkingDuplicateSeriesNo.Add(row.OriginalSeriesNumber))
                            {
                                continue;
                            }

                            purchaseOrders.Add(_purchaseOrderRepo.MapToPurchaseOrderEntity(row, lookup));
                            auditTrails.AddRange(_purchaseOrderRepo.AuditTrails(row, ipAddress ?? string.Empty));
                        }
                        else
                        {
                            var changes = _purchaseOrderRepo.Detect(existing, row, lookup.ExistingLogs);
                            if (changes.Any())
                            {
                                await _purchaseOrderRepo.LogChangesAsync(
                                    existing.OriginalDocumentId,
                                    changes,
                                    await _generalRepo.GetUserFullNameAsync(User.Identity!.Name!),
                                    existing.PurchaseOrderNo,
                                    "IBS-RCD");
                            }
                        }
                    }

                    _dbContext.PurchaseOrders.AddRange(purchaseOrders);
                    _dbContext.AuditTrails.AddRange(auditTrails);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }

                #endregion

                #region Receiving Report Import

                if (worksheet4 != null)
                {
                    var rows = _receivingReportRepo.ParseWorksheet(worksheet4);
                    var lookup = await _receivingReportRepo.BuildLookupReceivingReportContextAsync(rows, cancellationToken);

                    var receivingReports = new List<ReceivingReport>();
                    var auditTrails = new List<AuditTrail>();
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    var checkingDuplicateSeriesNo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var row in rows)
                    {
                        if (!lookup.ExistingReceivingReport.TryGetValue(row.OriginalSeriesNumber, out var existing))
                        {
                            if (!checkingDuplicateSeriesNo.Add(row.OriginalSeriesNumber))
                            {
                                continue;
                            }

                            receivingReports.Add(_receivingReportRepo.MapToReceivingReportEntity(row, lookup));
                            auditTrails.AddRange(_receivingReportRepo.AuditTrails(row, ipAddress ?? string.Empty));
                        }
                        else
                        {
                            var changes = _receivingReportRepo.Detect(existing, row, lookup.ExistingLogs);
                            if (changes.Any())
                            {
                                await _receivingReportRepo.LogChangesAsync(
                                    existing.OriginalDocumentId,
                                    changes,
                                    await _generalRepo.GetUserFullNameAsync(User.Identity!.Name!),
                                    existing.ReceivingReportNo,
                                    "IBS-RCD");
                            }
                        }
                    }

                    _dbContext.ReceivingReports.AddRange(receivingReports);
                    _dbContext.AuditTrails.AddRange(auditTrails);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }

                #endregion

                #region Check Voucher Header Import

                if (worksheet != null!)
                {
                    var rows = await _checkVoucherRepo.ParseWorksheet(worksheet, cancellationToken);
                    var lookup = await _checkVoucherRepo.BuildLookupCheckVoucherHeaderContextAsync(rows, cancellationToken);

                    var checkVoucherHeaders = new List<CheckVoucherHeader>();
                    var auditTrails = new List<AuditTrail>();
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    var checkingDuplicateSeriesNo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    var getLastCheckVoucherHeaderNo = await _dbContext.CheckVoucherHeaders
                        .OrderByDescending(x => x.CheckVoucherHeaderNo)
                        .Select(x => x.CheckVoucherHeaderNo)
                        .FirstOrDefaultAsync(cancellationToken);

                    foreach (var row in rows.OrderBy(x => x.Date).ThenBy(x => x.CreatedDate))
                    {
                        if (!lookup.ExistingCheckVoucherHeader.TryGetValue(row.OriginalSeriesNumber, out var existing))
                        {
                            if (!checkingDuplicateSeriesNo.Add(row.OriginalSeriesNumber))
                            {
                                continue;
                            }

                            getLastCheckVoucherHeaderNo = _checkVoucherRepo.GenerateCodeForUploadingExcelFile(getLastCheckVoucherHeaderNo, cancellationToken);
                            checkVoucherHeaders.Add(_checkVoucherRepo.MapToCheckVoucherHeaderEntity(row, checkVoucherHeaders, lookup, getLastCheckVoucherHeaderNo));
                            auditTrails.AddRange(_checkVoucherRepo.AuditTrails(row, ipAddress ?? string.Empty));
                        }
                        else
                        {
                            var changes = _checkVoucherRepo.Detect(existing, row, lookup.ExistingLogs);
                            if (changes.Any())
                            {
                                await _checkVoucherRepo.LogChangesAsync(
                                    existing.OriginalDocumentId,
                                    changes,
                                    await _generalRepo.GetUserFullNameAsync(User.Identity!.Name!),
                                    existing.CheckVoucherHeaderNo!,
                                    "IBS-RCD");
                            }
                        }
                    }

                    _dbContext.CheckVoucherHeaders.AddRange(checkVoucherHeaders);
                    _dbContext.AuditTrails.AddRange(auditTrails);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }

                #endregion

                #region -- Check Voucher Trade Payment Import --

                if (worksheet5 != null)
                {
                    var rows = _checkVoucherRepo.ParseWorksheetCvTradePayment(worksheet5);
                    var lookup = await _checkVoucherRepo.BuildLookupCvTradePaymentContextAsync(rows, cancellationToken);

                    var cvTradePayments = new List<CVTradePayment>();

                    foreach (var row in rows)
                    {
                        cvTradePayments.Add(_checkVoucherRepo.MapToCvTradePaymentEntity(row, lookup));
                    }

                    _dbContext.CVTradePayments.AddRange(cvTradePayments);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }

                #endregion -- Check Voucher Trade Payment Import --

                #region -- Check Voucher Multiple Payment Import --

                if (worksheet6 != null)
                {
                    var rows = _checkVoucherRepo.ParseWorksheetCvMultiplePayment(worksheet6);
                    var lookup = await _checkVoucherRepo.BuildLookupCvMultiplePaymentContextAsync(rows, cancellationToken);

                    var multipleCheckVoucherPayments = new List<MultipleCheckVoucherPayment>();

                    foreach (var row in rows)
                    {
                        multipleCheckVoucherPayments.Add(_checkVoucherRepo.MapToCvMultiplePaymentEntity(row, lookup));
                    }

                    _dbContext.MultipleCheckVoucherPayments.AddRange(multipleCheckVoucherPayments);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }

                #endregion -- Check Voucher Multiple Payment Import --

                #region -- Check Voucher Details Import --

                if (worksheet2 != null)
                {
                    var rows = _checkVoucherRepo.ParseWorksheetCheckVoucherDetails(worksheet2);
                    var lookup = await _checkVoucherRepo.BuildLookupCheckVoucherDetailsContextAsync(rows, cancellationToken);

                    var checkVoucherDetails = new List<CheckVoucherDetail>();
                    var originalDocumentId = new HashSet<int>();

                    foreach (var row in rows)
                    {
                        if (!lookup.ExistingCheckVoucherDetail.TryGetValue(row.OriginalDocumentId, out var existing))
                        {
                            if (!originalDocumentId.Add(row.OriginalDocumentId))
                            {
                                continue;
                            }

                            checkVoucherDetails.Add(_checkVoucherRepo.MapToCheckVoucherDetailsEntity(row, lookup));
                        }
                        else
                        {
                            var changes = _checkVoucherRepo.DetectCvDetails(existing, row, lookup.ExistingLogs);
                            if (changes.Any())
                            {
                                await _checkVoucherRepo.LogChangesForCVDAsync(
                                    existing.OriginalDocumentId ?? 0,
                                    changes,
                                    await _generalRepo.GetUserFullNameAsync(User.Identity!.Name!),
                                    existing.CheckVoucherHeader!.CheckVoucherHeaderNo,
                                    "IBS-RCD");
                            }
                        }
                    }

                    _dbContext.CheckVoucherDetails.AddRange(checkVoucherDetails);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }

                #endregion -- Check Voucher Details Import --

                var checkChangesOfRecord = await _dbContext.ImportExportLogs
                    .Where(iel => iel.Action == string.Empty).ToListAsync(cancellationToken);
                if (checkChangesOfRecord.Any())
                {
                    TempData["importChanges"] = "";
                }

                await transaction.CommitAsync(cancellationToken);
            }
            catch (OperationCanceledException oce)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = oce.Message;
                return RedirectToAction(nameof(Index), new { view = DynamicView.CheckVoucher });
            }
            catch (InvalidOperationException ioe)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["warning"] = ioe.Message;
                return RedirectToAction(nameof(Index), new { view = DynamicView.CheckVoucher });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index), new { view = DynamicView.CheckVoucher });
            }

            TempData["success"] = "Uploading Success!";
            return RedirectToAction(nameof(Index), new { view = DynamicView.CheckVoucher });
        }

        #endregion

        //Upload as .xlsx file.(Import)
        #region -- import xlsx record to AAS --

        [HttpPost]
        public async Task<IActionResult> AasImport(IFormFile file, CancellationToken cancellationToken)
        {
            if (file.Length == 0)
            {
                TempData["error"] = "The Excel file length is zero!.";
                return RedirectToAction(nameof(Index), new { view = DynamicView.CheckVoucher });
            }

            await using var transaction = await _aasDbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                await using var stream = file.OpenReadStream();
                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "CheckVoucherHeader");

                var worksheet2 = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "CheckVoucherDetails");

                var worksheet3 = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "PurchaseOrder");

                var worksheet4 = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "ReceivingReport");

                var worksheet5 = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "CheckVoucherTradePayments");

                var worksheet6 = package.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == "MultipleCheckVoucherPayments");

                if (worksheet == null)
                {
                    TempData["error"] = "The Excel file contains no worksheets of check voucher header.";
                    return RedirectToAction(nameof(Index), new { view = DynamicView.CheckVoucher });
                }

                if (worksheet2 == null)
                {
                    TempData["error"] = "The Excel file contains no worksheets of check voucher details.";
                    return RedirectToAction(nameof(Index), new { view = DynamicView.CheckVoucher });
                }

                if (worksheet.ToString() != "CheckVoucherHeader")
                {
                    TempData["error"] = "The Excel file is not related to check voucher.";
                    return RedirectToAction(nameof(Index), new { view = DynamicView.CheckVoucher });
                }

                #region Purchase Order Import

                if (worksheet3 != null)
                {
                    var rows = _purchaseOrderRepo.ParseWorksheet(worksheet3);
                    var lookup = await _purchaseOrderRepo.BuildLookupPurchaseOrderContextForAasAsync(rows, cancellationToken);

                    var purchaseOrders = new List<PurchaseOrder>();
                    var auditTrails = new List<AuditTrail>();
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    var checkingDuplicateSeriesNo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var row in rows)
                    {
                        if (!lookup.ExistingPurchaseOrder.TryGetValue(row.OriginalSeriesNumber, out var existing))
                        {
                            if (!checkingDuplicateSeriesNo.Add(row.OriginalSeriesNumber))
                            {
                                continue;
                            }

                            purchaseOrders.Add(_purchaseOrderRepo.MapToPurchaseOrderEntity(row, lookup));
                            auditTrails.AddRange(_purchaseOrderRepo.AuditTrails(row, ipAddress ?? string.Empty));
                        }
                        else
                        {
                            var changes = _purchaseOrderRepo.Detect(existing, row, lookup.ExistingLogs);
                            if (changes.Any())
                            {
                                await _purchaseOrderRepo.LogChangesAsync(
                                    existing.OriginalDocumentId,
                                    changes,
                                    await _generalRepo.GetUserFullNameAsync(User.Identity!.Name!),
                                    existing.PurchaseOrderNo,
                                    "AAS");
                            }
                        }
                    }

                    _aasDbContext.PurchaseOrders.AddRange(purchaseOrders);
                    _aasDbContext.AuditTrails.AddRange(auditTrails);
                    await _aasDbContext.SaveChangesAsync(cancellationToken);
                }

                #endregion

                #region Receiving Report Import

                if (worksheet4 != null)
                {
                    var rows = _receivingReportRepo.ParseWorksheet(worksheet4);
                    var lookup = await _receivingReportRepo.BuildLookupReceivingReportContextForAasAsync(rows, cancellationToken);

                    var receivingReports = new List<ReceivingReport>();
                    var auditTrails = new List<AuditTrail>();
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    var checkingDuplicateSeriesNo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var row in rows)
                    {
                        if (!lookup.ExistingReceivingReport.TryGetValue(row.OriginalSeriesNumber, out var existing))
                        {
                            if (!checkingDuplicateSeriesNo.Add(row.OriginalSeriesNumber))
                            {
                                continue;
                            }

                            receivingReports.Add(_receivingReportRepo.MapToReceivingReportEntity(row, lookup));
                            auditTrails.AddRange(_receivingReportRepo.AuditTrails(row, ipAddress ?? string.Empty));
                        }
                        else
                        {
                            var changes = _receivingReportRepo.Detect(existing, row, lookup.ExistingLogs);
                            if (changes.Any())
                            {
                                await _receivingReportRepo.LogChangesAsync(
                                    existing.OriginalDocumentId,
                                    changes,
                                    await _generalRepo.GetUserFullNameAsync(User.Identity!.Name!),
                                    existing.ReceivingReportNo,
                                    "AAS");
                            }
                        }
                    }

                    _aasDbContext.ReceivingReports.AddRange(receivingReports);
                    _aasDbContext.AuditTrails.AddRange(auditTrails);
                    await _aasDbContext.SaveChangesAsync(cancellationToken);
                }

                #endregion

                #region Check Voucher Header Import

                if (worksheet != null!)
                {
                    var rows = await _checkVoucherRepo.ParseWorksheet(worksheet, cancellationToken);
                    var lookup = await _checkVoucherRepo.BuildLookupCheckVoucherHeaderContextForAasAsync(rows, cancellationToken);

                    var checkVoucherHeaders = new List<CheckVoucherHeader>();
                    var auditTrails = new List<AuditTrail>();
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    var checkingDuplicateSeriesNo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    var getLastCheckVoucherHeaderNo = await _aasDbContext.CheckVoucherHeaders
                        .OrderByDescending(x => x.CheckVoucherHeaderNo)
                        .Select(x => x.CheckVoucherHeaderNo)
                        .FirstOrDefaultAsync(cancellationToken);

                    foreach (var row in rows.OrderBy(x => x.Date).ThenBy(x => x.CreatedDate))
                    {
                        if (!lookup.ExistingCheckVoucherHeader.TryGetValue(row.OriginalSeriesNumber, out var existing))
                        {
                            if (!checkingDuplicateSeriesNo.Add(row.OriginalSeriesNumber))
                            {
                                continue;
                            }

                            getLastCheckVoucherHeaderNo = _checkVoucherRepo.GenerateCodeForUploadingExcelFile(getLastCheckVoucherHeaderNo, cancellationToken);
                            checkVoucherHeaders.Add(_checkVoucherRepo.MapToCheckVoucherHeaderEntity(row, checkVoucherHeaders, lookup, getLastCheckVoucherHeaderNo));
                            auditTrails.AddRange(_checkVoucherRepo.AuditTrails(row, ipAddress ?? string.Empty));
                        }
                        else
                        {
                            var changes = _checkVoucherRepo.Detect(existing, row, lookup.ExistingLogs);
                            if (changes.Any())
                            {
                                await _checkVoucherRepo.LogChangesAsync(
                                    existing.OriginalDocumentId,
                                    changes,
                                    await _generalRepo.GetUserFullNameAsync(User.Identity!.Name!),
                                    existing.CheckVoucherHeaderNo!,
                                    "AAS");
                            }
                        }
                    }

                    _aasDbContext.CheckVoucherHeaders.AddRange(checkVoucherHeaders);
                    _aasDbContext.AuditTrails.AddRange(auditTrails);
                    await _aasDbContext.SaveChangesAsync(cancellationToken);
                }

                #endregion

                #region -- Check Voucher Trade Payment Import --

                if (worksheet5 != null)
                {
                    var rows = _checkVoucherRepo.ParseWorksheetCvTradePayment(worksheet5);
                    var lookup = await _checkVoucherRepo.BuildLookupCvTradePaymentContextForAasAsync(rows, cancellationToken);

                    var cvTradePayments = new List<CVTradePayment>();

                    foreach (var row in rows)
                    {
                        cvTradePayments.Add(_checkVoucherRepo.MapToCvTradePaymentEntity(row, lookup));
                    }

                    _aasDbContext.CVTradePayments.AddRange(cvTradePayments);
                    await _aasDbContext.SaveChangesAsync(cancellationToken);
                }

                #endregion -- Check Voucher Trade Payment Import --

                #region -- Check Voucher Multiple Payment Import --

                if (worksheet6 != null)
                {
                    var rows = _checkVoucherRepo.ParseWorksheetCvMultiplePayment(worksheet6);
                    var lookup = await _checkVoucherRepo.BuildLookupCvMultiplePaymentContextForAasAsync(rows, cancellationToken);

                    var multipleCheckVoucherPayments = new List<MultipleCheckVoucherPayment>();

                    foreach (var row in rows)
                    {
                        multipleCheckVoucherPayments.Add(_checkVoucherRepo.MapToCvMultiplePaymentEntity(row, lookup));
                    }

                    _aasDbContext.MultipleCheckVoucherPayments.AddRange(multipleCheckVoucherPayments);
                    await _aasDbContext.SaveChangesAsync(cancellationToken);
                }

                #endregion -- Check Voucher Multiple Payment Import --

                #region -- Check Voucher Details Import --

                if (worksheet2 != null)
                {
                    var rows = _checkVoucherRepo.ParseWorksheetCheckVoucherDetails(worksheet2);
                    var lookup = await _checkVoucherRepo.BuildLookupCheckVoucherDetailsContextForAasAsync(rows, cancellationToken);

                    var checkVoucherDetails = new List<CheckVoucherDetail>();
                    var originalDocumentId = new HashSet<int>();

                    foreach (var row in rows)
                    {
                        if (!lookup.ExistingCheckVoucherDetail.TryGetValue(row.OriginalDocumentId, out var existing))
                        {
                            if (!originalDocumentId.Add(row.OriginalDocumentId))
                            {
                                continue;
                            }

                            checkVoucherDetails.Add(_checkVoucherRepo.MapToCheckVoucherDetailsEntity(row, lookup));
                        }
                        else
                        {
                            var changes = _checkVoucherRepo.DetectCvDetails(existing, row, lookup.ExistingLogs);
                            if (changes.Any())
                            {
                                await _checkVoucherRepo.LogChangesForCVDAsync(
                                    existing.OriginalDocumentId ?? 0,
                                    changes,
                                    await _generalRepo.GetUserFullNameAsync(User.Identity!.Name!),
                                    existing.CheckVoucherHeader!.CheckVoucherHeaderNo,
                                    "AAS");
                            }
                        }
                    }

                    _aasDbContext.CheckVoucherDetails.AddRange(checkVoucherDetails);
                    await _aasDbContext.SaveChangesAsync(cancellationToken);
                }

                #endregion -- Check Voucher Details Import --

                var checkChangesOfRecord = await _dbContext.ImportExportLogs
                    .Where(iel => iel.Action == string.Empty).ToListAsync(cancellationToken);
                if (checkChangesOfRecord.Any())
                {
                    TempData["importChanges"] = "";
                }

                await transaction.CommitAsync(cancellationToken);
            }
            catch (OperationCanceledException oce)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = oce.Message;
                return RedirectToAction(nameof(Index), new { view = DynamicView.CheckVoucher });
            }
            catch (InvalidOperationException ioe)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["warning"] = ioe.Message;
                return RedirectToAction(nameof(Index), new { view = DynamicView.CheckVoucher });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index), new { view = DynamicView.CheckVoucher });
            }

            TempData["success"] = "Uploading Success!";
            return RedirectToAction(nameof(Index), new { view = DynamicView.CheckVoucher });
        }

        #endregion
    }
}
