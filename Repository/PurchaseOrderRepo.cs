using Accounting_System.Data;
using Accounting_System.DTOs;
using Accounting_System.Models;
using Accounting_System.Models.AccountsPayable;
using Accounting_System.Models.Reports;
using Accounting_System.Models.ViewModels;
using Accounting_System.Utility;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace Accounting_System.Repository
{
    public class PurchaseOrderRepo
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly GeneralRepo _generalRepo;

        private readonly AasDbContext _aasDbContext;

        public PurchaseOrderRepo(ApplicationDbContext dbContext, GeneralRepo generalRepo, AasDbContext aasDbContext)
        {
            _dbContext = dbContext;
            _generalRepo = generalRepo;
            _aasDbContext = aasDbContext;
        }

        public async Task<List<PurchaseOrder>> GetPurchaseOrderAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext
                .PurchaseOrders
                .Include(p => p.Supplier)
                .Include(p => p.Product)
                .ToListAsync(cancellationToken);
        }

        public async Task<string> GeneratePONo(CancellationToken cancellationToken = default)
        {
            var purchaseOrder = await _dbContext
                .PurchaseOrders
                .Where(po => !po.PurchaseOrderNo!.StartsWith("POBEG"))
                .OrderByDescending(s => s.PurchaseOrderNo)
                .FirstOrDefaultAsync(cancellationToken);

            if (purchaseOrder != null)
            {
                string lastSeries = purchaseOrder.PurchaseOrderNo ?? throw new InvalidOperationException("PONo is null pls Contact MIS Enterprise");
                string numericPart = lastSeries.Substring(2);
                int incrementedNumber = int.Parse(numericPart) + 1;

                return lastSeries.Substring(0,2) + incrementedNumber.ToString("D10");
            }
            else
            {
                return $"PO{1.ToString("D10")}";
            }
        }

        public async Task<int> GetSupplierNoAsync(int? id, CancellationToken cancellationToken = default)
        {
            if (id != 0)
            {
                var supplier = await _dbContext
                                .Suppliers
                                .FirstOrDefaultAsync(s => s.SupplierId == id, cancellationToken);
                return supplier!.Number;
            }
            else
            {
                throw new ArgumentException("No record found in supplier.");
            }
        }

        public async Task<string> GetProductNoAsync(int? id, CancellationToken cancellationToken = default)
        {
            if (id != 0)
            {
                var product = await _dbContext
                                .Products
                                .FirstOrDefaultAsync(s => s.ProductId == id, cancellationToken);
                return product!.ProductCode!;
            }

            throw new ArgumentException("No record found in supplier.");
        }

        public async Task<PurchaseOrder> FindPurchaseOrder(int? id, CancellationToken cancellationToken = default)
        {
            var po = await _dbContext
                .PurchaseOrders
                .Include(p => p.Supplier)
                .Include(p => p.Product)
                .FirstOrDefaultAsync(po => po.PurchaseOrderId == id, cancellationToken);

            if (po != null)
            {
                return po;
            }
            else
            {
                throw new ArgumentException("Invalid id value. The id must be greater than 0.");
            }
        }

        public async Task LogChangesAsync(int id, Dictionary<string, (string OriginalValue, string NewValue)> changes, string? modifiedBy, string seriesNumber, string databaseName)
        {
            foreach (var change in changes)
            {
                var logReport = new ImportExportLog()
                {
                    Id = Guid.NewGuid(),
                    TableName = nameof(DynamicView.PurchaseOrder),
                    DocumentRecordId = id,
                    ColumnName = change.Key,
                    Module = "Purchase Order",
                    OriginalValue = change.Value.OriginalValue,
                    AdjustedValue = change.Value.NewValue,
                    TimeStamp = DateTime.Now,
                    UploadedBy = modifiedBy,
                    Action = string.Empty,
                    Executed = false,
                    DocumentNo = seriesNumber,
                    DatabaseName = databaseName
                };
                await _dbContext.AddAsync(logReport);
            }
        }

        public IReadOnlyList<PurchaseOrderUploadExcelFileViewModel> ParseWorksheet(
            ExcelWorksheet worksheet)
        {
            var rows = new List<PurchaseOrderUploadExcelFileViewModel>();
            var rowCount = worksheet.Dimension.Rows;

            for (var row = 2; row <= rowCount; row++)
            {
                rows.Add(new PurchaseOrderUploadExcelFileViewModel
                {
                    PurchaseOrderNo = StringHelper.NormalizeString(worksheet.Cells[row, 16].GetValue<string>()),
                    Date = DateOnly.FromDateTime(worksheet.Cells[row, 1].GetValue<DateTime>()),
                    Terms = StringHelper.NormalizeString(worksheet.Cells[row, 2].GetValue<string>()),
                    Quantity = worksheet.Cells[row, 3].GetValue<decimal>(),
                    Price = worksheet.Cells[row, 4].GetValue<decimal>(),
                    Amount = worksheet.Cells[row, 5].GetValue<decimal>(),
                    FinalPrice = string.IsNullOrWhiteSpace(worksheet.Cells[row, 6].Text)
                        ? null
                        : worksheet.Cells[row, 6].GetValue<decimal>(),
                    Remarks = StringHelper.NormalizeString(worksheet.Cells[row, 10].GetValue<string>()),
                    CreatedBy = string.IsNullOrWhiteSpace(worksheet.Cells[row, 11].Text)
                        ? string.Empty
                        : StringHelper.NormalizeString(worksheet.Cells[row, 11].GetValue<string?>()),
                    CreatedDate = worksheet.Cells[row, 12].GetValue<DateTime>(),
                    PostedBy = string.IsNullOrWhiteSpace(worksheet.Cells[row, 19].Text)
                        ? string.Empty
                        : StringHelper.NormalizeString(worksheet.Cells[row, 19].GetValue<string?>()),
                    PostedDate = worksheet.Cells[row, 20].GetValue<DateTime>(),
                    IsClosed = worksheet.Cells[row, 13].GetValue<bool>(),
                    CancellationRemarks = string.IsNullOrWhiteSpace(worksheet.Cells[row, 14].Text)
                        ? string.Empty
                        : StringHelper.NormalizeString(worksheet.Cells[row, 14].GetValue<string?>()),
                    OriginalProductId = worksheet.Cells[row, 15].GetValue<int>(),
                    OriginalSeriesNumber = string.IsNullOrWhiteSpace(worksheet.Cells[row, 16].Text)
                        ? string.Empty
                        : StringHelper.NormalizeString(worksheet.Cells[row, 16].GetValue<string?>()),
                    OriginalSupplierId = worksheet.Cells[row, 17].GetValue<int>(),
                    OriginalDocumentId = worksheet.Cells[row, 18].GetValue<int>()
                });
            }

            return rows;
        }

        public async Task<FindPurchaseOrderInDbContextDto> BuildLookupPurchaseOrderContextAsync(
            IEnumerable<PurchaseOrderUploadExcelFileViewModel> rows,
            CancellationToken cancellationToken)
        {
            var originalProductIds = rows.Select(r => r.OriginalProductId).Distinct().ToList();
            var originalSupplierIds = rows.Select(r => r.OriginalSupplierId).Distinct().ToList();
            var originalSeriesNumbers = rows.Select(r => r.OriginalSeriesNumber).Distinct().ToList();

            return new FindPurchaseOrderInDbContextDto
            {
                ExistingPurchaseOrder = await _dbContext.PurchaseOrders
                    .Where(x => originalSeriesNumbers.Contains(x.OriginalSeriesNumber))
                    .GroupBy(x => x.OriginalSeriesNumber)
                    .Select(x => x.First())
                    .ToDictionaryAsync(x => x.OriginalSeriesNumber, cancellationToken),

                ExistingProduct = await _dbContext.Products
                    .Where(x => originalProductIds.Contains(x.OriginalProductId))
                    .GroupBy(x => x.OriginalProductId)
                    .Select(x => x.First())
                    .ToDictionaryAsync(
                        x => x.OriginalProductId,
                        x => (x.ProductId, x.ProductCode),
                        cancellationToken),

                ExistingSuppliers = await _dbContext.Suppliers
                    .Where(x => originalSupplierIds.Contains(x.OriginalSupplierId!.Value))
                    .GroupBy(x => x.OriginalSupplierId!.Value)
                    .Select(g => g.First())
                    .ToDictionaryAsync(
                        x => x.OriginalSupplierId!.Value,
                        x => (x.SupplierId, x.Number),
                        cancellationToken),

                ExistingLogs = await _dbContext.ImportExportLogs
                    .Where(x => originalSeriesNumbers.Contains(x.DocumentNo))
                    .ToListAsync(cancellationToken)
            };
        }

        public PurchaseOrder MapToPurchaseOrderEntity(
            PurchaseOrderUploadExcelFileViewModel row,
            FindPurchaseOrderInDbContextDto context)
        {
            if (!context.ExistingProduct.TryGetValue(row.OriginalProductId, out var productData))
            {
                throw new InvalidOperationException($"Product id missing for PO#{row.PurchaseOrderNo}.");
            }

            if (!context.ExistingSuppliers.TryGetValue(row.OriginalSupplierId, out var supplierData))
            {
                throw new InvalidOperationException($"Supplier id missing for PO#{row.PurchaseOrderNo}.");
            }

            return new PurchaseOrder
            {
                PurchaseOrderNo = row.PurchaseOrderNo,
                Date = row.Date,
                Terms = row.Terms,
                Quantity = row.Quantity,
                Price = row.Price,
                Amount = row.Amount,
                FinalPrice = row.FinalPrice,
                Remarks = row.Remarks,
                CreatedBy = row.CreatedBy,
                CreatedDate = row.CreatedDate,
                PostedBy = row.PostedBy,
                PostedDate = row.PostedDate,
                IsClosed = row.IsClosed,
                CancellationRemarks = row.CancellationRemarks,
                OriginalProductId = row.OriginalProductId,
                OriginalSeriesNumber = row.OriginalSeriesNumber,
                OriginalSupplierId = row.OriginalSupplierId,
                OriginalDocumentId = row.OriginalDocumentId,

                ProductId = productData.ProductId,
                ProductNo = productData.ProductCode,
                SupplierId = supplierData.SupplierId,
                SupplierNo = supplierData.SupplierNo
            };
        }

        public IEnumerable<AuditTrail> AuditTrails(
            PurchaseOrderUploadExcelFileViewModel row,
            string machineName)
        {
            var audits = new List<AuditTrail>();

            if (!string.IsNullOrWhiteSpace(row.CreatedBy))
            {
                audits.Add(new AuditTrail
                {
                    Username = row.CreatedBy,
                    Activity = $"Create new purchase order# {row.PurchaseOrderNo}",
                    DocumentType = "Purchase Order",
                    MachineName = machineName,
                    Date = row.CreatedDate
                });
            }

            if (!string.IsNullOrWhiteSpace(row.PostedBy) && row.PostedDate != default)
            {
                audits.Add(new AuditTrail
                {
                    Username = row.PostedBy,
                    Activity = $"Posted purchase order# {row.PurchaseOrderNo}",
                    DocumentType = "Purchase Order",
                    MachineName = machineName,
                    Date = row.PostedDate
                });
            }

            return audits;
        }

        public Dictionary<string, (string Original, string New)> Detect(
            PurchaseOrder entity,
            PurchaseOrderUploadExcelFileViewModel row,
            IReadOnlyList<ImportExportLog> logs)
        {
            var changes = new Dictionary<string, (string, string)>();

            _generalRepo.Compare(changes, logs, "PurchaseOrderNo",
                StringHelper.NormalizeString(entity.PurchaseOrderNo),
                row.PurchaseOrderNo);

            _generalRepo.Compare(changes, logs, "Date",
                entity.Date.ToString(CS.DateOnly_Format_For_Validation),
                row.Date.ToString(CS.DateOnly_Format_For_Validation));

            _generalRepo.Compare(changes, logs, "Terms",
                StringHelper.NormalizeString(entity.Terms),
                row.Terms);

            _generalRepo.Compare(changes, logs, "Quantity",
                entity.Quantity.ToString(CS.Four_Decimal_Format),
                row.Quantity.ToString(CS.Four_Decimal_Format));

            _generalRepo.Compare(changes, logs, "Price",
                entity.Price.ToString(CS.Four_Decimal_Format),
                row.Price.ToString(CS.Four_Decimal_Format));

            _generalRepo.Compare(changes, logs, "Amount",
                entity.Amount.ToString(CS.Four_Decimal_Format),
                row.Amount.ToString(CS.Four_Decimal_Format));

            _generalRepo.Compare(changes, logs, "FinalPrice",
                entity.FinalPrice?.ToString(CS.Four_Decimal_Format) ?? String.Empty,
                row.FinalPrice?.ToString(CS.Four_Decimal_Format) ?? string.Empty);

            _generalRepo.Compare(changes, logs, "Remarks",
                StringHelper.NormalizeString(entity.Remarks),
                row.Remarks);

            _generalRepo.Compare(changes, logs, "CreatedBy",
                StringHelper.NormalizeString(entity.CreatedBy),
                row.CreatedBy);

            _generalRepo.Compare(changes, logs, "CreatedDate",
                entity.CreatedDate.ToString(CS.DateTime_Format_For_Validation),
                row.CreatedDate.ToString(CS.DateTime_Format_For_Validation));

            _generalRepo.Compare(changes, logs, "PostedBy",
                StringHelper.NormalizeString(entity.PostedBy),
                row.PostedBy);

            _generalRepo.Compare(changes, logs, "PostedDate",
                (entity.PostedDate ?? DateTime.MinValue).ToString(CS.DateTime_Format_For_Validation),
                row.PostedDate.ToString(CS.DateTime_Format_For_Validation)
            );

            _generalRepo.Compare(changes, logs, "IsClosed",
                entity.IsClosed.ToString(),
                row.IsClosed.ToString());

            _generalRepo.Compare(changes, logs, "CancellationRemarks",
                StringHelper.NormalizeString(entity.CancellationRemarks),
                row.CancellationRemarks);

            _generalRepo.Compare(changes, logs, "OriginalProductId",
                entity.OriginalProductId?.ToString() ?? "0",
                row.OriginalProductId.ToString());

            _generalRepo.Compare(changes, logs, "OriginalSeriesNumber",
                StringHelper.NormalizeString(entity.OriginalSeriesNumber),
                row.OriginalSeriesNumber);

            _generalRepo.Compare(changes, logs, "OriginalSupplierId",
                entity.OriginalSupplierId?.ToString() ?? "0",
                row.OriginalSupplierId.ToString());

            _generalRepo.Compare(changes, logs, "OriginalDocumentId",
                entity.OriginalDocumentId.ToString(),
                row.OriginalDocumentId.ToString());

            return changes;
        }

        public async Task<FindPurchaseOrderInDbContextDto> BuildLookupPurchaseOrderContextForAasAsync(
            IEnumerable<PurchaseOrderUploadExcelFileViewModel> rows,
            CancellationToken cancellationToken)
        {
            var originalProductIds = rows.Select(r => r.OriginalProductId).Distinct().ToList();
            var originalSupplierIds = rows.Select(r => r.OriginalSupplierId).Distinct().ToList();
            var originalSeriesNumbers = rows.Select(r => r.OriginalSeriesNumber).Distinct().ToList();

            return new FindPurchaseOrderInDbContextDto
            {
                ExistingPurchaseOrder = await _aasDbContext.PurchaseOrders
                    .Where(x => originalSeriesNumbers.Contains(x.OriginalSeriesNumber))
                    .GroupBy(x => x.OriginalSeriesNumber)
                    .Select(x => x.First())
                    .ToDictionaryAsync(x => x.OriginalSeriesNumber, cancellationToken),

                ExistingProduct = await _aasDbContext.Products
                    .Where(x => originalProductIds.Contains(x.OriginalProductId))
                    .GroupBy(x => x.OriginalProductId)
                    .Select(x => x.First())
                    .ToDictionaryAsync(
                        x => x.OriginalProductId,
                        x => (x.ProductId, x.ProductCode),
                        cancellationToken),

                ExistingSuppliers = await _aasDbContext.Suppliers
                    .Where(x => originalSupplierIds.Contains(x.OriginalSupplierId!.Value))
                    .GroupBy(x => x.OriginalSupplierId!.Value)
                    .Select(g => g.First())
                    .ToDictionaryAsync(
                        x => x.OriginalSupplierId!.Value,
                        x => (x.SupplierId, x.Number),
                        cancellationToken),

                ExistingLogs = await _dbContext.ImportExportLogs
                    .Where(x => originalSeriesNumbers.Contains(x.DocumentNo))
                    .ToListAsync(cancellationToken)
            };
        }
    }
}
