using Accounting_System.Data;
using Accounting_System.Models;
using Accounting_System.Repository;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Accounting_System.Controllers
{
    public class CheckVoucherController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly CheckVoucherRepo _checkVoucherRepo;

        public CheckVoucherController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, CheckVoucherRepo checkVoucherRepo)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _checkVoucherRepo = checkVoucherRepo;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var cv = await _checkVoucherRepo.GetCheckVouchers(cancellationToken);

            return View(cv);
        }
        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var viewModel = new CheckVoucherVM
            {
                Header = new CheckVoucherHeader(),
                Details = new CheckVoucherDetail()
            };

            viewModel.Header.RR = await _dbContext.ReceivingReports
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.RRNo
                })
                .ToListAsync(cancellationToken);

            viewModel.Details.COA = await _dbContext.ChartOfAccounts
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Number + " " + s.Name
                })
                .ToListAsync(cancellationToken);

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CheckVoucherVM model, CancellationToken cancellationToken)
        {
            model.Header.RR = await _dbContext.ReceivingReports
               .Select(rr => new SelectListItem
               {
                   Value = rr.Id.ToString(),
                   Text = rr.RRNo
               })
               .ToListAsync(cancellationToken);

            model.Details.COA = await _dbContext.ChartOfAccounts
                .Select(coa => new SelectListItem
                {
                    Value = coa.Id.ToString(),
                    Text = coa.Number + " " + coa.Name
                })
                .ToListAsync(cancellationToken);

            if (ModelState.IsValid)
            {
                var getLastNumber = await _checkVoucherRepo.GetLastSeriesNumberCV(cancellationToken);

                if (getLastNumber > 9999999999)
                {
                    TempData["error"] = "You reached the maximum Series Number";
                    return View(model);
                }

                var totalRemainingSeries = 9999999999 - getLastNumber;
                if (getLastNumber >= 9999999899)
                {
                    TempData["warning"] = $"Check Voucher created successfully, Warning {totalRemainingSeries} series numbers remaining";
                }
                else
                {
                    TempData["success"] = "Check Voucher created successfully";
                }

                //CV Header Entry
                var generateCVNo = await _checkVoucherRepo.GenerateCVNo(cancellationToken);
                
                model.Header.SeriesNumber = getLastNumber;
                model.Header.CVNo = generateCVNo;
                model.Header.CreatedBy = _userManager.GetUserName(this.User);

                //CV Details Entry
                model.Details.CreatedBy = _userManager.GetUserName(this.User);


                await _dbContext.AddAsync(model.Header, cancellationToken);  // Add CheckVoucherHeader to the context
                _dbContext.Add(model.Details); // Add CheckVoucherDetails to the context

                await _dbContext.SaveChangesAsync(cancellationToken);  // await the SaveChangesAsync method
                return RedirectToAction("Index");
            }
            else
            {
                TempData["error"] = "The information you submitted is not valid!";
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Print()
        {
            return View();
        }

        public async Task<IActionResult> Printed(int id, CancellationToken cancellationToken)
        {
            var cv = await _dbContext.CheckVoucherHeaders.FindAsync(id, cancellationToken);
            if (cv != null && !cv.IsPrinted)
            {
                cv.IsPrinted = true;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            return RedirectToAction("Print", new { id = id });
        }

        public async Task<IActionResult> Post(int cvId, CancellationToken cancellationToken)
        {
            var model = await _dbContext.CheckVoucherHeaders.FindAsync(cvId, cancellationToken);

            if (model != null)
            {
                if (!model.IsPosted)
                {
                    model.IsPosted = true;

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Check Voucher has been Posted.";

                }
                //else
                //{
                //    model.IsVoid = true;
                //    await _dbContext.SaveChangesAsync();
                //    TempData["success"] = "Purchase Order has been Voided.";
                //}
                return RedirectToAction(nameof(Index));
            }

            return NotFound();
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id, CheckVoucherVM model, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }

            var existingHeaderModel = await _dbContext.CheckVoucherHeaders.FindAsync(id, cancellationToken);
            var existingDetailsModel = await _dbContext.CheckVoucherDetails.FindAsync(id, cancellationToken);

            if (existingHeaderModel == null || existingDetailsModel == null)
            {
                return NotFound();
            }

            existingHeaderModel.RR = await _dbContext.ReceivingReports
                .Select(rr => new SelectListItem
                {
                    Value = rr.Id.ToString(),
                    Text = rr.RRNo
                })
                .ToListAsync(cancellationToken);

            existingDetailsModel.COA = await _dbContext.ChartOfAccounts
                .Select(coa => new SelectListItem
                {
                    Value = coa.Id.ToString(),
                    Text = coa.Number + " " + coa.Name
                })
                .ToListAsync(cancellationToken);

            model.Header = existingHeaderModel; // Assign the updated header model to the view model
            model.Details = existingDetailsModel; // Assign the updated details model to the view model

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(CheckVoucherVM model, CancellationToken cancellationToken)
        {
            model.Header.RR = await _dbContext.ReceivingReports
               .Select(rr => new SelectListItem
               {
                   Value = rr.Id.ToString(),
                   Text = rr.RRNo
               })
               .ToListAsync(cancellationToken);

            model.Details.COA = await _dbContext.ChartOfAccounts
                .Select(coa => new SelectListItem
                {
                    Value = coa.Id.ToString(),
                    Text = coa.Number + " " + coa.Name
                })
                .ToListAsync(cancellationToken);

            

            if (ModelState.IsValid)
            {
                var getLastNumber = await _checkVoucherRepo.GetLastSeriesNumberCV(cancellationToken); 
                
                if (getLastNumber > 9999999999)
                {
                    TempData["error"] = "You reached the maximum Series Number";
                    return View(model);
                }

                var totalRemainingSeries = 9999999999 - getLastNumber;
                if (getLastNumber >= 9999999899)
                {
                    TempData["warning"] = $"Check Voucher created successfully, Warning {totalRemainingSeries} series numbers remaining";
                }
                else
                {
                    TempData["success"] = "Check Voucher created successfully";
                }

                var existingHeaderModel = await _dbContext.CheckVoucherHeaders.FindAsync(model.Header.Id, cancellationToken);
                var existingDetailsModel = await _dbContext.CheckVoucherDetails.FindAsync(model.Details.Id, cancellationToken);

                if (existingHeaderModel == null)
                {
                    return NotFound();
                }
                if (existingDetailsModel == null)
                {
                    return NotFound();
                }

                //CV Header Entry
                var generateCVNo = await _checkVoucherRepo.GenerateCVNo(cancellationToken);
                
                existingHeaderModel.SeriesNumber = model.Header.SeriesNumber;
                existingHeaderModel.CVNo = model.Header.CVNo;
                existingHeaderModel.CreatedBy = model.Header.CreatedBy;

                //CV Details Entry
                existingDetailsModel.CreatedBy = model.Details.CreatedBy;

                await _dbContext.SaveChangesAsync(cancellationToken);  // await the SaveChangesAsync method
                return RedirectToAction("Index");
            }
            else
            {
                TempData["error"] = "The information you submitted is not valid!";
                return View(model);
            }
        }
    }
}