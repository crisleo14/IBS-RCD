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

namespace Accounting_System.Controllers
{
    public class ServiceController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly ServiceRepo _serviceRepo;

        public ServiceController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, ServiceRepo serviceRepo)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _serviceRepo = serviceRepo;
        }

        // GET: Service
        public async Task<IActionResult> Index()
        {
            return _dbContext.Services != null ?
                        View(await _dbContext.Services.OrderBy(s => s.Id).ToListAsync()) :
                        Problem("Entity set 'ApplicationDbContext.Services'  is null.");
        }

        // GET: Service/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _dbContext.Services == null)
            {
                return NotFound();
            }

            var services = await _dbContext.Services
                .FirstOrDefaultAsync(m => m.Id == id);
            if (services == null)
            {
                return NotFound();
            }

            return View(services);
        }

        // GET: Service/Create
        public IActionResult Create()
        {
            var viewModel = new Services();

            viewModel.CurrentAndPreviousTitles = _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == "4" || coa.Level == "5")
                .OrderBy(coa => coa.Id)
                .Select(s => new SelectListItem
                {
                    Value = s.Number + " " + s.Name,
                    Text = s.Number + " " + s.Name
                })
                .ToList();

            viewModel.UnearnedTitle = _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == "4" || coa.Level == "5")
                .OrderBy(coa => coa.Id)
                .Select(s => new SelectListItem
                {
                    Value = s.Number + " " + s.Name,
                    Text = s.Number + " " + s.Name
                })
                .ToList();

            return View(viewModel);
        }

        // POST: Service/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create( Services services)
        {

            services.CurrentAndPreviousTitles = _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == "4" || coa.Level == "5")
                .OrderBy(coa => coa.Id)
                .Select(s => new SelectListItem
                {
                    Value = (s.Number + " " + s.Name).ToString(),
                    Text = s.Number + " " + s.Name
                })
                .ToList();

            services.UnearnedTitle = _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == "4" || coa.Level == "5")
                .OrderBy(coa => coa.Id)
                .Select(s => new SelectListItem
                {
                    Value = (s.Number + " " + s.Name).ToString(),
                    Text = s.Number + " " + s.Name
                })
                .ToList();

            if (ModelState.IsValid)
            {
                services.CreatedBy = _userManager.GetUserName(this.User).ToUpper();
                services.Number = await _serviceRepo.GetLastNumber();
                _dbContext.Add(services);
                await _dbContext.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(services);
        }

        // GET: Service/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _dbContext.Services == null)
            {
                return NotFound();
            }

            var services = await _dbContext.Services.FindAsync(id);
            if (services == null)
            {
                return NotFound();
            }
            return View(services);
        }

        // POST: Service/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Name,Percent,Id,CreatedBy,CreatedDate")] Services services)
        {
            if (id != services.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _dbContext.Update(services);
                    await _dbContext.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ServicesExists(services.Id))
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
            return View(services);
        }

        // GET: Service/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _dbContext.Services == null)
            {
                return NotFound();
            }

            var services = await _dbContext.Services
                .FirstOrDefaultAsync(m => m.Id == id);
            if (services == null)
            {
                return NotFound();
            }

            return View(services);
        }

        // POST: Service/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_dbContext.Services == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Services'  is null.");
            }
            var services = await _dbContext.Services.FindAsync(id);
            if (services != null)
            {
                _dbContext.Services.Remove(services);
            }

            await _dbContext.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ServicesExists(int id)
        {
            return (_dbContext.Services?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}