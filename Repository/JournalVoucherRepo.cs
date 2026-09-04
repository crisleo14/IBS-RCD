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
    public class JournalVoucherRepo
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly GeneralRepo _generalRepo;

        private readonly AasDbContext _aasDbContext;

        public JournalVoucherRepo(ApplicationDbContext dbContext, GeneralRepo generalRepo, AasDbContext aasDbContext)
        {
            _dbContext = dbContext;
            _generalRepo = generalRepo;
            _aasDbContext = aasDbContext;
        }

        public async Task<List<JournalVoucherHeader>> GetJournalVouchersAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.JournalVoucherHeaders
                .Include(j => j.CheckVoucherHeader)
                .ThenInclude(cv => cv!.Supplier)
                .ToListAsync(cancellationToken);
        }

        public async Task<string> GenerateJVNo(CancellationToken cancellationToken = default)
        {
            var journalVoucher = await _dbContext
                .JournalVoucherHeaders
                .OrderByDescending(j => j.JournalVoucherHeaderNo)
                .FirstOrDefaultAsync(cancellationToken);

            if (journalVoucher != null)
            {
                string lastSeries = journalVoucher.JournalVoucherHeaderNo ?? throw new InvalidOperationException("JVNo is null pls Contact MIS Enterprise");
                string numericPart = lastSeries.Substring(2);
                int incrementedNumber = int.Parse(numericPart) + 1;

                return lastSeries.Substring(0,2) + incrementedNumber.ToString("D10");
            }
            else
            {
                return $"JV{1.ToString("D10")}";
            }
        }

        public async Task LogChangesAsync(int id, Dictionary<string, (string OriginalValue, string NewValue)> changes, string? modifiedBy, string seriesNumber, string databaseName)
        {
            foreach (var change in changes)
            {
                var logReport = new ImportExportLog()
                {
                    Id = Guid.NewGuid(),
                    TableName = "JournalVoucherHeader",
                    DocumentRecordId = id,
                    ColumnName = change.Key,
                    Module = "Journal Voucher Header",
                    OriginalValue = change.Value.OriginalValue,
                    AdjustedValue = change.Value.NewValue,
                    TimeStamp = DateTime.Now,
                    UploadedBy = modifiedBy,
                    Action = string.Empty,
                    Executed = false,
                    DatabaseName = databaseName,
                    DocumentNo = seriesNumber
                };
                await _dbContext.AddAsync(logReport);
            }
        }

        public async Task LogChangesForJVDAsync(int? id, Dictionary<string, (string OriginalValue, string NewValue)> changes, string? modifiedBy, string seriesNumber, string databaseName)
        {
            foreach (var change in changes)
            {
                var logReport = new ImportExportLog()
                {
                    Id = Guid.NewGuid(),
                    TableName = "JournalVoucherDetails",
                    DocumentRecordId = id!.Value,
                    ColumnName = change.Key,
                    Module = "Journal Voucher Details",
                    OriginalValue = change.Value.OriginalValue,
                    AdjustedValue = change.Value.NewValue,
                    TimeStamp = DateTime.Now,
                    UploadedBy = modifiedBy,
                    Action = string.Empty,
                    Executed = false,
                    DatabaseName = databaseName,
                    DocumentNo = seriesNumber
                };
                await _dbContext.AddAsync(logReport);
            }
        }

        public IReadOnlyList<JournalVoucherUploadExcelFileViewModel> ParseWorksheet(
            ExcelWorksheet worksheet)
        {
            var rows = new List<JournalVoucherUploadExcelFileViewModel>();
            var rowCount = worksheet.Dimension.Rows;

            for (var row = 2; row <= rowCount; row++)
            {
                rows.Add(new JournalVoucherUploadExcelFileViewModel
                {
                    JournalVoucherHeaderNo = StringHelper.NormalizeString(worksheet.Cells[row, 10].GetValue<string>()),
                    Date = DateOnly.FromDateTime(worksheet.Cells[row, 1].GetValue<DateTime>()),
                    References = StringHelper.NormalizeString(worksheet.Cells[row, 2].GetValue<string>()),
                    Particulars = StringHelper.NormalizeString(worksheet.Cells[row, 3].GetValue<string>()),
                    CRNo = StringHelper.NormalizeString(worksheet.Cells[row, 4].GetValue<string>()),
                    JVReason = StringHelper.NormalizeString(worksheet.Cells[row, 5].GetValue<string>()),
                    CreatedBy = StringHelper.NormalizeString(worksheet.Cells[row, 6].GetValue<string>()),
                    CreatedDate = worksheet.Cells[row, 7].GetValue<DateTime>(),
                    PostedBy = StringHelper.NormalizeString(worksheet.Cells[row, 12].GetValue<string>()),
                    PostedDate = worksheet.Cells[row, 13].GetValue<DateTime>(),
                    CancellationRemarks = StringHelper.NormalizeString(worksheet.Cells[row, 8].GetValue<string>()),
                    OriginalCVId = worksheet.Cells[row, 9].GetValue<int>(),
                    OriginalSeriesNumber = StringHelper.NormalizeString(worksheet.Cells[row, 10].GetValue<string>()),
                    OriginalDocumentId = worksheet.Cells[row, 11].GetValue<int>(),
                    CanceledBy = string.IsNullOrWhiteSpace(worksheet.Cells[row, 16].Text)
                        ? string.Empty
                        : StringHelper.NormalizeString(worksheet.Cells[row, 16].GetValue<string>()),
                    CanceledDate = worksheet.Cells[row, 17].GetValue<DateTime>(),
                    VoidedBy = string.IsNullOrWhiteSpace(worksheet.Cells[row, 18].Text)
                        ? string.Empty
                        : StringHelper.NormalizeString(worksheet.Cells[row, 18].GetValue<string>()),
                    VoidedDate = worksheet.Cells[row, 19].GetValue<DateTime>(),
                    EditedBy = string.IsNullOrWhiteSpace(worksheet.Cells[row, 14].Text)
                        ? string.Empty
                        : StringHelper.NormalizeString(worksheet.Cells[row, 14].GetValue<string>()),
                    EditedDate = worksheet.Cells[row, 15].GetValue<DateTime>()
                });
            }

            return rows;
        }

        public async Task<FindJournalVoucherInDbContextDto> BuildLookupCheckVoucherHeaderContextAsync(
            IEnumerable<JournalVoucherUploadExcelFileViewModel> rows,
            CancellationToken cancellationToken)
        {
            var originalCheckVoucherIds = rows.Select(r => r.OriginalCVId).Distinct().ToList();
            var originalSeriesNumbers = rows.Select(r => r.OriginalSeriesNumber).Distinct().ToList();

            return new FindJournalVoucherInDbContextDto
            {
                ExistingJournalVoucherHeader = await _dbContext.JournalVoucherHeaders
                    .Where(x => originalSeriesNumbers.Contains(x.OriginalSeriesNumber!))
                    .GroupBy(x => x.OriginalSeriesNumber)
                    .Select(x => x.First())
                    .ToDictionaryAsync(x => x.OriginalSeriesNumber!, cancellationToken),

                CvId = await _dbContext.CheckVoucherHeaders
                    .Where(x => originalCheckVoucherIds.Contains(x.OriginalDocumentId))
                    .GroupBy(x => x.OriginalDocumentId)
                    .Select(x => x.First())
                    .ToDictionaryAsync(x => x.OriginalDocumentId, x => x.CheckVoucherHeaderId, cancellationToken),

                ExistingLogs = await _dbContext.ImportExportLogs
                    .Where(x => originalSeriesNumbers.Contains(x.DocumentNo))
                    .ToListAsync(cancellationToken)
            };
        }

        public async Task<FindJournalVoucherInDbContextDto> BuildLookupJournalVoucherHeaderContextForAasAsync(
            IEnumerable<JournalVoucherUploadExcelFileViewModel> rows,
            CancellationToken cancellationToken)
        {
            var originalCheckVoucherIds = rows.Select(r => r.OriginalCVId).Distinct().ToList();
            var originalSeriesNumbers = rows.Select(r => r.OriginalSeriesNumber).Distinct().ToList();

            return new FindJournalVoucherInDbContextDto
            {
                ExistingJournalVoucherHeader = await _aasDbContext.JournalVoucherHeaders
                    .Where(x => originalSeriesNumbers.Contains(x.OriginalSeriesNumber!))
                    .GroupBy(x => x.OriginalSeriesNumber)
                    .Select(x => x.First())
                    .ToDictionaryAsync(x => x.OriginalSeriesNumber!, cancellationToken),

                CvId = await _aasDbContext.CheckVoucherHeaders
                    .Where(x => originalCheckVoucherIds.Contains(x.OriginalDocumentId))
                    .GroupBy(x => x.OriginalDocumentId)
                    .Select(x => x.First())
                    .ToDictionaryAsync(x => x.OriginalDocumentId, x => x.CheckVoucherHeaderId, cancellationToken),

                ExistingLogs = await _dbContext.ImportExportLogs
                    .Where(x => originalSeriesNumbers.Contains(x.DocumentNo))
                    .ToListAsync(cancellationToken)
            };
        }

        public JournalVoucherHeader MapToJournalVoucherEntity(
            JournalVoucherUploadExcelFileViewModel row,
            FindJournalVoucherInDbContextDto context)
        {
            return new JournalVoucherHeader
            {
                JournalVoucherHeaderNo = row.JournalVoucherHeaderNo,
                Date = row.Date,
                References = row.References,
                Particulars = row.Particulars,
                CRNo = row.CRNo,
                JVReason = row.JVReason,
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
                OriginalCVId = row.OriginalCVId,
                OriginalSeriesNumber = row.OriginalSeriesNumber,
                OriginalDocumentId = row.OriginalDocumentId,

                CVId = context.CvId.TryGetValue(row.OriginalCVId, out var cvId)
                    ? cvId
                    : throw new InvalidOperationException(
                        "Please upload the Excel file for the check voucher first.")
            };
        }

        public IEnumerable<AuditTrail> AuditTrails(
            JournalVoucherUploadExcelFileViewModel row,
            string machineName)
        {
            var audits = new List<AuditTrail>();

            if (!string.IsNullOrWhiteSpace(row.CreatedBy))
            {
                audits.Add(new AuditTrail
                {
                    Username = row.CreatedBy,
                    Activity = $"Create new journal vouchcer# {row.JournalVoucherHeaderNo}",
                    DocumentType = "Journal Voucher",
                    MachineName = machineName,
                    Date = row.CreatedDate
                });
            }

            if (!string.IsNullOrWhiteSpace(row.PostedBy) && row.PostedDate != default)
            {
                audits.Add(new AuditTrail
                {
                    Username = row.PostedBy,
                    Activity = $"Posted journal voucher# {row.JournalVoucherHeaderNo}",
                    DocumentType = "Journal Voucher",
                    MachineName = machineName,
                    Date = row.PostedDate
                });
            }

            if (!string.IsNullOrWhiteSpace(row.CanceledBy) && row.CanceledDate != default)
            {
                audits.Add(new AuditTrail
                {
                    Username = row.CanceledBy,
                    Activity = $"Cancelled journal voucher# {row.JournalVoucherHeaderNo}",
                    DocumentType = "Journal Voucher",
                    MachineName = machineName,
                    Date = row.CanceledDate
                });
            }

            if (!string.IsNullOrWhiteSpace(row.VoidedBy) && row.VoidedDate != default)
            {
                audits.Add(new AuditTrail
                {
                    Username = row.VoidedBy,
                    Activity = $"Voided journal voucher# {row.JournalVoucherHeaderNo}",
                    DocumentType = "Journal Voucher",
                    MachineName = machineName,
                    Date = row.VoidedDate
                });
            }

            if (!string.IsNullOrWhiteSpace(row.EditedBy) && row.EditedDate != default)
            {
                audits.Add(new AuditTrail
                {
                    Username = row.EditedBy,
                    Activity = $"Edited journal voucher# {row.JournalVoucherHeaderNo}",
                    DocumentType = "Journal Voucher",
                    MachineName = machineName,
                    Date = row.EditedDate
                });
            }

            return audits;
        }

        public Dictionary<string, (string Original, string New)> Detect(
            JournalVoucherHeader entity,
            JournalVoucherUploadExcelFileViewModel row,
            IReadOnlyList<ImportExportLog> logs)
        {
            var changes = new Dictionary<string, (string, string)>();

            _generalRepo.Compare(changes, logs, "JournalVoucherHeaderNo",
                StringHelper.NormalizeString(entity.JournalVoucherHeaderNo),
                row.JournalVoucherHeaderNo);

            _generalRepo.Compare(changes, logs, "Date",
                entity.Date.ToString(CS.DateOnly_Format_For_Validation),
                row.Date.ToString(CS.DateOnly_Format_For_Validation));

            _generalRepo.Compare(changes, logs, "References",
                StringHelper.NormalizeString(entity.References),
                row.References);

            _generalRepo.Compare(changes, logs, "Particulars",
                StringHelper.NormalizeString(entity.Particulars),
                row.Particulars);

            _generalRepo.Compare(changes, logs, "CRNo",
                StringHelper.NormalizeString(entity.CRNo),
                row.CRNo);

            _generalRepo.Compare(changes, logs, "JVReason",
                StringHelper.NormalizeString(entity.JVReason),
                row.JVReason);

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
                entity.PostedDate?.ToString(CS.DateTime_Format_For_Validation) ?? "null",
                row.PostedDate.ToString(CS.DateTime_Format_For_Validation));

            _generalRepo.Compare(changes, logs, "CancellationRemarks",
                StringHelper.NormalizeString(entity.CancellationRemarks),
                row.CancellationRemarks);

            _generalRepo.Compare(changes, logs, "OriginalCVId",
                entity.OriginalCVId?.ToString() ?? "null",
                row.OriginalCVId.ToString());

            _generalRepo.Compare(changes, logs, "OriginalSeriesNumber",
                StringHelper.NormalizeString(entity.OriginalSeriesNumber),
                row.OriginalSeriesNumber);

            _generalRepo.Compare(changes, logs, "OriginalDocumentId",
                entity.OriginalDocumentId.ToString(),
                row.OriginalDocumentId.ToString());

            return changes;
        }

        public IReadOnlyList<JournalVoucherDetailsUploadExcelFileViewModel> ParseWorksheetJournalVoucherDetails(
            ExcelWorksheet worksheet)
        {
            var rows = new List<JournalVoucherDetailsUploadExcelFileViewModel>();
            var rowCount = worksheet.Dimension.Rows;

            for (var row = 2; row <= rowCount; row++)
            {
                rows.Add(new JournalVoucherDetailsUploadExcelFileViewModel
                {
                    AccountNo = StringHelper.NormalizeString(worksheet.Cells[row, 1].GetValue<string>()),
                    AccountName = StringHelper.NormalizeString(worksheet.Cells[row, 2].GetValue<string>()),
                    TransactionNo = StringHelper.NormalizeString(worksheet.Cells[row, 3].GetValue<string>()),
                    Debit = worksheet.Cells[row, 4].GetValue<decimal>(),
                    Credit = worksheet.Cells[row, 5].GetValue<decimal>(),
                    JournalVoucherHeaderId = worksheet.Cells[row, 6].GetValue<int>(),
                    OriginalDocumentId = worksheet.Cells[row, 7].GetValue<int>(),
                });
            }

            return rows;
        }

        public async Task<FindJournalVoucherDetailsInDbContextDto> BuildLookupJournalVoucherDetailsContextAsync(
            IEnumerable<JournalVoucherDetailsUploadExcelFileViewModel> rows,
            CancellationToken cancellationToken)
        {
            var jvHeaderIds = rows.Select(r => r.JournalVoucherHeaderId).Distinct().ToList();
            var originalDocumentId = rows.Select(r => r.OriginalDocumentId).Distinct().ToList();

            return new FindJournalVoucherDetailsInDbContextDto
            {
                ExistingJournalVoucherDetail = await _dbContext.JournalVoucherDetails
                    .Where(x => x.OriginalDocumentId.HasValue && originalDocumentId.Contains(x.OriginalDocumentId!.Value))
                    .GroupBy(x => x.OriginalDocumentId!.Value)
                    .Select(x => x.First())
                    .ToDictionaryAsync(x => x.OriginalDocumentId!.Value, cancellationToken),

                JournalVoucherHeader = await _dbContext.JournalVoucherHeaders
                    .Where(x => jvHeaderIds.Contains(x.OriginalDocumentId))
                    .GroupBy(x => x.OriginalDocumentId)
                    .Select(x => x.First())
                    .ToDictionaryAsync(x => x.OriginalDocumentId, cancellationToken)
            };
        }

        public async Task<FindJournalVoucherDetailsInDbContextDto> BuildLookupJournalVoucherDetailsContextForAasAsync(
            IEnumerable<JournalVoucherDetailsUploadExcelFileViewModel> rows,
            CancellationToken cancellationToken)
        {
            var jvHeaderIds = rows.Select(r => r.JournalVoucherHeaderId).Distinct().ToList();
            var originalDocumentId = rows.Select(r => r.OriginalDocumentId).Distinct().ToList();

            return new FindJournalVoucherDetailsInDbContextDto
            {
                ExistingJournalVoucherDetail = await _aasDbContext.JournalVoucherDetails
                    .Where(x => x.OriginalDocumentId.HasValue && originalDocumentId.Contains(x.OriginalDocumentId!.Value))
                    .GroupBy(x => x.OriginalDocumentId!.Value)
                    .Select(x => x.First())
                    .ToDictionaryAsync(x => x.OriginalDocumentId!.Value, cancellationToken),

                JournalVoucherHeader = await _aasDbContext.JournalVoucherHeaders
                    .Where(x => jvHeaderIds.Contains(x.OriginalDocumentId))
                    .GroupBy(x => x.OriginalDocumentId)
                    .Select(x => x.First())
                    .ToDictionaryAsync(x => x.OriginalDocumentId, cancellationToken),

                ExistingLogs = await _dbContext.ImportExportLogs
                    .Where(x => originalDocumentId.Contains(x.DocumentRecordId))
                    .ToListAsync(cancellationToken)
            };
        }

        public JournalVoucherDetail MapToJournalVoucherDetailsEntity(
            JournalVoucherDetailsUploadExcelFileViewModel row,
            FindJournalVoucherDetailsInDbContextDto context)
        {
            if (!context.JournalVoucherHeader.TryGetValue(row.JournalVoucherHeaderId, out var journalVoucherHeader))
            {
                throw new InvalidOperationException($"Journal voucher header id missing for JournalVoucherHeaderId #{row.JournalVoucherHeaderId}.");
            }
            return new JournalVoucherDetail
            {
                AccountNo = row.AccountNo,
                AccountName = row.AccountName,
                TransactionNo = journalVoucherHeader.JournalVoucherHeaderNo!,
                Debit = row.Debit,
                Credit = row.Credit,
                OriginalDocumentId = row.OriginalDocumentId,
                JournalVoucherHeaderId = journalVoucherHeader.JournalVoucherHeaderId
            };
        }

        public Dictionary<string, (string Original, string New)> DetectJvDetails(
            JournalVoucherDetail entity,
            JournalVoucherDetailsUploadExcelFileViewModel row,
            IReadOnlyList<ImportExportLog> logs)
        {
            var changes = new Dictionary<string, (string, string)>();

            _generalRepo.Compare(changes, logs, "AccountNo",
                StringHelper.NormalizeString(entity.AccountNo),
                row.AccountNo);

            _generalRepo.Compare(changes, logs, "AccountName",
                StringHelper.NormalizeString(entity.AccountName),
                row.AccountName);

            _generalRepo.Compare(changes, logs, "Debit",
                entity.Debit.ToString(CS.Four_Decimal_Format),
                row.Debit.ToString(CS.Four_Decimal_Format));

            _generalRepo.Compare(changes, logs, "Credit",
                entity.Credit.ToString(CS.Four_Decimal_Format),
                row.Credit.ToString(CS.Four_Decimal_Format));

            _generalRepo.Compare(changes, logs, "OriginalDocumentId",
                entity.OriginalDocumentId?.ToString() ?? "null",
                row.OriginalDocumentId.ToString());

            return changes;
        }
    }
}
