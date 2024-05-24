using Accounting_System.Data;
using Accounting_System.Models;
using Accounting_System.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Threading;

namespace Accounting_System.Controllers
{
    [Authorize]
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ReportRepo _reportRepo;
        private readonly ChartOfAccountRepo _chartOfAccountRepo;
        private readonly ICompositeViewEngine _viewEngine;
        private readonly ITempDataProvider _tempDataProvider;
        private readonly UserManager<IdentityUser> _userManager;

        public ReportController(ApplicationDbContext dbContext, ReportRepo reportRepo, ChartOfAccountRepo chartOfAccountRepo, ICompositeViewEngine viewEngine, ITempDataProvider tempDataProvider, UserManager<IdentityUser> userManager)
        {
            _dbContext = dbContext;
            _reportRepo = reportRepo;
            _chartOfAccountRepo = chartOfAccountRepo;
            _viewEngine = viewEngine;
            _tempDataProvider = tempDataProvider;
            _userManager = userManager;
        }

        public async Task<IActionResult> SalesBook()
        {
            var viewModel = new ViewModelBook();
            viewModel.SOA = await _dbContext.ServiceInvoices
                .Where(soa => soa.IsPosted)
                .Select(soa => new SelectListItem
                {
                    Value = soa.Id.ToString(),
                    Text = soa.SVNo
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
                        return RedirectToAction("TransactionReportsInSOA", new { soaList = soaList, siList = siList});
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

        public async Task<IActionResult> TransactionReportsInSOA(int? siList, int? soaList)
        {
            ViewBag.SIList = siList;
            ViewBag.SOAList = soaList;
            var id = siList != null ? siList : soaList;
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
                    else if (poListFrom == null && poListTo != null || poListFrom != null && poListTo == null)
                    {
                        TempData["error"] = "Please fill the two select list in PO Liquidation Per PO, lowest to highest";
                        return RedirectToAction("PurchaseBook");
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

            if (dateFrom == null && dateTo == null)
            {
                TempData["error"] = "Please input Date From and Date To";
                return RedirectToAction("PurchaseBook");
            }else if (dateFrom == null)
            {
                TempData["error"] = "Please input Date To";
                return RedirectToAction("PurchaseBook");
            }

            var receivingReport = _reportRepo.GetReceivingReport(dateFrom, dateTo, selectedFiltering);
            return View(receivingReport);
        }
        public IActionResult POLiquidationPerPO(int? poListFrom, int? poListTo)
        {
            var from = poListFrom;
            var to = poListTo;

            if (poListFrom > poListTo)
            {
                TempData["error"] = "Please input lowest to highest PO#!";
                return RedirectToAction("PurchaseBook");
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
                    var auditTrail = _reportRepo.GetAuditTrails(model.DateFrom, model.DateTo);
                    var lastRecord = auditTrail.LastOrDefault();
                    if (lastRecord != null)
                    {
                        ViewBag.LastRecord = lastRecord.Date;
                    }

                    return View(auditTrail);
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

        public IActionResult SummaryOfChartOfAccount(CancellationToken cancellationToken)
        {
            try
            {
                var summary = _chartOfAccountRepo
                .GetSummaryReportView(cancellationToken);

                return View(summary.OrderBy(x => x.AccountNumber));
            }
            catch (Exception ex)
            {
                return BadRequest();
            }
        }

        public IActionResult GenerateAuditTrailTxtFile(ViewModelBook model)
        {
            var dateFrom = model.DateFrom;
            var dateTo = model.DateTo;
            var extractedBy = _userManager.GetUserName(this.User);

            var auditTrail = _reportRepo.GetAuditTrails(model.DateFrom, model.DateTo);
            var lastRecord = auditTrail.LastOrDefault();
            var firstRecord = auditTrail.FirstOrDefault();
            if (lastRecord != null)
            {
                ViewBag.LastRecord = lastRecord.Date;
            }

            var fileContent = new StringBuilder();

            fileContent.AppendLine($"TAXPAYER'S NAME: Filpride Resources Inc.");
            fileContent.AppendLine($"TIN: 000-216-589-00000");
            fileContent.AppendLine($"ADDRESS: 57 Westgate Office, Sampson Road, CBD, Subic Bay Freeport Zone, Kalaklan, Olongapo City, 2200 Zambales, Philippines");
            fileContent.AppendLine();
            fileContent.AppendLine($"Accounting System: Accounting Administration System");
            fileContent.AppendLine($"Acknowledgement Certificate Control No.: ");
            fileContent.AppendLine($"Date Issued: ");
            fileContent.AppendLine();
            fileContent.AppendLine("Accounting Books File Attributes/Layout Definition");
            fileContent.AppendLine("File Name: Audit Trail Report");
            fileContent.AppendLine("File Type: Text File");
            fileContent.AppendLine($"Number of Records: {auditTrail.Count}");
            fileContent.AppendLine("Amount Field Control Total: N/A");
            fileContent.AppendLine($"Period Covered: {dateFrom} to {dateTo} ");
            fileContent.AppendLine($"Transaction cut-off Date & Time: {ViewBag.LastRecord}");
            fileContent.AppendLine($"Extracted By: {extractedBy}");
            fileContent.AppendLine();
            fileContent.AppendLine($"{"Field Name"}\t{"Description"}\t{"From"}\t{"To"}\t{"Length"}\t{"Example"}");
            fileContent.AppendLine($"{"Date",-8}\t{"Date",-8}\t{"1"}\t{"25"}\t{"25"}\t{firstRecord.Date}");
            fileContent.AppendLine($"{"Username"}\t{"Username"}\t{"27"}\t{"46"}\t{"20"}\t{firstRecord.Username}");
            fileContent.AppendLine($"{"MachineName"}\t{"Machine Name"}\t{"48"}\t{"77"}\t{"30"}\t{firstRecord.MachineName}");
            fileContent.AppendLine($"{"Activity"}\t{"Activity"}\t{"79"}\t{"278"}\t{"200"}\t{firstRecord.Activity}");
            fileContent.AppendLine($"{"DocumentType"}\t{"Document Type"}\t{"280"}\t{"299"}\t{"20"}\t{firstRecord.DocumentType}");
            fileContent.AppendLine();
            fileContent.AppendLine("AUDIT TRAIL REPORT");
            fileContent.AppendLine();
            fileContent.AppendLine($"{"Date",-25}\t{"Username",-20}\t{"Machine Name",-30}\t{"Activity",-200}\t{"Document Type",-20}");

            // Generate the records
            foreach (var record in auditTrail)
            {
                fileContent.AppendLine($"{record.Date.ToString(),-25}\t{record.Username,-20}\t{record.MachineName,-30}\t{record.Activity,-200}\t{record.DocumentType,-20}");
            }
            
            fileContent.AppendLine();
            fileContent.AppendLine($"Software Name: Accounting Administration System (AAS)");
            fileContent.AppendLine($"Version: v1.0");
            fileContent.AppendLine($"Extracted By: {extractedBy}");
            fileContent.AppendLine($"Date & Time Extracted: {@DateTime.Now.ToString("MM/dd/yyyy hh:mm tt")}");


            // Convert the content to a byte array
            var bytes = Encoding.UTF8.GetBytes(fileContent.ToString());

            // Return the file to the user
            return File(bytes, "text/plain", "AuditTrailReport.txt");
        }
        public IActionResult GenerateDisbursementBookTxtFile(ViewModelBook model)
        {
            var dateFrom = model.DateFrom;
            var dateTo = model.DateTo;
            var extractedBy = _userManager.GetUserName(this.User);

            var disbursementBooks = _reportRepo.GetDisbursementBooks(model.DateFrom, model.DateTo);
            var lastRecord = disbursementBooks.LastOrDefault();
            var firstRecord = disbursementBooks.FirstOrDefault();
            if (lastRecord != null)
            {
                ViewBag.LastRecord = lastRecord.CreatedDate;
            }

            var fileContent = new StringBuilder();

            fileContent.AppendLine($"TAXPAYER'S NAME: Filpride Resources Inc.");
            fileContent.AppendLine($"TIN: 000-216-589-00000");
            fileContent.AppendLine($"ADDRESS: 57 Westgate Office, Sampson Road, CBD, Subic Bay Freeport Zone, Kalaklan, Olongapo City, 2200 Zambales, Philippines");
            fileContent.AppendLine();
            fileContent.AppendLine($"Accounting System: Accounting Administration System");
            fileContent.AppendLine($"Acknowledgement Certificate Control No.: ");
            fileContent.AppendLine($"Date Issued: ");
            fileContent.AppendLine();
            fileContent.AppendLine("Accounting Books File Attributes/Layout Definition");
            fileContent.AppendLine("File Name: Audit Trail Report");
            fileContent.AppendLine("File Type: Text File");
            fileContent.AppendLine($"Number of Records: {disbursementBooks.Count}");
            fileContent.AppendLine("Amount Field Control Total: N/A");
            fileContent.AppendLine($"Period Covered: {dateFrom} to {dateTo} ");
            fileContent.AppendLine($"Transaction cut-off Date & Time: {ViewBag.LastRecord}");
            fileContent.AppendLine($"Extracted By: {extractedBy}");
            fileContent.AppendLine();
            fileContent.AppendLine($"{"Field Name"}\t{"Description"}\t{"From"}\t{"To"}\t{"Length"}\t{"Example"}");
            fileContent.AppendLine($"{"Date",-8}\t{"Date",-8}\t{"1"}\t{"10"}\t{"10"}\t{firstRecord.Date}");
            fileContent.AppendLine($"{"CVNo",-8}\t{"CV No",-8}\t{"12"}\t{"23"}\t{"12"}\t{firstRecord.CVNo}");
            fileContent.AppendLine($"{"Payee",-8}\t{"Payee",-8}\t{"25"}\t{"124"}\t{"100"}\t{firstRecord.Payee}");
            fileContent.AppendLine($"{"Particulars"}\t{"Particulars"}\t{"126"}\t{"325"}\t{"200"}\t{firstRecord.Particulars}");
            fileContent.AppendLine($"{"Bank",-8}\t{"Bank",-8}\t{"327"}\t{"336"}\t{"10"}\t{firstRecord.Bank}");
            fileContent.AppendLine($"{"CheckNo",-8}\t{"Check No",-8}\t{"338"}\t{"357"}\t{"20"}\t{firstRecord.CheckNo}");
            fileContent.AppendLine($"{"CheckDate"}\t{"Check Date"}\t{"359"}\t{"368"}\t{"10"}\t{firstRecord.CheckDate}");
            fileContent.AppendLine($"{"ChartOfAccount"}\t{"Chart Of Account"}\t{"370"}\t{"469"}\t{"100"}\t{firstRecord.ChartOfAccount}");
            fileContent.AppendLine($"{"Debit",-8}\t{"Debit",-8}\t{"471"}\t{"488"}\t{"18"}\t{firstRecord.Debit}");
            fileContent.AppendLine($"{"Credit",-8}\t{"Credit",-8}\t{"490"}\t{"507"}\t{"18"}\t{firstRecord.Credit}");
            fileContent.AppendLine();
            fileContent.AppendLine("AUDIT TRAIL REPORT");
            fileContent.AppendLine();
            fileContent.AppendLine($"{"Date",-10}\t{"CV No",-12}\t{"Payee",-100}\t{"Particulars",-200}\t{"Bank",-10}\t{"Check No",-20}\t{"Check Date",-10}\t{"Chart Of Account",-100}\t{"Debit",-18}\t{"Credit",-18}");

            // Generate the records
            foreach (var record in disbursementBooks)
            {
                fileContent.AppendLine($"{record.Date.ToString(),-10}\t{record.CVNo,-12}\t{record.Payee,-100}\t{record.Particulars,-200}\t{record.Bank,-10}\t{record.CheckNo,-20}\t{record.CheckDate,-10}\t{record.ChartOfAccount,-100}\t{record.Debit,-18}\t{record.Credit,-18}");
            }

            fileContent.AppendLine();
            fileContent.AppendLine($"Software Name: Accounting Administration System (AAS)");
            fileContent.AppendLine($"Version: v1.0");
            fileContent.AppendLine($"Extracted By: {extractedBy}");
            fileContent.AppendLine($"Date & Time Extracted: {@DateTime.Now.ToString("MM/dd/yyyy hh:mm tt")}");


            // Convert the content to a byte array
            var bytes = Encoding.UTF8.GetBytes(fileContent.ToString());

            // Return the file to the user
            return File(bytes, "text/plain", "DisbursementBookReport.txt");
        }

        public IActionResult GenerateCashReceiptBookTxtFile(ViewModelBook model)
        {
            var dateFrom = model.DateFrom;
            var dateTo = model.DateTo;
            var extractedBy = _userManager.GetUserName(this.User);

            var cashReceiptBooks = _reportRepo.GetCashReceiptBooks(model.DateFrom, model.DateTo);
            var lastRecord = cashReceiptBooks.LastOrDefault();
            var firstRecord = cashReceiptBooks.FirstOrDefault();
            if (lastRecord != null)
            {
                ViewBag.LastRecord = lastRecord.CreatedDate;
            }

            var fileContent = new StringBuilder();

            fileContent.AppendLine($"TAXPAYER'S NAME: Filpride Resources Inc.");
            fileContent.AppendLine($"TIN: 000-216-589-00000");
            fileContent.AppendLine($"ADDRESS: 57 Westgate Office, Sampson Road, CBD, Subic Bay Freeport Zone, Kalaklan, Olongapo City, 2200 Zambales, Philippines");
            fileContent.AppendLine();
            fileContent.AppendLine($"Accounting System: Accounting Administration System");
            fileContent.AppendLine($"Acknowledgement Certificate Control No.: ");
            fileContent.AppendLine($"Date Issued: ");
            fileContent.AppendLine();
            fileContent.AppendLine("Accounting Books File Attributes/Layout Definition");
            fileContent.AppendLine("File Name: Audit Trail Report");
            fileContent.AppendLine("File Type: Text File");
            fileContent.AppendLine($"Number of Records: {cashReceiptBooks.Count}");
            fileContent.AppendLine("Amount Field Control Total: N/A");
            fileContent.AppendLine($"Period Covered: {dateFrom} to {dateTo} ");
            fileContent.AppendLine($"Transaction cut-off Date & Time: {ViewBag.LastRecord}");
            fileContent.AppendLine($"Extracted By: {extractedBy}");
            fileContent.AppendLine();
            fileContent.AppendLine($"{"Field Name"}\t{"Description"}\t{"From"}\t{"To"}\t{"Length"}\t{"Example"}");
            fileContent.AppendLine($"{"Date",-8}\t{"Date",-8}\t{"1"}\t{"10"}\t{"10"}\t{firstRecord.Date}");
            fileContent.AppendLine($"{"RefNo",-8}\t{"Ref No.",-8}\t{"12"}\t{"23"}\t{"12"}\t{firstRecord.RefNo}");
            fileContent.AppendLine($"{"CustomerName"}\t{"Customer Name"}\t{"25"}\t{"40"}\t{"16"}\t{firstRecord.CustomerName}");
            fileContent.AppendLine($"{"Bank",-8}\t{"Bank",-8}\t{"42"}\t{"141"}\t{"100"}\t{firstRecord.Bank}");
            fileContent.AppendLine($"{"CheckNo",-8}\t{"Check No.",-8}\t{"143"}\t{"162"}\t{"20"}\t{firstRecord.CheckNo}");
            fileContent.AppendLine($"{"COA",-8}\t{"Chart Of Account",-8}\t{"164"}\t{"263"}\t{"100"}\t{firstRecord.COA}");
            fileContent.AppendLine($"{"Particulars"}\t{"Particulars"}\t{"265"}\t{"464"}\t{"200"}\t{firstRecord.Particulars}");
            fileContent.AppendLine($"{"Debit",-8}\t{"Debit",-8}\t{"466"}\t{"483"}\t{"18"}\t{firstRecord.Debit}");
            fileContent.AppendLine($"{"Credit",-8}\t{"Credit",-8}\t{"485"}\t{"502"}\t{"18"}\t{firstRecord.Credit}");
            fileContent.AppendLine();
            fileContent.AppendLine("AUDIT TRAIL REPORT");
            fileContent.AppendLine();
            fileContent.AppendLine($"{"Date",-10}\t{"Ref No.",-12}\t{"Customer Name",-16}\t{"Bank",-100}\t{"Check No.",-20}\t{"Chart Of Account",-100}\t{"Particulars",-200}\t{"Debit",-18}\t{"Credit",-18}");

            // Generate the records
            foreach (var record in cashReceiptBooks)
            {
                fileContent.AppendLine($"{record.Date.ToString(),-10}\t{record.RefNo,-12}\t{record.CustomerName,-16}\t{record.Bank,-100}\t{record.CheckNo,-20}\t{record.COA,-100}\t{record.Particulars,-200}\t{record.Debit,-18}\t{record.Credit,-18}");
            }

            fileContent.AppendLine();
            fileContent.AppendLine($"Software Name: Accounting Administration System (AAS)");
            fileContent.AppendLine($"Version: v1.0");
            fileContent.AppendLine($"Extracted By: {extractedBy}");
            fileContent.AppendLine($"Date & Time Extracted: {@DateTime.Now.ToString("MM/dd/yyyy hh:mm tt")}");


            // Convert the content to a byte array
            var bytes = Encoding.UTF8.GetBytes(fileContent.ToString());

            // Return the file to the user
            return File(bytes, "text/plain", "CashReceiptBookReport.txt");
        }
        public IActionResult GenerateGeneralLedgerBookTxtFile(ViewModelBook model)
        {
            var dateFrom = model.DateFrom;
            var dateTo = model.DateTo;
            var extractedBy = _userManager.GetUserName(this.User);

            var generalBooks = _reportRepo.GetGeneralLedgerBooks(model.DateFrom, model.DateTo);
            var lastRecord = generalBooks.LastOrDefault();
            var firstRecord = generalBooks.FirstOrDefault();
            if (lastRecord != null)
            {
                ViewBag.LastRecord = lastRecord.CreatedDate;
            }

            var fileContent = new StringBuilder();

            fileContent.AppendLine($"TAXPAYER'S NAME: Filpride Resources Inc.");
            fileContent.AppendLine($"TIN: 000-216-589-00000");
            fileContent.AppendLine($"ADDRESS: 57 Westgate Office, Sampson Road, CBD, Subic Bay Freeport Zone, Kalaklan, Olongapo City, 2200 Zambales, Philippines");
            fileContent.AppendLine();
            fileContent.AppendLine($"Accounting System: Accounting Administration System");
            fileContent.AppendLine($"Acknowledgement Certificate Control No.: ");
            fileContent.AppendLine($"Date Issued: ");
            fileContent.AppendLine();
            fileContent.AppendLine("Accounting Books File Attributes/Layout Definition");
            fileContent.AppendLine("File Name: Audit Trail Report");
            fileContent.AppendLine("File Type: Text File");
            fileContent.AppendLine($"Number of Records: {generalBooks.Count}");
            fileContent.AppendLine("Amount Field Control Total: N/A");
            fileContent.AppendLine($"Period Covered: {dateFrom} to {dateTo} ");
            fileContent.AppendLine($"Transaction cut-off Date & Time: {ViewBag.LastRecord}");
            fileContent.AppendLine($"Extracted By: {extractedBy}");
            fileContent.AppendLine();
            fileContent.AppendLine($"{"Field Name"}\t{"Description"}\t{"From"}\t{"To"}\t{"Length"}\t{"Example"}");
            fileContent.AppendLine($"{"Date",-8}\t{"Date",-8}\t{"1"}\t{"10"}\t{"10"}\t{firstRecord.Date}");
            fileContent.AppendLine($"{"Reference"}\t{"Reference"}\t{"12"}\t{"23"}\t{"12"}\t{firstRecord.Reference}");
            fileContent.AppendLine($"{"Description"}\t{"Description"}\t{"25"}\t{"74"}\t{"50"}\t{firstRecord.Description}");
            fileContent.AppendLine($"{"AccountTitle"}\t{"Account Title"}\t{"76"}\t{"125"}\t{"50"}\t{firstRecord.AccountNo + " " + firstRecord.AccountTitle}");
            fileContent.AppendLine($"{"Debit",-8}\t{"Debit",-8}\t{"127"}\t{"144"}\t{"18"}\t{firstRecord.Debit}");
            fileContent.AppendLine($"{"Credit",-8}\t{"Credit",-8}\t{"146"}\t{"163"}\t{"18"}\t{firstRecord.Credit}");
            fileContent.AppendLine();
            fileContent.AppendLine("AUDIT TRAIL REPORT");
            fileContent.AppendLine();
            fileContent.AppendLine($"{"Date",-10}\t{"Reference",-12}\t{"Description",-50}\t{"Account Title",-50}\t{"Debit",-18}\t{"Credit",-18}");

            // Generate the records
            foreach (var record in generalBooks)
            {
                fileContent.AppendLine($"{record.Date.ToString(),-10}\t{record.Reference,-12}\t{record.Description,-50}\t{record.AccountNo + " " + record.AccountTitle,-50}\t{record.Debit,-18}\t{record.Credit,-18}");
            }

            fileContent.AppendLine();
            fileContent.AppendLine($"Software Name: Accounting Administration System (AAS)");
            fileContent.AppendLine($"Version: v1.0");
            fileContent.AppendLine($"Extracted By: {extractedBy}");
            fileContent.AppendLine($"Date & Time Extracted: {@DateTime.Now.ToString("MM/dd/yyyy hh:mm tt")}");


            // Convert the content to a byte array
            var bytes = Encoding.UTF8.GetBytes(fileContent.ToString());

            // Return the file to the user
            return File(bytes, "text/plain", "GeneralLedgerBookReport.txt");
        }
    }
}