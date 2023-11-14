using Accounting_System.Data;
using Accounting_System.Models;
using Accounting_System.Repository;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Accounting_System.Controllers
{
    public class ReceiptController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly CollectionReceiptRepo _collectionReceiptRepo;

        public ReceiptController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public IActionResult CreateCollectionReceipt()
        {
            var viewModel = new CollectionReceipt();
            viewModel.ReceivedFrom = _dbContext.SalesInvoices
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.SoldTo
                })
                .ToList();

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCollectionReceipt(CollectionReceipt model)
        {

            if (ModelState.IsValid)
            {
                var generateCRNo = await _collectionReceiptRepo.GenerateCRNo();

                model.CRNo = generateCRNo;
                model.CreatedBy = _userManager.GetUserName(this.User);
                _dbContext.Add(model);
                await _dbContext.SaveChangesAsync();
                TempData["success"] = "Sales Order created successfully";
                return RedirectToAction("OrderSlip");
            }
            else
            {
                ModelState.AddModelError("", "The information you submitted is not valid!");
                return View(model);
            }
        }


        public IActionResult CreateOfficialReceipt()
        {
            return View();
        }

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
