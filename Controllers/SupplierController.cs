using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Accounting_System.Data;
using Accounting_System.Models;
using Microsoft.AspNetCore.Identity;
using Accounting_System.Repository;
using Microsoft.AspNetCore.Authorization;

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

        // GET: Supplier
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            return _context.Suppliers != null ?
                        View(await _context.Suppliers.OrderByDescending(s => s.Id).ToListAsync(cancellationToken)) :
                        Problem("Entity set 'ApplicationDbContext.Suppliers'  is null.");
        }

        // GET: Supplier/Details/5
        public async Task<IActionResult> Details(int? id, CancellationToken cancellationToken)
        {
            if (id == null || _context.Suppliers == null)
            {
                return NotFound();
            }

            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
            if (supplier == null)
            {
                return NotFound();
            }

            return View(supplier);
        }

        // GET: Supplier/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Supplier/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Supplier supplier, IFormFile? document, IFormFile? registration, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                supplier.CreatedBy = _userManager.GetUserName(this.User).ToString();
                supplier.Number = await _supplierRepo.GetLastNumber(cancellationToken);

                if (document != null && document.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Proof of Exemption", supplier.Name);

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
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Proof of Registration", supplier.Name);

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

        // GET: Supplier/Edit/5
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

        // POST: Supplier/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
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

        // GET: Supplier/Delete/5
        public async Task<IActionResult> Delete(int? id, CancellationToken cancellationToken)
        {
            if (id == null || _context.Suppliers == null)
            {
                return NotFound();
            }

            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
            if (supplier == null)
            {
                return NotFound();
            }

            return View(supplier);
        }

        // POST: Supplier/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
        {
            if (_context.Suppliers == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Suppliers'  is null.");
            }
            var supplier = await _context.Suppliers.FindAsync(id, cancellationToken);
            if (supplier != null)
            {
                _context.Suppliers.Remove(supplier);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return RedirectToAction(nameof(Index));
        }

        private bool SupplierExists(int id)
        {
            return (_context.Suppliers?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}