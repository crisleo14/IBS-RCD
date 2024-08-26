using Accounting_System.Data;
using Accounting_System.Models;
using Accounting_System.Models.Reports;
using Accounting_System.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Accounting_System.Controllers
{
    [Authorize]
    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly CustomerRepo _customerRepo;

        private readonly UserManager<IdentityUser> _userManager;

        public CustomerController(ApplicationDbContext dbContext, CustomerRepo customerRepo, UserManager<IdentityUser> userManager)
        {
            _dbContext = dbContext;
            _customerRepo = customerRepo;
            this._userManager = userManager;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var customer = await _customerRepo.GetCustomersAsync(cancellationToken);

            return View(customer);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new Customer());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer customer, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                if (await _customerRepo.IsCustomerExist(customer.Name, cancellationToken))
                {
                    ModelState.AddModelError("Name", "Customer already exist!");
                    return View(customer);
                }

                if (await _customerRepo.IsTinNoExist(customer.TinNo, cancellationToken))
                {
                    ModelState.AddModelError("TinNo", "Tin# already exist!");
                    return View(customer);
                }

                customer.Number = await _customerRepo.GetLastNumber(cancellationToken);
                customer.CreatedBy = _userManager.GetUserName(this.User);
                await _dbContext.AddAsync(customer, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                #region --Audit Trail Recording

                AuditTrail auditTrail = new(customer.CreatedBy, $"Created new customer {customer.Name}", "Customer");
                await _dbContext.AddAsync(auditTrail, cancellationToken);

                #endregion --Audit Trail Recording

                TempData["success"] = "Customer created successfully";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                ModelState.AddModelError("", "The information you submitted is not valid!");
                return View(customer);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            try
            {
                var customers = await _dbContext.Customers.FindAsync(id, cancellationToken);
                return View(customers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred. Please try again later.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Customer customer, CancellationToken cancellationToken)
        {
            if (id != customer.Id)
            {
                return NotFound();
            }
            var existingModel = await _customerRepo.FindCustomerAsync(id, cancellationToken);

            if (ModelState.IsValid)
            {
                try
                {
                    existingModel.Name = customer.Name;
                    existingModel.Address = customer.Address;
                    existingModel.TinNo = customer.TinNo;
                    existingModel.BusinessStyle = customer.BusinessStyle;
                    existingModel.Terms = customer.Terms;
                    existingModel.CustomerType = customer.CustomerType;
                    existingModel.WithHoldingTax = customer.WithHoldingTax;
                    existingModel.WithHoldingVat = customer.WithHoldingVat;

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(_userManager.GetUserName(this.User), $"Updated customer {customer.Name}", "Customer");
                    await _dbContext.AddAsync(auditTrail, cancellationToken);

                    #endregion --Audit Trail Recording

                    _dbContext.Update(existingModel);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Customer updated successfully";
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(customer);
        }
    }
}