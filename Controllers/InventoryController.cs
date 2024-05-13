using Accounting_System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Accounting_System.Controllers
{
    [Authorize]
    public class InventoryController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        public InventoryController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var result = await _dbContext
                .Inventories
                .Include(inventory => inventory.Product)
                .ToListAsync(cancellationToken);

            return View(result);
        }
    }
}