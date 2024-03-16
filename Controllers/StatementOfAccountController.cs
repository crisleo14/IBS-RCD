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
    public class StatementOfAccountController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly StatementOfAccountRepo _statementOfAccountRepo;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly GeneralRepo _generalRepo;

        public StatementOfAccountController(ApplicationDbContext dbContext, StatementOfAccountRepo statementOfAccountRepo, UserManager<IdentityUser> userManager, GeneralRepo generalRepo)
        {
            _dbContext = dbContext;
            _statementOfAccountRepo = statementOfAccountRepo;
            _userManager = userManager;
            _generalRepo = generalRepo;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var results = await _statementOfAccountRepo
                .GetSOAListAsync(cancellationToken);

            return View(results);
        }

        [HttpGet]
        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            var viewModel = new StatementOfAccount();
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
        public async Task<IActionResult> Create(StatementOfAccount model, CancellationToken cancellationToken)
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

                model.SOANo = await _statementOfAccountRepo.GenerateSOANo(cancellationToken);

                model.CreatedBy = _userManager.GetUserName(this.User);

                model.ServiceNo = services.Number;

                for (int i = 0; i < model.Period.Length; i++)
                {
                    if (model.CreatedDate >= model.Period[i])
                    {
                        model.CurrentAndPreviousAmount += model.Amount[i];
                    }
                }
                model.Total = model.CurrentAndPreviousAmount;

                if (customer.CustomerType == "Vatable")
                {
                    model.NetAmount = (model.CurrentAndPreviousAmount - model.Discount) / 1.12m;
                    model.VatAmount = (model.CurrentAndPreviousAmount - model.Discount) - model.NetAmount;
                    model.WithholdingTaxAmount = model.NetAmount * (services.Percent / 100m);
                    if (customer.WithHoldingVat)
                    {
                        model.WithholdingVatAmount = model.NetAmount * 0.05m;
                    }
                }
                else
                {
                    model.NetAmount = model.CurrentAndPreviousAmount - model.Discount;
                    model.WithholdingTaxAmount = model.NetAmount * (services.Percent / 100m);
                    if (customer.WithHoldingVat)
                    {
                        model.WithholdingVatAmount = model.NetAmount * 0.05m;
                    }
                }

                if (customer.CustomerType == "Vatable")
                {
                    model.CurrentAndPreviousAmount = Math.Round(model.CurrentAndPreviousAmount / 1.12m, 2);

                    var total = model.CurrentAndPreviousAmount;

                    var roundedNetAmount = Math.Round(model.NetAmount, 2);

                    if (roundedNetAmount > total)
                    {
                        var shortAmount = model.NetAmount - total;

                        model.CurrentAndPreviousAmount += shortAmount;
                    }
                }

                await _dbContext.AddAsync(model, cancellationToken);

                #endregion --Saving the default properties

                #region --Audit Trail Recording

                AuditTrail auditTrail = new(model.CreatedBy, $"Create new statement of account# {model.SOANo}", "Statement Of Account");
                await _dbContext.AddAsync(auditTrail, cancellationToken);

                #endregion --Audit Trail Recording

                await _dbContext.SaveChangesAsync(cancellationToken);
                return RedirectToAction("Index");
            }

            return View(model);
        }

        public async Task<IActionResult> Generate(int id, CancellationToken cancellationToken)
        {
            var soa = await _statementOfAccountRepo
                .FindSOA(id, cancellationToken);

            return View(soa);
        }

        public async Task<IActionResult> Preview(int id, CancellationToken cancellationToken)
        {
            var soa = await _statementOfAccountRepo
                .FindSOA(id, cancellationToken);

            return PartialView("_PreviewPartialView", soa);
        }

        public async Task<IActionResult> PrintedSOA(int id, CancellationToken cancellationToken)
        {
            var findIdOfSOA = await _statementOfAccountRepo.FindSOA(id, cancellationToken);
            if (findIdOfSOA != null && !findIdOfSOA.IsPrinted)
            {
                #region --Audit Trail Recording

                var printedBy = _userManager.GetUserName(this.User);
                AuditTrail auditTrail = new(printedBy, $"Printed original copy of soa# {findIdOfSOA.SOANo}", "Statement Of Account");
                await _dbContext.AddAsync(auditTrail, cancellationToken);

                #endregion --Audit Trail Recording

                findIdOfSOA.IsPrinted = true;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            return RedirectToAction("Generate", new { id = id });
        }

        public async Task<IActionResult> Post(int id, CancellationToken cancellationToken, ViewModelDMCM viewModelSOA)
        {
            var model = await _statementOfAccountRepo.FindSOA(id, cancellationToken);

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

                    for (int i = 0; i < model.Period.Length; i++)
                    {
                            #region --SOA Computation--

                        if (customer.CustomerType == "Vatable")
                        {
                            viewModelSOA.Total = model.Amount[i];
                            viewModelSOA.NetAmount = (model.Amount[i] - model.Discount) / 1.12m;
                            viewModelSOA.VatAmount = (model.Amount[i] - model.Discount) - viewModelSOA.NetAmount;
                            viewModelSOA.WithholdingTaxAmount = viewModelSOA.NetAmount * (services.Percent / 100m);
                            if (customer.WithHoldingVat)
                            {
                                viewModelSOA.WithholdingVatAmount = viewModelSOA.NetAmount * 0.05m;
                            }
                        }
                        else
                        {
                            viewModelSOA.NetAmount = model.Amount[i] - model.Discount;
                            viewModelSOA.WithholdingTaxAmount = viewModelSOA.NetAmount * (services.Percent / 100m);
                            if (customer.WithHoldingVat)
                            {
                                viewModelSOA.WithholdingVatAmount = viewModelSOA.NetAmount * 0.05m;
                            }
                        }

                        if (customer.CustomerType == "Vatable")
                        {
                            var total = Math.Round(model.Amount[i] / 1.12m, 2);

                            var roundedNetAmount = Math.Round(viewModelSOA.NetAmount, 2);

                            if (roundedNetAmount > total)
                            {
                                var shortAmount = viewModelSOA.NetAmount - total;

                                viewModelSOA.Amount[i] += shortAmount;
                            }
                        }

                        #endregion --SOA Computation--

                        if (model.CreatedDate >= model.Period[i])
                        {
                            #region --Sales Book Recording

                            var sales = new SalesBook();

                            if (model.Customer.CustomerType == "Vatable")
                            {
                                sales.TransactionDate = model.CreatedDate.ToShortDateString();
                                sales.SerialNo = model.SOANo;
                                sales.SoldTo = model.Customer.Name;
                                sales.TinNo = model.Customer.TinNo;
                                sales.Address = model.Customer.Address;
                                sales.Description = model.Service.Name;
                                sales.Amount = model.Total;
                                sales.VatAmount = model.VatAmount;
                                sales.VatableSales = model.Total / 1.12m;
                                sales.Discount = model.Discount;
                                sales.NetSales = model.NetAmount;
                                sales.CreatedBy = model.CreatedBy;
                                sales.CreatedDate = model.CreatedDate;
                                sales.DueDate = model.DueDate;
                                sales.DocumentId = model.Id;
                            }
                            else if (model.Customer.CustomerType == "Exempt")
                            {
                                sales.TransactionDate = model.CreatedDate.ToShortDateString();
                                sales.SerialNo = model.SOANo;
                                sales.SoldTo = model.Customer.Name;
                                sales.TinNo = model.Customer.TinNo;
                                sales.Address = model.Customer.Address;
                                sales.Description = model.Service.Name;
                                sales.Amount = model.Total;
                                sales.VatExemptSales = model.Total;
                                sales.Discount = model.Discount;
                                sales.NetSales = model.NetAmount;
                                sales.CreatedBy = model.CreatedBy;
                                sales.CreatedDate = model.CreatedDate;
                                sales.DueDate = model.DueDate;
                                sales.DocumentId = model.Id;
                            }
                            else
                            {
                                sales.TransactionDate = model.CreatedDate.ToShortDateString();
                                sales.SerialNo = model.SOANo;
                                sales.SoldTo = model.Customer.Name;
                                sales.TinNo = model.Customer.TinNo;
                                sales.Address = model.Customer.Address;
                                sales.Description = model.Service.Name;
                                sales.Amount = model.Total;
                                sales.ZeroRated = model.Total;
                                sales.Discount = model.Discount;
                                sales.NetSales = model.NetAmount;
                                sales.CreatedBy = model.CreatedBy;
                                sales.CreatedDate = model.CreatedDate;
                                sales.DueDate = model.DueDate;
                                sales.DocumentId = model.Id;
                            }

                            await _dbContext.AddAsync(sales, cancellationToken);

                            #endregion --Sales Book Recording

                            #region --General Ledger Book Recording

                            //var ledgers = new List<GeneralLedgerBook>();

                            //ledgers.Add(
                            //        new GeneralLedgerBook
                            //        {
                            //            Date = model.CreatedDate.ToShortDateString(),
                            //            Reference = model.SOANo,
                            //            Description = model.Service.Name,
                            //            AccountTitle = "1010204 AR-Non Trade Receivable",
                            //            Debit = model.Total - (model.WithholdingTaxAmount + model.WithholdingVatAmount),
                            //            Credit = 0,
                            //            CreatedBy = model.CreatedBy,
                            //            CreatedDate = model.CreatedDate
                            //        }
                            //    );
                            //if (model.WithholdingTaxAmount > 0)
                            //{
                            //    ledgers.Add(
                            //        new GeneralLedgerBook
                            //        {
                            //            Date = model.CreatedDate.ToShortDateString(),
                            //            Reference = model.SOANo,
                            //            Description = model.Service.Name,
                            //            AccountTitle = "1010202 Deferred Creditable Withholding Tax",
                            //            Debit = model.WithholdingTaxAmount,
                            //            Credit = 0,
                            //            CreatedBy = model.CreatedBy,
                            //            CreatedDate = model.CreatedDate
                            //        }
                            //    );
                            //}
                            //if (model.WithholdingVatAmount > 0)
                            //{
                            //    ledgers.Add(
                            //        new GeneralLedgerBook
                            //        {
                            //            Date = model.CreatedDate.ToShortDateString(),
                            //            Reference = model.SOANo,
                            //            Description = model.Service.Name,
                            //            AccountTitle = "1010203 Deferred Creditable Withholding Vat",
                            //            Debit = model.WithholdingVatAmount,
                            //            Credit = 0,
                            //            CreatedBy = model.CreatedBy,
                            //            CreatedDate = model.CreatedDate
                            //        }
                            //    );
                            //}

                            //if (model.CurrentAndPreviousAmount > 0)
                            //{
                            //    ledgers.Add(
                            //        new GeneralLedgerBook
                            //        {
                            //            Date = model.CreatedDate.ToShortDateString(),
                            //            Reference = model.SOANo,
                            //            Description = model.Service.Name,
                            //            AccountTitle = model.Service.CurrentAndPrevious,
                            //            Debit = 0,
                            //            Credit = model.CurrentAndPreviousAmount,
                            //            CreatedBy = model.CreatedBy,
                            //            CreatedDate = model.CreatedDate
                            //        }
                            //    );
                            //}

                            //if (model.UnearnedAmount > 0)
                            //{
                            //    ledgers.Add(
                            //        new GeneralLedgerBook
                            //        {
                            //            Date = model.CreatedDate.ToShortDateString(),
                            //            Reference = model.SOANo,
                            //            Description = model.Service.Name,
                            //            AccountTitle = model.Service.Unearned,
                            //            Debit = 0,
                            //            Credit = model.UnearnedAmount,
                            //            CreatedBy = model.CreatedBy,
                            //            CreatedDate = model.CreatedDate
                            //        }
                            //    );
                            //}

                            //if (model.VatAmount > 0)
                            //{
                            //    ledgers.Add(
                            //        new GeneralLedgerBook
                            //        {
                            //            Date = model.CreatedDate.ToShortDateString(),
                            //            Reference = model.SOANo,
                            //            Description = model.Service.Name,
                            //            AccountTitle = "2010304 Deferred Vat Output",
                            //            Debit = 0,
                            //            Credit = model.VatAmount,
                            //            CreatedBy = model.CreatedBy,
                            //            CreatedDate = model.CreatedDate
                            //        }
                            //    );
                            //}

                            //await _dbContext.GeneralLedgerBooks.AddRangeAsync(ledgers, cancellationToken);

                            #endregion --General Ledger Book Recording
                        }
                        else
                        {
                            #region --Sales Book Recording

                            var sales = new SalesBook();

                            if (model.Customer.CustomerType == "Vatable")
                            {
                                sales.TransactionDate = model.Period[i].AddMonths(1).AddDays(-1).ToShortDateString();
                                sales.SerialNo = model.SOANo;
                                sales.SoldTo = model.Customer.Name;
                                sales.TinNo = model.Customer.TinNo;
                                sales.Address = model.Customer.Address;
                                sales.Description = model.Service.Name;
                                sales.Amount = viewModelSOA.Total;
                                sales.VatAmount = viewModelSOA.VatAmount;
                                sales.VatableSales = viewModelSOA.Total / 1.12m;
                                sales.Discount = model.Discount;
                                sales.NetSales = viewModelSOA.NetAmount;
                                sales.CreatedBy = model.CreatedBy;
                                sales.CreatedDate = model.CreatedDate;
                                sales.DueDate = model.DueDate;
                                sales.DocumentId = model.Id;
                            }
                            else if (model.Customer.CustomerType == "Exempt")
                            {
                                sales.TransactionDate = model.Period[i].AddMonths(1).AddDays(-1).ToShortDateString();
                                sales.SerialNo = model.SOANo;
                                sales.SoldTo = model.Customer.Name;
                                sales.TinNo = model.Customer.TinNo;
                                sales.Address = model.Customer.Address;
                                sales.Description = model.Service.Name;
                                sales.Amount = viewModelSOA.Total;
                                sales.VatExemptSales = viewModelSOA.Total;
                                sales.Discount = model.Discount;
                                sales.NetSales = viewModelSOA.NetAmount;
                                sales.CreatedBy = model.CreatedBy;
                                sales.CreatedDate = model.CreatedDate;
                                sales.DueDate = model.DueDate;
                                sales.DocumentId = model.Id;
                            }
                            else
                            {
                                sales.TransactionDate = model.Period[i].AddMonths(1).AddDays(-1).ToShortDateString();
                                sales.SerialNo = model.SOANo;
                                sales.SoldTo = model.Customer.Name;
                                sales.TinNo = model.Customer.TinNo;
                                sales.Address = model.Customer.Address;
                                sales.Description = model.Service.Name;
                                sales.Amount = viewModelSOA.Total;
                                sales.ZeroRated = viewModelSOA.Total;
                                sales.Discount = model.Discount;
                                sales.NetSales = viewModelSOA.NetAmount;
                                sales.CreatedBy = model.CreatedBy;
                                sales.CreatedDate = model.CreatedDate;
                                sales.DueDate = model.DueDate;
                                sales.DocumentId = model.Id;
                            }

                            await _dbContext.AddAsync(sales, cancellationToken);

                            #endregion --Sales Book Recording

                            #region --General Ledger Book Recording

                            //var ledgers = new List<GeneralLedgerBook>();

                            //ledgers.Add(
                            //        new GeneralLedgerBook
                            //        {
                            //            Date = model.Period[i].AddMonths(1).AddDays(-1).ToShortDateString(),
                            //            Reference = model.SOANo,
                            //            Description = model.Service.Name,
                            //            AccountTitle = "1010204 AR-Non Trade Receivable",
                            //            Debit = viewModelSOA.Total - (viewModelSOA.WithholdingTaxAmount + viewModelSOA.WithholdingVatAmount),
                            //            Credit = 0,
                            //            CreatedBy = model.CreatedBy,
                            //            CreatedDate = model.CreatedDate
                            //        }
                            //    );
                            //if (viewModelSOA.WithholdingTaxAmount > 0)
                            //{
                            //    ledgers.Add(
                            //        new GeneralLedgerBook
                            //        {
                            //            Date = model.Period[i].AddMonths(1).AddDays(-1).ToShortDateString(),
                            //            Reference = model.SOANo,
                            //            Description = model.Service.Name,
                            //            AccountTitle = "1010202 Deferred Creditable Withholding Tax",
                            //            Debit = viewModelSOA.WithholdingTaxAmount,
                            //            Credit = 0,
                            //            CreatedBy = model.CreatedBy,
                            //            CreatedDate = model.CreatedDate
                            //        }
                            //    );
                            //}
                            //if (viewModelSOA.WithholdingVatAmount > 0)
                            //{
                            //    ledgers.Add(
                            //        new GeneralLedgerBook
                            //        {
                            //            Date = model.Period[i].AddMonths(1).AddDays(-1).ToShortDateString(),
                            //            Reference = model.SOANo,
                            //            Description = model.Service.Name,
                            //            AccountTitle = "1010203 Deferred Creditable Withholding Vat",
                            //            Debit = viewModelSOA.WithholdingVatAmount,
                            //            Credit = 0,
                            //            CreatedBy = model.CreatedBy,
                            //            CreatedDate = model.CreatedDate
                            //        }
                            //    );
                            //}

                            //if (viewModelSOA.Total > 0)
                            //{
                            //    ledgers.Add(
                            //        new GeneralLedgerBook
                            //        {
                            //            Date = model.Period[i].AddMonths(1).AddDays(-1).ToShortDateString(),
                            //            Reference = model.SOANo,
                            //            Description = model.Service.Name,
                            //            AccountTitle = model.Service.CurrentAndPrevious,
                            //            Debit = 0,
                            //            Credit = viewModelSOA.Total / 1.12m,
                            //            CreatedBy = model.CreatedBy,
                            //            CreatedDate = model.CreatedDate
                            //        }
                            //    );
                            //}

                            //if (viewModelSOA.VatAmount > 0)
                            //{
                            //    ledgers.Add(
                            //        new GeneralLedgerBook
                            //        {
                            //            Date = model.Period[i].AddMonths(1).AddDays(-1).ToShortDateString(),
                            //            Reference = model.SOANo,
                            //            Description = model.Service.Name,
                            //            AccountTitle = "2010304 Deferred Vat Output",
                            //            Debit = 0,
                            //            Credit = viewModelSOA.VatAmount,
                            //            CreatedBy = model.CreatedBy,
                            //            CreatedDate = model.CreatedDate
                            //        }
                            //    );
                            //}

                            //await _dbContext.GeneralLedgerBooks.AddRangeAsync(ledgers, cancellationToken);

                            #endregion --General Ledger Book Recording
                        }
                    }

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(model.PostedBy, $"Posted statement of account# {model.SOANo}", "Statement Of Account");
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
            var model = await _dbContext.StatementOfAccounts.FindAsync(id, cancellationToken);

            if (model != null)
            {
                if (!model.IsCanceled)
                {
                    model.IsCanceled = true;
                    model.CanceledBy = _userManager.GetUserName(this.User);
                    model.CanceledDate = DateTime.Now;

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(model.CanceledBy, $"Cancelled statement of account# {model.SOANo}", "Statement Of Account");
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
            var model = await _dbContext.StatementOfAccounts.FindAsync(id, cancellationToken);

            if (model != null)
            {
                if (!model.IsVoided)
                {
                    model.IsVoided = true;
                    model.VoidedBy = _userManager.GetUserName(this.User);
                    model.VoidedDate = DateTime.Now;

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(model.VoidedBy, $"Voided statement of account# {model.SOANo}", "Statement Of Account");
                    await _dbContext.AddAsync(auditTrail, cancellationToken);

                    #endregion --Audit Trail Recording

                    await _generalRepo.RemoveRecords<GeneralLedgerBook>(gl => gl.Reference == model.SOANo, cancellationToken);

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
            var existingModel = await _statementOfAccountRepo.FindSOA(id, cancellationToken);

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
        public async Task<IActionResult> Edit(StatementOfAccount model, CancellationToken cancellationToken)
        {
            var existingModel = await _statementOfAccountRepo.FindSOA(model.Id, cancellationToken);

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
                for (int i = 0; i < model.Amount.Length; i++)
                {
                    total += model.Amount[i];
                }
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

                AuditTrail auditTrail = new(existingModel.CreatedBy, $"Edit statement of account# {existingModel.SOANo}", "Statement Of Account");
                await _dbContext.AddAsync(auditTrail, cancellationToken);

                #endregion --Audit Trail Recording

                await _dbContext.SaveChangesAsync(cancellationToken);
                return RedirectToAction("Index");
            }

            return View(existingModel);
        }
    }
}
