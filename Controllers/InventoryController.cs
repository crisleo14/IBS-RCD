using Accounting_System.Data;
using Accounting_System.Models.ViewModels;
using Accounting_System.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Accounting_System.Controllers
{
    [Authorize]
    public class InventoryController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly InventoryRepo _inventoryRepo;

        public InventoryController(ApplicationDbContext dbContext, InventoryRepo inventoryRepo)
        {
            _dbContext = dbContext;
            _inventoryRepo = inventoryRepo;
        }

        [HttpGet]
        public async Task<IActionResult> BeginningInventory(CancellationToken cancellationToken)
        {
            BeginningInventoryViewModel? viewModel = new();

            viewModel.ProductList = await _dbContext.Products
                .OrderBy(p => p.Code)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = $"{p.Code} {p.Name}"
                })
                .ToListAsync(cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> BeginningInventory(BeginningInventoryViewModel viewModel, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid && viewModel.Cost > 0 && viewModel.Quantity > 0)
            {
                try
                {
                    var hasBeginningInventory = await _inventoryRepo.HasAlreadyBeginningInventory(viewModel.ProductId, cancellationToken);

                    if (hasBeginningInventory)
                    {
                        TempData["error"] = "Beginning Inventory for this product already exists. Please contact MIS if you think this was a mistake.";
                        return View(viewModel);
                    }

                    await _inventoryRepo.AddBeginningInventory(viewModel, cancellationToken);
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.ToString();
                    return View();
                }
            }

            viewModel.ProductList = await _dbContext.Products
                .OrderBy(p => p.Code)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = $"{p.Code} {p.Name}"
                })
                .ToListAsync(cancellationToken);

            TempData["error"] = "The information you submitted is not valid!";
            return View(viewModel);
        }
    }
}