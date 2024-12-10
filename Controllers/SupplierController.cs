using System.Diagnostics.CodeAnalysis;
using Accounting_System.Data;
using Accounting_System.Models.AccountsReceivable;
using Accounting_System.Models.MasterFile;
using Accounting_System.Models.Reports;
using Accounting_System.Repository;
using Accounting_System.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

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

        public async Task<IActionResult> Index(string? view, CancellationToken cancellationToken)
        {
            var data = await _context.Suppliers.ToListAsync(cancellationToken);

            if (view == nameof(DynamicView.Supplier))
            {
                return View("ImportExportIndex", data);
            }

            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllSupplierIds(CancellationToken cancellationToken)
        {
            var supplierIds = await _context.Suppliers
                                     .Select(s => s.Id) // Assuming Id is the primary key
                                     .ToListAsync(cancellationToken);
            return Json(supplierIds);
        }


        public async Task<IActionResult> Create(CancellationToken cancellationToken)
        {
            Supplier model = new();
            model.DefaultExpenses = await _context.ChartOfAccounts
                .Where(coa => coa.Level == 4 || coa.Level == 5)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber + " " + s.AccountName,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);
            model.WithholdingTaxList = await _context.ChartOfAccounts
                .Where(coa => coa.AccountNumber == "2010302" || coa.AccountNumber == "2010303")
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber + " " + s.AccountName,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Supplier supplier, IFormFile? document, IFormFile? registration, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid)
            {
                if (await _supplierRepo.IsSupplierNameExist(supplier.Name, supplier.Category, cancellationToken))
                {
                    ModelState.AddModelError("Name", "Supplier name already exist!");
                    supplier.DefaultExpenses = await _context.ChartOfAccounts
                        .Select(s => new SelectListItem
                        {
                            Value = s.AccountNumber + " " + s.AccountName,
                            Text = s.AccountNumber + " " + s.AccountName
                        })
                        .ToListAsync(cancellationToken);
                    supplier.WithholdingTaxList = await _context.ChartOfAccounts
                        .Where(coa => coa.AccountNumber == "2010302" || coa.AccountNumber == "2010303")
                        .Select(s => new SelectListItem
                        {
                            Value = s.AccountNumber + " " + s.AccountName,
                            Text = s.AccountNumber + " " + s.AccountName
                        })
                        .ToListAsync(cancellationToken);
                    return View(supplier);
                }

                if (await _supplierRepo.IsSupplierTinExist(supplier.TinNo, supplier.Category, cancellationToken))
                {
                    ModelState.AddModelError("TinNo", "Supplier tin already exist!");
                    supplier.DefaultExpenses = await _context.ChartOfAccounts
                        .Select(s => new SelectListItem
                        {
                            Value = s.AccountNumber + " " + s.AccountName,
                            Text = s.AccountNumber + " " + s.AccountName
                        })
                        .ToListAsync(cancellationToken);
                    supplier.WithholdingTaxList = await _context.ChartOfAccounts
                        .Where(coa => coa.AccountNumber == "2010302" || coa.AccountNumber == "2010303")
                        .Select(s => new SelectListItem
                        {
                            Value = s.AccountNumber + " " + s.AccountName,
                            Text = s.AccountNumber + " " + s.AccountName
                        })
                        .ToListAsync(cancellationToken);
                    return View(supplier);
                }

                supplier.CreatedBy = _userManager.GetUserName(this.User).ToString();
                supplier.Number = await _supplierRepo.GetLastNumber(cancellationToken);
                if (supplier.WithholdingTaxtitle != null && supplier.WithholdingTaxPercent != 0)
                {
                    supplier.WithholdingTaxPercent = supplier.WithholdingTaxtitle.StartsWith("2010302") ? 1 : 2;
                }

                if (document != null && document.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Proof of Exemption", supplier.Number.ToString());

                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string fileName = Path.GetFileName(document.FileName);
                    string fileSavePath = Path.Combine(uploadsFolder, fileName);

                    using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
                    {
                        await document.CopyToAsync(stream, cancellationToken);
                    }

                    supplier.ProofOfExemptionFilePath = fileSavePath;
                }

                if (registration != null && registration.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Proof of Registration", supplier.Number.ToString());

                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string fileName = Path.GetFileName(registration.FileName);
                    string fileSavePath = Path.Combine(uploadsFolder, fileName);

                    using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
                    {
                        await registration.CopyToAsync(stream, cancellationToken);
                    }

                    supplier.ProofOfRegistrationFilePath = fileSavePath;
                }
                else
                {
                    TempData["error"] = "There's something wrong in your file. Contact MIS.";
                    supplier.DefaultExpenses = await _context.ChartOfAccounts
                        .Select(s => new SelectListItem
                        {
                            Value = s.AccountNumber + " " + s.AccountName,
                            Text = s.AccountNumber + " " + s.AccountName
                        })
                        .ToListAsync(cancellationToken);
                    supplier.WithholdingTaxList = await _context.ChartOfAccounts
                        .Where(coa => coa.AccountNumber == "2010302" || coa.AccountNumber == "2010303")
                        .Select(s => new SelectListItem
                        {
                            Value = s.AccountNumber + " " + s.AccountName,
                            Text = s.AccountNumber + " " + s.AccountName
                        })
                        .ToListAsync(cancellationToken);
                    return View(supplier);
                }

                await _context.AddAsync(supplier, cancellationToken);

                #region --Audit Trail Recording

                if (supplier.OriginalSupplierId == 0)
                {
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    AuditTrail auditTrailBook = new(supplier.CreatedBy, $"Create new supplier {supplier.Name}", "Supplier Master File", ipAddress);
                    await _context.AddAsync(auditTrailBook, cancellationToken);
                }

                #endregion --Audit Trail Recording

                await _context.SaveChangesAsync(cancellationToken);
                TempData["success"] = $"Supplier {supplier.Name} has been created.";
                return RedirectToAction(nameof(Index));
            }
            return View(supplier);
        }

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
            supplier.DefaultExpenses = await _context.ChartOfAccounts
                .Where(coa => coa.Level == 4 || coa.Level == 5)
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber + " " + s.AccountName,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);
            supplier.WithholdingTaxList = await _context.ChartOfAccounts
                .Where(coa => coa.AccountNumber == "2010302" || coa.AccountNumber == "2010303")
                .Select(s => new SelectListItem
                {
                    Value = s.AccountNumber + " " + s.AccountName,
                    Text = s.AccountNumber + " " + s.AccountName
                })
                .ToListAsync(cancellationToken);
            return View(supplier);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Supplier supplier, IFormFile? document, IFormFile? registration, CancellationToken cancellationToken)
        {
            if (id != supplier.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingModel = await _context.Suppliers.FindAsync(supplier.Id, cancellationToken);

                    existingModel.ReasonOfExemption = supplier.ReasonOfExemption;
                    existingModel.Validity = supplier.Validity;
                    existingModel.ValidityDate = supplier.ValidityDate;
                    existingModel.Name = supplier.Name;
                    existingModel.Address = supplier.Address;
                    existingModel.TinNo = supplier.TinNo;
                    existingModel.Terms = supplier.Terms;
                    existingModel.VatType = supplier.VatType;
                    existingModel.TaxType = supplier.TaxType;
                    existingModel.Category = supplier.Category;
                    existingModel.TradeName = supplier.TradeName;
                    existingModel.Branch = supplier.Branch;
                    existingModel.DefaultExpenseNumber = supplier.DefaultExpenseNumber;
                    if (existingModel.WithholdingTaxtitle != null && existingModel.WithholdingTaxPercent != 0)
                    {
                        existingModel.WithholdingTaxPercent = supplier.WithholdingTaxtitle.StartsWith("2010302") ? 1 : 2;
                    }
                    existingModel.WithholdingTaxtitle = supplier.WithholdingTaxtitle;
                    supplier.Number = existingModel.Number;
                    supplier.CreatedBy = _userManager.GetUserName(this.User).ToString();

                    #region -- Upload file -- 

                    if (document != null && document.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Proof of Exemption", supplier.Number.ToString());

                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        string fileName = Path.GetFileName(document.FileName);
                        string fileSavePath = Path.Combine(uploadsFolder, fileName);

                        using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
                        {
                            await document.CopyToAsync(stream, cancellationToken);
                        }

                        existingModel.ProofOfExemptionFilePath = fileSavePath;
                    }

                    if (registration != null && registration.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Proof of Registration", supplier.Number.ToString());

                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        string fileName = Path.GetFileName(registration.FileName);
                        string fileSavePath = Path.Combine(uploadsFolder, fileName);

                        using (FileStream stream = new FileStream(fileSavePath, FileMode.Create))
                        {
                            await registration.CopyToAsync(stream, cancellationToken);
                        }

                        existingModel.ProofOfRegistrationFilePath = fileSavePath;
                    }
                    else
                    {
                        supplier.DefaultExpenses = await _context.ChartOfAccounts
                            .Select(s => new SelectListItem
                            {
                                Value = s.AccountNumber + " " + s.AccountName,
                                Text = s.AccountNumber + " " + s.AccountName
                            })
                            .ToListAsync(cancellationToken);
                        supplier.WithholdingTaxList = await _context.ChartOfAccounts
                            .Where(coa => coa.AccountNumber == "2010302" || coa.AccountNumber == "2010303")
                            .Select(s => new SelectListItem
                            {
                                Value = s.AccountNumber + " " + s.AccountName,
                                Text = s.AccountNumber + " " + s.AccountName
                            })
                            .ToListAsync(cancellationToken);
                        TempData["error"] = "There's something wrong in your file. Contact MIS.";
                        return View(supplier);
                    }

                    #endregion -- Upload file -- 

                    #region --Audit Trail Recording

                    if (supplier.OriginalSupplierId == 0)
                    {
                        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                        AuditTrail auditTrailBook = new(_userManager.GetUserName(this.User), $"Update supplier {supplier.Name}", "Supplier Master File", ipAddress);
                        await _context.AddAsync(auditTrailBook, cancellationToken);
                    }

                    #endregion --Audit Trail Recording

                    await _context.SaveChangesAsync(cancellationToken);
                    TempData["success"] = $"Supplier {supplier.Name} has been edited.";
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

        private bool SupplierExists(int id)
        {
            return (_context.Suppliers?.Any(e => e.Id == id)).GetValueOrDefault();
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
            var selectedList = await _context.Suppliers
                .Where(supp => recordIds.Contains(supp.Id))
                .OrderBy(supp => supp.Number)
                .ToListAsync();

            // Create the Excel package
            using var package = new ExcelPackage();
            // Add a new worksheet to the Excel package
            var worksheet = package.Workbook.Worksheets.Add("Supplier");

            worksheet.Cells["A1"].Value = "Name";
            worksheet.Cells["B1"].Value = "Address";
            worksheet.Cells["C1"].Value = "TinNo";
            worksheet.Cells["D1"].Value = "Terms";
            worksheet.Cells["E1"].Value = "VatType";
            worksheet.Cells["F1"].Value = "TaxType";
            worksheet.Cells["G1"].Value = "ProofOfRegistrationFilePath";
            worksheet.Cells["H1"].Value = "ReasonOfExemption";
            worksheet.Cells["I1"].Value = "Validity";
            worksheet.Cells["J1"].Value = "ValidityDate";
            worksheet.Cells["K1"].Value = "ProofOfExemptionFilePath";
            worksheet.Cells["L1"].Value = "CreatedBy";
            worksheet.Cells["M1"].Value = "CreatedDate";
            worksheet.Cells["N1"].Value = "Branch";
            worksheet.Cells["O1"].Value = "Category";
            worksheet.Cells["P1"].Value = "TradeName";
            worksheet.Cells["Q1"].Value = "DefaultExpenseNumber";
            worksheet.Cells["R1"].Value = "WithholdingTaxPercent";
            worksheet.Cells["S1"].Value = "WithholdingTaxTitle";
            worksheet.Cells["T1"].Value = "OriginalSupplierId";

            int row = 2;

            foreach (var item in selectedList)
            {
                worksheet.Cells[row, 1].Value = item.Name;
                worksheet.Cells[row, 2].Value = item.Address;
                worksheet.Cells[row, 3].Value = item.TinNo;
                worksheet.Cells[row, 4].Value = item.Terms;
                worksheet.Cells[row, 5].Value = item.VatType;
                worksheet.Cells[row, 6].Value = item.TaxType;
                worksheet.Cells[row, 7].Value = item.ProofOfRegistrationFilePath;
                worksheet.Cells[row, 8].Value = item.ReasonOfExemption;
                worksheet.Cells[row, 9].Value = item.Validity;
                worksheet.Cells[row, 10].Value = item.ValidityDate?.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                worksheet.Cells[row, 11].Value = item.ProofOfExemptionFilePath;
                worksheet.Cells[row, 12].Value = item.CreatedBy;
                worksheet.Cells[row, 13].Value = item.CreatedDate.ToString("yyyy-MM-dd hh:mm:ss.ffffff");
                worksheet.Cells[row, 14].Value = item.Branch;
                worksheet.Cells[row, 15].Value = item.Category;
                worksheet.Cells[row, 16].Value = item.TradeName;
                worksheet.Cells[row, 17].Value = item.DefaultExpenseNumber;
                worksheet.Cells[row, 18].Value = item.WithholdingTaxPercent;
                worksheet.Cells[row, 19].Value = item.WithholdingTaxtitle;
                worksheet.Cells[row, 20].Value = item.Id;

                row++;
            }

            // Convert the Excel package to a byte array
            var excelBytes = await package.GetAsByteArrayAsync();

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "SupplierList.xlsx");
        }

        #endregion -- export xlsx record --

        //Upload as .xlsx file.(Import)
        #region -- import xlsx record --

        [HttpPost]
        [SuppressMessage("ReSharper.DPA", "DPA0000: DPA issues")]
        public async Task<IActionResult> Import(IFormFile file, CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0)
            {
                return RedirectToAction(nameof(Index));
            }

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                stream.Position = 0;
                await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                
                try
                {
                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                        if (worksheet == null)
                        {
                            TempData["error"] = "The Excel file contains no worksheets.";
                            return RedirectToAction(nameof(Index), new { view = DynamicView.Supplier });
                        }
                        if (worksheet.ToString() != nameof(DynamicView.Supplier))
                        {
                            TempData["error"] = "The Excel file is not related to supplier master file.";
                            return RedirectToAction(nameof(Index), new { view = DynamicView.Supplier });
                        }

                        var rowCount = worksheet.Dimension.Rows;
                        var supplierList = await _context
                            .Suppliers
                            .ToListAsync(cancellationToken);
                        
                        for (int row = 2; row <= rowCount; row++)  // Assuming the first row is the header
                        {
                            var supplier = new Supplier
                            {
                                Number = await _supplierRepo.GetLastNumber(),
                                Name = worksheet.Cells[row, 1].Text,
                                Address = worksheet.Cells[row, 2].Text,
                                TinNo = worksheet.Cells[row, 3].Text,
                                Terms = worksheet.Cells[row, 4].Text,
                                VatType = worksheet.Cells[row, 5].Text,
                                TaxType = worksheet.Cells[row, 6].Text,
                                ProofOfRegistrationFilePath = worksheet.Cells[row, 7].Text,
                                ReasonOfExemption = worksheet.Cells[row, 8].Text,
                                Validity = worksheet.Cells[row, 9].Text,
                                ValidityDate = DateTime.TryParse(worksheet.Cells[row, 10].Text, out DateTime validityDate) ? validityDate : default,
                                ProofOfExemptionFilePath = worksheet.Cells[row, 11].Text,
                                CreatedBy = worksheet.Cells[row, 12].Text,
                                CreatedDate = DateTime.TryParse(worksheet.Cells[row, 13].Text, out DateTime createdDate) ? createdDate : default,
                                Branch = worksheet.Cells[row, 14].Text,
                                Category = worksheet.Cells[row, 15].Text,
                                TradeName = worksheet.Cells[row, 16].Text,
                                DefaultExpenseNumber = worksheet.Cells[row, 17].Text,
                                WithholdingTaxPercent = int.TryParse(worksheet.Cells[row, 18].Text, out int withholdingTaxPercent) ? withholdingTaxPercent : 0,
                                WithholdingTaxtitle = worksheet.Cells[row, 19].Text,
                                OriginalSupplierId = int.TryParse(worksheet.Cells[row, 20].Text, out int originalSupplierId) ? originalSupplierId : 0,
                            };
                            
                            if (supplierList.Any(supp => supp.OriginalSupplierId == supplier.OriginalSupplierId))
                            {
                                continue;
                            }

                            await _context.Suppliers.AddAsync(supplier, cancellationToken);
                        }
                        await _context.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                    }
                }
                catch (OperationCanceledException oce)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = oce.Message;
                    return RedirectToAction(nameof(Index), new { view = DynamicView.Supplier });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    TempData["error"] = ex.Message;
                    return RedirectToAction(nameof(Index), new { view = DynamicView.Supplier });
                }
            }

            TempData["success"] = "Uploading Success!";
            return RedirectToAction(nameof(Index), new { view = DynamicView.Supplier });
        }

        #endregion -- import xlsx record --
    }
}