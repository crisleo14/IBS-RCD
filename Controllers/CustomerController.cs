using Accounting_System.Data;
using Accounting_System.Models;
using Accounting_System.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;

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

        public async Task<IActionResult> Index()
        {
            var customer = await _customerRepo.GetCustomersAsync();

            return View(customer);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new Customer());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer customer)
        {
            if (ModelState.IsValid)
            {
                var tinExist = await _customerRepo.CheckIfTinNoExist(customer.TinNo);

                if (tinExist != null)
                {
                    ModelState.AddModelError("", "Tin# already exist!");
                    return View(customer);
                }

                customer.Number = await _customerRepo.GetLastNumber();
                customer.CreatedBy = _userManager.GetUserName(this.User);
                _dbContext.Add(customer);
                await _dbContext.SaveChangesAsync();

                return RedirectToAction("Index");
            }
            else
            {
                ModelState.AddModelError("", "The information you submitted is not valid!");
                return View(customer);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customer = await _customerRepo.FindCustomerAsync(id);

            return PartialView("_EditCustomerPartialView", customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Customer customer)
        {
            if (id != customer.Id)
            {
                return NotFound();
            }
            var existingModel = await _customerRepo.FindCustomerAsync(id);

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

                    _dbContext.Update(existingModel);
                    await _dbContext.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_customerRepo.CustomerExist(customer.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(customer);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customer = await _customerRepo.FindCustomerAsync(id);

            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }
    }
}