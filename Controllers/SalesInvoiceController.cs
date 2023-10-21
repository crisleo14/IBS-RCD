using Accounting_System.Data;
using Accounting_System.Repository;
using Microsoft.AspNetCore.Mvc;
using Accounting_System.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

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
            var viewModel = new SalesInvoice();
            viewModel.Customers = _dbContext.Customers
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                })
                .ToList();

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SalesInvoice sales)
        {
            if (ModelState.IsValid)
            {
                var lastSerialNo = await _salesInvoiceRepo.GetLastSerialNo();

                sales.SerialNo = lastSerialNo;
                sales.Amount = sales.Quantity * sales.UnitPrice;
                _dbContext.Add(sales);
                _dbContext.SaveChanges();

                return RedirectToAction("Index");
            }
            else
            {
                ModelState.AddModelError("", "The information you submitted is not valid!");
                return View(sales);
            }
        }

        [HttpGet]
        public JsonResult GetCustomerDetails(int customerId)
        {
            var customer = _dbContext.Customers.FirstOrDefault(c => c.Id == customerId);
            if (customer != null)
            {
                return Json(new
                {
                    SoldTo = customer.Name,
                    Address = customer.Address,
                    TinNo = customer.TinNo,
                    BusinessStyle = customer.BusinessStyle,
                    Terms = customer.Terms,
                    CustomerType = customer.CustomerType
                    // Add other properties as needed
                });
            }
            return Json(null); // Return null if no matching customer is found
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var salesInvoice = await _salesInvoiceRepo.FindSalesInvoice(id);
                return View(salesInvoice);
            }
            catch (Exception ex)
            {
                // Handle other exceptions, log them, and return an error response.
                _logger.LogError(ex, "An error occurred.");
                return StatusCode(500, "An error occurred. Please try again later.");
            }
        }
    }
}