using Accounting_System.Data;
using Accounting_System.Models;
using Accounting_System.Repository;
using Accounting_System.Utility;
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

        #region -- Generate Audit Trail .Txt File --
        public IActionResult GenerateAuditTrailTxtFile(ViewModelBook model)
        {
            var dateFrom = model.DateFrom;
            var dateTo = model.DateTo;
            var extractedBy = _userManager.GetUserName(this.User);

            var auditTrail = _reportRepo.GetAuditTrails(model.DateFrom, model.DateTo);
            if (auditTrail.Count == 0)
            {
                TempData["error"] = "No Record Found";
                return RedirectToAction(nameof(AuditTrail));
            }
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
            fileContent.AppendLine($"Acknowledgement Certificate Control No.: {CS.ACCN}");
            fileContent.AppendLine($"Date Issued: {CS.DateIssued}");
            fileContent.AppendLine();
            fileContent.AppendLine("Accounting Books File Attributes/Layout Definition");
            fileContent.AppendLine("File Name: Audit Trail Report");
            fileContent.AppendLine("File Type: Text File");
            fileContent.AppendLine($"{"Number of Records: ", -35}{auditTrail.Count}");
            fileContent.AppendLine($"{"Amount Field Control Total: ", -35}{"N/A"}");
            fileContent.AppendLine($"{"Period Covered: ", -35}{dateFrom}{" to "}{dateTo} ");
            fileContent.AppendLine($"{"Transaction cut-off Date & Time: ", -35}{ViewBag.LastRecord}");
            fileContent.AppendLine($"{"Extracted By: ", -35}{extractedBy.ToUpper()}");
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
            fileContent.AppendLine($"Extracted By: {extractedBy.ToUpper()}");
            fileContent.AppendLine($"Date & Time Extracted: {@DateTime.Now.ToString("MM/dd/yyyy hh:mm tt")}");


            // Convert the content to a byte array
            var bytes = Encoding.UTF8.GetBytes(fileContent.ToString());

            // Return the file to the user
            return File(bytes, "text/plain", "AuditTrailReport.txt");
        }
        #endregion -- Generate Audit Trail .Txt File --

        #region -- Generate Disbursement Book .Txt File --
        public IActionResult GenerateDisbursementBookTxtFile(ViewModelBook model)
        {
            var dateFrom = model.DateFrom;
            var dateTo = model.DateTo;
            var extractedBy = _userManager.GetUserName(this.User);

            var disbursementBooks = _reportRepo.GetDisbursementBooks(model.DateFrom, model.DateTo);
            if (disbursementBooks.Count == 0)
            {
                TempData["error"] = "No Record Found";
                return RedirectToAction(nameof(DisbursementBook));
            }
            var totalDebit = disbursementBooks.Sum(db => db.Debit);
            var totalCredit = disbursementBooks.Sum(db => db.Credit);
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
            fileContent.AppendLine($"Acknowledgement Certificate Control No.: {CS.ACCN}");
            fileContent.AppendLine($"Date Issued: {CS.DateIssued}");
            fileContent.AppendLine();
            fileContent.AppendLine("Accounting Books File Attributes/Layout Definition");
            fileContent.AppendLine("File Name: Disbursement Book Report");
            fileContent.AppendLine("File Type: Text File");
            fileContent.AppendLine($"{"Number of Records: ",-35}{disbursementBooks.Count}");
            fileContent.AppendLine($"{"Amount Field Control Total: ",-35}{totalDebit}");
            fileContent.AppendLine($"{"Period Covered: ",-35}{dateFrom}{" to "}{dateTo} ");
            fileContent.AppendLine($"{"Transaction cut-off Date & Time: ",-35}{ViewBag.LastRecord}");
            fileContent.AppendLine($"{"Extracted By: ",-35}{extractedBy.ToUpper()}");
            fileContent.AppendLine();
            fileContent.AppendLine($"{"Field Name"}\t{"Description",-18}\t{"From"}\t{"To"}\t{"Length"}\t{"Example"}");
            fileContent.AppendLine($"{"Date",-8}\t{"Date",-18}\t{"1"}\t{"10"}\t{"10"}\t{firstRecord.Date}");
            fileContent.AppendLine($"{"CVNo",-8}\t{"CV No",-18}\t{"12"}\t{"23"}\t{"12"}\t{firstRecord.CVNo}");
            fileContent.AppendLine($"{"Payee",-8}\t{"Payee",-18}\t{"25"}\t{"124"}\t{"100"}\t{firstRecord.Payee}");
            fileContent.AppendLine($"{"Particulars"}\t{"Particulars",-18}\t{"126"}\t{"325"}\t{"200"}\t{firstRecord.Particulars}");
            fileContent.AppendLine($"{"Bank",-8}\t{"Bank",-18}\t{"327"}\t{"336"}\t{"10"}\t{firstRecord.Bank}");
            fileContent.AppendLine($"{"CheckNo",-8}\t{"Check No",-18}\t{"338"}\t{"357"}\t{"20"}\t{firstRecord.CheckNo}");
            fileContent.AppendLine($"{"CheckDate"}\t{"Check Date",-18}\t{"359"}\t{"368"}\t{"10"}\t{firstRecord.CheckDate}");
            fileContent.AppendLine($"{"ChartOfAccount"}\t{"Chart Of Account"}\t{"370"}\t{"469"}\t{"100"}\t{firstRecord.ChartOfAccount}");
            fileContent.AppendLine($"{"Debit",-8}\t{"Debit",-18}\t{"471"}\t{"488"}\t{"18"}\t{firstRecord.Debit}");
            fileContent.AppendLine($"{"Credit",-8}\t{"Credit",-18}\t{"490"}\t{"507"}\t{"18"}\t{firstRecord.Credit}");
            fileContent.AppendLine();
            fileContent.AppendLine("DISBURSEMENT BOOK");
            fileContent.AppendLine();
            fileContent.AppendLine($"{"Date",-10}\t{"CV No",-12}\t{"Payee",-100}\t{"Particulars",-200}\t{"Bank",-10}\t{"Check No",-20}\t{"Check Date",-10}\t{"Chart Of Account",-100}\t{"Debit",-18}\t{"Credit",-18}");

            // Generate the records
            foreach (var record in disbursementBooks)
            {
                fileContent.AppendLine($"{record.Date.ToString(),-10}\t{record.CVNo,-12}\t{record.Payee,-100}\t{record.Particulars,-200}\t{record.Bank,-10}\t{record.CheckNo,-20}\t{record.CheckDate,-10}\t{record.ChartOfAccount,-100}\t{record.Debit,-18}\t{record.Credit,-18}");
            }
            fileContent.AppendLine(new string('-', 536));
            fileContent.AppendLine($"{"",-10}\t{"",-12}\t{"",-100}\t{"",-200}\t{"",-10}\t{"",-20}\t{"",-10}\t{"TOTAL:",-100}\t{totalDebit,-18}\t{totalCredit,-18}");

            fileContent.AppendLine();
            fileContent.AppendLine($"Software Name: Accounting Administration System (AAS)");
            fileContent.AppendLine($"Version: v1.0");
            fileContent.AppendLine($"Extracted By: {extractedBy.ToUpper()}");
            fileContent.AppendLine($"Date & Time Extracted: {@DateTime.Now.ToString("MM/dd/yyyy hh:mm tt")}");


            // Convert the content to a byte array
            var bytes = Encoding.UTF8.GetBytes(fileContent.ToString());

            // Return the file to the user
            return File(bytes, "text/plain", "DisbursementBookReport.txt");
        }
        #endregion -- Generate Disbursement Book .Txt File --

        #region -- Generate Cash Receipt Book .Txt File --
        public IActionResult GenerateCashReceiptBookTxtFile(ViewModelBook model)
        {
            var dateFrom = model.DateFrom;
            var dateTo = model.DateTo;
            var extractedBy = _userManager.GetUserName(this.User);

            var cashReceiptBooks = _reportRepo.GetCashReceiptBooks(model.DateFrom, model.DateTo);
            if (cashReceiptBooks.Count == 0)
            {
                TempData["error"] = "No Record Found";
                return RedirectToAction(nameof(CashReceiptBook));
            }
            var totalDebit = cashReceiptBooks.Sum(crb => crb.Debit);
            var totalCredit = cashReceiptBooks.Sum(crb => crb.Credit);
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
            fileContent.AppendLine($"Acknowledgement Certificate Control No.: {CS.ACCN}");
            fileContent.AppendLine($"Date Issued: {CS.DateIssued}");
            fileContent.AppendLine();
            fileContent.AppendLine("Accounting Books File Attributes/Layout Definition");
            fileContent.AppendLine("File Name: Cash Receipt Book Report");
            fileContent.AppendLine("File Type: Text File");
            fileContent.AppendLine($"{"Number of Records: ",-35}{cashReceiptBooks.Count}");
            fileContent.AppendLine($"{"Amount Field Control Total: ",-35}{totalDebit}");
            fileContent.AppendLine($"{"Period Covered: ",-35}{dateFrom}{" to "}{dateTo} ");
            fileContent.AppendLine($"{"Transaction cut-off Date & Time: ",-35}{ViewBag.LastRecord}");
            fileContent.AppendLine($"{"Extracted By: ",-35}{extractedBy.ToUpper()}");
            fileContent.AppendLine();
            fileContent.AppendLine($"{"Field Name"}\t{"Description",-18}\t{"From"}\t{"To"}\t{"Length"}\t{"Example"}");
            fileContent.AppendLine($"{"Date",-8}\t{"Date",-18}\t{"1"}\t{"10"}\t{"10"}\t{firstRecord.Date}");
            fileContent.AppendLine($"{"RefNo",-8}\t{"Ref No.",-18}\t{"12"}\t{"23"}\t{"12"}\t{firstRecord.RefNo}");
            fileContent.AppendLine($"{"CustomerName"}\t{"Customer Name",-18}\t{"25"}\t{"40"}\t{"16"}\t{firstRecord.CustomerName}");
            fileContent.AppendLine($"{"Bank",-8}\t{"Bank",-18}\t{"42"}\t{"141"}\t{"100"}\t{firstRecord.Bank}");
            fileContent.AppendLine($"{"CheckNo",-8}\t{"Check No.",-18}\t{"143"}\t{"162"}\t{"20"}\t{firstRecord.CheckNo}");
            fileContent.AppendLine($"{"COA",-8}\t{"Chart Of Account",-18}\t{"164"}\t{"263"}\t{"100"}\t{firstRecord.COA}");
            fileContent.AppendLine($"{"Particulars"}\t{"Particulars",-18}\t{"265"}\t{"464"}\t{"200"}\t{firstRecord.Particulars}");
            fileContent.AppendLine($"{"Debit",-8}\t{"Debit",-18}\t{"466"}\t{"483"}\t{"18"}\t{firstRecord.Debit}");
            fileContent.AppendLine($"{"Credit",-8}\t{"Credit",-18}\t{"485"}\t{"502"}\t{"18"}\t{firstRecord.Credit}");
            fileContent.AppendLine();
            fileContent.AppendLine("CASH RECEIPT BOOK");
            fileContent.AppendLine();
            fileContent.AppendLine($"{"Date",-10}\t{"Ref No.",-12}\t{"Customer Name",-16}\t{"Bank",-100}\t{"Check No.",-20}\t{"Chart Of Account",-100}\t{"Particulars",-200}\t{"Debit",-18}\t{"Credit",-18}");

            // Generate the records
            foreach (var record in cashReceiptBooks)
            {
                fileContent.AppendLine($"{record.Date.ToString(),-10}\t{record.RefNo,-12}\t{record.CustomerName,-16}\t{record.Bank,-100}\t{record.CheckNo,-20}\t{record.COA,-100}\t{record.Particulars,-200}\t{record.Debit,-18}\t{record.Credit,-18}");
            }
            fileContent.AppendLine(new string('-', 528));
            fileContent.AppendLine($"{"",-10}\t{"",-12}\t{"",-16}\t{"",-100}\t{"",-20}\t{"",-100}\t{"TOTAL:",200}\t{totalDebit,-18}\t{totalCredit,-18}");

            fileContent.AppendLine();
            fileContent.AppendLine($"Software Name: Accounting Administration System (AAS)");
            fileContent.AppendLine($"Version: v1.0");
            fileContent.AppendLine($"Extracted By: {extractedBy.ToUpper()}");
            fileContent.AppendLine($"Date & Time Extracted: {@DateTime.Now.ToString("MM/dd/yyyy hh:mm tt")}");


            // Convert the content to a byte array
            var bytes = Encoding.UTF8.GetBytes(fileContent.ToString());

            // Return the file to the user
            return File(bytes, "text/plain", "CashReceiptBookReport.txt");
        }

        #endregion -- Generate Cash Receipt Book .Txt File --

        #region -- Generate General Ledger Book .Txt File --
        public IActionResult GenerateGeneralLedgerBookTxtFile(ViewModelBook model)
        {
            var dateFrom = model.DateFrom;
            var dateTo = model.DateTo;
            var extractedBy = _userManager.GetUserName(this.User);

            var generalBooks = _reportRepo.GetGeneralLedgerBooks(model.DateFrom, model.DateTo);
            if (generalBooks.Count == 0)
            {
                TempData["error"] = "No Record Found";
                return RedirectToAction(nameof(GeneralLedgerBook));
            }
            var totalDebit = generalBooks.Sum(gb => gb.Debit);
            var totalCredit = generalBooks.Sum(gb => gb.Credit);
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
            fileContent.AppendLine($"Acknowledgement Certificate Control No.: {CS.ACCN}");
            fileContent.AppendLine($"Date Issued: {CS.DateIssued}");
            fileContent.AppendLine();
            fileContent.AppendLine("Accounting Books File Attributes/Layout Definition");
            fileContent.AppendLine("File Name: General Ledger Book Report");
            fileContent.AppendLine("File Type: Text File");
            fileContent.AppendLine($"{"Number of Records: ",-35}{generalBooks.Count}");
            fileContent.AppendLine($"{"Amount Field Control Total: ",-35}{totalDebit}");
            fileContent.AppendLine($"{"Period Covered: ",-35}{dateFrom}{" to "}{dateTo} ");
            fileContent.AppendLine($"{"Transaction cut-off Date & Time: ",-35}{ViewBag.LastRecord}");
            fileContent.AppendLine($"{"Extracted By: ",-35}{extractedBy.ToUpper()}");
            fileContent.AppendLine();
            fileContent.AppendLine($"{"Field Name"}\t{"Description"}\t{"From"}\t{"To"}\t{"Length"}\t{"Example"}");
            fileContent.AppendLine($"{"Date",-8}\t{"Date",-8}\t{"1"}\t{"10"}\t{"10"}\t{firstRecord.Date}");
            fileContent.AppendLine($"{"Reference"}\t{"Reference"}\t{"12"}\t{"23"}\t{"12"}\t{firstRecord.Reference}");
            fileContent.AppendLine($"{"Description"}\t{"Description"}\t{"25"}\t{"74"}\t{"50"}\t{firstRecord.Description}");
            fileContent.AppendLine($"{"AccountTitle"}\t{"Account Title"}\t{"76"}\t{"125"}\t{"50"}\t{firstRecord.AccountNo + " " + firstRecord.AccountTitle}");
            fileContent.AppendLine($"{"Debit",-8}\t{"Debit",-8}\t{"127"}\t{"144"}\t{"18"}\t{firstRecord.Debit}");
            fileContent.AppendLine($"{"Credit",-8}\t{"Credit",-8}\t{"146"}\t{"163"}\t{"18"}\t{firstRecord.Credit}");
            fileContent.AppendLine();
            fileContent.AppendLine("GENERAL LEDGER BOOK");
            fileContent.AppendLine();
            fileContent.AppendLine($"{"Date",-10}\t{"Reference",-12}\t{"Description",-50}\t{"Account Title",-50}\t{"Debit",-18}\t{"Credit",-18}");

            // Generate the records
            foreach (var record in generalBooks)
            {
                fileContent.AppendLine($"{record.Date.ToString(),-10}\t{record.Reference,-12}\t{record.Description,-50}\t{record.AccountNo + " " + record.AccountTitle,-50}\t{record.Debit,-18}\t{record.Credit,-18}");
            }
            fileContent.AppendLine(new string('-', 177));
            fileContent.AppendLine($"{"",-10}\t{"",-12}\t{"",-50}\t{"TOTAL:",50}\t{totalDebit,-18}\t{totalCredit,-18}");

            fileContent.AppendLine();
            fileContent.AppendLine($"Software Name: Accounting Administration System (AAS)");
            fileContent.AppendLine($"Version: v1.0");
            fileContent.AppendLine($"Extracted By: {extractedBy.ToUpper()}");
            fileContent.AppendLine($"Date & Time Extracted: {@DateTime.Now.ToString("MM/dd/yyyy hh:mm tt")}");


            // Convert the content to a byte array
            var bytes = Encoding.UTF8.GetBytes(fileContent.ToString());

            // Return the file to the user
            return File(bytes, "text/plain", "GeneralLedgerBookReport.txt");
        }

        #endregion -- Generate General Ledger Book .Txt File --

        #region -- Generate Inventory Book .Txt File --

        public IActionResult GenerateInventoryBookTxtFile(ViewModelBook model)
        {
            var dateFrom = model.DateFrom;
            var dateTo = model.DateTo;
            var extractedBy = _userManager.GetUserName(this.User);

            var inventoryBooks = _reportRepo.GetInventoryBooks(model.DateFrom, model.DateTo);
            if (inventoryBooks.Count == 0)
            {
                TempData["error"] = "No Record Found";
                return RedirectToAction(nameof(InventoryBook));
            }
            var totalAmount = inventoryBooks.Sum(ib => ib.Amount);
            var totalQuantity = inventoryBooks.Sum(ib => ib.Quantity);
            var totalPrice = inventoryBooks.Sum(ib => ib.Price);
            var lastRecord = inventoryBooks.LastOrDefault();
            var firstRecord = inventoryBooks.FirstOrDefault();
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
            fileContent.AppendLine($"Acknowledgement Certificate Control No.: {CS.ACCN}");
            fileContent.AppendLine($"Date Issued: {CS.DateIssued}");
            fileContent.AppendLine();
            fileContent.AppendLine("Accounting Books File Attributes/Layout Definition");
            fileContent.AppendLine("File Name: Inventory Book Report");
            fileContent.AppendLine("File Type: Text File");
            fileContent.AppendLine($"{"Number of Records: ",-35}{inventoryBooks.Count}");
            fileContent.AppendLine($"{"Amount Field Control Total: ",-35}{totalAmount}");
            fileContent.AppendLine($"{"Period Covered: ",-35}{dateFrom}{" to "}{dateTo} ");
            fileContent.AppendLine($"{"Transaction cut-off Date & Time: ",-35}{ViewBag.LastRecord}");
            fileContent.AppendLine($"{"Extracted By: ",-35}{extractedBy.ToUpper()}");
            fileContent.AppendLine();
            fileContent.AppendLine($"{"Field Name"}\t{"Description"}\t{"From"}\t{"To"}\t{"Length"}\t{"Example"}");
            fileContent.AppendLine($"{"Date",-8}\t{"Date",-8}\t{"1"}\t{"10"}\t{"10"}\t{firstRecord.Date}");
            fileContent.AppendLine($"{"ProductCode",-8}\t{"Product Code",-8}\t{"12"}\t{"31"}\t{"20"}\t{firstRecord.ProductCode}");
            fileContent.AppendLine($"{"ProductName",-8}\t{"Product Name",-8}\t{"33"}\t{"82"}\t{"50"}\t{firstRecord.ProductName}");
            fileContent.AppendLine($"{"Unit",-8}\t{"Unit",-8}\t{"84"}\t{"85"}\t{"2"}\t{firstRecord.Unit}");
            fileContent.AppendLine($"{"Quantity",-8}\t{"Quantity",-8}\t{"87"}\t{"104"}\t{"18"}\t{firstRecord.Quantity}");
            fileContent.AppendLine($"{"Price",-8}\t{"Price Per Unit",-8}\t{"106"}\t{"123"}\t{"18"}\t{firstRecord.Price}");
            fileContent.AppendLine($"{"Amount",-8}\t{"Amount",-8}\t{"125"}\t{"142"}\t{"18"}\t{firstRecord.Amount}");
            fileContent.AppendLine();
            fileContent.AppendLine("INVENTORY BOOK");
            fileContent.AppendLine();
            fileContent.AppendLine($"{"Date",-10}\t{"Product Code",-20}\t{"Product Name",-50}\t{"Unit",-2}\t{"Quantity",-18}\t{"Price Per Unit",-18}\t{"Amount",-18}");

            // Generate the records
            foreach (var record in inventoryBooks)
            {
                fileContent.AppendLine($"{record.Date.ToString(),-10}\t{record.ProductCode,-20}\t{record.ProductName,-50}\t{record.Unit,-2}\t{record.Quantity,-18}\t{record.Price,-18}\t{record.Amount,-18}");
            }
            fileContent.AppendLine(new string('-', 160));
            fileContent.AppendLine($"{"",-10}\t{"",-20}\t{"",-50}\t{"TOTAL:",2}\t{totalQuantity,-18}\t{totalPrice,-18}\t{totalAmount,-18}");

            fileContent.AppendLine();
            fileContent.AppendLine($"Software Name: Accounting Administration System (AAS)");
            fileContent.AppendLine($"Version: v1.0");
            fileContent.AppendLine($"Extracted By: {extractedBy.ToUpper()}");
            fileContent.AppendLine($"Date & Time Extracted: {@DateTime.Now.ToString("MM/dd/yyyy hh:mm tt")}");


            // Convert the content to a byte array
            var bytes = Encoding.UTF8.GetBytes(fileContent.ToString());

            // Return the file to the user
            return File(bytes, "text/plain", "InventoryBookReport.txt");
        }

        #endregion -- Generate Inventory Book .Txt File --

        #region -- Generate Journal Book .Txt File --
        public IActionResult GenerateJournalBookTxtFile(ViewModelBook model)
        {
            var dateFrom = model.DateFrom;
            var dateTo = model.DateTo;
            var extractedBy = _userManager.GetUserName(this.User);

            var journalBooks = _reportRepo.GetJournalBooks(model.DateFrom, model.DateTo);
            if (journalBooks.Count == 0)
            {
                TempData["error"] = "No Record Found";
                return RedirectToAction(nameof(JournalBook));
            }
            var lastRecord = journalBooks.LastOrDefault();
            var firstRecord = journalBooks.FirstOrDefault();
            var totalDebit = journalBooks.Sum(jb => jb.Debit);
            var totalCredit = journalBooks.Sum(jb => jb.Credit);
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
            fileContent.AppendLine($"Acknowledgement Certificate Control No.: {CS.ACCN}");
            fileContent.AppendLine($"Date Issued: {CS.DateIssued}");
            fileContent.AppendLine();
            fileContent.AppendLine("Accounting Books File Attributes/Layout Definition");
            fileContent.AppendLine("File Name: Journal Book Report");
            fileContent.AppendLine("File Type: Text File");
            fileContent.AppendLine($"{"Number of Records: ",-35}{journalBooks.Count}");
            fileContent.AppendLine($"{"Amount Field Control Total: ",-35}{totalDebit}");
            fileContent.AppendLine($"{"Period Covered: ",-35}{dateFrom}{" to "}{dateTo} ");
            fileContent.AppendLine($"{"Transaction cut-off Date & Time: ",-35}{ViewBag.LastRecord}");
            fileContent.AppendLine($"{"Extracted By: ",-35}{extractedBy.ToUpper()}");
            fileContent.AppendLine();
            fileContent.AppendLine($"{"Field Name"}\t{"Description"}\t{"From"}\t{"To"}\t{"Length"}\t{"Example"}");
            fileContent.AppendLine($"{"Date",-8}\t{"Date",-8}\t{"1"}\t{"10"}\t{"10"}\t{firstRecord.Date}");
            fileContent.AppendLine($"{"Reference",-8}\t{"Reference",-8}\t{"12"}\t{"23"}\t{"12"}\t{firstRecord.Reference}");
            fileContent.AppendLine($"{"Description",-8}\t{"Description",-8}\t{"25"}\t{"74"}\t{"50"}\t{firstRecord.Description}");
            fileContent.AppendLine($"{"AccountTitle",-8}\t{"Account Title",-8}\t{"76"}\t{"125"}\t{"50"}\t{firstRecord.AccountTitle}");
            fileContent.AppendLine($"{"Debit",-8}\t{"Debit",-8}\t{"127"}\t{"144"}\t{"18"}\t{firstRecord.Debit}");
            fileContent.AppendLine($"{"Credit",-8}\t{"Credit",-8}\t{"146"}\t{"163"}\t{"18"}\t{firstRecord.Credit}");
            fileContent.AppendLine();
            fileContent.AppendLine("JOURNAL BOOK");
            fileContent.AppendLine();
            fileContent.AppendLine($"{"Date",-10}\t{"Reference",-12}\t{"Description",-50}\t{"Account Title",-50}\t{"Debit",-18}\t{"Credit",-18}");

            // Generate the records
            foreach (var record in journalBooks)
            {
                fileContent.AppendLine($"{record.Date.ToString(),-10}\t{record.Reference,-12}\t{record.Description,-50}\t{record.AccountTitle,-50}\t{record.Debit,-18}\t{record.Credit,-18}");
            }
            fileContent.AppendLine(new string('-', 176));
            fileContent.AppendLine($"{"",-10}\t{"",-12}\t{"",-50}\t{"TOTAL:",50}\t{totalDebit,-18}\t{totalCredit,-18}");

            fileContent.AppendLine();
            fileContent.AppendLine($"Software Name: Accounting Administration System (AAS)");
            fileContent.AppendLine($"Version: v1.0");
            fileContent.AppendLine($"Extracted By: {extractedBy.ToUpper()}");
            fileContent.AppendLine($"Date & Time Extracted: {@DateTime.Now.ToString("MM/dd/yyyy hh:mm tt")}");


            // Convert the content to a byte array
            var bytes = Encoding.UTF8.GetBytes(fileContent.ToString());

            // Return the file to the user
            return File(bytes, "text/plain", "JournalBookReport.txt");
        }

        #endregion -- Generate Journal Book .Txt File --

        #region -- Generate Purchase Book .Txt File --
        public IActionResult GeneratePurchaseBookTxtFile(ViewModelBook model, string? selectedFiltering, string? poListFrom, string? poListTo)
        {
            var dateFrom = model.DateFrom;
            var dateTo = model.DateTo;
            var extractedBy = _userManager.GetUserName(this.User);

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
            if (purchaseOrders.Count == 0)
            {
                TempData["error"] = "No Record Found";
                return RedirectToAction(nameof(PurchaseBook));
            }
            var totalAmount = purchaseOrders.Sum(sb => sb.Amount);
            var totalVatAmount = purchaseOrders.Sum(sb => sb.VatAmount);
            var totalWhtAmount = purchaseOrders.Sum(sb => sb.WhtAmount);
            var totalNetPurchases = purchaseOrders.Sum(sb => sb.NetPurchases);
            var lastRecord = purchaseOrders.LastOrDefault();
            var firstRecord = purchaseOrders.FirstOrDefault();
            if (lastRecord != null)
            {
                ViewBag.LastRecord = lastRecord.CreatedDate;
            }
            ViewBag.SelectedFiltering = selectedFiltering;

            var fileContent = new StringBuilder();

            fileContent.AppendLine($"TAXPAYER'S NAME: Filpride Resources Inc.");
            fileContent.AppendLine($"TIN: 000-216-589-00000");
            fileContent.AppendLine($"ADDRESS: 57 Westgate Office, Sampson Road, CBD, Subic Bay Freeport Zone, Kalaklan, Olongapo City, 2200 Zambales, Philippines");
            fileContent.AppendLine();
            fileContent.AppendLine($"Accounting System: Accounting Administration System");
            fileContent.AppendLine($"Acknowledgement Certificate Control No.: {CS.ACCN}");
            fileContent.AppendLine($"Date Issued: {CS.DateIssued}");
            fileContent.AppendLine();
            fileContent.AppendLine("Accounting Books File Attributes/Layout Definition");
            fileContent.AppendLine("File Name: Purchase Journal Book Report");
            fileContent.AppendLine("File Type: Text File");
            fileContent.AppendLine($"{"Number of Records: ",-35}{purchaseOrders.Count}");
            fileContent.AppendLine($"{"Amount Field Control Total: ",-35}{totalAmount}");
            fileContent.AppendLine($"{"Period Covered: ",-35}{dateFrom}{" to "}{dateTo} ");
            fileContent.AppendLine($"{"Transaction cut-off Date & Time: ",-35}{ViewBag.LastRecord}");
            fileContent.AppendLine($"{"Extracted By: ",-35}{extractedBy.ToUpper()}");
            fileContent.AppendLine();
            fileContent.AppendLine($"{"Field Name",-18}\t{"Description",-18}\t{"From"}\t{"To"}\t{"Length"}\t{"Example"}");
            fileContent.AppendLine($"{"Date",-18}\t{"Date",-18}\t{"1"}\t{"10"}\t{"10"}\t{firstRecord.Date}");
            fileContent.AppendLine($"{"SupplierName",-18}\t{"Supplier Name",-18}\t{"12"}\t{"61"}\t{"50"}\t{firstRecord.SupplierName}");
            fileContent.AppendLine($"{"SupplierTin",-18}\t{"Supplier TIN",-18}\t{"63"}\t{"82"}\t{"20"}\t{firstRecord.SupplierTin}");
            fileContent.AppendLine($"{"SupplierAddress",-18}\t{"Supplier Address",-18}\t{"84"}\t{"283"}\t{"200"}\t{firstRecord.SupplierAddress}");
            fileContent.AppendLine($"{"PONo",-18}\t{"PO No.",-18}\t{"285"}\t{"296"}\t{"12"}\t{firstRecord.PONo}");
            fileContent.AppendLine($"{"DocumentNo",-18}\t{"Document No",-18}\t{"298"}\t{"309"}\t{"12"}\t{firstRecord.DocumentNo}");
            fileContent.AppendLine($"{"Description",-18}\t{"Description",-18}\t{"311"}\t{"360"}\t{"50"}\t{firstRecord.Description}");
            fileContent.AppendLine($"{"Amount",-18}\t{"Amount",-18}\t{"362"}\t{"379"}\t{"18"}\t{firstRecord.Amount}");
            fileContent.AppendLine($"{"VatAmount",-18}\t{"Vat Amount",-18}\t{"381"}\t{"398"}\t{"18"}\t{firstRecord.VatAmount}");
            fileContent.AppendLine($"{"DefAmount",-18}\t{"Def VAT Amount",-18}\t{"400"}\t{"417"}\t{"18"}\t{0.00}");
            fileContent.AppendLine($"{"WhtAmount",-18}\t{"WHT Amount",-18}\t{"419"}\t{"436"}\t{"18"}\t{firstRecord.WhtAmount}");
            fileContent.AppendLine($"{"NetPurchases",-18}\t{"Net Purchases",-18}\t{"438"}\t{"455"}\t{"18"}\t{firstRecord.NetPurchases}");
            fileContent.AppendLine();
            fileContent.AppendLine("PURCHASE BOOK");
            fileContent.AppendLine();
            fileContent.AppendLine($"{"Date",-10}\t{"Supplier Name",-50}\t{"Supplier TIN",-20}\t{"Supplier Address",-200}\t{"PO No.",-12}\t{"Document No",-12}\t{"Description",-50}\t{"Amount",-18}\t{"Vat Amount",-18}\t{"Def VAT Amount",-18}\t{"WHT Amount",-18}\t{"Net Purchases",-18}");

            // Generate the records
            foreach (var record in purchaseOrders)
            {
                fileContent.AppendLine($"{record.Date.ToString(),-10}\t{record.SupplierName,-50}\t{record.SupplierTin,-20}\t{record.SupplierAddress,-200}\t{record.PONo,-12}\t{record.DocumentNo,-12}\t{record.Description,-50}\t{record.Amount,-18}\t{record.VatAmount,-18}\t{0.00m,-18}\t{record.WhtAmount,-18}\t{record.NetPurchases,-18}");
            }
            fileContent.AppendLine(new string('-', 503));
            fileContent.AppendLine($"{"",-10}\t{"",-50}\t{"",-20}\t{"",-200}\t{"",-12}\t{"",-12}\t{"TOTAL:",50}\t{totalAmount,-18}\t{totalVatAmount,-18}\t{0.00m,-18}\t{totalWhtAmount,-18}\t{totalNetPurchases,-18}");

            fileContent.AppendLine();
            fileContent.AppendLine($"Software Name: Accounting Administration System (AAS)");
            fileContent.AppendLine($"Version: v1.0");
            fileContent.AppendLine($"Extracted By: {extractedBy.ToUpper()}");
            fileContent.AppendLine($"Date & Time Extracted: {@DateTime.Now.ToString("MM/dd/yyyy hh:mm tt")}");


            // Convert the content to a byte array
            var bytes = Encoding.UTF8.GetBytes(fileContent.ToString());

            // Return the file to the user
            return File(bytes, "text/plain", "PurchaseBookReport.txt");
        }

        #endregion -- Generate Purchase Book .Txt File --

        #region -- Generate Sales Book .Txt File --
        public IActionResult GenerateSalesBookTxtFile(ViewModelBook model, string? selectedDocument, string? soaList, string? siList)
        {
            var dateFrom = model.DateFrom;
            var dateTo = model.DateTo;
            var extractedBy = _userManager.GetUserName(this.User);

            if (soaList != null || siList != null)
            {
                return RedirectToAction("TransactionReportsInSOA", new { soaList = soaList, siList = siList });
            }

            var salesBook = _reportRepo.GetSalesBooks(model.DateFrom, model.DateTo, selectedDocument);
            if (salesBook.Count == 0)
            {
                TempData["error"] = "No Record Found";
                return RedirectToAction(nameof(SalesBook));
            }
            var totalAmount = salesBook.Sum(sb => sb.Amount);
            var totalVatAmount = salesBook.Sum(sb => sb.VatAmount);
            var totalVatableSales = salesBook.Sum(sb => sb.VatableSales);
            var totalVatExemptSales = salesBook.Sum(sb => sb.VatExemptSales);
            var totalZeroRatedSales = salesBook.Sum(sb => sb.ZeroRated);
            var totalDiscount = salesBook.Sum(sb => sb.Discount);
            var totalNetSales = salesBook.Sum(sb => sb.NetSales);
            var lastRecord = salesBook.LastOrDefault();
            var firstRecord = salesBook.FirstOrDefault();
            if (lastRecord != null)
            {
                ViewBag.LastRecord = lastRecord.CreatedDate;
            }
            ViewBag.SelectedDocument = selectedDocument;

            var fileContent = new StringBuilder();

            fileContent.AppendLine($"TAXPAYER'S NAME: Filpride Resources Inc.");
            fileContent.AppendLine($"TIN: 000-216-589-00000");
            fileContent.AppendLine($"ADDRESS: 57 Westgate Office, Sampson Road, CBD, Subic Bay Freeport Zone, Kalaklan, Olongapo City, 2200 Zambales, Philippines");
            fileContent.AppendLine();
            fileContent.AppendLine($"Accounting System: Accounting Administration System");
            fileContent.AppendLine($"Acknowledgement Certificate Control No.: {CS.ACCN}");
            fileContent.AppendLine($"Date Issued: {CS.DateIssued}");
            fileContent.AppendLine();
            fileContent.AppendLine("Accounting Books File Attributes/Layout Definition");
            fileContent.AppendLine("File Name: Sales Book Report");
            fileContent.AppendLine("File Type: Text File");
            fileContent.AppendLine($"{"Number of Records: ",-35}{salesBook.Count}");
            fileContent.AppendLine($"{"Amount Field Control Total: ",-35}{totalAmount}");
            fileContent.AppendLine($"{"Period Covered: ",-35}{dateFrom}{" to "}{dateTo} ");
            fileContent.AppendLine($"{"Transaction cut-off Date & Time: ",-35}{ViewBag.LastRecord}");
            fileContent.AppendLine($"{"Extracted By: ",-35}{extractedBy.ToUpper()}");
            fileContent.AppendLine();
            fileContent.AppendLine($"{"Field Name",-18}\t{"Description",-18}\t{"From"}\t{"To"}\t{"Length"}\t{"Example"}");
            fileContent.AppendLine($"{"TransactionDate",-18}\t{"Tran. Date",-18}\t{"1"}\t{"10"}\t{"10"}\t{firstRecord.TransactionDate}");
            fileContent.AppendLine($"{"SerialNo",-18}\t{"Serial Number",-18}\t{"12"}\t{"23"}\t{"12"}\t{firstRecord.SerialNo}");
            fileContent.AppendLine($"{"Date",-18}\t{"Customer Name",-18}\t{"25"}\t{"124"}\t{"100"}\t{firstRecord.SoldTo}");
            fileContent.AppendLine($"{"TinNo",-18}\t{"Tin#",-18}\t{"126"}\t{"145"}\t{"20"}\t{firstRecord.TinNo}");
            fileContent.AppendLine($"{"Address",-18}\t{"Address",-18}\t{"147"}\t{"346"}\t{"200"}\t{firstRecord.Address}");
            fileContent.AppendLine($"{"Description",-18}\t{"Description",-18}\t{"348"}\t{"397"}\t{"50"}\t{firstRecord.Description}");
            fileContent.AppendLine($"{"Amount",-18}\t{"Amount",-18}\t{"399"}\t{"416"}\t{"18"}\t{firstRecord.Amount}");
            fileContent.AppendLine($"{"VatAmount",-18}\t{"Vat Amount",-18}\t{"418"}\t{"435"}\t{"18"}\t{firstRecord.VatAmount}");
            fileContent.AppendLine($"{"VatableSales",-18}\t{"Vatable Sales",-18}\t{"437"}\t{"454"}\t{"18"}\t{firstRecord.VatableSales}");
            fileContent.AppendLine($"{"VatExemptSales",-18}\t{"Vat-Exempt Sales",-18}\t{"456"}\t{"473"}\t{"18"}\t{firstRecord.VatExemptSales}");
            fileContent.AppendLine($"{"ZeroRated",-18}\t{"Zero-Rated Sales",-18}\t{"475"}\t{"492"}\t{"18"}\t{firstRecord.ZeroRated}");
            fileContent.AppendLine($"{"Discount",-18}\t{"Discount",-18}\t{"494"}\t{"511"}\t{"18"}\t{firstRecord.Discount}");
            fileContent.AppendLine($"{"NetSales",-18}\t{"Net Sales",-18}\t{"513"}\t{"530"}\t{"18"}\t{firstRecord.NetSales}");
            fileContent.AppendLine();
            fileContent.AppendLine("SALES BOOK");
            fileContent.AppendLine();
            fileContent.AppendLine($"{"Tran. Date",-10}\t{"Serial Number",-12}\t{"Customer Name",-100}\t{"Tin#",-20}\t{"Address",-200}\t{"Description",-50}\t{"Amount",-18}\t{"Vat Amount",-18}\t{"Vatable Sales",-18}\t{"Vat-Exempt Sales",-18}\t{"Zero-Rated Sales",-18}\t{"Discount",-18}\t{"Net Sales",-18}");

            // Generate the records
            foreach (var record in salesBook)
            {
                fileContent.AppendLine($"{record.TransactionDate.ToString(),-10}\t{record.SerialNo,-12}\t{record.SoldTo,-100}\t{record.TinNo,-20}\t{record.Address,-200}\t{record.Description,-50}\t{record.Amount,-18}\t{record.VatAmount,-18}\t{record.VatableSales,-18}\t{record.VatExemptSales,-18}\t{record.ZeroRated,-18}\t{record.Discount,-18}\t{record.NetSales,-18}");
            }
            fileContent.AppendLine(new string('-', 580));
            fileContent.AppendLine($"{"",-10}\t{"",-12}\t{"",-100}\t{"",-20}\t{"",-200}\t{"TOTAL:",50}\t{totalAmount,-18}\t{totalVatAmount,-18}\t{totalVatableSales,-18}\t{totalVatExemptSales,-18}\t{totalZeroRatedSales,-18}\t{totalDiscount,-18}\t{totalNetSales,-18}");

            fileContent.AppendLine();
            fileContent.AppendLine($"Software Name: Accounting Administration System (AAS)");
            fileContent.AppendLine($"Version: v1.0");
            fileContent.AppendLine($"Extracted By: {extractedBy.ToUpper()}");
            fileContent.AppendLine($"Date & Time Extracted: {@DateTime.Now.ToString("MM/dd/yyyy hh:mm tt")}");


            // Convert the content to a byte array
            var bytes = Encoding.UTF8.GetBytes(fileContent.ToString());

            // Return the file to the user
            return File(bytes, "text/plain", "SalesBookReport.txt");
        }

        #endregion -- Generate Sales Book .Txt File --
    }
}