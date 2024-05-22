using Accounting_System.Data;
using Accounting_System.Models;
using Accounting_System.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Accounting_System.Controllers
{
    [Authorize]
    public class ServiceInvoiceController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly ServiceInvoiceRepo _statementOfAccountRepo;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly GeneralRepo _generalRepo;

        public ServiceInvoiceController(ApplicationDbContext dbContext, ServiceInvoiceRepo statementOfAccountRepo, UserManager<IdentityUser> userManager, GeneralRepo generalRepo)
        {
            _dbContext = dbContext;
            _statementOfAccountRepo = statementOfAccountRepo;
            _userManager = userManager;
            _generalRepo = generalRepo;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var results = await _statementOfAccountRepo
                .GetSvListAsync(cancellationToken);

            return View(results);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var viewModel = new ServiceInvoice();
            viewModel.Customers = await _dbContext.Customers
                .OrderBy(c => c.Id)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                })
                .ToListAsync(cancellationToken);
            viewModel.Services = await _dbContext.Services
                .OrderBy(s => s.Id)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Name
                })
                .ToListAsync(cancellationToken);
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(ServiceInvoice model, CancellationToken cancellationToken)
        {
            model.Customers = await _dbContext.Customers
                .OrderBy(c => c.Id)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                })
                .ToListAsync(cancellationToken);
            model.Services = await _dbContext.Services
                .OrderBy(s => s.Id)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Name
                })
                .ToListAsync(cancellationToken);
            if (ModelState.IsValid)
            {
                #region --Validating the series

                var getLastNumber = await _statementOfAccountRepo.GetLastSeriesNumber(cancellationToken);

                if (getLastNumber > 9999999999)
                {
                    TempData["error"] = "You reach the maximum Series Number";
                    return View(model);
                }
                var totalRemainingSeries = 9999999999 - getLastNumber;
                if (getLastNumber >= 9999999899)
                {
                    TempData["warning"] = $"Statement of Account created successfully, Warning {totalRemainingSeries} series number remaining";
                }
                else
                {
                    TempData["success"] = "Statement of Account created successfully";
                }

                #endregion --Validating the series

                #region --Retrieval of Services

                var services = await _statementOfAccountRepo.GetServicesAsync(model.ServicesId, cancellationToken);

                #endregion --Retrieval of Services

                #region --Retrieval of Customer

                var customer = await _statementOfAccountRepo.FindCustomerAsync(model.CustomerId, cancellationToken);

                #endregion --Retrieval of Customer

                #region --Saving the default properties

                model.SeriesNumber = getLastNumber;

                model.SVNo = await _statementOfAccountRepo.GenerateSvNo(cancellationToken);

                model.CreatedBy = _userManager.GetUserName(this.User);

                model.ServiceNo = services.Number;

                model.Total = model.Amount;

                if (customer.CustomerType == "Vatable")
                {
                    model.NetAmount = (model.Total - model.Discount) / 1.12m;
                    model.VatAmount = (model.Total - model.Discount) - model.NetAmount;
                    model.WithholdingTaxAmount = model.NetAmount * (services.Percent / 100m);
                    if (customer.WithHoldingVat)
                    {
                        model.WithholdingVatAmount = model.NetAmount * 0.05m;
                    }
                }
                else
                {
                    model.NetAmount = model.Total - model.Discount;
                    model.WithholdingTaxAmount = model.NetAmount * (services.Percent / 100m);
                    if (customer.WithHoldingVat)
                    {
                        model.WithholdingVatAmount = model.NetAmount * 0.05m;
                    }
                }

                if (model.CreatedDate < model.Period)
                {
                    model.UnearnedAmount += model.Amount;
                }
                else
                {
                    model.CurrentAndPreviousAmount += model.Amount;
                }



                if (customer.CustomerType == "Vatable")
                {
                    model.CurrentAndPreviousAmount = Math.Round(model.CurrentAndPreviousAmount / 1.12m, 2);
                    model.UnearnedAmount = Math.Round(model.UnearnedAmount / 1.12m, 2);

                    var total = model.CurrentAndPreviousAmount + model.UnearnedAmount;

                    var roundedNetAmount = Math.Round(model.NetAmount, 2);

                    if (roundedNetAmount > total)
                    {
                        var shortAmount = model.NetAmount - total;

                        model.CurrentAndPreviousAmount += shortAmount;
                    }
                }

                _dbContext.Add(model);

                #endregion --Saving the default properties

                #region --Audit Trail Recording

                AuditTrail auditTrail = new(model.CreatedBy, $"Create new service invoice# {model.SVNo}", "Service Invoice");
                _dbContext.Add(auditTrail);

                #endregion --Audit Trail Recording

                await _dbContext.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            return View(model);
        }

        public async Task<IActionResult> Generate(int id, CancellationToken cancellationToken)
        {
            var soa = await _statementOfAccountRepo
                .FindSv(id, cancellationToken);

            return View(soa);
        }

        public async Task<IActionResult> Preview(int id, CancellationToken cancellationToken)
        {
            var soa = await _statementOfAccountRepo
                .FindSv(id, cancellationToken);

            return PartialView("_PreviewPartialView", soa);
        }

        public async Task<IActionResult> PrintedSOA(int id, CancellationToken cancellationToken)
        {
            var findIdOfSOA = await _statementOfAccountRepo.FindSv(id, cancellationToken);
            if (findIdOfSOA != null && !findIdOfSOA.IsPrinted)
            {
                #region --Audit Trail Recording

                var printedBy = _userManager.GetUserName(this.User);
                AuditTrail auditTrail = new(printedBy, $"Printed original copy of sv# {findIdOfSOA.SVNo}", "Service Invoice");
                await _dbContext.AddAsync(auditTrail, cancellationToken);

                #endregion --Audit Trail Recording

                findIdOfSOA.IsPrinted = true;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            return RedirectToAction("Generate", new { id = id });
        }

        public async Task<IActionResult> Post(int id, CancellationToken cancellationToken, ViewModelDMCM viewModelSV)
        {
            var model = await _statementOfAccountRepo.FindSv(id, cancellationToken);

            if (model != null)
            {
                if (!model.IsPosted)
                {
                    model.IsPosted = true;
                    model.PostedBy = _userManager.GetUserName(this.User);
                    model.PostedDate = DateTime.Now;

                    #region --Retrieval of Services

                    var services = await _statementOfAccountRepo.GetServicesAsync(model.ServicesId, cancellationToken);

                    #endregion --Retrieval of Services

                    #region --Retrieval of Customer

                    var customer = await _statementOfAccountRepo.FindCustomerAsync(model.CustomerId, cancellationToken);

                    #endregion --Retrieval of Customer


                    if (model.Amount > 0)
                    {
                        #region --SOA Computation--

                        viewModelSV.Period = model.CreatedDate >= model.Period ? model.CreatedDate : model.Period.AddMonths(1).AddDays(-1);

                        if (customer.CustomerType == "Vatable")
                        {
                            viewModelSV.Total = model.Amount;
                            viewModelSV.NetAmount = (model.Amount - model.Discount) / 1.12m;
                            viewModelSV.VatAmount = (model.Amount - model.Discount) - viewModelSV.NetAmount;
                            viewModelSV.WithholdingTaxAmount = viewModelSV.NetAmount * (services.Percent / 100m);
                            if (customer.WithHoldingVat)
                            {
                                viewModelSV.WithholdingVatAmount = viewModelSV.NetAmount * 0.05m;
                            }
                        }
                        else
                        {
                            viewModelSV.NetAmount = model.Amount - model.Discount;
                            viewModelSV.WithholdingTaxAmount = viewModelSV.NetAmount * (services.Percent / 100m);
                            if (customer.WithHoldingVat)
                            {
                                viewModelSV.WithholdingVatAmount = viewModelSV.NetAmount * 0.05m;
                            }
                        }

                        if (customer.CustomerType == "Vatable")
                        {
                            var total = Math.Round(model.Amount / 1.12m, 2);

                            var roundedNetAmount = Math.Round(viewModelSV.NetAmount, 2);

                            if (roundedNetAmount > total)
                            {
                                var shortAmount = viewModelSV.NetAmount - total;

                                viewModelSV.Amount = shortAmount;
                            }
                        }

                        #endregion --SOA Computation--

                        #region --Sales Book Recording

                        var sales = new SalesBook();

                        if (model.Customer.CustomerType == "Vatable")
                        {
                            sales.TransactionDate = viewModelSV.Period.ToShortDateString();
                            sales.SerialNo = model.SVNo;
                            sales.SoldTo = model.Customer.Name;
                            sales.TinNo = model.Customer.TinNo;
                            sales.Address = model.Customer.Address;
                            sales.Description = model.Service.Name;
                            sales.Amount = viewModelSV.Total;
                            sales.VatAmount = viewModelSV.VatAmount;
                            sales.VatableSales = viewModelSV.Total / 1.12m;
                            sales.Discount = viewModelSV.Discount;
                            sales.NetSales = viewModelSV.NetAmount;
                            sales.CreatedBy = model.CreatedBy;
                            sales.CreatedDate = model.CreatedDate;
                            sales.DueDate = model.DueDate;
                            sales.DocumentId = model.Id;
                        }
                        else if (model.Customer.CustomerType == "Exempt")
                        {
                            sales.TransactionDate = viewModelSV.Period.ToShortDateString();
                            sales.SerialNo = model.SVNo;
                            sales.SoldTo = model.Customer.Name;
                            sales.TinNo = model.Customer.TinNo;
                            sales.Address = model.Customer.Address;
                            sales.Description = model.Service.Name;
                            sales.Amount = viewModelSV.Total;
                            sales.VatExemptSales = viewModelSV.Total;
                            sales.Discount = viewModelSV.Discount;
                            sales.NetSales = viewModelSV.NetAmount;
                            sales.CreatedBy = model.CreatedBy;
                            sales.CreatedDate = model.CreatedDate;
                            sales.DueDate = model.DueDate;
                            sales.DocumentId = model.Id;
                        }
                        else
                        {
                            sales.TransactionDate = viewModelSV.Period.ToShortDateString();
                            sales.SerialNo = model.SVNo;
                            sales.SoldTo = model.Customer.Name;
                            sales.TinNo = model.Customer.TinNo;
                            sales.Address = model.Customer.Address;
                            sales.Description = model.Service.Name;
                            sales.Amount = viewModelSV.Total;
                            sales.ZeroRated = viewModelSV.Total;
                            sales.Discount = viewModelSV.Discount;
                            sales.NetSales = viewModelSV.NetAmount;
                            sales.CreatedBy = model.CreatedBy;
                            sales.CreatedDate = model.CreatedDate;
                            sales.DueDate = model.DueDate;
                            sales.DocumentId = model.Id;
                        }

                        await _dbContext.AddAsync(sales, cancellationToken);

                        #endregion --Sales Book Recording

                        #region --General Ledger Book Recording

                        var ledgers = new List<GeneralLedgerBook>();

                        ledgers.Add(
                                new GeneralLedgerBook
                                {
                                    Date = viewModelSV.Period.ToShortDateString(),
                                    Reference = model.SVNo,
                                    Description = model.Service.Name,
                                    AccountNo = "1010204",
                                    AccountTitle = "AR-Non Trade Receivable",
                                    Debit = viewModelSV.Total - (viewModelSV.WithholdingTaxAmount + viewModelSV.WithholdingVatAmount),
                                    Credit = 0,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        if (model.WithholdingTaxAmount > 0)
                        {
                            ledgers.Add(
                                new GeneralLedgerBook
                                {
                                    Date = viewModelSV.Period.ToShortDateString(),
                                    Reference = model.SVNo,
                                    Description = model.Service.Name,
                                    AccountNo = "1010202",
                                    AccountTitle = "Deferred Creditable Withholding Tax",
                                    Debit = viewModelSV.WithholdingTaxAmount,
                                    Credit = 0,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }
                        if (model.WithholdingVatAmount > 0)
                        {
                            ledgers.Add(
                                new GeneralLedgerBook
                                {
                                    Date = viewModelSV.Period.ToShortDateString(),
                                    Reference = model.SVNo,
                                    Description = model.Service.Name,
                                    AccountNo = "1010203",
                                    AccountTitle = "Deferred Creditable Withholding Vat",
                                    Debit = viewModelSV.WithholdingVatAmount,
                                    Credit = 0,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }

                        if (model.Period < model.CreatedDate)
                        {
                            ledgers.Add(
                                new GeneralLedgerBook
                                {
                                    Date = viewModelSV.Period.ToShortDateString(),
                                    Reference = model.SVNo,
                                    Description = model.Service.Name,
                                    AccountNo = model.Service.CurrentAndPreviousNo,
                                    AccountTitle = model.Service.CurrentAndPreviousTitle,
                                    Debit = 0,
                                    Credit = viewModelSV.Total / 1.12m,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }
                        else if (model.Period > model.CreatedDate)
                        {
                            ledgers.Add(
                                new GeneralLedgerBook
                                {
                                    Date = viewModelSV.Period.ToShortDateString(),
                                    Reference = model.SVNo,
                                    Description = model.Service.Name,
                                    AccountNo = model.Service.UnearnedNo,
                                    AccountTitle = model.Service.UnearnedTitle,
                                    Debit = 0,
                                    Credit = viewModelSV.Total / 1.12m,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }

                        if (model.VatAmount > 0)
                        {
                            ledgers.Add(
                                new GeneralLedgerBook
                                {
                                    Date = viewModelSV.Period.ToShortDateString(),
                                    Reference = model.SVNo,
                                    Description = model.Service.Name,
                                    AccountNo = "2010301",
                                    AccountTitle = "Vat Output",
                                    Debit = 0,
                                    Credit = viewModelSV.VatAmount,
                                    CreatedBy = model.CreatedBy,
                                    CreatedDate = model.CreatedDate
                                }
                            );
                        }

                        await _dbContext.GeneralLedgerBooks.AddRangeAsync(ledgers, cancellationToken);

                        #endregion --General Ledger Book Recording
                    }

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(model.PostedBy, $"Posted service invoice# {model.SVNo}", "Service Invoice");
                    await _dbContext.AddAsync(auditTrail, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Statement of Account has been posted.";
                    return RedirectToAction("Index");
                }
                else
                {
                    return RedirectToAction("Index");
                }
            }

            return null;
        }

        public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
        {
            var model = await _dbContext.ServiceInvoices.FindAsync(id, cancellationToken);

            if (model != null)
            {
                if (!model.IsCanceled)
                {
                    model.IsCanceled = true;
                    model.CanceledBy = _userManager.GetUserName(this.User);
                    model.CanceledDate = DateTime.Now;

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(model.CanceledBy, $"Cancelled service invoice# {model.SVNo}", "Service Invoice");
                    await _dbContext.AddAsync(auditTrail, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Statement of Account has been Cancelled.";
                }
                return RedirectToAction("Index");
            }

            return NotFound();
        }

        public async Task<IActionResult> Void(int id, CancellationToken cancellationToken)
        {
            var model = await _dbContext.ServiceInvoices.FindAsync(id, cancellationToken);

            if (model != null)
            {
                if (!model.IsVoided)
                {
                    model.IsVoided = true;
                    model.VoidedBy = _userManager.GetUserName(this.User);
                    model.VoidedDate = DateTime.Now;

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(model.VoidedBy, $"Voided service invoice# {model.SVNo}", "Service Invoice");
                    await _dbContext.AddAsync(auditTrail, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _generalRepo.RemoveRecords<GeneralLedgerBook>(gl => gl.Reference == model.SVNo, cancellationToken);

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Statement of Account has been voided.";
                }
                return RedirectToAction("Index");
            }

            return NotFound();
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            if (id == null)
            {
                return NotFound();
            }
            var existingModel = await _statementOfAccountRepo.FindSv(id, cancellationToken);

            if (existingModel == null)
            {
                return NotFound();
            }

            existingModel.Customers = await _dbContext.Customers
                .OrderBy(c => c.Id)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                })
                .ToListAsync(cancellationToken);
            existingModel.Services = await _dbContext.Services
                .OrderBy(s => s.Id)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Name
                })
                .ToListAsync(cancellationToken);

            return View(existingModel);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(ServiceInvoice model, CancellationToken cancellationToken)
        {
            var existingModel = await _statementOfAccountRepo.FindSv(model.Id, cancellationToken);

            if (existingModel == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                #region --Validating the series

                var getLastNumber = await _statementOfAccountRepo.GetLastSeriesNumber(cancellationToken);

                if (getLastNumber > 9999999999)
                {
                    TempData["error"] = "You reach the maximum Series Number";
                    return View(model);
                }
                var totalRemainingSeries = 9999999999 - getLastNumber;
                if (getLastNumber >= 9999999899)
                {
                    TempData["warning"] = $"Statement of Account created successfully, Warning {totalRemainingSeries} series number remaining";
                }
                else
                {
                    TempData["success"] = "Statement of Account created successfully";
                }

                #endregion --Validating the series

                #region --Retrieval of Services

                var services = await _statementOfAccountRepo.GetServicesAsync(model.ServicesId, cancellationToken);

                #endregion --Retrieval of Services

                #region --Retrieval of Customer

                var customer = await _statementOfAccountRepo.FindCustomerAsync(model.CustomerId, cancellationToken);

                #endregion --Retrieval of Customer

                #region --Saving the default properties

                existingModel.Discount = model.Discount;
                existingModel.Amount = model.Amount;
                existingModel.Period = model.Period;

                decimal total = 0;
                total += model.Amount;
                existingModel.Total = total;

                if (customer.CustomerType == "Vatable")
                {
                    existingModel.NetAmount = (existingModel.Total - existingModel.Discount) / 1.12m;
                    existingModel.VatAmount = (existingModel.Total - existingModel.Discount) - existingModel.NetAmount;
                    existingModel.WithholdingTaxAmount = existingModel.NetAmount * (services.Percent / 100m);
                    if (customer.WithHoldingVat)
                    {
                        existingModel.WithholdingVatAmount = existingModel.NetAmount * 0.05m;
                    }
                }
                else
                {
                    existingModel.NetAmount = existingModel.Total - existingModel.Discount;
                    existingModel.WithholdingTaxAmount = existingModel.NetAmount * (services.Percent / 100m);
                    if (customer.WithHoldingVat)
                    {
                        existingModel.WithholdingVatAmount = existingModel.NetAmount * 0.05m;
                    }
                }

                #endregion --Saving the default properties

                #region --Audit Trail Recording

                AuditTrail auditTrail = new(existingModel.CreatedBy, $"Edit service invoice# {existingModel.SVNo}", "Service Invoice");
                await _dbContext.AddAsync(auditTrail, cancellationToken);

                #endregion --Audit Trail Recording

                await _dbContext.SaveChangesAsync(cancellationToken);
                return RedirectToAction("Index");
            }

            return View(existingModel);
        }
    }
}
