using Accounting_System.Data;
using Accounting_System.Models;
using Accounting_System.Repository;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;

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

            viewModel.Details.COA = await _dbContext.ChartOfAccounts
                .Where(coa => !new[] { "2010102", "2010101", "1010101" }.Any(excludedNumber => coa.Number.Contains(excludedNumber)))
                .Select(s => new SelectListItem
                {
                    Value = s.Number,
                    Text = s.Name
                })
                .ToListAsync(cancellationToken);

            viewModel.Header.Suppliers = await _dbContext.Suppliers
                .Select(sup => new SelectListItem
                {
                    Value = sup.Id.ToString(),
                    Text = sup.Name
                })
                .ToListAsync();
            viewModel.Header.BankAccounts = await _dbContext.BankAccounts
                .Select(ba => new SelectListItem
                {
                    Value = ba.Id.ToString(),
                    Text = ba.AccountName
                })
                .ToListAsync();

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CheckVoucherVM? model, CancellationToken cancellationToken, string[] accountNumberText, string[] accountNumber, decimal[]? debit, decimal[]? credit, string? siNo, string? poNo, string? criteria)
        {

            model.Header.Suppliers = await _dbContext.Suppliers
                .Select(sup => new SelectListItem
                {
                    Value = sup.Id.ToString(),
                    Text = sup.Name
                })
                .ToListAsync();
            model.Header.BankAccounts = await _dbContext.BankAccounts
                .Select(ba => new SelectListItem
                {
                    Value = ba.Id.ToString(),
                    Text = ba.AccountName
                })
                .ToListAsync();

                if (ModelState.IsValid)
                {
                    #region --Validating series
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
                    #endregion --Validating series

                    #region --Multiple input of SI and PO No.
                    if (poNo != null)
                    {
                        string[] inputs = poNo.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                        // Display each input
                        for (int i = 0; i < inputs.Length; i++)
                        {
                            model.Header.PONo = inputs;
                        }
                    }

                    if (siNo != null)
                    {
                        string[] inputs = siNo.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                        // Display each input
                        for (int i = 0; i < inputs.Length; i++)
                        {
                            model.Header.SINo = inputs;
                        }
                    }
                    #endregion --Multiple input of SI and PO No.

                    #region --Check if duplicate record
                    if (model.Header.CheckNo != null && !model.Header.CheckNo.Contains("DM"))
                    {
                        var cv = await _dbContext
                        .CheckVoucherHeaders
                        .Where(cv => cv.CheckNo == model.Header.CheckNo)
                        .ToListAsync(cancellationToken);
                        if (cv.Any())
                        {
                            TempData["error"] = "Check No. Is already exist";
                            return View(model);
                        }
                    }
                    #endregion --Check if duplicate record

                    #region --Saving the default entries

                    if (criteria != null)
                    {
                        model.Header.Criteria = criteria;
                    }
                    //CV Header Entry
                    var generateCVNo = await _checkVoucherRepo.GenerateCVNo(cancellationToken);

                    model.Header.SeriesNumber = getLastNumber;
                    model.Header.CVNo = generateCVNo;
                    model.Header.CreatedBy = _userManager.GetUserName(this.User);

                    #endregion --Saving the default entries
                    //CV Details Entry
                    //model.Details.CreatedBy = _userManager.GetUserName(this.User);
                    #region --CV Details Entry

                    var cvDetails = new List<CheckVoucherDetail>();

                    for (int i = 0; i < accountNumber.Length; i++)
                    {
                        var currentAccountNumber = accountNumber[i];
                        var currentAccountNumberText = accountNumberText[i];
                        var currentDebit = debit[i];
                        var currentCredit = credit[i];

                        cvDetails.Add(
                            new CheckVoucherDetail
                            {
                                AccountNo = currentAccountNumber,
                                AccountName = currentAccountNumberText,
                                TransactionNo = model.Header.CVNo,
                                Debit = currentDebit,
                                Credit = currentCredit,
                                CreatedBy = _userManager.GetUserName(this.User),
                                CreatedDate = DateTime.Today,
                            }
                        );

                        await _dbContext.AddRangeAsync(cvDetails, cancellationToken);
                    }

                    #endregion --CV Details Entry

                    await _dbContext.AddAsync(model.Header, cancellationToken);  // Add CheckVoucherHeader to the context
                    await _dbContext.SaveChangesAsync(cancellationToken);  // await the SaveChangesAsync method
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["error"] = "The information you submitted is not valid!";
                    return View(model);
                }
        }
        public async Task<IActionResult> GetPOs(int supplierId)
        {
            var purchaseOrders = await _dbContext.PurchaseOrders
                .Where(po => po.SupplierId == supplierId)
                .ToListAsync();

            if (purchaseOrders != null && purchaseOrders.Count > 0)
            {
                var poList = purchaseOrders.Select(po => new { Id = po.Id, PONumber = po.PONo }).ToList();
                return Json(poList);
            }

            return Json(null);
        }
        public async Task<IActionResult> GetRRs(string[] poNumber)
        {
                var receivingReports = await _dbContext.ReceivingReports
                .Where(rr => poNumber.Contains(rr.PONo))
                .ToListAsync();

            if (receivingReports != null && receivingReports.Count > 0)
            {
                var rrList = receivingReports.Select(rr => new { Id = rr.Id, RRNumber = rr.RRNo }).ToList();
                return Json(rrList);
            }

            return Json(null);
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