using Accounting_System.Data;
using Accounting_System.Models;
using Accounting_System.Repository;
using Microsoft.AspNetCore.Mvc;

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

        public IActionResult SalesBook()
        {
            return View();
        }

        public IActionResult SalesBookReport(ViewModelBook model, string? selectedDocument)
        {
            ViewBag.DateFrom = model.DateFrom;
            ViewBag.DateTo = model.DateTo;
            if (ModelState.IsValid)
            {
                try
                {
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

        public IActionResult PurchaseBook()
        {
            return View();
        }

        public IActionResult PurchaseBookReport(ViewModelBook model, string? selectedAging)
        {
            ViewBag.DateFrom = model.DateFrom;
            ViewBag.DateTo = model.DateTo;
            if (ModelState.IsValid)
            {
                try
                {
                    var purchaseOrders = _reportRepo.GetPurchaseBooks(model.DateFrom, model.DateTo, selectedAging);
                    var lastRecord = purchaseOrders.LastOrDefault();
                    if (lastRecord != null)
                    {
                        ViewBag.LastRecord = lastRecord.CreatedDate;
                    }
                        ViewBag.SelectedAging = selectedAging;

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

        public IActionResult MaturityAging()
        {
            return View();
        }

        public IActionResult MaturityAgingReport(ViewModelBook model)
        {
            ViewBag.DateFrom = model.DateFrom;
            ViewBag.DateTo = model.DateTo;
            if (ModelState.IsValid)
            {
                try
                {
                    var maturityAging = _reportRepo.GetMaturityAging(model.DateFrom, model.DateTo);
                    var lastRecord = maturityAging.LastOrDefault();
                    if (lastRecord != null)
                    {
                        ViewBag.LastRecord = lastRecord.CreatedDate;
                    }

                    return View(maturityAging);
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(MaturityAging));
                }
            }

            return View(model);
        }
    }
}