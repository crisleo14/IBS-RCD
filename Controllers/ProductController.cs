using Accounting_System.Data;
using Accounting_System.Models.MasterFile;
using Accounting_System.Models.Reports;
using Accounting_System.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Accounting_System.Controllers
{
    [Authorize]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly ProductRepository _productRepository;

        public ProductController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, ProductRepository productRepository)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _productRepository = productRepository;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            return _dbContext.Products != null ?
                        View(await _dbContext.Products.ToListAsync(cancellationToken)) :
                        Problem("Entity set 'ApplicationDbContext.Products'  is null.");
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Code,Name,Unit,Id,CreatedBy,CreatedDate")] Product product, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                if (await _productRepository.IsProductCodeExist(product.Code, cancellationToken))
                {
                    ModelState.AddModelError("Code", "Product code already exist!");
                    return View(product);
                }

                if (await _productRepository.IsProductNameExist(product.Name, cancellationToken))
                {
                    ModelState.AddModelError("Name", "Product name already exist!");
                    return View(product);
                }

                product.CreatedBy = _userManager.GetUserName(this.User).ToUpper();

                #region --Audit Trail Recording

                AuditTrail auditTrail = new(product.CreatedBy, $"Created new product {product.Name}", "Product");
                await _dbContext.AddAsync(auditTrail, cancellationToken);

                #endregion --Audit Trail Recording

                await _dbContext.AddAsync(product, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                TempData["success"] = "Product created successfully";
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
            if (id == null || _dbContext.Products == null)
            {
                return NotFound();
            }

            var product = await _dbContext.Products.FindAsync(id, cancellationToken);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Code,Name,Unit,Id,CreatedBy,CreatedDate")] Product product, CancellationToken cancellationToken)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _dbContext.Update(product);

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(_userManager.GetUserName(this.User), $"Updated product {product.Name}", "Product");
                    await _dbContext.AddAsync(auditTrail, cancellationToken);

                    #endregion --Audit Trail Recording

                    TempData["success"] = "Product updated successfully";
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id))
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
            return View(product);
        }

        private bool ProductExists(int id)
        {
            return (_dbContext.Products?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}