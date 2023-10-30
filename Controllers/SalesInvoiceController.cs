using Accounting_System.Data;
using Accounting_System.Repository;
using Microsoft.AspNetCore.Mvc;
using Accounting_System.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace Accounting_System.Controllers
{
    [Authorize]
    public class SalesInvoiceController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly SalesInvoiceRepo _salesInvoiceRepo;

        private readonly ILogger<HomeController> _logger;

        private readonly UserManager<IdentityUser> _userManager;

        public SalesInvoiceController(ILogger<HomeController> logger, ApplicationDbContext dbContext, SalesInvoiceRepo salesInvoiceRepo, UserManager<IdentityUser> userManager)
        {
            _dbContext = dbContext;
            _salesInvoiceRepo = salesInvoiceRepo;
            _logger = logger;
            this._userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var salesInvoice = await _salesInvoiceRepo.GetSalesInvoicesAsync();
            
            return View(salesInvoice);
        }

        public async Task<IActionResult> Post(int invoiceId)
        {
            var model = await _dbContext.SalesInvoices.FindAsync(invoiceId);

            if (model != null)
            {
                if (!model.IsPosted)
                {
                    model.IsPosted = true;
                    await _dbContext.SaveChangesAsync();
                    TempData["success"] = "Sales Invoice has been Posted.";
                }
                else if (!model.IsPosted || !model.IsVoid)
                {
                    if (!model.IsVoid)
                    {
                        model.IsVoid = true;
                        await _dbContext.SaveChangesAsync();
                        TempData["success"] = "Sales Invoice has been Voided.";
                    }
                }
            }
            
            return Redirect("Index");
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
            viewModel.Products = _dbContext.Products
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.Name
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

                sales.CreatedBy = _userManager.GetUserName(this.User);
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
                    CustomerType = customer.CustomerType,
                    WithHoldingTax = customer.WithHoldingTax
                    // Add other properties as needed
                });
            }
            return Json(null); // Return null if no matching customer is found
        }

        [HttpGet]
        public JsonResult GetProductDetails(int productId)
        {
            var product = _dbContext.Products.FirstOrDefault(c => c.Id == productId);
            if (product != null)
            {
                return Json(new
                {
                    ProductName = product.Name,
                    ProductUnit = product.Unit
                });
            }
            return Json(null); // Return null if no matching product is found
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
                existingModel.OtherRefNo = model.OtherRefNo;
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


        public async Task<IActionResult> PrintInvoice(int id)
        {
            var sales = await _salesInvoiceRepo.FindSalesInvoice(id);
            return View(sales);
        }

        public async Task<IActionResult> PrintedInvoice(int id)
        {
            var sales = await _salesInvoiceRepo.FindSalesInvoice(id);
            if (sales != null)
            {
                if (sales.OriginalCopy == true)
                {
                    sales.OriginalCopy = false;
                    await _dbContext.SaveChangesAsync();
                }
            }
            return RedirectToAction("PrintInvoice", new {id = id});
        }
    }
}