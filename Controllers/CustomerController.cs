using Accounting_System.Data;
using Accounting_System.Repository;
using Microsoft.AspNetCore.Mvc;

namespace Accounting_System.Controllers
{
    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly CustomerRepo _customerRepo;

        public CustomerController(ApplicationDbContext dbContext, CustomerRepo customerRepo)
        {
            _dbContext = dbContext;
            _customerRepo = customerRepo;
        }

        public async Task<IActionResult> Index()
        {
            var customer = await _customerRepo.GetCustomersAsync();

            return View(customer);
        }
    }
}