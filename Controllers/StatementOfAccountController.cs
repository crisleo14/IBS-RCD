using Microsoft.AspNetCore.Mvc;

namespace Accounting_System.Controllers
{
    public class StatementOfAccountController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}