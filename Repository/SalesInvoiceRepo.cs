using Accounting_System.Data;
using Accounting_System.DTOs;
using Accounting_System.Models;
using Accounting_System.Models.AccountsReceivable;
using Accounting_System.Models.Reports;
using Accounting_System.Models.ViewModels;
using Accounting_System.Utility;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OfficeOpenXml;

namespace Accounting_System.Repository
{
    public class SalesInvoiceRepo
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly GeneralRepo _generalRepo;
        private readonly AasDbContext _aasDbContext;

        public SalesInvoiceRepo(ApplicationDbContext dbContext, GeneralRepo generalRepo, AasDbContext aasDbContext)
        {
            _dbContext = dbContext;
            _generalRepo = generalRepo;
            _aasDbContext = aasDbContext;
        }

        public async Task<List<SalesInvoice>> GetSalesInvoicesAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext
                .SalesInvoices
                .Include(s => s.Product)
                .Include(c => c.Customer)
                .ToListAsync(cancellationToken);
        }

        public async Task<string> GenerateSINo(CancellationToken cancellationToken = default)
        {
            var salesInvoice = await _dbContext
                .SalesInvoices
                .OrderByDescending(s => s.SalesInvoiceNo)
                .FirstOrDefaultAsync(cancellationToken);

            if (salesInvoice != null)
            {
                string lastSeries = salesInvoice.SalesInvoiceNo!;
                string numericPart = lastSeries.Substring(2);
                int incrementedNumber = int.Parse(numericPart) + 1;

                return lastSeries.Substring(0,2) + incrementedNumber.ToString("D10");
            }
            else
            {
                return $"SI{1.ToString("D10")}";
            }
        }

        public async Task<SalesInvoice> FindSalesInvoice(int? id, CancellationToken cancellationToken = default)
        {
            var invoice = await _dbContext
                .SalesInvoices
                .Include(c => c.Customer)
                .Include(s => s.Product)
                .FirstOrDefaultAsync(invoice => invoice.SalesInvoiceId == id);

            if (invoice != null)
            {
                return invoice;
            }
            else
            {
                throw new ArgumentException("Invalid id value. The id must be greater than 0.");
            }
        }

        public DateOnly ComputeDueDateAsync(string customerTerms, DateOnly date, CancellationToken cancellationToken = default)
        {
            if (!customerTerms.IsNullOrEmpty())
            {
                DateOnly dueDate;

                switch (customerTerms)
                {
                    case "7D":
                        return date.AddDays(7);

                    case "10D":
                        return date.AddDays(7);

                    case "15D":
                        return date.AddDays(15);

                    case "30D":
                        return date.AddDays(30);

                    case "45D":
                    case "45PDC":
                        return date.AddDays(45);

                    case "60D":
                    case "60PDC":
                        return date.AddDays(60);

                    case "90D":
                        return date.AddDays(90);

                    case "M15":
                        return date.AddMonths(1).AddDays(15 - date.Day);

                    case "M30":
                        if (date.Month == 1)
                        {
                            dueDate = new DateOnly(date.Year, date.Month, 1).AddMonths(2).AddDays(-1);
                        }
                        else
                        {
                            dueDate = new DateOnly(date.Year, date.Month, 1).AddMonths(2).AddDays(-1);

                            if (dueDate.Day == 31)
                            {
                                dueDate = dueDate.AddDays(-1);
                            }
                        }
                        return dueDate;

                    case "M29":
                        if (date.Month == 1)
                        {
                            dueDate = new DateOnly(date.Year, date.Month, 1).AddMonths(2).AddDays(-1);
                        }
                        else
                        {
                            dueDate = new DateOnly(date.Year, date.Month, 1).AddMonths(2).AddDays(-1);

                            if (dueDate.Day == 31)
                            {
                                dueDate = dueDate.AddDays(-2);
                            }
                            else if (dueDate.Day == 30)
                            {
                                dueDate = dueDate.AddDays(-1);
                            }
                        }
                        return dueDate;

                    default:
                        return date;
                }
            }

            throw new ArgumentException("No record found.");
        }

        public async Task LogChangesAsync(int id, Dictionary<string, (string OriginalValue, string NewValue)> changes, string? modifiedBy, string seriesNumber, string databaseName)
        {
            foreach (var change in changes)
            {
                var logReport = new ImportExportLog()
                {
                    Id = Guid.NewGuid(),
                    TableName = nameof(DynamicView.SalesInvoice),
                    DocumentRecordId = id,
                    ColumnName = change.Key,
                    Module = "Sales Invoice",
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

        public IReadOnlyList<SalesInvoiceUploadExcelFileViewModel> ParseWorksheet(
            ExcelWorksheet worksheet)
        {
            var rows = new List<SalesInvoiceUploadExcelFileViewModel>();
            var rowCount = worksheet.Dimension.Rows;

            for (var row = 2; row <= rowCount; row++)
            {
                rows.Add(new SalesInvoiceUploadExcelFileViewModel
                {
                    SalesInvoiceNo = StringHelper.NormalizeString(worksheet.Cells[row, 21].GetValue<string>()),
                    OtherRefNo = StringHelper.NormalizeString(worksheet.Cells[row, 1].GetValue<string>()),

                    Quantity = worksheet.Cells[row, 2].GetValue<decimal>(),
                    UnitPrice = worksheet.Cells[row, 3].GetValue<decimal>(),
                    Amount = worksheet.Cells[row, 4].GetValue<decimal>(),
                    Discount = worksheet.Cells[row, 8].GetValue<decimal>(),

                    Remarks = StringHelper.NormalizeString(worksheet.Cells[row, 5].GetValue<string>()),
                    Status = StringHelper.NormalizeString(worksheet.Cells[row, 6].GetValue<string>()),

                    TransactionDate = DateOnly.FromDateTime(worksheet.Cells[row, 7].GetValue<DateTime>()),
                    DueDate = DateOnly.FromDateTime(worksheet.Cells[row, 13].GetValue<DateTime>()),

                    CreatedBy = string.IsNullOrWhiteSpace(worksheet.Cells[row, 14].Text)
                        ? string.Empty
                        : StringHelper.NormalizeString(worksheet.Cells[row, 14].GetValue<string>()),
                    CreatedDate = worksheet.Cells[row, 15].GetValue<DateTime>(),

                    PostedBy = string.IsNullOrWhiteSpace(worksheet.Cells[row, 23].Text)
                        ? string.Empty
                        : StringHelper.NormalizeString(worksheet.Cells[row, 23].GetValue<string>()),
                    PostedDate = worksheet.Cells[row, 24].GetValue<DateTime>(),

                    CancellationRemarks = string.IsNullOrWhiteSpace(worksheet.Cells[row, 16].Text)
                        ? string.Empty
                        : StringHelper.NormalizeString(worksheet.Cells[row, 16].GetValue<string?>()),
                    OriginalCustomerId = worksheet.Cells[row, 18].GetValue<int>(),
                    OriginalProductId = worksheet.Cells[row, 20].GetValue<int>(),
                    OriginalDocumentId = worksheet.Cells[row, 22].GetValue<int>(),
                    OriginalSeriesNumber = StringHelper.NormalizeString(worksheet.Cells[row, 21].GetValue<string>())
                });
            }

            return rows;
        }

        public async Task<FindSalesInvoiceInDbContextDto> BuildLookupSalesInvoiceContextAsync(
            IEnumerable<SalesInvoiceUploadExcelFileViewModel> rows,
            CancellationToken cancellationToken)
        {
            var originalCustomerIds = rows.Select(r => r.OriginalCustomerId).Distinct().ToList();
            var originalProductIds = rows.Select(r => r.OriginalProductId).Distinct().ToList();
            var originalSeriesNumbers = rows.Select(r => r.OriginalSeriesNumber).Distinct().ToList();

            return new FindSalesInvoiceInDbContextDto
            {
                ExistingInvoices = await _dbContext.SalesInvoices
                    .Where(x => originalSeriesNumbers.Contains(x.OriginalSeriesNumber))
                    .GroupBy(x => x.OriginalSeriesNumber)
                    .Select(x => x.First())
                    .ToDictionaryAsync(x => x.OriginalSeriesNumber, cancellationToken),

                CustomerId = await _dbContext.Customers
                    .Where(x => originalCustomerIds.Contains(x.OriginalCustomerId))
                    .GroupBy(x => x.OriginalCustomerId)
                    .Select(x => x.First())
                    .ToDictionaryAsync(x => x.OriginalCustomerId, x => x.CustomerId, cancellationToken),

                ProductId = await _dbContext.Products
                    .Where(x => originalProductIds.Contains(x.OriginalProductId))
                    .GroupBy(x => x.OriginalProductId)
                    .Select(x => x.First())
                    .ToDictionaryAsync(x => x.OriginalProductId, x => x.ProductId, cancellationToken),

                ExistingLogs = await _dbContext.ImportExportLogs
                    .Where(x => originalSeriesNumbers.Contains(x.DocumentNo))
                    .ToListAsync(cancellationToken)
            };
        }

        public SalesInvoice MapToSalesInvoiceEntity(
            SalesInvoiceUploadExcelFileViewModel row,
            FindSalesInvoiceInDbContextDto context)
        {
            return new SalesInvoice
            {
                SalesInvoiceNo = row.SalesInvoiceNo,
                OtherRefNo = row.OtherRefNo,
                Quantity = row.Quantity,
                UnitPrice = row.UnitPrice,
                Amount = row.Amount,
                Discount = row.Discount,

                Remarks = row.Remarks,
                Status = row.Status,

                TransactionDate = row.TransactionDate,
                DueDate = row.DueDate,

                CreatedBy = row.CreatedBy,
                CreatedDate = row.CreatedDate,

                PostedBy = row.PostedBy,
                PostedDate = row.PostedDate,

                CancellationRemarks = row.CancellationRemarks,
                OriginalCustomerId = row.OriginalCustomerId,
                OriginalProductId = row.OriginalProductId,
                OriginalDocumentId = row.OriginalDocumentId,
                OriginalSeriesNumber = row.OriginalSeriesNumber,

                CustomerId = context.CustomerId.TryGetValue(row.OriginalCustomerId, out var cId)
                    ? cId
                    : throw new InvalidOperationException($"Customer id missing for SI#{row.SalesInvoiceNo}."),

                ProductId = context.ProductId.TryGetValue(row.OriginalProductId, out var pId)
                    ? pId
                    : throw new InvalidOperationException($"Product id missing for SI#{row.SalesInvoiceNo}.")
            };
        }

        public IEnumerable<AuditTrail> AuditTrails(
            SalesInvoiceUploadExcelFileViewModel row,
            string machineName)
        {
            var audits = new List<AuditTrail>();

            if (!string.IsNullOrWhiteSpace(row.CreatedBy))
            {
                audits.Add(new AuditTrail
                {
                    Username = row.CreatedBy,
                    Activity = $"Create new invoice# {row.SalesInvoiceNo}",
                    DocumentType = "Sales Invoice",
                    MachineName = machineName,
                    Date = row.CreatedDate
                });
            }

            if (!string.IsNullOrWhiteSpace(row.PostedBy) && row.PostedDate != default)
            {
                audits.Add(new AuditTrail
                {
                    Username = row.PostedBy,
                    Activity = $"Posted invoice# {row.SalesInvoiceNo}",
                    DocumentType = "Sales Invoice",
                    MachineName = machineName,
                    Date = row.PostedDate
                });
            }

            return audits;
        }

        public Dictionary<string, (string Original, string New)> Detect(
            SalesInvoice entity,
            SalesInvoiceUploadExcelFileViewModel row,
            IReadOnlyList<ImportExportLog> logs)
        {
            var changes = new Dictionary<string, (string, string)>();

            _generalRepo.Compare(changes, logs, "SalesInvoiceNo",
                StringHelper.NormalizeString(entity.SalesInvoiceNo),
                row.SalesInvoiceNo);

            _generalRepo.Compare(changes, logs, "OtherRefNo",
                StringHelper.NormalizeString(entity.OtherRefNo),
                row.OtherRefNo);

            _generalRepo.Compare(changes, logs, "Quantity",
                entity.Quantity.ToString(CS.Four_Decimal_Format),
                row.Quantity.ToString(CS.Four_Decimal_Format));

            _generalRepo.Compare(changes, logs, "UnitPrice",
                entity.UnitPrice.ToString(CS.Four_Decimal_Format),
                row.UnitPrice.ToString(CS.Four_Decimal_Format));

            _generalRepo.Compare(changes, logs, "Amount",
                entity.Amount.ToString(CS.Four_Decimal_Format),
                row.Amount.ToString(CS.Four_Decimal_Format));

            _generalRepo.Compare(changes, logs, "Remarks",
                StringHelper.NormalizeString(entity.Remarks),
                row.Remarks);

            _generalRepo.Compare(changes, logs, "Status",
                StringHelper.NormalizeString(entity.Status),
                row.Status);

            _generalRepo.Compare(changes, logs, "TransactionDate",
                entity.TransactionDate.ToString(CS.DateOnly_Format_For_Validation),
                row.TransactionDate.ToString(CS.DateOnly_Format_For_Validation));

            _generalRepo.Compare(changes, logs, "Discount",
                entity.Discount.ToString(CS.Four_Decimal_Format),
                row.Discount.ToString(CS.Four_Decimal_Format));

            _generalRepo.Compare(changes, logs, "DueDate",
                entity.DueDate.ToString(CS.DateOnly_Format_For_Validation),
                row.DueDate.ToString(CS.DateOnly_Format_For_Validation));

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
                entity.PostedDate?.ToString(CS.DateTime_Format_For_Validation) ?? string.Empty,
                row.PostedDate.ToString(CS.DateTime_Format_For_Validation));

            _generalRepo.Compare(changes, logs, "CancellationRemarks",
                StringHelper.NormalizeString(entity.CancellationRemarks),
                row.CancellationRemarks);

            _generalRepo.Compare(changes, logs, "OriginalCustomerId",
                StringHelper.NormalizeString(entity.OriginalCustomerId.ToString()),
                StringHelper.NormalizeString(row.OriginalCustomerId.ToString()));

            _generalRepo.Compare(changes, logs, "OriginalProductId",
                StringHelper.NormalizeString(entity.OriginalProductId.ToString()),
                StringHelper.NormalizeString(row.OriginalProductId.ToString()));

            _generalRepo.Compare(changes, logs, "OriginalSeriesNumber",
                StringHelper.NormalizeString(entity.OriginalSeriesNumber),
                StringHelper.NormalizeString(row.OriginalSeriesNumber));

            _generalRepo.Compare(changes, logs, "OriginalDocumentId",
                StringHelper.NormalizeString(entity.OriginalDocumentId.ToString()),
                StringHelper.NormalizeString(row.OriginalDocumentId.ToString()));

            return changes;
        }

        public async Task<FindSalesInvoiceInDbContextDto> BuildLookupSalesInvoiceContextForAasAsync(
            IEnumerable<SalesInvoiceUploadExcelFileViewModel> rows,
            CancellationToken cancellationToken)
        {
            var originalCustomerIds = rows.Select(r => r.OriginalCustomerId).Distinct().ToList();
            var originalProductIds = rows.Select(r => r.OriginalProductId).Distinct().ToList();
            var originalSeriesNumbers = rows.Select(r => r.OriginalSeriesNumber).Distinct().ToList();

            return new FindSalesInvoiceInDbContextDto
            {
                ExistingInvoices = await _aasDbContext.SalesInvoices
                    .Where(x => originalSeriesNumbers.Contains(x.OriginalSeriesNumber))
                    .GroupBy(x => x.OriginalSeriesNumber)
                    .Select(x => x.First())
                    .ToDictionaryAsync(x => x.OriginalSeriesNumber, cancellationToken),

                CustomerId = await _aasDbContext.Customers
                    .Where(x => originalCustomerIds.Contains(x.OriginalCustomerId))
                    .GroupBy(x => x.OriginalCustomerId)
                    .Select(x => x.First())
                    .ToDictionaryAsync(x => x.OriginalCustomerId, x => x.CustomerId, cancellationToken),

                ProductId = await _aasDbContext.Products
                    .Where(x => originalProductIds.Contains(x.OriginalProductId))
                    .GroupBy(x => x.OriginalProductId)
                    .Select(x => x.First())
                    .ToDictionaryAsync(x => x.OriginalProductId, x => x.ProductId, cancellationToken),

                ExistingLogs = await _dbContext.ImportExportLogs
                    .Where(x => originalSeriesNumbers.Contains(x.DocumentNo))
                    .ToListAsync(cancellationToken)
            };
        }
    }
}
