using Accounting_System.Data;
using Accounting_System.DTOs;
using Accounting_System.Models;
using Accounting_System.Models.AccountsReceivable;
using Accounting_System.Models.Reports;
using Accounting_System.Models.ViewModels;
using Accounting_System.Utility;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace Accounting_System.Repository
{
    public class ServiceInvoiceRepo
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly GeneralRepo _generalRepo;
        private readonly AasDbContext _aasDbContext;

        public ServiceInvoiceRepo(ApplicationDbContext dbContext, GeneralRepo generalRepo, AasDbContext aasDbContext)
        {
            _dbContext = dbContext;
            _generalRepo = generalRepo;
            _aasDbContext = aasDbContext;
        }

        public async Task<List<ServiceInvoice>> GetServiceInvoicesAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext
                .ServiceInvoices
                .Include(sv => sv.Customer)
                .Include(sv => sv.Service)
                .ToListAsync(cancellationToken);
        }

        public async Task<string> GenerateSvNo(CancellationToken cancellationToken = default)
        {
            var serviceInvoice = await _dbContext
                .ServiceInvoices
                .OrderByDescending(s => s.ServiceInvoiceNo)
                .FirstOrDefaultAsync(cancellationToken);

            if (serviceInvoice != null)
            {
                string lastSeries = serviceInvoice.ServiceInvoiceNo!;
                string numericPart = lastSeries.Substring(2);
                int incrementedNumber = int.Parse(numericPart) + 1;

                return lastSeries.Substring(0,2) + incrementedNumber.ToString("D10");
            }
            else
            {
                return $"SV{1.ToString("D10")}";
            }
        }

        public async Task<ServiceInvoice> FindSv(int id, CancellationToken cancellationToken = default)
        {
            var serviceInvoice = await _dbContext
                .ServiceInvoices
                .Include(s => s.Customer)
                .Include(s => s.Service)
                .FirstOrDefaultAsync(s => s.ServiceInvoiceId == id, cancellationToken);

            if (serviceInvoice != null)
            {
                return serviceInvoice;
            }
            else
            {
                throw new ArgumentException("Invalid id value. The id must be greater than 0.");
            }
        }

        public async Task<Services> GetServicesAsync(int? id, CancellationToken cancellationToken = default)
        {
            var services = await _dbContext
                .Services
                .FirstOrDefaultAsync(s => s.ServiceId == id, cancellationToken);

            if (services != null)
            {
                return services;
            }
            else
            {
                throw new ArgumentException("Invalid id value. The id must be greater than 0.");
            }
        }

        public async Task<Customer> FindCustomerAsync(int? id, CancellationToken cancellationToken = default)
        {
            var customer = await _dbContext
                .Customers
                .FirstOrDefaultAsync(s => s.CustomerId == id, cancellationToken);

            if (customer != null)
            {
                return customer;
            }
            else
            {
                throw new ArgumentException("Invalid id value. The id must be greater than 0.");
            }
        }

        public async Task LogChangesAsync(int id, Dictionary<string, (string OriginalValue, string NewValue)> changes, string? modifiedBy, string seriesNumber, string databaseName)
        {
            var logReport = new List<ImportExportLog>();

            foreach (var change in changes)
            {
                logReport.Add(
                    new ImportExportLog
                    {
                    Id = Guid.NewGuid(),
                    TableName = nameof(DynamicView.ServiceInvoice),
                    DocumentRecordId = id,
                    ColumnName = change.Key,
                    Module = "Service Invoice",
                    OriginalValue = change.Value.OriginalValue,
                    AdjustedValue = change.Value.NewValue,
                    TimeStamp = DateTime.Now,
                    UploadedBy = modifiedBy,
                    Action = string.Empty,
                    Executed = false,
                    DocumentNo = seriesNumber,
                    DatabaseName = databaseName
                });
            }
            await _dbContext.AddRangeAsync(logReport);
        }

        public IReadOnlyList<ServiceInvoiceUploadExcelFileViewModel> ParseWorksheet(
            ExcelWorksheet worksheet)
        {
            var rows = new List<ServiceInvoiceUploadExcelFileViewModel>();
            var rowCount = worksheet.Dimension.Rows;

            for (var row = 2; row <= rowCount; row++)
            {
                rows.Add(new ServiceInvoiceUploadExcelFileViewModel
                {
                    ServiceInvoiceNo = StringHelper.NormalizeString(worksheet.Cells[row, 17].GetValue<string>()),
                    DueDate = DateOnly.FromDateTime(worksheet.Cells[row, 1].GetValue<DateTime>()),
                    Period = DateOnly.FromDateTime(worksheet.Cells[row, 2].GetValue<DateTime>()),
                    Amount = worksheet.Cells[row, 3].GetValue<decimal>(),
                    Total = worksheet.Cells[row, 4].GetValue<decimal>(),
                    Discount = worksheet.Cells[row, 5].GetValue<decimal>(),
                    CurrentAndPreviousAmount = worksheet.Cells[row, 6].GetValue<decimal>(),
                    UnearnedAmount = worksheet.Cells[row, 7].GetValue<decimal>(),
                    Status = StringHelper.NormalizeString(worksheet.Cells[row, 8].GetValue<string>()),
                    Instructions = StringHelper.NormalizeString(worksheet.Cells[row, 11].GetValue<string>()),
                    CreatedBy = string.IsNullOrWhiteSpace(worksheet.Cells[row, 13].Text)
                        ? string.Empty
                        : StringHelper.NormalizeString(worksheet.Cells[row, 13].GetValue<string>()),
                    CreatedDate = worksheet.Cells[row, 14].GetValue<DateTime>(),
                    PostedBy = string.IsNullOrWhiteSpace(worksheet.Cells[row, 20].Text)
                        ? string.Empty
                        : StringHelper.NormalizeString(worksheet.Cells[row, 20].GetValue<string>()),
                    PostedDate = worksheet.Cells[row, 21].GetValue<DateTime>(),
                    CancellationRemarks = string.IsNullOrWhiteSpace(worksheet.Cells[row, 20].Text)
                        ? string.Empty
                        : StringHelper.NormalizeString(worksheet.Cells[row, 15].GetValue<string>()),
                    OriginalCustomerId = worksheet.Cells[row, 16].GetValue<int>(),
                    OriginalSeriesNumber = StringHelper.NormalizeString(worksheet.Cells[row, 17].GetValue<string>()),
                    OriginalServicesId = worksheet.Cells[row, 18].GetValue<int>(),
                    OriginalDocumentId = worksheet.Cells[row, 19].GetValue<int>(),
                    CanceledBy = string.IsNullOrWhiteSpace(worksheet.Cells[row, 24].Text)
                        ? string.Empty
                        : StringHelper.NormalizeString(worksheet.Cells[row, 24].GetValue<string>()),
                    CanceledDate = worksheet.Cells[row, 25].GetValue<DateTime>(),
                    VoidedBy = string.IsNullOrWhiteSpace(worksheet.Cells[row, 26].Text)
                        ? string.Empty
                        : StringHelper.NormalizeString(worksheet.Cells[row, 26].GetValue<string>()),
                    VoidedDate = worksheet.Cells[row, 27].GetValue<DateTime>(),
                    EditedBy = string.IsNullOrWhiteSpace(worksheet.Cells[row, 22].Text)
                        ? string.Empty
                        : StringHelper.NormalizeString(worksheet.Cells[row, 22].GetValue<string>()),
                    EditedDate = worksheet.Cells[row, 23].GetValue<DateTime>()
                });
            }

            return rows;
        }

        public async Task<FindServiceInvoiceInDbContextDto> BuildLookupServiceInvoiceContextAsync(
            IEnumerable<ServiceInvoiceUploadExcelFileViewModel> rows,
            CancellationToken cancellationToken)
        {
            var originalCustomerIds = rows.Select(r => r.OriginalCustomerId).Distinct().ToList();
            var originalServicesIds = rows.Select(r => r.OriginalServicesId).Distinct().ToList();
            var originalSeriesNumbers = rows.Select(r => r.OriginalSeriesNumber).Distinct().ToList();

            return new FindServiceInvoiceInDbContextDto
            {
                ExistingInvoices = await _dbContext.ServiceInvoices
                    .Where(x => originalSeriesNumbers.Contains(x.OriginalSeriesNumber))
                    .GroupBy(x => x.OriginalSeriesNumber)
                    .Select(x => x.First())
                    .ToDictionaryAsync(x => x.OriginalSeriesNumber, cancellationToken),

                CustomerId = await _dbContext.Customers
                    .Where(x => originalCustomerIds.Contains(x.OriginalCustomerId))
                    .GroupBy(x => x.OriginalCustomerId)
                    .Select(x => x.First())
                    .ToDictionaryAsync(x => x.OriginalCustomerId, x => x.CustomerId, cancellationToken),

                ServicesId = await _dbContext.Services
                    .Where(x => x.OriginalServiceId.HasValue && originalServicesIds.Contains(x.OriginalServiceId.Value))
                    .GroupBy(x => x.OriginalServiceId!.Value)
                    .Select(x => x.First())
                    .ToDictionaryAsync(x => x.OriginalServiceId!.Value, x => x.ServiceId, cancellationToken),

                ExistingLogs = await _dbContext.ImportExportLogs
                    .Where(x => originalSeriesNumbers.Contains(x.DocumentNo))
                    .ToListAsync(cancellationToken)
            };
        }

        public ServiceInvoice MapToServiceInvoiceEntity(
            ServiceInvoiceUploadExcelFileViewModel row,
            FindServiceInvoiceInDbContextDto context)
        {
            return new ServiceInvoice
            {
                ServiceInvoiceNo = row.ServiceInvoiceNo,
                DueDate = row.DueDate,
                Period = row.Period,
                Amount = row.Amount,
                Total = row.Total,
                Discount = row.Discount,
                CurrentAndPreviousAmount = row.CurrentAndPreviousAmount,
                UnearnedAmount = row.UnearnedAmount,
                Status = row.Status,
                Instructions = row.Instructions,
                CreatedBy = row.CreatedBy,
                CreatedDate = row.CreatedDate,
                PostedBy = row.PostedBy,
                PostedDate = row.PostedDate,
                CancellationRemarks = row.CancellationRemarks,
                CanceledBy = row.CanceledBy,
                CanceledDate = row.CanceledDate,
                VoidedBy = row.VoidedBy,
                VoidedDate = row.VoidedDate,
                EditedBy = row.EditedBy,
                EditedDate = row.EditedDate,
                OriginalCustomerId = row.OriginalCustomerId,
                OriginalSeriesNumber = row.OriginalSeriesNumber,
                OriginalServicesId = row.OriginalServicesId,
                OriginalDocumentId = row.OriginalDocumentId,

                CustomerId = context.CustomerId.TryGetValue(row.OriginalCustomerId, out var cId)
                    ? cId
                    : throw new InvalidOperationException($"Customer id missing for SV#{row.ServiceInvoiceNo}."),

                ServicesId = context.ServicesId.TryGetValue(row.OriginalServicesId, out var pId)
                    ? pId
                    : throw new InvalidOperationException($"Service id missing for SV#{row.ServiceInvoiceNo}.")
            };
        }

        public IEnumerable<AuditTrail> AuditTrails(
            ServiceInvoiceUploadExcelFileViewModel row,
            string machineName)
        {
            var audits = new List<AuditTrail>();

            if (!string.IsNullOrWhiteSpace(row.CreatedBy))
            {
                audits.Add(new AuditTrail
                {
                    Username = row.CreatedBy,
                    Activity = $"Create new service invoice# {row.ServiceInvoiceNo}",
                    DocumentType = "Service Invoice",
                    MachineName = machineName,
                    Date = row.CreatedDate
                });
            }

            if (!string.IsNullOrWhiteSpace(row.PostedBy) && row.PostedDate != default)
            {
                audits.Add(new AuditTrail
                {
                    Username = row.PostedBy,
                    Activity = $"Posted service invoice# {row.ServiceInvoiceNo}",
                    DocumentType = "Service Invoice",
                    MachineName = machineName,
                    Date = row.PostedDate
                });
            }

            if (!string.IsNullOrWhiteSpace(row.CanceledBy) && row.CanceledDate != default)
            {
                audits.Add(new AuditTrail
                {
                    Username = row.CanceledBy,
                    Activity = $"Cancelled service invoice# {row.ServiceInvoiceNo}",
                    DocumentType = "Service Invoice",
                    MachineName = machineName,
                    Date = row.CanceledDate
                });
            }

            if (!string.IsNullOrWhiteSpace(row.VoidedBy) && row.VoidedDate != default)
            {
                audits.Add(new AuditTrail
                {
                    Username = row.VoidedBy,
                    Activity = $"Voided service invoice# {row.ServiceInvoiceNo}",
                    DocumentType = "Service Invoice",
                    MachineName = machineName,
                    Date = row.VoidedDate
                });
            }

            if (!string.IsNullOrWhiteSpace(row.EditedBy) && row.EditedDate != default)
            {
                audits.Add(new AuditTrail
                {
                    Username = row.EditedBy,
                    Activity = $"Edited service invoice# {row.ServiceInvoiceNo}",
                    DocumentType = "Service Invoice",
                    MachineName = machineName,
                    Date = row.EditedDate
                });
            }

            return audits;
        }

        public Dictionary<string, (string Original, string New)> Detect(
            ServiceInvoice entity,
            ServiceInvoiceUploadExcelFileViewModel row,
            IReadOnlyList<ImportExportLog> logs)
        {
            var changes = new Dictionary<string, (string, string)>();

            _generalRepo.Compare(changes, logs, "ServiceInvoiceNo",
                StringHelper.NormalizeString(entity.ServiceInvoiceNo),
                row.ServiceInvoiceNo);

            _generalRepo.Compare(changes, logs, "DueDate",
                entity.DueDate.ToString(CS.DateOnly_Format_For_Validation),
                row.DueDate.ToString(CS.DateOnly_Format_For_Validation));

            _generalRepo.Compare(changes, logs, "Period",
                entity.Period.ToString(CS.DateOnly_Format_For_Validation),
                row.Period.ToString(CS.DateOnly_Format_For_Validation));

            _generalRepo.Compare(changes, logs, "Amount",
                entity.Amount.ToString(CS.Four_Decimal_Format),
                row.Amount.ToString(CS.Four_Decimal_Format));

            _generalRepo.Compare(changes, logs, "Total",
                entity.Total.ToString(CS.Four_Decimal_Format),
                row.Total.ToString(CS.Four_Decimal_Format));

            _generalRepo.Compare(changes, logs, "Discount",
                entity.Discount.ToString(CS.Four_Decimal_Format),
                row.Discount.ToString(CS.Four_Decimal_Format));

            _generalRepo.Compare(changes, logs, "CurrentAndPreviousAmount",
                entity.CurrentAndPreviousAmount.ToString(CS.Four_Decimal_Format),
                row.CurrentAndPreviousAmount.ToString(CS.Four_Decimal_Format));

            _generalRepo.Compare(changes, logs, "UnearnedAmount",
                entity.UnearnedAmount.ToString(CS.Four_Decimal_Format),
                row.UnearnedAmount.ToString(CS.Four_Decimal_Format));

            _generalRepo.Compare(changes, logs, "Status",
                StringHelper.NormalizeString(entity.Status),
                row.Status);

            _generalRepo.Compare(changes, logs, "Instructions",
                StringHelper.NormalizeString(entity.Instructions),
                row.Instructions);

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
                StringHelper.NormalizeString(entity.PostedDate?.ToString(CS.DateTime_Format_For_Validation)),
                row.PostedDate.ToString(CS.DateTime_Format_For_Validation));

            _generalRepo.Compare(changes, logs, "CancellationRemarks",
                StringHelper.NormalizeString(entity.CancellationRemarks),
                row.CancellationRemarks);

            _generalRepo.Compare(changes, logs, "OriginalCustomerId",
                StringHelper.NormalizeString(entity.OriginalCustomerId.ToString()),
                StringHelper.NormalizeString(row.OriginalCustomerId.ToString()));

            _generalRepo.Compare(changes, logs, "OriginalSeriesNumber",
                StringHelper.NormalizeString(entity.OriginalSeriesNumber),
                row.OriginalSeriesNumber);

            _generalRepo.Compare(changes, logs, "OriginalServicesId",
                StringHelper.NormalizeString(entity.OriginalServicesId.ToString()),
                StringHelper.NormalizeString(row.OriginalServicesId.ToString()));

            _generalRepo.Compare(changes, logs, "OriginalDocumentId",
                StringHelper.NormalizeString(entity.OriginalDocumentId.ToString()),
                StringHelper.NormalizeString(row.OriginalDocumentId.ToString()));

            return changes;
        }

        public async Task<FindServiceInvoiceInDbContextDto> BuildLookupServiceInvoiceContextForAasAsync(
            IEnumerable<ServiceInvoiceUploadExcelFileViewModel> rows,
            CancellationToken cancellationToken)
        {
            var originalCustomerIds = rows.Select(r => r.OriginalCustomerId).Distinct().ToList();
            var originalServicesIds = rows.Select(r => r.OriginalServicesId).Distinct().ToList();
            var originalSeriesNumbers = rows.Select(r => r.OriginalSeriesNumber).Distinct().ToList();

            return new FindServiceInvoiceInDbContextDto
            {
                ExistingInvoices = await _aasDbContext.ServiceInvoices
                    .Where(x => originalSeriesNumbers.Contains(x.OriginalSeriesNumber))
                    .GroupBy(x => x.OriginalSeriesNumber)
                    .Select(x => x.First())
                    .ToDictionaryAsync(x => x.OriginalSeriesNumber, cancellationToken),

                CustomerId = await _aasDbContext.Customers
                    .Where(x => originalCustomerIds.Contains(x.OriginalCustomerId))
                    .GroupBy(x => x.OriginalCustomerId)
                    .Select(x => x.First())
                    .ToDictionaryAsync(x => x.OriginalCustomerId, x => x.CustomerId, cancellationToken),

                ServicesId = await _aasDbContext.Services
                    .Where(x => x.OriginalServiceId.HasValue && originalServicesIds.Contains(x.OriginalServiceId.Value))
                    .GroupBy(x => x.OriginalServiceId!.Value)
                    .Select(x => x.First())
                    .ToDictionaryAsync(x => x.OriginalServiceId!.Value, x => x.ServiceId, cancellationToken),

                ExistingLogs = await _dbContext.ImportExportLogs
                    .Where(x => originalSeriesNumbers.Contains(x.DocumentNo))
                    .ToListAsync(cancellationToken)
            };
        }
    }
}
