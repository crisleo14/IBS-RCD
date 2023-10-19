using Microsoft.AspNetCore.Mvc;

namespace Accounting_System.Controllers
{
    public class SalesController : Controller
    {
        public IActionResult SalesInvoice()
        {
            return View();
        }
    }
}
