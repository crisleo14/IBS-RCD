using Accounting_System.Data;
using Accounting_System.Models;
using Accounting_System.Models.Reports;
using Accounting_System.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Accounting_System.Controllers
{
    [Authorize]
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

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            return _dbContext.Services != null ?
                        View(await _dbContext.Services.ToListAsync(cancellationToken)) :
                        Problem("Entity set 'ApplicationDbContext.Services'  is null.");
        }

        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var viewModel = new Services();

            viewModel.CurrentAndPreviousTitles = await _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == 4 || coa.Level == 5)
                .OrderBy(coa => coa.Id)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Number + " " + s.Name
                })
                .ToListAsync(cancellationToken);

            viewModel.UnearnedTitles = await _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == 4 || coa.Level == 5)
                .OrderBy(coa => coa.Id)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Number + " " + s.Name
                })
                .ToListAsync(cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Services services, CancellationToken cancellationToken)
        {
            services.CurrentAndPreviousTitles = await _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == 4 || coa.Level == 5)
                .OrderBy(coa => coa.Id)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Number + " " + s.Name
                })
                .ToListAsync(cancellationToken);

            services.UnearnedTitles = await _dbContext.ChartOfAccounts
                .Where(coa => coa.Level == 4 || coa.Level == 5)
                .OrderBy(coa => coa.Id)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Number + " " + s.Name
                })
                .ToListAsync(cancellationToken);

            if (ModelState.IsValid)
            {
                if (await _serviceRepo.IsServicesExist(services.Name, cancellationToken))
                {
                    ModelState.AddModelError("Name", "Services already exist!");
                    return View(services);
                }

                if (services.Percent == 0)
                {
                    ModelState.AddModelError("Percent", "Please input percent!");
                    return View(services);
                }

                var currentAndPrevious = await _dbContext.ChartOfAccounts
                    .FindAsync(services.CurrentAndPreviousId, cancellationToken);

                var unearned = await _dbContext.ChartOfAccounts
                    .FindAsync(services.UnearnedId, cancellationToken);

                services.CurrentAndPreviousNo = currentAndPrevious.Number;
                services.CurrentAndPreviousTitle = currentAndPrevious.Name;

                services.UnearnedNo = unearned.Number;
                services.UnearnedTitle = unearned.Name;

                services.CreatedBy = _userManager.GetUserName(this.User).ToUpper();
                services.Number = await _serviceRepo.GetLastNumber(cancellationToken);

                TempData["success"] = "Services created successfully";

                #region --Audit Trail Recording

                AuditTrail auditTrail = new(services.CreatedBy, $"Created new service {services.Name}", "Service");
                await _dbContext.AddAsync(auditTrail, cancellationToken);

                #endregion --Audit Trail Recording

                await _dbContext.AddAsync(services, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                return RedirectToAction(nameof(Index));
            }
            return View(services);
        }

        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
            if (id == null || _dbContext.Services == null)
            {
                return NotFound();
            }

            var services = await _dbContext.Services.FindAsync(id, cancellationToken);
            if (services == null)
            {
                return NotFound();
            }
            return View(services);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Name,Percent,Id,CreatedBy,CreatedDate")] Services services, CancellationToken cancellationToken)
        {
            if (id != services.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                if (services.Percent == 0)
                {
                    ModelState.AddModelError("Percent", "Please input percent!");
                    return View(services);
                }
                try
                {
                    _dbContext.Update(services);

                    TempData["success"] = "Services updated successfully";

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(_userManager.GetUserName(this.User), $"Update service {services.Name}", "Service");
                    await _dbContext.AddAsync(auditTrail, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
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

        private bool ServicesExists(int id)
        {
            return (_dbContext.Services?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}