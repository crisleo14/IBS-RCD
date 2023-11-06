using Microsoft.AspNetCore.Mvc;

namespace Accounting_System.Controllers
{
    public class ReceiptController : Controller
    {
        public IActionResult CollectionReceipt()
        {
            return View();
        }
        public IActionResult OfficialReceipt()
        {
            return View();
        }
    }
}
