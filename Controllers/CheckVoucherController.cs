using Accounting_System.Data;
using Accounting_System.Repository;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Accounting_System.Controllers
{
    public class CheckVoucherController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly CheckVoucherRepo _checkVoucherRepo;

        public CheckVoucherController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, CheckVoucherRepo checkVoucherRepo)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _checkVoucherRepo = checkVoucherRepo;
        }

        public async Task<IActionResult> Index()
        {
            var cv = await _checkVoucherRepo.GetCheckVouchers();

            return View(cv);
        }
    }
}