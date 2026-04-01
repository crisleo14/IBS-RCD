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
    public class DebitMemoRepo
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly GeneralRepo _generalRepo;

        public DebitMemoRepo(ApplicationDbContext dbContext, GeneralRepo generalRepo)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _generalRepo = generalRepo;
        }

        public async Task<List<DebitMemo>> GetDebitMemosAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.DebitMemos
                .Include(dm => dm.SalesInvoice)
                .ThenInclude(s => s!.Customer)
                .Include(dm => dm.SalesInvoice)
                .ThenInclude(s => s!.Product)
                .Include(dm => dm.ServiceInvoice)
                .ThenInclude(sv => sv!.Customer)
                .Include(dm => dm.ServiceInvoice)
                .ThenInclude(sv => sv!.Service)
                .ToListAsync(cancellationToken);
        }

        public async Task<string> GenerateDMNo(CancellationToken cancellationToken = default)
        {
            var debitMemo = await _dbContext
                .DebitMemos
                .OrderByDescending(s => s.DebitMemoNo)
                .FirstOrDefaultAsync(cancellationToken);

            if (debitMemo != null)
            {
                string lastSeries = debitMemo.DebitMemoNo ?? throw new InvalidOperationException("DMNo is null pls Contact MIS Enterprise");
                string numericPart = lastSeries.Substring(2);
                int incrementedNumber = int.Parse(numericPart) + 1;

                return lastSeries.Substring(0,2) + incrementedNumber.ToString("D10");
            }
            else
            {
                return $"DM{1.ToString("D10")}";
            }
        }

        public async Task<DebitMemo> FindDM(int id, CancellationToken cancellationToken = default)
        {
            var debitMemo = await _dbContext
                .DebitMemos
                .Include(s => s.SalesInvoice)
                .ThenInclude(s => s!.Customer)
                .Include(s => s.SalesInvoice)
                .ThenInclude(s => s!.Product)
                .Include(soa => soa.ServiceInvoice)
                .ThenInclude(soa => soa!.Customer)
                .Include(c => c.ServiceInvoice)
                .ThenInclude(soa => soa!.Service)
                .FirstOrDefaultAsync(debitMemo => debitMemo.DebitMemoId == id, cancellationToken);

            if (debitMemo != null)
            {
                return debitMemo;
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

        public async Task LogChangesAsync(int id, Dictionary<string, (string OriginalValue, string NewValue)> changes, string? modifiedBy, string seriesNumber, string databaseName)
        {
            foreach (var change in changes)
            {
                var logReport = new ImportExportLog()
                {
                    Id = Guid.NewGuid(),
                    TableName = nameof(DynamicView.DebitMemo),
                    DocumentRecordId = id,
                    ColumnName = change.Key,
                    Module = "Debit Memo",
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

        public IReadOnlyList<DebitMemoUploadExcelFileViewModel> ParseWorksheet(
            ExcelWorksheet worksheet)
        {
            var rows = new List<DebitMemoUploadExcelFileViewModel>();
            var rowCount = worksheet.Dimension.Rows;

            for (var row = 2; row <= rowCount; row++)
            {
                rows.Add(new DebitMemoUploadExcelFileViewModel
                {
                    DebitMemoNo = StringHelper.NormalizeString(worksheet.Cells[row, 17].GetValue<string>()),
                    TransactionDate = DateOnly.FromDateTime(worksheet.Cells[row, 1].GetValue<DateTime>()),
                    DebitAmount = worksheet.Cells[row, 2].GetValue<decimal>(),
                    Description = StringHelper.NormalizeString(worksheet.Cells[row, 3].GetValue<string>()),
                    AdjustedPrice = worksheet.Cells[row, 4].GetValue<decimal>(),
                    Quantity = worksheet.Cells[row, 5].GetValue<decimal>(),
                    Source = StringHelper.NormalizeString(worksheet.Cells[row, 6].GetValue<string>()),
                    Remarks = StringHelper.NormalizeString(worksheet.Cells[row, 7].GetValue<string>()),
                    Period = DateOnly.FromDateTime(worksheet.Cells[row, 8].GetValue<DateTime>()),
                    Amount = worksheet.Cells[row, 9].GetValue<decimal>(),
                    CurrentAndPreviousAmount = worksheet.Cells[row, 10].GetValue<decimal>(),
                    UnearnedAmount = worksheet.Cells[row, 11].GetValue<decimal>(),
                    ServicesId = worksheet.Cells[row, 12].GetValue<int>(),
                    CreatedBy = StringHelper.NormalizeString(worksheet.Cells[row, 13].GetValue<string>()),
                    CreatedDate = worksheet.Cells[row, 14].GetValue<DateTime>(),
                    PostedBy = StringHelper.NormalizeString(worksheet.Cells[row, 20].GetValue<string>()),
                    PostedDate = worksheet.Cells[row, 21].GetValue<DateTime>(),
                    CancellationRemarks = StringHelper.NormalizeString(worksheet.Cells[row, 15].GetValue<string>()),
                    OriginalSalesInvoiceId = worksheet.Cells[row, 16].GetValue<int>(),
                    OriginalSeriesNumber = StringHelper.NormalizeString(worksheet.Cells[row, 17].GetValue<string>()),
                    OriginalServiceInvoiceId = worksheet.Cells[row, 18].GetValue<int>(),
                    OriginalDocumentId = worksheet.Cells[row, 19].GetValue<int>(),
                });
            }

            return rows;
        }

        public async Task<FindDebitMemoInDbContextDto> BuildLookupDebitMemoContextAsync(
            IEnumerable<DebitMemoUploadExcelFileViewModel> rows,
            CancellationToken cancellationToken)
        {
            var originalSalesInvoiceIds = rows.Select(r => r.OriginalSalesInvoiceId).Distinct().ToList();
            var originalServiceInvoicess = rows.Select(r => r.OriginalServiceInvoiceId).Distinct().ToList();
            var originalSeriesNumbers = rows.Select(r => r.OriginalSeriesNumber).Distinct().ToList();

            return new FindDebitMemoInDbContextDto
            {
                ExistingDebitMemo = await _dbContext.DebitMemos
                    .Where(x => originalSeriesNumbers.Contains(x.OriginalSeriesNumber))
                    .GroupBy(x => x.OriginalSeriesNumber)
                    .Select(x => x.First())
                    .ToDictionaryAsync(x => x.OriginalSeriesNumber, cancellationToken),

                SalesInvoiceId = await _dbContext.SalesInvoices
                    .Where(x => originalSalesInvoiceIds.Contains(x.OriginalDocumentId))
                    .GroupBy(x => x.OriginalDocumentId)
                    .Select(x => x.First())
                    .ToDictionaryAsync(x => x.OriginalDocumentId, x => x.SalesInvoiceId, cancellationToken),

                ServiceInvoiceId = await _dbContext.ServiceInvoices
                    .Where(x => originalServiceInvoicess.Contains(x.OriginalDocumentId))
                    .GroupBy(x => x.OriginalDocumentId)
                    .Select(x => x.First())
                    .ToDictionaryAsync(x => x.OriginalDocumentId, x => x.ServiceInvoiceId, cancellationToken),

                ExistingLogs = await _dbContext.ImportExportLogs
                    .Where(x => originalSeriesNumbers.Contains(x.DocumentNo))
                    .ToListAsync(cancellationToken)
            };
        }

        public DebitMemo MapToDebitMemoEntity(
            DebitMemoUploadExcelFileViewModel row,
            FindDebitMemoInDbContextDto context)
        {
            return new DebitMemo
            {
                DebitMemoNo = row.DebitMemoNo,
                TransactionDate = row.TransactionDate,
                DebitAmount = row.DebitAmount,
                Description = row.Description,
                AdjustedPrice = row.AdjustedPrice,
                Quantity = row.Quantity,
                Source = row.Source,
                Remarks = row.Remarks,
                Period = row.Period,
                Amount = row.Amount,
                CurrentAndPreviousAmount = row.CurrentAndPreviousAmount,
                UnearnedAmount = row.UnearnedAmount,
                ServicesId = row.ServicesId,
                CreatedBy = row.CreatedBy,
                CreatedDate = row.CreatedDate,
                PostedBy = row.PostedBy,
                PostedDate = row.PostedDate,
                CancellationRemarks = row.CancellationRemarks,
                OriginalSalesInvoiceId = row.OriginalSalesInvoiceId,
                OriginalSeriesNumber = row.OriginalSeriesNumber,
                OriginalServiceInvoiceId = row.OriginalServiceInvoiceId,
                OriginalDocumentId = row.OriginalDocumentId,

                SalesInvoiceId = context.SalesInvoiceId.TryGetValue(row.OriginalSalesInvoiceId, out var cId)
                    ? cId
                    : null,

                ServiceInvoiceId = context.ServiceInvoiceId.TryGetValue(row.OriginalServiceInvoiceId, out var pId)
                    ? pId
                    : null
            };
        }

        public IEnumerable<AuditTrail> AuditTrails(
            DebitMemoUploadExcelFileViewModel row,
            string machineName)
        {
            var audits = new List<AuditTrail>();

            if (!string.IsNullOrWhiteSpace(row.CreatedBy))
            {
                audits.Add(new AuditTrail
                {
                    Username = row.CreatedBy,
                    Activity = $"Create new debit memo# {row.DebitMemoNo}",
                    DocumentType = "Debit Memo",
                    MachineName = machineName,
                    Date = row.CreatedDate
                });
            }

            if (!string.IsNullOrWhiteSpace(row.PostedBy) && row.PostedDate != default)
            {
                audits.Add(new AuditTrail
                {
                    Username = row.PostedBy,
                    Activity = $"Posted debit memo# {row.DebitMemoNo}",
                    DocumentType = "Debit Memo",
                    MachineName = machineName,
                    Date = row.PostedDate
                });
            }

            return audits;
        }

        public Dictionary<string, (string Original, string New)> Detect(
            DebitMemo entity,
            DebitMemoUploadExcelFileViewModel row,
            IReadOnlyList<ImportExportLog> logs)
        {
            var changes = new Dictionary<string, (string, string)>();

            _generalRepo.Compare(changes, logs, "DebitMemoNo",
                StringHelper.NormalizeString(entity.DebitMemoNo),
                row.DebitMemoNo);

            _generalRepo.Compare(changes, logs, "TransactionDate",
                entity.TransactionDate.ToString(CS.DateOnly_Format_For_Validation),
                row.TransactionDate.ToString(CS.DateOnly_Format_For_Validation));

            _generalRepo.Compare(changes, logs, "DebitAmount",
                entity.DebitAmount.ToString(CS.Four_Decimal_Format),
                row.DebitAmount.ToString(CS.Four_Decimal_Format));

            _generalRepo.Compare(changes, logs, "Description",
                StringHelper.NormalizeString(entity.Description),
                row.Description);

            _generalRepo.Compare(changes, logs, "AdjustedPrice",
                entity.AdjustedPrice?.ToString(CS.Four_Decimal_Format) ?? "0.0000",
                row.AdjustedPrice.ToString(CS.Four_Decimal_Format));

            _generalRepo.Compare(changes, logs, "Quantity",
                entity.Quantity?.ToString(CS.Four_Decimal_Format) ?? "0.0000",
                row.Quantity.ToString(CS.Four_Decimal_Format));

            _generalRepo.Compare(changes, logs, "Source",
                StringHelper.NormalizeString(entity.Source),
                row.Source);

            _generalRepo.Compare(changes, logs, "Remarks",
                StringHelper.NormalizeString(entity.Remarks),
                row.Remarks);

            _generalRepo.Compare(changes, logs, "Period",
                entity.Period.ToString(CS.DateOnly_Format_For_Validation),
                row.Period.ToString(CS.DateOnly_Format_For_Validation));

            _generalRepo.Compare(changes, logs, "Amount",
                entity.Amount?.ToString(CS.Four_Decimal_Format) ?? "0.0000",
                row.Amount.ToString(CS.Four_Decimal_Format));

            _generalRepo.Compare(changes, logs, "CurrentAndPreviousAmount",
                entity.CurrentAndPreviousAmount.ToString(CS.Four_Decimal_Format),
                row.CurrentAndPreviousAmount.ToString(CS.Four_Decimal_Format));

            _generalRepo.Compare(changes, logs, "UnearnedAmount",
                entity.UnearnedAmount.ToString(CS.Four_Decimal_Format),
                row.UnearnedAmount.ToString(CS.Four_Decimal_Format));

            _generalRepo.Compare(changes, logs, "ServicesId",
                StringHelper.NormalizeString(entity.ServicesId.ToString()),
                StringHelper.NormalizeString(row.ServicesId.ToString()));

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

            _generalRepo.Compare(changes, logs, "OriginalSalesInvoiceId",
                StringHelper.NormalizeString(entity.OriginalSalesInvoiceId.ToString()),
                StringHelper.NormalizeString(row.OriginalSalesInvoiceId.ToString()));

            _generalRepo.Compare(changes, logs, "OriginalSeriesNumber",
                StringHelper.NormalizeString(entity.OriginalSeriesNumber),
                StringHelper.NormalizeString(row.OriginalSeriesNumber));

            _generalRepo.Compare(changes, logs, "OriginalServiceInvoiceId",
                StringHelper.NormalizeString(entity.OriginalServiceInvoiceId.ToString()),
                StringHelper.NormalizeString(row.OriginalServiceInvoiceId.ToString()));

            _generalRepo.Compare(changes, logs, "OriginalDocumentId",
                StringHelper.NormalizeString(entity.OriginalDocumentId.ToString()),
                StringHelper.NormalizeString(row.OriginalDocumentId.ToString()));

            return changes;
        }
    }
}
