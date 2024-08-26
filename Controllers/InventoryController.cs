using Accounting_System.Data;
using Accounting_System.Models;
using Accounting_System.Models.AccountsPayable;
using Accounting_System.Models.Reports;
using Accounting_System.Models.ViewModels;
using Accounting_System.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Accounting_System.Controllers
{
    [Authorize]
    public class InventoryController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly InventoryRepo _inventoryRepo;

        private readonly JournalVoucherRepo _journalVoucherRepo;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly ILogger<InventoryController> _logger;

        public InventoryController(ApplicationDbContext dbContext, InventoryRepo inventoryRepo, JournalVoucherRepo journalVoucherRepo, UserManager<IdentityUser> userManager, ILogger<InventoryController> logger)
        {
            _dbContext = dbContext;
            _inventoryRepo = inventoryRepo;
            _journalVoucherRepo = journalVoucherRepo;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> BeginningInventory(CancellationToken cancellationToken)
        {
            BeginningInventoryViewModel? viewModel = new();

            viewModel.ProductList = await _dbContext.Products
                .OrderBy(p => p.Code)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = $"{p.Code} {p.Name}"
                })
                .ToListAsync(cancellationToken);

            viewModel.PO = await _dbContext.PurchaseOrders
                .OrderBy(p => p.SeriesNumber)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.PONo
                })
                .ToListAsync(cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> BeginningInventory(BeginningInventoryViewModel viewModel, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var hasBeginningInventory = await _inventoryRepo.HasAlreadyBeginningInventory(viewModel.ProductId, viewModel.POId, cancellationToken);

                    if (hasBeginningInventory)
                    {
                        viewModel.ProductList = await _dbContext.Products
                            .OrderBy(p => p.Code)
                            .Select(p => new SelectListItem
                            {
                                Value = p.Id.ToString(),
                                Text = $"{p.Code} {p.Name}"
                            })
                            .ToListAsync(cancellationToken);

                        TempData["error"] = "Beginning Inventory for this product already exists. Please contact MIS if you think this was a mistake.";
                        return View(viewModel);
                    }

                    await _inventoryRepo.AddBeginningInventory(viewModel, User, cancellationToken);
                    TempData["success"] = "Beginning balance created successfully";
                    return RedirectToAction(nameof(BeginningInventory));
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.ToString();
                    return View();
                }
            }

            viewModel.ProductList = await _dbContext.Products
                .OrderBy(p => p.Code)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = $"{p.Code} {p.Name}"
                })
                .ToListAsync(cancellationToken);

            viewModel.PO = await _dbContext.PurchaseOrders
                .OrderBy(p => p.SeriesNumber)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.PONo
                })
                .ToListAsync(cancellationToken);

            TempData["error"] = "The information you submitted is not valid!";
            return View(viewModel);
        }

        public async Task<IActionResult> InventoryReport(CancellationToken cancellationToken)
        {
            InventoryReportViewModel viewModel = new InventoryReportViewModel();

            viewModel.Products = await _dbContext.Products
                .OrderBy(p => p.Code)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = $"{p.Code} {p.Name}"
                })
                .ToListAsync(cancellationToken);

            viewModel.PO = await _dbContext.PurchaseOrders
                .OrderBy(p => p.SeriesNumber)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.PONo
                })
                .ToListAsync(cancellationToken);

            return View(viewModel);
        }

        public async Task<IActionResult> DisplayInventory(InventoryReportViewModel viewModel, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                var dateFrom = viewModel.DateTo.AddDays(-viewModel.DateTo.Day + 1);

                var previousMonth = viewModel.DateTo.Month - 1;

                var endingBalance = await _dbContext.Inventories
                    .OrderBy(e => e.Date)
                    .ThenBy(e => e.Id)
                    .LastOrDefaultAsync(e => (viewModel.POId == null || e.POId == viewModel.POId) && e.Date.Month == previousMonth, cancellationToken);

                List<Inventory> inventories = new List<Inventory>();
                if (endingBalance != null)
                {
                    inventories = await _dbContext.Inventories
                       .Where(i => i.Date >= dateFrom && i.Date <= viewModel.DateTo && i.ProductId == viewModel.ProductId && (viewModel.POId == null || i.POId == viewModel.POId) || i.Id == endingBalance.Id)
                       .ToListAsync(cancellationToken);
                }
                else
                {
                    inventories = await _dbContext.Inventories
                       .Where(i => i.Date >= dateFrom && i.Date <= viewModel.DateTo && i.ProductId == viewModel.ProductId && (viewModel.POId == null || i.POId == viewModel.POId))
                       .ToListAsync(cancellationToken);
                }

                var product = await _dbContext.Products
                    .FindAsync(viewModel.ProductId, cancellationToken);

                ViewData["Product"] = product.Name;
                ViewBag.ProductId = viewModel.ProductId;
                ViewBag.POId = viewModel.POId;

                return View(inventories);
            }

            return View(viewModel);
        }

        public async Task<IActionResult> ConsolidatedPO(InventoryReportViewModel viewModel, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                var dateFrom = viewModel.DateTo.AddDays(-viewModel.DateTo.Day + 1);
                List<Inventory> inventories = new List<Inventory>();
                if (viewModel.POId == null)
                {
                    inventories = await _dbContext.Inventories
                        .Include(i => i.PurchaseOrder)
                        .Where(i => i.Date >= dateFrom && i.Date <= viewModel.DateTo && i.ProductId == viewModel.ProductId)
                        .OrderBy(i => i.Date)
                        .ThenBy(i => i.Id)
                        .ToListAsync(cancellationToken);
                }
                else
                {
                    inventories = await _dbContext.Inventories
                        .Include(i => i.PurchaseOrder)
                        .Where(i => i.Date >= dateFrom && i.Date <= viewModel.DateTo && i.ProductId == viewModel.ProductId && i.POId == viewModel.POId)
                        .OrderBy(i => i.Date)
                        .ThenBy(i => i.Id)
                        .ToListAsync(cancellationToken);
                }

                var product = await _dbContext.Products
                    .FindAsync(viewModel.ProductId, cancellationToken);

                ViewData["Product"] = product.Name;
                ViewBag.ProductId = viewModel.ProductId;
                ViewBag.POId = viewModel.POId;

                return View(inventories);
            }

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> ActualInventory(CancellationToken cancellationToken)
        {
            ActualInventoryViewModel? viewModel = new();

            viewModel.ProductList = await _dbContext.Products
                .OrderBy(p => p.Code)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = $"{p.Code} {p.Name}"
                })
                .ToListAsync(cancellationToken);

            viewModel.COA = await _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == 4 && (coa.Name.StartsWith("AR-Non Trade Receivable") || coa.Name.StartsWith("Cost of Goods Sold") || coa.Number.StartsWith("6010103")))
                .Select(s => new SelectListItem
                {
                    Value = s.Number,
                    Text = s.Number + " " + s.Name
                })
                .ToListAsync(cancellationToken);
            viewModel.PO = await _dbContext.PurchaseOrders
                .OrderBy(p => p.SeriesNumber)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.PONo
                })
                .ToListAsync(cancellationToken);

            return View(viewModel);
        }

        public async Task<IActionResult> GetProducts(int poId, int id, DateOnly dateTo, CancellationToken cancellationToken)
        {
            if (id != 0)
            {
                var dateFrom = dateTo.AddDays(-dateTo.Day + 1);

                var getPerBook = await _dbContext.Inventories
                    .Where(i => i.Date >= dateFrom && i.Date <= dateTo && i.ProductId == id && i.POId == poId)
                    .OrderByDescending(model => model.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (getPerBook != null)
                {
                    return Json(new { InventoryBalance = getPerBook.InventoryBalance, AverageCost = getPerBook.AverageCost, TotalBalance = getPerBook.TotalBalance });
                }
            }
            return Json(new { InventoryBalance = 0.00, AverageCost = 0.00, TotalBalance = 0.00 });
        }

        [HttpPost]
        public async Task<IActionResult> ActualInventory(ActualInventoryViewModel viewModel, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _inventoryRepo.AddActualInventory(viewModel, User, cancellationToken);
                    TempData["success"] = "Actual inventory created successfully";
                    return RedirectToAction(nameof(ActualInventory));
                }
                catch (Exception ex)
                {
                    viewModel.ProductList = await _dbContext.Products
                        .OrderBy(p => p.Code)
                        .Select(p => new SelectListItem
                        {
                            Value = p.Id.ToString(),
                            Text = $"{p.Code} {p.Name}"
                        })
                        .ToListAsync(cancellationToken);

                    viewModel.PO = await _dbContext.PurchaseOrders
                        .OrderBy(p => p.SeriesNumber)
                        .Select(p => new SelectListItem
                        {
                            Value = p.Id.ToString(),
                            Text = p.PONo
                        })
                        .ToListAsync(cancellationToken);

                    viewModel.COA = await _dbContext.ChartOfAccounts
                        .Where(coa => coa.Level == 4 && (coa.Name.StartsWith("AR-Non Trade Receivable") || coa.Name.StartsWith("Cost of Goods Sold") || coa.Number.StartsWith("6010103")))
                        .Select(s => new SelectListItem
                        {
                            Value = s.Number,
                            Text = s.Number + " " + s.Name
                        })
                        .ToListAsync(cancellationToken);

                    TempData["error"] = ex.Message;
                    return View(viewModel);
                }
            }

            viewModel.ProductList = await _dbContext.Products
                .OrderBy(p => p.Code)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = $"{p.Code} {p.Name}"
                })
                .ToListAsync(cancellationToken);

            viewModel.COA = await _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == 4 && (coa.Name.StartsWith("AR-Non Trade Receivable") || coa.Name.StartsWith("Cost of Goods Sold") || coa.Number.StartsWith("6010103")))
                .Select(s => new SelectListItem
                {
                    Value = s.Number,
                    Text = s.Number + " " + s.Name
                })
                .ToListAsync(cancellationToken);

            TempData["error"] = "The information provided was invalid.";
            return View(viewModel);
        }

        public async Task<IActionResult> ValidateInventory(int? id, CancellationToken cancellationToken)
        {
            try
            {
                if (id == null || id == 0)
                {
                    return NotFound();
                }

                Inventory? inventory = await _dbContext.Inventories
                    .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

                IEnumerable<GeneralLedgerBook>? ledgerEntries = await _dbContext.GeneralLedgerBooks
                    .Where(l => l.Reference == inventory.Id.ToString())
                    .ToListAsync(cancellationToken);

                if (inventory != null || ledgerEntries != null)
                {
                    #region -- Journal Voucher entry --

                    var header = new JournalVoucherHeader
                    {
                        SeriesNumber = await _journalVoucherRepo.GetLastSeriesNumberJV(cancellationToken),
                        JVNo = await _journalVoucherRepo.GenerateJVNo(cancellationToken),
                        JVReason = "Actual Inventory",
                        Particulars = inventory.Particular,
                        Date = inventory.Date,
                        CreatedBy = _userManager.GetUserName(this.User),
                        CreatedDate = DateTime.Now,
                        IsPosted = true,
                        PostedBy = _userManager.GetUserName(this.User),
                        PostedDate = DateTime.Now
                    };

                    var details = new List<JournalVoucherDetail>();

                    foreach (var entry in ledgerEntries)
                    {
                        entry.IsPosted = true;
                        entry.Reference = header.JVNo;

                        details.Add(new JournalVoucherDetail
                        {
                            AccountNo = entry.AccountNo,
                            AccountName = entry.AccountTitle,
                            TransactionNo = header.JVNo,
                            Debit = entry.Debit,
                            Credit = entry.Credit
                        });
                    }

                    inventory.IsValidated = true;
                    inventory.ValidatedBy = _userManager.GetUserName(this.User);
                    inventory.ValidatedDate = DateTime.Now;

                    await _dbContext.JournalVoucherHeaders.AddAsync(header, cancellationToken);
                    await _dbContext.JournalVoucherDetails.AddRangeAsync(details, cancellationToken);

                    #endregion -- Journal Voucher entry --

                    #region -- Journal Book Entry --

                    var journalBook = new List<JournalBook>();

                    foreach (var entry in ledgerEntries)
                    {
                        journalBook.Add(new JournalBook
                        {
                            Date = entry.Date,
                            Reference = header.JVNo,
                            Description = "Actual Inventory",
                            AccountTitle = entry.AccountNo + " " + entry.AccountTitle,
                            Debit = Math.Abs(entry.Debit),
                            Credit = Math.Abs(entry.Credit),
                            CreatedBy = _userManager.GetUserName(this.User),
                            CreatedDate = DateTime.Now
                        });
                    }
                    await _dbContext.AddRangeAsync(journalBook, cancellationToken);

                    #endregion -- Journal Book Entry --

                    await _dbContext.SaveChangesAsync(cancellationToken);

                    return Ok();
                }
                else
                {
                    return BadRequest();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error");
                return BadRequest();
            }
        }
    }
}