using Accounting_System.Data;
using Accounting_System.Repository;
using Microsoft.AspNetCore.Mvc;
using Accounting_System.Models;

namespace Accounting_System.Controllers
{
    public class SalesInvoiceController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly SalesInvoiceRepo _salesInvoiceRepo;

        public SalesInvoiceController(ApplicationDbContext dbContext, SalesInvoiceRepo salesInvoiceRepo)
        {
            _dbContext = dbContext;
            _salesInvoiceRepo = salesInvoiceRepo;
        }

        public async Task<IActionResult> Index()
        {
            var salesInvoice = await _salesInvoiceRepo.GetSalesInvoicesAsync();

            return View(salesInvoice);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new SalesInvoice());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(SalesInvoice sales)
        {
            if (ModelState.IsValid)
            {
                sales.Amount = sales.Quantity * sales.UnitPrice;
                _dbContext.Add(sales);
                _dbContext.SaveChanges();

                return RedirectToAction("Index");
            }
            else
            {
                ModelState.AddModelError("", "The information you submitted is not valid!");
                return View();
            }
        }
    }
}