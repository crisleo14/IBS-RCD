using Accounting_System.Data;
using Accounting_System.Models.MasterFile;
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
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly ProductRepository _productRepository;

        public ProductController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, ProductRepository productRepository)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _productRepository = productRepository;
        }

        public async Task<IActionResult> Index(string? view, CancellationToken cancellationToken)
        {
            var data = await _dbContext.Products.ToListAsync(cancellationToken);

            if (view == nameof(DynamicView.Product))
            {
                return View("ImportExportIndex", data);
            }

            return View(data);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Code,Name,Unit,Id,CreatedBy,CreatedDate")] Product product, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                if (await _productRepository.IsProductCodeExist(product.Code, cancellationToken))
                {
                    ModelState.AddModelError("Code", "Product code already exist!");
                    return View(product);
                }

                if (await _productRepository.IsProductNameExist(product.Name, cancellationToken))
                {
                    ModelState.AddModelError("Name", "Product name already exist!");
                    return View(product);
                }

                product.CreatedBy = _userManager.GetUserName(this.User).ToUpper();

                //#region --Audit Trail Recording

                //AuditTrail auditTrail = new(product.CreatedBy, $"Created new product {product.Name}", "Product");
                //await _dbContext.AddAsync(auditTrail, cancellationToken);

                //#endregion --Audit Trail Recording

                await _dbContext.AddAsync(product, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                TempData["success"] = "Product created successfully";
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        public async Task<IActionResult> Edit(int? id, CancellationToken cancellationToken)
        {
            if (id == null || _dbContext.Products == null)
            {
                return NotFound();
            }

            var product = await _dbContext.Products.FindAsync(id, cancellationToken);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Code,Name,Unit,Id,CreatedBy,CreatedDate")] Product product, CancellationToken cancellationToken)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _dbContext.Update(product);

                    //#region --Audit Trail Recording

                    //AuditTrail auditTrail = new(_userManager.GetUserName(this.User), $"Updated product {product.Name}", "Product");
                    //await _dbContext.AddAsync(auditTrail, cancellationToken);

                    //#endregion --Audit Trail Recording

                    TempData["success"] = "Product updated successfully";
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id))
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
            return View(product);
        }

        private bool ProductExists(int id)
        {
            return (_dbContext.Products?.Any(e => e.Id == id)).GetValueOrDefault();
        }

        //Download as .xlsx file.(Export)
        #region -- export xlsx record --

        [HttpPost]
        public async Task<IActionResult> Export(string selectedRecord)
        {
            if (string.IsNullOrEmpty(selectedRecord))
            {
                // Handle the case where no invoices are selected
                return RedirectToAction(nameof(Index));
            }

            var recordIds = selectedRecord.Split(',').Select(int.Parse).ToList();

            // Retrieve the selected invoices from the database
            var selectedList = await _dbContext.Products
                .Where(p => recordIds.Contains(p.Id))
                .OrderBy(p => p.Id)
                .ToListAsync();

            // Create the Excel package
            using var package = new ExcelPackage();
            // Add a new worksheet to the Excel package
            var worksheet = package.Workbook.Worksheets.Add("Product");

            worksheet.Cells["A1"].Value = "ProductCode";
            worksheet.Cells["B1"].Value = "ProductName";
            worksheet.Cells["C1"].Value = "ProductUnit";
            worksheet.Cells["D1"].Value = "CreatedBy";
            worksheet.Cells["E1"].Value = "CreatedDate";
            worksheet.Cells["F1"].Value = "OriginalProductId";
            worksheet.Cells["G1"].Value = "OriginalProductCode";

            int row = 2;

            foreach (var item in selectedList)
            {
                worksheet.Cells[row, 1].Value = item.Code;
                worksheet.Cells[row, 2].Value = item.Name;
                worksheet.Cells[row, 3].Value = item.Unit;
                worksheet.Cells[row, 4].Value = item.CreatedBy;
                worksheet.Cells[row, 5].Value = item.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                worksheet.Cells[row, 6].Value = item.Id;
                worksheet.Cells[row, 7].Value = item.Code;

                row++;
            }

            // Convert the Excel package to a byte array
            var excelBytes = await package.GetAsByteArrayAsync();

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ProductList.xlsx");
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
                            var product = new Product
                            {
                                Code = worksheet.Cells[row, 1].Text,
                                Name = worksheet.Cells[row, 2].Text,
                                Unit = worksheet.Cells[row, 3].Text,
                                CreatedBy = worksheet.Cells[row, 4].Text,
                                CreatedDate = DateTime.TryParse(worksheet.Cells[row, 5].Text, out DateTime createdDate) ? createdDate : default,
                                OriginalProductId = int.TryParse(worksheet.Cells[row, 6].Text, out int originalProductId) ? originalProductId : 0,
                            };
                            await _dbContext.Products.AddAsync(product);
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