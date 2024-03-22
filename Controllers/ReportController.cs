using Accounting_System.Data;
using Accounting_System.Models;
using Accounting_System.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Threading;

namespace Accounting_System.Controllers
{
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ReportRepo _reportRepo;

        public ReportController(ApplicationDbContext dbContext, ReportRepo reportRepo)
        {
            _dbContext = dbContext;
            _reportRepo = reportRepo;
        }

        public async Task<IActionResult> SalesBook()
        {
            var viewModel = new ViewModelBook();
            viewModel.SOA = await _dbContext.StatementOfAccounts
                .Where(soa => soa.IsPosted)
                .Select(soa => new SelectListItem
                {
                    Value = soa.Id.ToString(),
                    Text = soa.SOANo
                })
                .ToListAsync();
            viewModel.SI = await _dbContext.SalesInvoices
                .Where(si => si.IsPosted)
                .Select(soa => new SelectListItem
                {
                    Value = soa.Id.ToString(),
                    Text = soa.SINo
                })
                .ToListAsync();

            return View(viewModel);
        }

        public IActionResult SalesBookReport(ViewModelBook model, string? selectedDocument, string? soaList, string? siList)
        {
            ViewBag.DateFrom = model.DateFrom;
            ViewBag.DateTo = model.DateTo;
            if (ModelState.IsValid)
            {
                try
                {
                    if (soaList != null || siList != null)
                    {
                        var id = siList != null ? siList : soaList;
                        return RedirectToAction("TransactionReportsInSOA", new { id = id });
                    }

                    var salesBook = _reportRepo.GetSalesBooks(model.DateFrom, model.DateTo, selectedDocument);
                    var lastRecord = salesBook.LastOrDefault();
                    if (lastRecord != null)
                    {
                        ViewBag.LastRecord = lastRecord.CreatedDate;
                    }
                        ViewBag.SelectedDocument = selectedDocument;

                    return View(salesBook);
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(SalesBook));
                }
            }

            return View(model);
        }

        public async Task<IActionResult> TransactionReportsInSOA(int? id)
        {
                var sales = await _dbContext
                    .SalesBooks
                    .Where(s => s.DocumentId == id)
                    .ToListAsync();

            return View(sales);
        }

        public IActionResult CashReceiptBook()
        {
            return View();
        }

        public IActionResult CashReceiptBookReport(ViewModelBook model)
        {
            ViewBag.DateFrom = model.DateFrom;
            ViewBag.DateTo = model.DateTo;
            if (ModelState.IsValid)
            {
                try
                {
                    var cashReceiptBooks = _reportRepo.GetCashReceiptBooks(model.DateFrom, model.DateTo);
                    var lastRecord = cashReceiptBooks.LastOrDefault();
                    if (lastRecord != null)
                    {
                        ViewBag.LastRecord = lastRecord.CreatedDate;
                    }

                    return View(cashReceiptBooks);
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(CashReceiptBook));
                }
            }

            return View(model);
        }

        public async Task<IActionResult> PurchaseBook()
        {
            var viewModel = new ViewModelBook();
            viewModel.PO = await _dbContext.PurchaseOrders
                .Where(po => po.IsPosted)
                .Select(po => new SelectListItem
                {
                    Value = po.Id.ToString(),
                    Text = po.PONo
                })
                .ToListAsync();

            return View(viewModel);
        }

        public IActionResult PurchaseBookReport(ViewModelBook model, string? selectedFiltering, string? poListFrom, string? poListTo)
        {
            ViewBag.DateFrom = model.DateFrom;
            ViewBag.DateTo = model.DateTo;
            if (ModelState.IsValid)
            {
                try
                {
                    if (poListFrom != null && poListTo != null)
                    {
                        return RedirectToAction("POLiquidationPerPO", new { poListFrom = poListFrom, poListTo = poListTo });
                    }

                    if (selectedFiltering == "UnpostedRR" || selectedFiltering == "POLiquidation")
                    {
                        return RedirectToAction("GetRR", new { DateFrom = model.DateFrom, DateTo = model.DateTo, selectedFiltering });
                    }

                    var purchaseOrders = _reportRepo.GetPurchaseBooks(model.DateFrom, model.DateTo, selectedFiltering);
                    var lastRecord = purchaseOrders.LastOrDefault();
                    if (lastRecord != null)
                    {
                        ViewBag.LastRecord = lastRecord.CreatedDate;
                    }
                        ViewBag.SelectedFiltering = selectedFiltering;

                    return View(purchaseOrders);
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(PurchaseBook));
                }
            }

            return View(model);
        }

        public IActionResult GetRR(string dateFrom, string dateTo, string selectedFiltering)
        {
            ViewBag.DateFrom = dateFrom;
            ViewBag.DateTo = dateTo;
            ViewBag.SelectedFiltering = selectedFiltering;

            var receivingReport = _reportRepo.GetReceivingReport(dateFrom, dateTo, selectedFiltering);
            return View(receivingReport);
        }
        public IActionResult POLiquidationPerPO(int? poListFrom, int? poListTo)
        {
            var from = poListFrom;
            var to = poListTo;

            if (poListFrom > poListTo)
            {
                throw new ArgumentException("Date From must be greater than Date To !");
            }

            var po = _dbContext
                 .ReceivingReports
                 .Include(rr => rr.PurchaseOrder)
                 .ThenInclude(po => po.Supplier)
                 .Include(rr => rr.PurchaseOrder)
                 .ThenInclude(po => po.Product)
                 .AsEnumerable()
                 .Where(rr => rr.POId >= from && rr.POId <= to && rr.IsPosted)
                 .OrderBy(rr => rr.POId)
                 .ToList();
            return View(po);
        }

        public IActionResult InventoryBook()
        {
            return View();
        }

        public IActionResult InventoryBookReport(ViewModelBook model)
        {
            ViewBag.DateFrom = model.DateFrom;
            ViewBag.DateTo = model.DateTo;
            if (ModelState.IsValid)
            {
                try
                {
                    var inventoryBooks = _reportRepo.GetInventoryBooks(model.DateFrom, model.DateTo);
                    var lastRecord = inventoryBooks.LastOrDefault();
                    if (lastRecord != null)
                    {
                        ViewBag.LastRecord = lastRecord.CreatedDate;
                    }

                    return View(inventoryBooks);
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(InventoryBook));
                }
            }

            return View(model);
        }

        public IActionResult GeneralLedgerBook()
        {
            return View();
        }

        public IActionResult GeneralLedgerBookReport(ViewModelBook model)
        {
            ViewBag.DateFrom = model.DateFrom;
            ViewBag.DateTo = model.DateTo;
            if (ModelState.IsValid)
            {
                try
                {
                    var inventoryBooks = _reportRepo.GetGeneralLedgerBooks(model.DateFrom, model.DateTo);
                    var lastRecord = inventoryBooks.LastOrDefault();
                    if (lastRecord != null)
                    {
                        ViewBag.LastRecord = lastRecord.CreatedDate;
                    }

                    return View(inventoryBooks);
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(GeneralLedgerBook));
                }
            }

            return View(model);
        }

        public IActionResult DisbursementBook()
        {
            return View();
        }

        public IActionResult DisbursementBookReport(ViewModelBook model)
        {
            ViewBag.DateFrom = model.DateFrom;
            ViewBag.DateTo = model.DateTo;
            if (ModelState.IsValid)
            {
                try
                {
                    var disbursementBooks = _reportRepo.GetDisbursementBooks(model.DateFrom, model.DateTo);
                    var lastRecord = disbursementBooks.LastOrDefault();
                    if (lastRecord != null)
                    {
                        ViewBag.LastRecord = lastRecord.CreatedDate;
                    }

                    return View(disbursementBooks);
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(DisbursementBook));
                }
            }

            return View(model);
        }

        public IActionResult JournalBook()
        {
            return View();
        }

        public IActionResult JournalBookReport(ViewModelBook model)
        {
            ViewBag.DateFrom = model.DateFrom;
            ViewBag.DateTo = model.DateTo;
            if (ModelState.IsValid)
            {
                try
                {
                    var journalBooks = _reportRepo.GetJournalBooks(model.DateFrom, model.DateTo);
                    var lastRecord = journalBooks.LastOrDefault();
                    if (lastRecord != null)
                    {
                        ViewBag.LastRecord = lastRecord.CreatedDate;
                    }

                    return View(journalBooks);
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(JournalBook));
                }
            }

            return View(model);
        }

        public IActionResult AuditTrail()
        {
            return View();
        }

        public IActionResult AuditTrailReport(ViewModelBook model)
        {
            ViewBag.DateFrom = model.DateFrom;
            ViewBag.DateTo = model.DateTo;
            if (ModelState.IsValid)
            {
                try
                {
                    var disbursementBooks = _reportRepo.GetAuditTrails(model.DateFrom, model.DateTo);
                    var lastRecord = disbursementBooks.LastOrDefault();
                    if (lastRecord != null)
                    {
                        ViewBag.LastRecord = lastRecord.Date;
                    }

                    return View(disbursementBooks);
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(AuditTrail));
                }
            }

            return View(model);
        }

        public async Task<IActionResult> CustomerProfile()
        {
            var customers = await _reportRepo.GetCustomersAsync();

            return View(customers);
        }

        public async Task<IActionResult> ProductList()
        {
            var products = await _reportRepo.GetProductsAsync();

            return View(products);
        }
    }
}