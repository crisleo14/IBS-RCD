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
    public class SupplierController : Controller
    {
        private readonly ApplicationDbContext _context;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly SupplierRepo _supplierRepo;

        private readonly IWebHostEnvironment _webHostEnvironment;

        public SupplierController(ApplicationDbContext context, UserManager<IdentityUser> userManager, SupplierRepo supplierRepo, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _supplierRepo = supplierRepo;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            return _context.Suppliers != null ?
                        View(await _context.Suppliers.ToListAsync(cancellationToken)) :
                        Problem("Entity set 'ApplicationDbContext.Suppliers'  is null.");
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Supplier supplier, IFormFile? document, IFormFile? registration, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                if (await _supplierRepo.IsSupplierNameExist(supplier.Name, cancellationToken))
                {
                    ModelState.AddModelError("Name", "Supplier name already exist!");
                    return View(supplier);
                }

                if (await _supplierRepo.IsSupplierTinExist(supplier.TinNo, cancellationToken))
                {
                    ModelState.AddModelError("TinNo", "Supplier tin already exist!");
                    return View(supplier);
                }

                supplier.CreatedBy = _userManager.GetUserName(this.User).ToString();
                supplier.Number = await _supplierRepo.GetLastNumber(cancellationToken);

                if (document != null && document.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Proof of Exemption", supplier.Number.ToString());

                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string fileName = Path.GetFileName(document.FileName);
                    string fileSavePath = Path.Combine(uploadsFolder, fileName);

                    using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
                    {
                        await document.CopyToAsync(stream);
                    }

                    supplier.ProofOfExemptionFilePath = fileSavePath;
                }

                if (registration != null && registration.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Proof of Registration", supplier.Number.ToString());

                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string fileName = Path.GetFileName(registration.FileName);
                    string fileSavePath = Path.Combine(uploadsFolder, fileName);

                    using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
                    {
                        await registration.CopyToAsync(stream);
                    }

                    supplier.ProofOfRegistrationFilePath = fileSavePath;
                }
                else
                {
                    TempData["error"] = "There's something wrong in your file. Contact MIS.";
                    return View(supplier);
                }

                await _context.AddAsync(supplier, cancellationToken);

                #region --Audit Trail Recording

                AuditTrail auditTrail = new(supplier.CreatedBy, $"Create new supplier {supplier.Name}", "Supplier Master File");
                await _context.AddAsync(auditTrail, cancellationToken);

                #endregion --Audit Trail Recording

                await _context.SaveChangesAsync(cancellationToken);
                TempData["success"] = $"Supplier {supplier.Name} has been created.";
                return RedirectToAction(nameof(Index));
            }
            return View(supplier);
        }

        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
            if (id == null || _context.Suppliers == null)
            {
                return NotFound();
            }

            var supplier = await _context.Suppliers.FindAsync(id, cancellationToken);
            if (supplier == null)
            {
                return NotFound();
            }
            return View(supplier);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Supplier supplier, CancellationToken cancellationToken)
        {
            if (id != supplier.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(supplier);
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SupplierExists(supplier.Id))
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
            return View(supplier);
        }

        private bool SupplierExists(int id)
        {
            return (_context.Suppliers?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}