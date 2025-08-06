﻿using Accounting_System.Data;
using Accounting_System.Models;
using Accounting_System.Models.Reports;
using Accounting_System.Repository;
using Accounting_System.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace Accounting_System.Controllers
{
    [Authorize]
    public class CustomerController : Controller
    {
        private readonly CustomerRepo _customerRepo;
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        public CustomerController(ApplicationDbContext dbContext, CustomerRepo customerRepo, UserManager<IdentityUser> userManager)
        {
            _dbContext = dbContext;
            _customerRepo = customerRepo;
            this._userManager = userManager;
        }

        public async Task<IActionResult> Index(string? view, CancellationToken cancellationToken)
        {
            var customer = await _customerRepo.GetCustomersAsync(cancellationToken);

            if (view == nameof(DynamicView.Customer))
            {
                return View("ImportExportIndex", customer);
            }

            return View(customer);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCustomerIds(CancellationToken cancellationToken)
        {
            var customerIds = await _dbContext.Customers
                                     .Select(c => c.CustomerId) // Assuming Id is the primary key
                                     .ToListAsync(cancellationToken);
            return Json(customerIds);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new Customer());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer customer, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    if (await _customerRepo.IsCustomerExist(customer.CustomerName, cancellationToken))
                    {
                        ModelState.AddModelError("Name", "Customer already exist!");
                        return View(customer);
                    }

                    if (await _customerRepo.IsTinNoExist(customer.CustomerTin, cancellationToken))
                    {
                        ModelState.AddModelError("TinNo", "Tin# already exist!");
                        return View(customer);
                    }

                    customer.Number = await _customerRepo.GetLastNumber(cancellationToken);
                    customer.CreatedBy = _userManager.GetUserName(this.User);

                    #region --Audit Trail Recording

                    if (customer.OriginalCustomerId == 0)
                    {
                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        AuditTrail auditTrailBook = new(customer.CreatedBy, $"Created new customer {customer.CustomerName}", "Customer", ipAddress);
                        await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                    }

                    #endregion --Audit Trail Recording

                    await _dbContext.AddAsync(customer, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    TempData["success"] = "Customer created successfully";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(Index));
                }
            }
            else
            {
                ModelState.AddModelError("", "The information you submitted is not valid!");
                return View(customer);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
        {
            try
            {
                var customers = await _dbContext.Customers.FindAsync(id, cancellationToken);
                return View(customers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred. Please try again later.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Customer customer, CancellationToken cancellationToken)
        {
            if (id != customer.CustomerId)
            {
                return NotFound();
            }
            var existingModel = await _customerRepo.FindCustomerAsync(id, cancellationToken);

            if (ModelState.IsValid)
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    existingModel.CustomerName = customer.CustomerName;
                    existingModel.CustomerAddress = customer.CustomerAddress;
                    existingModel.CustomerTin = customer.CustomerTin;
                    existingModel.BusinessStyle = customer.BusinessStyle;
                    existingModel.CustomerTerms = customer.CustomerTerms;
                    existingModel.CustomerType = customer.CustomerType;
                    existingModel.WithHoldingTax = customer.WithHoldingTax;
                    existingModel.WithHoldingVat = customer.WithHoldingVat;
                    existingModel.ZipCode = customer.ZipCode;

                    if (_dbContext.ChangeTracker.HasChanges())
                    {
                        #region --Audit Trail Recording

                        if (customer.OriginalCustomerId == 0)
                        {
                            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                            AuditTrail auditTrailBook = new(_userManager.GetUserName(this.User), $"Updated customer {customer.CustomerName}", "Customer", ipAddress);
                            await _dbContext.AddAsync(auditTrailBook, cancellationToken);
                        }

                        #endregion --Audit Trail Recording

                        await _dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        TempData["success"] = "Customer updated successfully";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        throw new InvalidOperationException("No data changes!");
                    }
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return View(existingModel);
                }

            }
            return View(existingModel);
        }

        //Download as .xlsx file.(Export)

        #region -- export xlsx record --

        [HttpPost]
        public async Task<IActionResult> Export(string selectedRecord, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(selectedRecord))
            {
                // Handle the case where no invoices are selected
                return RedirectToAction(nameof(Index));
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
		    {
                var recordIds = selectedRecord.Split(',').Select(int.Parse).ToList();

                // Retrieve the selected invoices from the database
                var selectedList = await _dbContext.Customers
                    .Where(c => recordIds.Contains(c.CustomerId))
                    .OrderBy(c => c.CustomerId)
                    .ToListAsync();

                // Create the Excel package
                using var package = new ExcelPackage();
                // Add a new worksheet to the Excel package
                var worksheet = package.Workbook.Worksheets.Add("Customers");

                worksheet.Cells["A1"].Value = "CustomerName";
                worksheet.Cells["B1"].Value = "CustomerAddress";
                worksheet.Cells["C1"].Value = "CustomerZipCode";
                worksheet.Cells["D1"].Value = "CustomerTinNumber";
                worksheet.Cells["E1"].Value = "BusinessStyle";
                worksheet.Cells["F1"].Value = "Terms";
                worksheet.Cells["G1"].Value = "CustomerType";
                worksheet.Cells["H1"].Value = "WithHoldingVat";
                worksheet.Cells["I1"].Value = "WithHoldingTax";
                worksheet.Cells["J1"].Value = "CreatedBy";
                worksheet.Cells["K1"].Value = "CreatedDate";
                worksheet.Cells["L1"].Value = "OriginalCustomerId";
                worksheet.Cells["M1"].Value = "OriginalCustomerNumber";

                int row = 2;

                foreach (var item in selectedList)
                {
                    worksheet.Cells[row, 1].Value = item.CustomerName;
                    worksheet.Cells[row, 2].Value = item.CustomerAddress;
                    worksheet.Cells[row, 3].Value = item.ZipCode;
                    worksheet.Cells[row, 4].Value = item.CustomerTin;
                    worksheet.Cells[row, 5].Value = item.BusinessStyle;
                    worksheet.Cells[row, 6].Value = item.CustomerTerms;
                    worksheet.Cells[row, 7].Value = item.CustomerType;
                    worksheet.Cells[row, 8].Value = item.WithHoldingVat;
                    worksheet.Cells[row, 9].Value = item.WithHoldingTax;
                    worksheet.Cells[row, 10].Value = item.CreatedBy;
                    worksheet.Cells[row, 11].Value = item.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                    worksheet.Cells[row, 12].Value = item.CustomerId;
                    worksheet.Cells[row, 13].Value = item.Number;

                    row++;
                }

                // Convert the Excel package to a byte array
                var excelBytes = await package.GetAsByteArrayAsync();
                await transaction.CommitAsync(cancellationToken);
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "CustomerList.xlsx");
		    }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index), new { view = DynamicView.BankAccount });
            }
        }

        #endregion -- export xlsx record --

        //Upload as .xlsx file.(Import)

        #region -- import xlsx record --

        [HttpPost]
        public async Task<IActionResult> Import(IFormFile file, CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0)
            {
                return RedirectToAction(nameof(Index));
            }

            var customers = new List<Customer>();

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                stream.Position = 0;
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                        if (worksheet == null)
                        {
                            TempData["error"] = "The Excel file contains no worksheets.";
                            return RedirectToAction(nameof(Index), new { view = DynamicView.Customer });
                        }
                        if (worksheet.ToString() != "Customers")
                        {
                            TempData["error"] = "The Excel file is not related to customer master file.";
                            return RedirectToAction(nameof(Index), new { view = DynamicView.Customer });
                        }

                        var rowCount = worksheet.Dimension.Rows;
                        var customerList = await _dbContext
                            .Customers
                            .ToListAsync(cancellationToken);

                        for (int row = 2; row <= rowCount; row++)  // Assuming the first row is the header
                        {
                            var customer = new Customer
                            {
                                Number = await _customerRepo.GetLastNumber(),
                                CustomerName = worksheet.Cells[row, 1].Text,
                                CustomerAddress = worksheet.Cells[row, 2].Text,
                                ZipCode = worksheet.Cells[row, 3].Text,
                                CustomerTin = worksheet.Cells[row, 4].Text,
                                BusinessStyle = worksheet.Cells[row, 5].Text,
                                CustomerTerms = worksheet.Cells[row, 6].Text,
                                CustomerType = worksheet.Cells[row, 7].Text,
                                WithHoldingVat = bool.TryParse(worksheet.Cells[row, 8].Text, out bool withHoldingVat) ? withHoldingVat : false,
                                WithHoldingTax = bool.TryParse(worksheet.Cells[row, 9].Text, out bool withHoldingTax) ? withHoldingTax : false,
                                CreatedBy = worksheet.Cells[row, 10].Text,
                                CreatedDate = DateTime.TryParse(worksheet.Cells[row, 11].Text, out DateTime createdDate) ? createdDate : default,
                                OriginalCustomerId = int.TryParse(worksheet.Cells[row, 12].Text, out int customerId) ? customerId : 0,
                                OriginalCustomerNumber = worksheet.Cells[row, 13].Text,
                            };

                            if (customerList.Any(c => c.OriginalCustomerId == customer.OriginalCustomerId))
                            {
                                continue;
                            }

                            await _dbContext.Customers.AddAsync(customer, cancellationToken);
                            await _dbContext.SaveChangesAsync(cancellationToken);
                        }
                        await transaction.CommitAsync(cancellationToken);
                    }
                }
                catch (OperationCanceledException oce)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = oce.Message;
                    return RedirectToAction(nameof(Index), new { view = DynamicView.Customer });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(Index), new { view = DynamicView.Customer });
                }
            }
            TempData["success"] = "Uploading Success!";
            return RedirectToAction(nameof(Index), new { view = DynamicView.Customer });
        }

        #endregion -- import xlsx record --
    }
}
