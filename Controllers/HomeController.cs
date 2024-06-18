using Accounting_System.Data;
using Accounting_System.Models;
using Accounting_System.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Accounting_System.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _dbContext;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext dbContext, UserManager<IdentityUser> userManager)
        {
            _logger = logger;
            this._userManager = userManager;
            _dbContext = dbContext;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            ViewData["Id"] = _userManager.GetUserName(this.User);

            #region -- Query to count how many in each document to show in graph --

            var salesInvoiceSummary = await _dbContext.SalesInvoices
                .GroupBy(i => 1)
                .Select(g => new List<int>
                {
                g.Count(i => i.IsPosted),
                g.Count(i => i.IsCanceled),
                g.Count(i => i.IsVoided)
                })
                .FirstOrDefaultAsync(cancellationToken) ?? new List<int> { 0, 0, 0 };

            var serviceInvoiceSummary = await _dbContext.ServiceInvoices
                .GroupBy(i => 1)
                .Select(g => new List<int>
                {
                g.Count(i => i.IsPosted),
                g.Count(i => i.IsCanceled),
                g.Count(i => i.IsVoided)
                })
                .FirstOrDefaultAsync(cancellationToken) ?? new List<int> { 0, 0, 0 };

            var collectionReceiptSummary = await _dbContext.CollectionReceipts
                .GroupBy(i => 1)
                .Select(g => new List<int>
                {
                g.Count(i => i.IsPosted),
                g.Count(i => i.IsCanceled),
                g.Count(i => i.IsVoided)
                })
                .FirstOrDefaultAsync(cancellationToken) ?? new List<int> { 0, 0, 0 };

            var debitMemoSummary = await _dbContext.DebitMemos
                .GroupBy(i => 1)
                .Select(g => new List<int>
                {
                g.Count(i => i.IsPosted),
                g.Count(i => i.IsCanceled),
                g.Count(i => i.IsVoided)
                })
                .FirstOrDefaultAsync(cancellationToken) ?? new List<int> { 0, 0, 0 };

            var creditMemoSummary = await _dbContext.CreditMemos
                .GroupBy(i => 1)
                .Select(g => new List<int>
                {
                g.Count(i => i.IsPosted),
                g.Count(i => i.IsCanceled),
                g.Count(i => i.IsVoided)
                })
                .FirstOrDefaultAsync(cancellationToken) ?? new List<int> { 0, 0, 0 };

            var purchaseOrderSummary = await _dbContext.PurchaseOrders
                .GroupBy(i => 1)
                .Select(g => new List<int>
                {
                g.Count(i => i.IsPosted),
                g.Count(i => i.IsCanceled),
                g.Count(i => i.IsVoided)
                })
                .FirstOrDefaultAsync(cancellationToken) ?? new List<int> { 0, 0, 0 };

            var receivingReportSummary = await _dbContext.ReceivingReports
                .GroupBy(i => 1)
                .Select(g => new List<int>
                {
                g.Count(i => i.IsPosted),
                g.Count(i => i.IsCanceled),
                g.Count(i => i.IsVoided)
                })
                .FirstOrDefaultAsync(cancellationToken) ?? new List<int> { 0, 0, 0 };

            var checkVoucherSummary = await _dbContext.CheckVoucherHeaders
                .GroupBy(i => 1)
                .Select(g => new List<int>
                {
                g.Count(i => i.IsPosted),
                g.Count(i => i.IsCanceled),
                g.Count(i => i.IsVoided)
                })
                .FirstOrDefaultAsync(cancellationToken) ?? new List<int> { 0, 0, 0 };

            var journalVoucherSummary = await _dbContext.JournalVoucherHeaders
                .GroupBy(i => 1)
                .Select(g => new List<int>
                {
                g.Count(i => i.IsPosted),
                g.Count(i => i.IsCanceled),
                g.Count(i => i.IsVoided)
                })
                .FirstOrDefaultAsync(cancellationToken) ?? new List<int> { 0, 0, 0 };

            #endregion -- Query to count how many in each document to show in graph --

            #region -- Query of length to change the range of graph --

            var maxCountSalesInvoice = salesInvoiceSummary.Max();
            var maxCountServiceInvoice = serviceInvoiceSummary.Max();
            var maxCountCR = collectionReceiptSummary.Max();
            var maxCountDM = debitMemoSummary.Max();
            var maxCountCM = creditMemoSummary.Max();
            var maxCountPO = purchaseOrderSummary.Max();
            var maxCountRR = receivingReportSummary.Max();
            var maxCountCV = checkVoucherSummary.Max();
            var maxCountJV = journalVoucherSummary.Max();

            var maxCounts = new List<int>
            {
                maxCountSalesInvoice,
                maxCountServiceInvoice,
                maxCountCR,
                maxCountDM,
                maxCountCM,
                maxCountPO,
                maxCountRR,
                maxCountCV,
                maxCountJV
            };

            var overallMaxValue = maxCounts.Max();

            #endregion -- Query of length to change the range of graph --

            #region -- query that count the total record in each master file --

            var totalCustomers = await _dbContext.Customers.ToListAsync(cancellationToken);
            var totalProducts = await _dbContext.Products.ToListAsync(cancellationToken);
            var totalServices = await _dbContext.Services.ToListAsync(cancellationToken);
            var totalSuppliers = await _dbContext.Suppliers.ToListAsync(cancellationToken);
            var totalBankAccounts = await _dbContext.BankAccounts.ToListAsync(cancellationToken);
            var totalChartOfAccount = await _dbContext.ChartOfAccounts.ToListAsync(cancellationToken);

            #endregion -- query that count the total record in each master file --

            #region -- ViewModel --

            var viewModel = new HomePageViewModel
            {
                SalesInvoice = salesInvoiceSummary,
                ServiceInvoice = serviceInvoiceSummary,
                CollectionReceipt = collectionReceiptSummary,
                DebitMemo = debitMemoSummary,
                CreditMemo = creditMemoSummary,
                PurchaseOrder = purchaseOrderSummary,
                ReceivingReport = receivingReportSummary,
                CheckVoucher = checkVoucherSummary,
                JournalVoucher = journalVoucherSummary,
                OverallMaxValue = overallMaxValue,
                Customers = totalCustomers.Count(),
                Products = totalProducts.Count(),
                Services = totalServices.Count(),
                Suppliers = totalSuppliers.Count(),
                BankAccounts = totalBankAccounts.Count(),
                ChartOfAccount = totalChartOfAccount.Count(),
            };

            return View(viewModel);

            #endregion -- ViewModel --
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}