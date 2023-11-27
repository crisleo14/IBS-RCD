using Accounting_System.Data;
using Accounting_System.Models;
using Accounting_System.Repository;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Accounting_System.Controllers
{
    public class DebitMemoController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly DebitMemoRepo _debitMemoRepo;

        public DebitMemoController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, DebitMemoRepo dmcmRepo)
        {
            _dbContext = dbContext;
            this._userManager = userManager;
            _debitMemoRepo = dmcmRepo;
        }
        public async Task<IActionResult> Index()
        {
            var viewData = await _debitMemoRepo.GetDMAsync();

            return View(viewData);
        }
        [HttpGet]
        public IActionResult Create()
        {
            var viewModel = new DebitMemo();
            viewModel.SalesInvoices = _dbContext.SalesInvoices
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.SINo
                })
                .ToList();
            viewModel.StatementOfAccounts = _dbContext.StatementOfAccounts
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.SOANo  
                })
                .ToList();

            return View(viewModel);
        }
        [HttpPost]
        public async Task<IActionResult> Create(DebitMemo model)
        {

            if (ModelState.IsValid)
            {
                var generateDMNo = await _debitMemoRepo.GenerateDMNo();

                model.DMNo = generateDMNo;
                //Computation
                //var multiply = model.DebitAmount * model.SalesInvoice.Quantity;
                //model.Amount = multiply - model.SalesInvoice.Amount;
                //if (model.SalesInvoice.CustomerType == "Vatable")
                //{
                //    model.VatableSales = model.Amount / (decimal)1.12;
                //    model.VatAmount = model.Amount - model.VatableSales;
                //}

                model.CreatedBy = _userManager.GetUserName(this.User);
                _dbContext.Add(model);
                await _dbContext.SaveChangesAsync();
                TempData["success"] = "Debit Memo created successfully";
                return RedirectToAction("Index");
            }
            else
            {
                ModelState.AddModelError("", "The information you submitted is not valid!");
                return View(model);
            }
        }
        public async Task<IActionResult> Print(int id)
        {
            var cr = await _debitMemoRepo.FindDM(id);
            return View(cr);
        }

        public async Task<IActionResult> PrintedDM(int id)
        {
            var findIdOfDM = await _debitMemoRepo.FindDM(id);
            if (findIdOfDM != null && !findIdOfDM.IsPrinted)
            {
                findIdOfDM  .IsPrinted = true;
                await _dbContext.SaveChangesAsync();
            }
            return RedirectToAction("Print", new { id = id });
        }
    }
}
