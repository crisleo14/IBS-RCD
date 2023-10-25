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

        private readonly ILogger<HomeController> _logger;

        public SalesInvoiceController(ILogger<HomeController> logger, ApplicationDbContext dbContext, SalesInvoiceRepo salesInvoiceRepo)
        {
            _dbContext = dbContext;
            _salesInvoiceRepo = salesInvoiceRepo;
            _logger = logger;
        }

        public async Task<IActionResult> Index(int id)
        {
            var salesInvoice = await _salesInvoiceRepo.GetSalesInvoicesAsync();
            var model = await _dbContext.SalesInvoices.FindAsync(id);

            if (model != null)
            {
                if (model.IsPosted != true)
                {
                    model.IsPosted = true;
                    await _dbContext.SaveChangesAsync();
                }
                else if (model.IsPosted == false || model.IsVoid != true)
                {

                    if (model.IsVoid != true)
                    {
                        model.IsVoid = true;
                        await _dbContext.SaveChangesAsync();
                    }
                }
            }
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SalesInvoice model)
        {
            try
            {
                var existingModel = await _salesInvoiceRepo.FindSalesInvoice(model.Id);

                if (existingModel == null)
                {
                    return NotFound(); // Return a "Not Found" response when the entity is not found.
                }

                existingModel.TransactionDate = model.TransactionDate;
                existingModel.RefDrNo = model.RefDrNo;
                existingModel.PoNo = model.PoNo;
                existingModel.ProductNo = model.ProductNo;
                existingModel.Quantity = model.Quantity;
                existingModel.UnitPrice = model.UnitPrice;
                existingModel.Amount = model.Quantity * model.UnitPrice;
                existingModel.Remarks = model.Remarks;

                // Save the changes to the database
                await _dbContext.SaveChangesAsync();

                return RedirectToAction("Index"); // Redirect to a success page or the index page
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred.");
                return StatusCode(500, "An error occurred. Please try again later.");
            }
        }
        [HttpGet]
        public IActionResult PrintInvoice()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> PrintInvoice(int id)
        {
            var sales = _dbContext.SalesInvoices.FirstOrDefault(x => x.Id == id);
            return View(sales);
        }

    }
}
