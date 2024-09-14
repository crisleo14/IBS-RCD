using Accounting_System.Data;
using Accounting_System.Models;
using Accounting_System.Models.Reports;
using Accounting_System.Repository;
using Accounting_System.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;

namespace Accounting_System.Controllers
{
    [Authorize]
    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly CustomerRepo _customerRepo;

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
                if (await _customerRepo.IsCustomerExist(customer.Name, cancellationToken))
                {
                    ModelState.AddModelError("Name", "Customer already exist!");
                    return View(customer);
                }

                if (await _customerRepo.IsTinNoExist(customer.TinNo, cancellationToken))
                {
                    ModelState.AddModelError("TinNo", "Tin# already exist!");
                    return View(customer);
                }

                customer.Number = await _customerRepo.GetLastNumber(cancellationToken);
                customer.CreatedBy = _userManager.GetUserName(this.User);
                await _dbContext.AddAsync(customer, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                #region --Audit Trail Recording

                AuditTrail auditTrail = new(customer.CreatedBy, $"Created new customer {customer.Name}", "Customer");
                await _dbContext.AddAsync(auditTrail, cancellationToken);

                #endregion --Audit Trail Recording

                TempData["success"] = "Customer created successfully";
                return RedirectToAction(nameof(Index));
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
            if (id != customer.Id)
            {
                return NotFound();
            }
            var existingModel = await _customerRepo.FindCustomerAsync(id, cancellationToken);

            if (ModelState.IsValid)
            {
                try
                {
                    existingModel.Name = customer.Name;
                    existingModel.Address = customer.Address;
                    existingModel.TinNo = customer.TinNo;
                    existingModel.BusinessStyle = customer.BusinessStyle;
                    existingModel.Terms = customer.Terms;
                    existingModel.CustomerType = customer.CustomerType;
                    existingModel.WithHoldingTax = customer.WithHoldingTax;
                    existingModel.WithHoldingVat = customer.WithHoldingVat;

                    #region --Audit Trail Recording

                    AuditTrail auditTrail = new(_userManager.GetUserName(this.User), $"Updated customer {customer.Name}", "Customer");
                    await _dbContext.AddAsync(auditTrail, cancellationToken);

                    #endregion --Audit Trail Recording

                    _dbContext.Update(existingModel);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    TempData["success"] = "Customer updated successfully";
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(customer);
        }

        //Download as .xlsx file.(Export)
        #region -- export xlsx record --

        [HttpPost]
        public IActionResult Export(string selectedRecord)
        {
            if (string.IsNullOrEmpty(selectedRecord))
            {
                // Handle the case where no invoices are selected
                return RedirectToAction(nameof(Index));
            }

            var recordIds = selectedRecord.Split(',').Select(int.Parse).ToList();

            // Retrieve the selected invoices from the database
            var selectedList = _dbContext.Customers
                .Where(c => recordIds.Contains(c.Id))
                .OrderBy(c => c.Id)
                .ToList();

            // Create the Excel package
            using var package = new ExcelPackage();
            // Add a new worksheet to the Excel package
            var worksheet = package.Workbook.Worksheets.Add("Customers");

            worksheet.Cells["A1"].Value = "CustomerName";
            worksheet.Cells["B1"].Value = "CustomerAddress";
            worksheet.Cells["C1"].Value = "CustomerTinNumber";
            worksheet.Cells["D1"].Value = "BusinessStyle";
            worksheet.Cells["E1"].Value = "Terms";
            worksheet.Cells["F1"].Value = "CustomerType";
            worksheet.Cells["G1"].Value = "WithHoldingVat";
            worksheet.Cells["H1"].Value = "WithHoldingTax";
            worksheet.Cells["I1"].Value = "CreatedBy";
            worksheet.Cells["J1"].Value = "CreatedDate";
            worksheet.Cells["K1"].Value = "OriginalCustomerId";
            worksheet.Cells["L1"].Value = "OriginalCustomerNumber";

            int row = 2;

            foreach (var item in selectedList)
            {
                worksheet.Cells[row, 1].Value = item.Name;
                worksheet.Cells[row, 2].Value = item.Address;
                worksheet.Cells[row, 3].Value = item.TinNo;
                worksheet.Cells[row, 4].Value = item.BusinessStyle;
                worksheet.Cells[row, 5].Value = item.Terms;
                worksheet.Cells[row, 6].Value = item.CustomerType;
                worksheet.Cells[row, 7].Value = item.WithHoldingVat;
                worksheet.Cells[row, 8].Value = item.WithHoldingTax;
                worksheet.Cells[row, 9].Value = item.CreatedBy;
                worksheet.Cells[row, 10].Value = item.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                worksheet.Cells[row, 11].Value = item.Id;
                worksheet.Cells[row, 12].Value = item.Number;

                row++;
            }

            // Convert the Excel package to a byte array
            var excelBytes = package.GetAsByteArray();

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "CustomerList.xlsx");
        }

        #endregion -- export xlsx record --

        //Upload as .xlsx file.(Import)
        #region -- import xlsx record --

        [HttpPost]
        public async Task<IActionResult> Import(IFormFile file)
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
                try
                {
                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                        if (worksheet == null)
                        {
                            return RedirectToAction(nameof(Index), new { errorMessage = "The Excel file contains no worksheets." });
                        }

                        var rowCount = worksheet.Dimension.Rows;

                        for (int row = 2; row <= rowCount; row++)  // Assuming the first row is the header
                        {
                            var customer = new Customer
                            {
                                Number = await _customerRepo.GetLastNumber(),
                                Name = worksheet.Cells[row, 1].Text,
                                Address = worksheet.Cells[row, 2].Text,
                                TinNo = worksheet.Cells[row, 3].Text,
                                BusinessStyle = worksheet.Cells[row, 4].Text,
                                Terms = worksheet.Cells[row, 5].Text,
                                CustomerType = worksheet.Cells[row, 6].Text,
                                WithHoldingVat = bool.TryParse(worksheet.Cells[row, 7].Text, out bool withHoldingVat) ? withHoldingVat : false,
                                WithHoldingTax = bool.TryParse(worksheet.Cells[row, 8].Text, out bool withHoldingTax) ? withHoldingTax : false,
                                CreatedBy = worksheet.Cells[row, 9].Text,
                                CreatedDate = DateTime.TryParse(worksheet.Cells[row, 10].Text, out DateTime createdDate) ? createdDate : default,
                                OriginalCustomerId = int.TryParse(worksheet.Cells[row, 11].Text, out int customerId) ? customerId : 0,
                                OriginalCustomerNumber = int.TryParse(worksheet.Cells[row, 12].Text, out int customerNumber) ? customerNumber : 0,
                            };
                            await _dbContext.Customers.AddAsync(customer);
                            await _dbContext.SaveChangesAsync();
                        }
                    }
                }
                catch (OperationCanceledException oce)
                {
                    TempData["error"] = oce.Message;
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(Index));
                }
            }

            return RedirectToAction(nameof(Index));
        }

        #endregion -- import xlsx record --
    }
}