using Accounting_System.Data;
using Accounting_System.DTOs;
using Accounting_System.Models;
using Accounting_System.Models.AccountsPayable;
using Accounting_System.Models.Reports;
using Accounting_System.Models.ViewModels;
using Accounting_System.Utility;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OfficeOpenXml;

namespace Accounting_System.Repository
{
    public class CheckVoucherRepo
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly AasDbContext _aasDbContext;

        private readonly GeneralRepo _generalRepo;

        public CheckVoucherRepo(ApplicationDbContext dbContext, AasDbContext aasDbContext, GeneralRepo generalRepo)
        {
            _dbContext = dbContext;
            _aasDbContext = aasDbContext;
            _generalRepo = generalRepo;
        }

        public async Task<List<CheckVoucherHeader>> GetCheckVouchersAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.CheckVoucherHeaders
                .Include(s => s.Supplier)
                .ToListAsync(cancellationToken);
        }

        public async Task<string> GenerateCVNo(CancellationToken cancellationToken = default)
        {
            var checkVoucher = await _dbContext
                .CheckVoucherHeaders
                .OrderByDescending(s => s.CheckVoucherHeaderNo)
                .FirstOrDefaultAsync(cancellationToken);

            if (checkVoucher != null)
            {
                string lastSeries = checkVoucher.CheckVoucherHeaderNo ?? throw new InvalidOperationException("CVNo is null pls Contact MIS Enterprise");
                string numericPart = lastSeries.Substring(2);
                int incrementedNumber = int.Parse(numericPart) + 1;

                return lastSeries.Substring(0, 2) + incrementedNumber.ToString("D10");
            }

            return $"CV{1.ToString("D10")}";
        }

        public async Task<string> GenerateAasCvNo(CancellationToken cancellationToken = default)
        {
            var checkVoucher = await _aasDbContext
                .CheckVoucherHeaders
                .OrderByDescending(s => s.CheckVoucherHeaderNo)
                .FirstOrDefaultAsync(cancellationToken);

            if (checkVoucher != null)
            {
                string lastSeries = checkVoucher.CheckVoucherHeaderNo ?? throw new InvalidOperationException("CVNo is null pls Contact MIS Enterprise");
                string numericPart = lastSeries.Substring(2);
                int incrementedNumber = int.Parse(numericPart) + 1;

                return lastSeries.Substring(0, 2) + incrementedNumber.ToString("D10");
            }

            return $"CV{1.ToString("D10")}";
        }

        public async Task UpdateInvoicingVoucher(decimal paymentAmount, int invoiceVoucherId, CancellationToken cancellationToken = default)
        {
            var invoiceVoucher = await _dbContext.CheckVoucherHeaders
                .FirstOrDefaultAsync(x => x.CheckVoucherHeaderId == invoiceVoucherId, cancellationToken);

            if (invoiceVoucher != null)
            {
                invoiceVoucher.AmountPaid += paymentAmount;

                if (invoiceVoucher.AmountPaid >= invoiceVoucher.Total)
                {
                    invoiceVoucher.IsPaid = true;
                }
            }
            else
            {
                throw new InvalidOperationException($"Check voucher with id '{invoiceVoucherId}' not found.");
            }
        }

        public async Task LogChangesAsync(int id, Dictionary<string, (string OriginalValue, string NewValue)> changes, string? modifiedBy, string seriesNumber, string databaseName)
        {
            foreach (var change in changes)
            {
                var logReport = new ImportExportLog
                {
                    Id = Guid.NewGuid(),
                    TableName = "CheckVoucherHeader",
                    DocumentRecordId = id,
                    ColumnName = change.Key,
                    Module = "Check Voucher Header",
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

        public async Task LogChangesForCVDAsync(int? id, Dictionary<string, (string OriginalValue, string NewValue)> changes, string? modifiedBy, string seriesNumber, string databaseName)
        {
            foreach (var change in changes)
            {
                var logReport = new ImportExportLog()
                {
                    Id = Guid.NewGuid(),
                    TableName = "CheckVoucherDetails",
                    DocumentRecordId = id!.Value,
                    ColumnName = change.Key,
                    Module = "Check Voucher Details",
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

        public async Task<IReadOnlyList<CheckVoucherUploadExcelFileViewModel>> ParseWorksheet(
            ExcelWorksheet worksheet,
            CancellationToken cancellationToken = default)
        {
            var rows = new List<CheckVoucherUploadExcelFileViewModel>();
            var rowCount = worksheet.Dimension.Rows;


            for (var row = 2; row <= rowCount; row++)
            {


                rows.Add(new CheckVoucherUploadExcelFileViewModel
                {
                    CheckVoucherHeaderNo = string.Empty,
                    Date = DateOnly.FromDateTime(worksheet.Cells[row, 1].GetValue<DateTime>()),
                    RRNo = worksheet.Cells[row, 2].Text.Split(',').Select(rrNo => rrNo.Trim()).ToArray(),
                    SINo = worksheet.Cells[row, 3].Text.Split(',').Select(rrNo => rrNo.Trim()).ToArray(),
                    PONo = worksheet.Cells[row, 4].Text.Split(',').Select(rrNo => rrNo.Trim()).ToArray(),
                    Particulars = StringHelper.NormalizeString(worksheet.Cells[row, 5].GetValue<string>()),
                    CheckNo = StringHelper.NormalizeString(worksheet.Cells[row, 6].GetValue<string>()),
                    Category = StringHelper.NormalizeString(worksheet.Cells[row, 7].GetValue<string>()),
                    Payee = StringHelper.NormalizeString(worksheet.Cells[row, 8].GetValue<string>()),
                    CheckDate = DateOnly.FromDateTime(worksheet.Cells[row, 9].GetValue<DateTime>()),
                    StartDate = DateOnly.FromDateTime(worksheet.Cells[row, 10].GetValue<DateTime>()),
                    EndDate = DateOnly.FromDateTime(worksheet.Cells[row, 11].GetValue<DateTime>()),
                    NumberOfMonths = worksheet.Cells[row, 12].GetValue<int>(),
                    NumberOfMonthsCreated = worksheet.Cells[row, 13].GetValue<int>(),
                    LastCreatedDate = worksheet.Cells[row, 14].GetValue<DateTime>(),
                    AmountPerMonth = worksheet.Cells[row, 15].GetValue<decimal>(),
                    IsComplete = worksheet.Cells[row, 16].GetValue<bool>(),
                    AccruedType = StringHelper.NormalizeString(worksheet.Cells[row, 17].GetValue<string>()),
                    Reference = StringHelper.NormalizeString(worksheet.Cells[row, 18].GetValue<string>()),
                    CreatedBy = StringHelper.NormalizeString(worksheet.Cells[row, 19].GetValue<string>()),
                    CreatedDate = worksheet.Cells[row, 20].GetValue<DateTime>(),
                    PostedBy = StringHelper.NormalizeString(worksheet.Cells[row, 32].GetValue<string>()),
                    PostedDate = worksheet.Cells[row, 33].GetValue<DateTime>(),
                    Total = worksheet.Cells[row, 21].GetValue<decimal>(),
                    Amount = worksheet.Cells[row, 22].Text.Split(' ').Select(arrayAmount =>
                                                             decimal.TryParse(arrayAmount.Trim(), out decimal amount) ? amount : 0).ToArray(),
                    CheckAmount = worksheet.Cells[row, 23].GetValue<decimal>(),
                    CvType = StringHelper.NormalizeString(worksheet.Cells[row, 24].GetValue<string>()),
                    AmountPaid = worksheet.Cells[row, 25].GetValue<decimal>(),
                    IsPaid = worksheet.Cells[row, 26].GetValue<bool>(),
                    CancellationRemarks = StringHelper.NormalizeString(worksheet.Cells[row, 27].GetValue<string>()),
                    OriginalBankId = worksheet.Cells[row, 28].GetValue<int>(),
                    OriginalSeriesNumber = StringHelper.NormalizeString(worksheet.Cells[row, 29].GetValue<string>()),
                    OriginalSupplierId = worksheet.Cells[row, 30].GetValue<int>(),
                    OriginalDocumentId = worksheet.Cells[row, 31].GetValue<int>(),
                    CanceledBy = string.IsNullOrWhiteSpace(worksheet.Cells[row, 36].Text)
                        ? string.Empty
                        : StringHelper.NormalizeString(worksheet.Cells[row, 36].GetValue<string>()),
                    CanceledDate = worksheet.Cells[row, 37].GetValue<DateTime>(),
                    VoidedBy = string.IsNullOrWhiteSpace(worksheet.Cells[row, 38].Text)
                        ? string.Empty
                        : StringHelper.NormalizeString(worksheet.Cells[row, 38].GetValue<string>()),
                    VoidedDate = worksheet.Cells[row, 39].GetValue<DateTime>(),
                    EditedBy = string.IsNullOrWhiteSpace(worksheet.Cells[row, 34].Text)
                        ? string.Empty
                        : StringHelper.NormalizeString(worksheet.Cells[row, 34].GetValue<string>()),
                    EditedDate = worksheet.Cells[row, 35].GetValue<DateTime>()
                });
            }

            return rows;
        }

        public async Task<FindCheckVoucherInDbContextDto> BuildLookupCheckVoucherHeaderContextAsync(
            IEnumerable<CheckVoucherUploadExcelFileViewModel> rows,
            CancellationToken cancellationToken)
        {
            var originalSupplierIds = rows.Select(r => r.OriginalSupplierId).Distinct().ToList();
            var originalBankIds = rows.Select(r => r.OriginalBankId).Distinct().ToList();
            var originalSeriesNumbers = rows.Select(r => r.OriginalSeriesNumber).Distinct().ToList();

            return new FindCheckVoucherInDbContextDto
            {
                ExistingCheckVoucherHeader = await _dbContext.CheckVoucherHeaders
                    .Where(x => originalSeriesNumbers.Contains(x.OriginalSeriesNumber!))
                    .GroupBy(x => x.OriginalSeriesNumber)
                    .Select(x => x.First())
                    .ToDictionaryAsync(x => x.OriginalSeriesNumber!, cancellationToken),

                SupplierId = await _dbContext.Suppliers
                    .Where(x => originalSupplierIds.Contains(x.OriginalSupplierId!.Value))
                    .GroupBy(x => x.OriginalSupplierId!.Value)
                    .Select(x => x.First())
                    .ToDictionaryAsync(x => x.OriginalSupplierId!.Value, x => x.SupplierId, cancellationToken),

                BankId = await _dbContext.BankAccounts
                    .Where(x => originalBankIds.Contains(x.OriginalBankId!.Value))
                    .GroupBy(x => x.OriginalBankId!.Value)
                    .Select(x => x.First())
                    .ToDictionaryAsync(x => x.OriginalBankId!.Value, x => x.BankAccountId, cancellationToken),

                ExistingLogs = await _dbContext.ImportExportLogs
                    .Where(x => originalSeriesNumbers.Contains(x.DocumentNo))
                    .ToListAsync(cancellationToken)
            };
        }

        public CheckVoucherHeader MapToCheckVoucherHeaderEntity(
            CheckVoucherUploadExcelFileViewModel row,
            List<CheckVoucherHeader> checkVoucherInvoices,
            FindCheckVoucherInDbContextDto context, string? checkVoucherHeaderNo)
        {
            var getReference = checkVoucherInvoices
                .Where(x => x.OriginalSeriesNumber == row.Reference)
                .Select(x => x.CheckVoucherHeaderNo)
                .FirstOrDefault();
            return new CheckVoucherHeader
            {
                CheckVoucherHeaderNo = checkVoucherHeaderNo,
                Date = row.Date,
                RRNo = row.RRNo,
                SINo = row.SINo,
                PONo = row.PONo,
                Particulars = row.Particulars,
                CheckNo = row.CheckNo,
                Category = row.Category,
                Payee = row.Payee,
                CheckDate = row.CheckDate,
                StartDate = row.StartDate,
                EndDate = row.EndDate,
                NumberOfMonths = row.NumberOfMonths,
                NumberOfMonthsCreated = row.NumberOfMonthsCreated,
                LastCreatedDate = row.LastCreatedDate,
                AmountPerMonth = row.AmountPerMonth,
                IsComplete = row.IsComplete,
                AccruedType = row.AccruedType,
                Reference = row.CvType == "Payment" ? getReference : string.Empty,
                CreatedBy = row.CreatedBy,
                CreatedDate = row.CreatedDate,
                PostedBy = row.PostedBy,
                PostedDate = row.PostedDate,
                Total = row.Total,
                Amount = row.Amount,
                CheckAmount = row.CheckAmount,
                CvType = row.CvType,
                AmountPaid = row.AmountPaid,
                IsPaid = row.IsPaid,
                CancellationRemarks = row.CancellationRemarks,
                CanceledBy = row.CanceledBy,
                CanceledDate = row.CanceledDate,
                VoidedBy = row.VoidedBy,
                VoidedDate = row.VoidedDate,
                EditedBy = row.EditedBy,
                EditedDate = row.EditedDate,
                OriginalBankId = row.OriginalBankId,
                OriginalSeriesNumber = row.OriginalSeriesNumber,
                OriginalSupplierId = row.OriginalSupplierId,
                OriginalDocumentId = row.OriginalDocumentId,

                SupplierId = context.SupplierId.TryGetValue(row.OriginalSupplierId, out var supplierId)
                    ? supplierId
                    : null,

                BankId = row.CvType != "Invoicing" ? context.BankId.TryGetValue(row.OriginalBankId, out var bankId)
                    ? bankId
                    : throw new InvalidOperationException(
                        "Please upload the Excel file for the bank account master file first.")
                    : null
            };
        }

        public IEnumerable<AuditTrail> AuditTrails(
            CheckVoucherUploadExcelFileViewModel row,
            string machineName)
        {
            var audits = new List<AuditTrail>();

            if (!string.IsNullOrWhiteSpace(row.CreatedBy))
            {
                audits.Add(new AuditTrail
                {
                    Username = row.CreatedBy,
                    Activity = $"Create new check vouchcer# {row.CheckVoucherHeaderNo}",
                    DocumentType = "Check Voucher",
                    MachineName = machineName,
                    Date = row.CreatedDate
                });
            }

            if (!string.IsNullOrWhiteSpace(row.PostedBy) && row.PostedDate != default)
            {
                audits.Add(new AuditTrail
                {
                    Username = row.PostedBy,
                    Activity = $"Posted check voucher# {row.CheckVoucherHeaderNo}",
                    DocumentType = "Check Voucher",
                    MachineName = machineName,
                    Date = row.PostedDate
                });
            }

            if (!string.IsNullOrWhiteSpace(row.CanceledBy) && row.CanceledDate != default)
            {
                audits.Add(new AuditTrail
                {
                    Username = row.CanceledBy,
                    Activity = $"Cancelled check voucher# {row.CheckVoucherHeaderNo}",
                    DocumentType = "Check Voucher",
                    MachineName = machineName,
                    Date = row.CanceledDate
                });
            }

            if (!string.IsNullOrWhiteSpace(row.VoidedBy) && row.VoidedDate != default)
            {
                audits.Add(new AuditTrail
                {
                    Username = row.VoidedBy,
                    Activity = $"Voided check voucher# {row.CheckVoucherHeaderNo}",
                    DocumentType = "Check Voucher",
                    MachineName = machineName,
                    Date = row.VoidedDate
                });
            }

            if (!string.IsNullOrWhiteSpace(row.EditedBy) && row.EditedDate != default)
            {
                audits.Add(new AuditTrail
                {
                    Username = row.EditedBy,
                    Activity = $"Edited check voucher# {row.CheckVoucherHeaderNo}",
                    DocumentType = "Check Voucher",
                    MachineName = machineName,
                    Date = row.EditedDate
                });
            }

            return audits;
        }

        public Dictionary<string, (string Original, string New)> Detect(
            CheckVoucherHeader entity,
            CheckVoucherUploadExcelFileViewModel row,
            IReadOnlyList<ImportExportLog> logs)
        {
            var changes = new Dictionary<string, (string, string)>();

            _generalRepo.Compare(changes, logs, "Date",
                entity.Date.ToString(CS.DateOnly_Format_For_Validation),
                row.Date.ToString(CS.DateOnly_Format_For_Validation));

            _generalRepo.Compare(changes, logs, "RRNo",
                string.Join(",", entity.RRNo ?? Array.Empty<string>()),
                string.Join(",", row.RRNo));

            _generalRepo.Compare(changes, logs, "SINo",
                string.Join(",", entity.SINo ?? Array.Empty<string>()),
                string.Join(",", row.SINo));

            _generalRepo.Compare(changes, logs, "PONo",
                string.Join(",", entity.PONo ?? Array.Empty<string>()),
                string.Join(",", row.PONo));

            _generalRepo.Compare(changes, logs, "Particulars",
                StringHelper.NormalizeString(entity.Particulars),
                row.Particulars);

            _generalRepo.Compare(changes, logs, "CheckNo",
                StringHelper.NormalizeString(entity.CheckNo),
                row.CheckNo);

            _generalRepo.Compare(changes, logs, "Category",
                StringHelper.NormalizeString(entity.Category),
                row.Category);

            _generalRepo.Compare(changes, logs, "Payee",
                StringHelper.NormalizeString(entity.Payee),
                row.Payee);

            _generalRepo.Compare(changes, logs, "CheckDate",
                (entity.CheckDate ?? DateOnly.MinValue).ToString(CS.DateOnly_Format_For_Validation),
                row.CheckDate.ToString(CS.DateOnly_Format_For_Validation));

            _generalRepo.Compare(changes, logs, "StartDate",
                (entity.StartDate ?? DateOnly.MinValue).ToString(CS.DateOnly_Format_For_Validation),
                row.StartDate.ToString(CS.DateOnly_Format_For_Validation));

            _generalRepo.Compare(changes, logs, "EndDate",
                (entity.EndDate ?? DateOnly.MinValue).ToString(CS.DateOnly_Format_For_Validation),
                row.EndDate.ToString(CS.DateOnly_Format_For_Validation));

            _generalRepo.Compare(changes, logs, "NumberOfMonths",
                entity.NumberOfMonths.ToString(),
                row.NumberOfMonths.ToString());

            _generalRepo.Compare(changes, logs, "NumberOfMonthsCreated",
                entity.NumberOfMonthsCreated.ToString(),
                row.NumberOfMonthsCreated.ToString());

            _generalRepo.Compare(changes, logs, "LastCreatedDate",
                (entity.LastCreatedDate ?? DateTime.MinValue).ToString(CS.DateTime_Format_For_Validation),
                row.LastCreatedDate.ToString(CS.DateTime_Format_For_Validation));

            _generalRepo.Compare(changes, logs, "AmountPerMonth",
                entity.AmountPerMonth.ToString(CS.Four_Decimal_Format),
                row.AmountPerMonth.ToString(CS.Four_Decimal_Format));

            _generalRepo.Compare(changes, logs, "IsComplete",
                entity.IsComplete.ToString(),
                row.IsComplete.ToString());

            _generalRepo.Compare(changes, logs, "AccruedType",
                StringHelper.NormalizeString(entity.AccruedType),
                row.AccruedType);

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
                row.PostedDate.ToString(CS.DateTime_Format_For_Validation));

            _generalRepo.Compare(changes, logs, "Total",
                entity.Total.ToString(CS.Four_Decimal_Format),
                row.Total.ToString(CS.Four_Decimal_Format));

            _generalRepo.Compare(changes, logs, "Amount",
                string.Join(",", entity.Amount ?? Array.Empty<decimal>()),
                string.Join(",", row.Amount.Select(x => x.ToString(CS.Four_Decimal_Format))));

            _generalRepo.Compare(changes, logs, "CheckAmount",
                entity.CheckAmount.ToString(CS.Four_Decimal_Format),
                row.CheckAmount.ToString(CS.Four_Decimal_Format));

            _generalRepo.Compare(changes, logs, "CvType",
                StringHelper.NormalizeString(entity.CvType),
                row.CvType);

            _generalRepo.Compare(changes, logs, "AmountPaid",
                entity.AmountPaid.ToString(CS.Four_Decimal_Format),
                row.AmountPaid.ToString(CS.Four_Decimal_Format));

            _generalRepo.Compare(changes, logs, "IsPaid",
                entity.IsPaid.ToString(),
                row.IsPaid.ToString());

            _generalRepo.Compare(changes, logs, "CancellationRemarks",
                StringHelper.NormalizeString(entity.CancellationRemarks),
                row.CancellationRemarks);

            _generalRepo.Compare(changes, logs, "OriginalBankId",
                (entity.OriginalBankId ?? 0).ToString(),
                row.OriginalBankId.ToString());

            _generalRepo.Compare(changes, logs, "OriginalSeriesNumber",
                StringHelper.NormalizeString(entity.OriginalSeriesNumber),
                row.OriginalSeriesNumber);

            _generalRepo.Compare(changes, logs, "OriginalSupplierId",
                (entity.OriginalSupplierId ?? 0).ToString(),
                row.OriginalSupplierId.ToString());

            _generalRepo.Compare(changes, logs, "OriginalDocumentId",
                entity.OriginalDocumentId.ToString(),
                row.OriginalDocumentId.ToString());

            return changes;
        }

        public string GenerateCodeForUploadingExcelFile(string? getLastCheckVoucherHeaderNo, CancellationToken cancellationToken = default)
        {
            if (!getLastCheckVoucherHeaderNo.IsNullOrEmpty())
            {
                var lastSeries = getLastCheckVoucherHeaderNo ?? throw new InvalidOperationException("CVNo is null pls Contact MIS Enterprise");
                var numericPart = lastSeries.Substring(2);
                var incrementedNumber = int.Parse(numericPart) + 1;
                return lastSeries.Substring(0, 2) + incrementedNumber.ToString("D10");
            }

            return $"CV{1:D10}";
        }

        public IReadOnlyList<CvTradePaymentUploadExcelFileViewModel> ParseWorksheetCvTradePayment(
            ExcelWorksheet worksheet)
        {
            var rows = new List<CvTradePaymentUploadExcelFileViewModel>();
            var rowCount = worksheet.Dimension.Rows;

            for (var row = 2; row <= rowCount; row++)
            {
                rows.Add(new CvTradePaymentUploadExcelFileViewModel
                {
                    DocumentId = worksheet.Cells[row, 2].GetValue<int>(),
                    DocumentType = "RR",
                    CheckVoucherId = worksheet.Cells[row, 4].GetValue<int>(),
                    AmountPaid = worksheet.Cells[row, 5].GetValue<decimal>(),
                });
            }

            return rows;
        }

        public async Task<FindCvTradePaymentInDbContextDto> BuildLookupCvTradePaymentContextAsync(
            IEnumerable<CvTradePaymentUploadExcelFileViewModel> rows,
            CancellationToken cancellationToken)
        {
            var receivingReportIds = rows.Select(r => r.DocumentId).Distinct().ToList();
            var checkVoucherIds = rows.Select(r => r.CheckVoucherId).Distinct().ToList();

            return new FindCvTradePaymentInDbContextDto
            {
                ReceivingReportId = await _dbContext.ReceivingReports
                    .Where(x => receivingReportIds.Contains(x.OriginalDocumentId!))
                    .GroupBy(x => x.OriginalDocumentId)
                    .Select(x => x.First())
                    .ToDictionaryAsync(x => x.OriginalDocumentId!, x => x.ReceivingReportId, cancellationToken),

                CheckVoucherHeaderId = await _dbContext.CheckVoucherHeaders
                    .Where(x => checkVoucherIds.Contains(x.OriginalDocumentId))
                    .GroupBy(x => x.OriginalDocumentId)
                    .Select(x => x.First())
                    .ToDictionaryAsync(x => x.OriginalDocumentId, x => x.CheckVoucherHeaderId, cancellationToken)
            };
        }

        public CVTradePayment MapToCvTradePaymentEntity(
            CvTradePaymentUploadExcelFileViewModel row,
            FindCvTradePaymentInDbContextDto context)
        {
            return new CVTradePayment
            {
                DocumentId = context.ReceivingReportId.TryGetValue(row.DocumentId, out var receivingReportId)
                    ? receivingReportId
                    : 0,
                DocumentType = "RR",
                CheckVoucherId = context.CheckVoucherHeaderId.TryGetValue(row.CheckVoucherId, out var checkVoucherHeaderId)
                    ? checkVoucherHeaderId
                    : 0,
                AmountPaid = row.AmountPaid
            };
        }

        public IReadOnlyList<CvMultiplePaymentUploadExcelFileViewModel> ParseWorksheetCvMultiplePayment(
            ExcelWorksheet worksheet)
        {
            var rows = new List<CvMultiplePaymentUploadExcelFileViewModel>();
            var rowCount = worksheet.Dimension.Rows;

            for (var row = 2; row <= rowCount; row++)
            {
                rows.Add(new CvMultiplePaymentUploadExcelFileViewModel
                {
                    Id = Guid.NewGuid(),
                    CheckVoucherHeaderPaymentId = worksheet.Cells[row, 2].GetValue<int>(),
                    CheckVoucherHeaderInvoiceId =worksheet.Cells[row, 3].GetValue<int>(),
                    AmountPaid = worksheet.Cells[row, 4].GetValue<int>()
                });
            }

            return rows;
        }

        public async Task<FindCvMultiplePaymentInDbContextDto> BuildLookupCvMultiplePaymentContextAsync(
            IEnumerable<CvMultiplePaymentUploadExcelFileViewModel> rows,
            CancellationToken cancellationToken)
        {
            var checkVoucherHeaderPaymentIds = rows.Select(r => r.CheckVoucherHeaderPaymentId).Distinct().ToList();
            var checkVoucherHeaderInvoiceIds = rows.Select(r => r.CheckVoucherHeaderInvoiceId).Distinct().ToList();

            return new FindCvMultiplePaymentInDbContextDto
            {

                CheckVoucherHeaderPaymentId =  await _dbContext.CheckVoucherHeaders
                    .Where(x => checkVoucherHeaderPaymentIds.Contains(x.OriginalDocumentId!))
                    .GroupBy(x => x.OriginalDocumentId)
                    .Select(x => x.First())
                    .ToDictionaryAsync(x => x.OriginalDocumentId, x => x.CheckVoucherHeaderId, cancellationToken),

                CheckVoucherHeaderInvoiceId = await _dbContext.CheckVoucherHeaders
                    .Where(x => checkVoucherHeaderInvoiceIds.Contains(x.OriginalDocumentId))
                    .GroupBy(x => x.OriginalDocumentId)
                    .Select(x => x.First())
                    .ToDictionaryAsync(x => x.OriginalDocumentId, x => x.CheckVoucherHeaderId, cancellationToken)
            };
        }

        public MultipleCheckVoucherPayment MapToCvMultiplePaymentEntity(
            CvMultiplePaymentUploadExcelFileViewModel row,
            FindCvMultiplePaymentInDbContextDto context)
        {
            return new MultipleCheckVoucherPayment
            {
                Id = row.Id,
                CheckVoucherHeaderPaymentId = context.CheckVoucherHeaderPaymentId.TryGetValue(row.CheckVoucherHeaderPaymentId, out var checkVoucherHeaderPaymentId)
                    ? checkVoucherHeaderPaymentId
                    : 0,
                CheckVoucherHeaderInvoiceId = context.CheckVoucherHeaderInvoiceId.TryGetValue(row.CheckVoucherHeaderInvoiceId, out var checkVoucherHeaderInvoiceId)
                ? checkVoucherHeaderInvoiceId
                : 0,
                AmountPaid = row.AmountPaid
            };
        }

        public IReadOnlyList<CheckVoucherDetailsUploadExcelFileViewModel> ParseWorksheetCheckVoucherDetails(
            ExcelWorksheet worksheet)
        {
            var rows = new List<CheckVoucherDetailsUploadExcelFileViewModel>();
            var rowCount = worksheet.Dimension.Rows;

            for (var row = 2; row <= rowCount; row++)
            {
                rows.Add(new CheckVoucherDetailsUploadExcelFileViewModel
                {
                    AccountNo = worksheet.Cells[row, 1].GetValue<string>(),
                    AccountName = worksheet.Cells[row, 2].GetValue<string>(),
                    Debit = worksheet.Cells[row, 4].GetValue<decimal>(),
                    Credit = worksheet.Cells[row, 5].GetValue<decimal>(),
                    CvHeaderId = worksheet.Cells[row, 6].GetValue<int>(),
                    OriginalDocumentId = worksheet.Cells[row, 7].GetValue<int>(),
                    Amount = worksheet.Cells[row, 8].GetValue<decimal>(),
                    AmountPaid = worksheet.Cells[row, 9].GetValue<decimal>(),
                    SupplierId = worksheet.Cells[row, 10].GetValue<int>(),
                    EwtPercent = worksheet.Cells[row, 11].GetValue<decimal>(),
                    IsUserSelected = worksheet.Cells[row, 12].GetValue<bool>(),
                    IsVatable = worksheet.Cells[row, 13].GetValue<bool>()
                });
            }

            return rows;
        }

        public async Task<FindCheckVoucherDetailsInDbContextDto> BuildLookupCheckVoucherDetailsContextAsync(
            IEnumerable<CheckVoucherDetailsUploadExcelFileViewModel> rows,
            CancellationToken cancellationToken)
        {
            var cvHeaderIds = rows.Select(r => r.CvHeaderId).Distinct().ToList();
            var supplierIds = rows.Select(r => r.SupplierId).Distinct().ToList();
            var originalDocumentId = rows.Select(r => r.OriginalDocumentId).Distinct().ToList();

            return new FindCheckVoucherDetailsInDbContextDto
            {
                ExistingCheckVoucherDetail = await _dbContext.CheckVoucherDetails
                    .Where(x => x.OriginalDocumentId.HasValue && originalDocumentId.Contains(x.OriginalDocumentId!.Value))
                    .GroupBy(x => x.OriginalDocumentId!.Value)
                    .Select(x => x.First())
                    .ToDictionaryAsync(x => x.OriginalDocumentId!.Value, cancellationToken),

                SupplierId =  await _dbContext.Suppliers
                    .Where(x => x.OriginalSupplierId.HasValue && supplierIds.Contains(x.OriginalSupplierId.Value))
                    .GroupBy(x => x.OriginalSupplierId!.Value)
                    .Select(x => x.First())
                    .ToDictionaryAsync(x => x.OriginalSupplierId!.Value, x => x.SupplierId, cancellationToken),

                CheckVoucherHeader = await _dbContext.CheckVoucherHeaders
                    .Where(x => cvHeaderIds.Contains(x.OriginalDocumentId))
                    .GroupBy(x => x.OriginalDocumentId)
                    .Select(x => x.First())
                    .ToDictionaryAsync(x => x.OriginalDocumentId, cancellationToken)
            };
        }

        public CheckVoucherDetail MapToCheckVoucherDetailsEntity(
            CheckVoucherDetailsUploadExcelFileViewModel row,
            FindCheckVoucherDetailsInDbContextDto context)
        {
            return new CheckVoucherDetail
            {
                AccountNo = row.AccountNo,
                AccountName = row.AccountName,
                Debit = row.Debit,
                Credit = row.Credit,
                CheckVoucherHeaderId = context.CheckVoucherHeader.TryGetValue(row.CvHeaderId, out var checkVouchHeader)
                    ? checkVouchHeader.CheckVoucherHeaderId
                    : null,
                OriginalDocumentId = row.OriginalDocumentId,
                Amount = row.Amount,
                AmountPaid = row.AmountPaid,
                SupplierId = context.SupplierId.TryGetValue(row.SupplierId, out var supplierId)
                    ? supplierId
                    : null,
                EwtPercent = row.EwtPercent,
                IsUserSelected = row.IsUserSelected,
                IsVatable = row.IsVatable,
                TransactionNo = checkVouchHeader!.CheckVoucherHeaderNo
            };
        }

        public Dictionary<string, (string Original, string New)> DetectCvDetails(
            CheckVoucherDetail entity,
            CheckVoucherDetailsUploadExcelFileViewModel row,
            IReadOnlyList<ImportExportLog> logs)
        {
            var changes = new Dictionary<string, (string, string)>();

            _generalRepo.Compare(changes, logs, "AccountNo",
                StringHelper.NormalizeString(entity.AccountNo),
                StringHelper.NormalizeString(row.AccountNo));

            _generalRepo.Compare(changes, logs, "AccountName",
                StringHelper.NormalizeString(entity.AccountName),
                StringHelper.NormalizeString(row.AccountName));

            _generalRepo.Compare(changes, logs, "Debit",
                entity.Debit.ToString(CS.Four_Decimal_Format),
                row.Debit.ToString(CS.Four_Decimal_Format));

            _generalRepo.Compare(changes, logs, "Credit",
                entity.Credit.ToString(CS.Four_Decimal_Format),
                row.Credit.ToString(CS.Four_Decimal_Format));

            _generalRepo.Compare(changes, logs, "CheckVoucherHeaderId",
                entity.CheckVoucherHeader?.OriginalDocumentId.ToString() ?? "null",
                row.CvHeaderId.ToString());

            _generalRepo.Compare(changes, logs, "OriginalDocumentId",
                entity.OriginalDocumentId.ToString() ?? "null",
                row.OriginalDocumentId.ToString());

            _generalRepo.Compare(changes, logs, "Amount",
                entity.Amount.ToString(CS.Four_Decimal_Format),
                row.Amount.ToString(CS.Four_Decimal_Format));

            _generalRepo.Compare(changes, logs, "AmountPaid",
                entity.AmountPaid.ToString(CS.Four_Decimal_Format),
                row.AmountPaid.ToString(CS.Four_Decimal_Format));

            _generalRepo.Compare(changes, logs, "EwtPercent",
                entity.EwtPercent.ToString(CS.Four_Decimal_Format),
                row.EwtPercent.ToString(CS.Four_Decimal_Format));

            _generalRepo.Compare(changes, logs, "IsUserSelected",
                entity.IsUserSelected.ToString(),
                row.IsUserSelected.ToString());

            _generalRepo.Compare(changes, logs, "IsVatable",
                entity.IsVatable.ToString(),
                row.IsVatable.ToString());

            return changes;
        }

        public async Task<FindCheckVoucherInDbContextDto> BuildLookupCheckVoucherHeaderContextForAasAsync(
            IEnumerable<CheckVoucherUploadExcelFileViewModel> rows,
            CancellationToken cancellationToken)
        {
            var originalSupplierIds = rows.Select(r => r.OriginalSupplierId).Distinct().ToList();
            var originalBankIds = rows.Select(r => r.OriginalBankId).Distinct().ToList();
            var originalSeriesNumbers = rows.Select(r => r.OriginalSeriesNumber).Distinct().ToList();

            return new FindCheckVoucherInDbContextDto
            {
                ExistingCheckVoucherHeader = await _aasDbContext.CheckVoucherHeaders
                    .Where(x => originalSeriesNumbers.Contains(x.OriginalSeriesNumber!))
                    .GroupBy(x => x.OriginalSeriesNumber)
                    .Select(x => x.First())
                    .ToDictionaryAsync(x => x.OriginalSeriesNumber!, cancellationToken),

                SupplierId = await _aasDbContext.Suppliers
                    .Where(x => originalSupplierIds.Contains(x.OriginalSupplierId!.Value))
                    .GroupBy(x => x.OriginalSupplierId!.Value)
                    .Select(x => x.First())
                    .ToDictionaryAsync(x => x.OriginalSupplierId!.Value, x => x.SupplierId, cancellationToken),

                BankId = await _aasDbContext.BankAccounts
                    .Where(x => originalBankIds.Contains(x.OriginalBankId!.Value))
                    .GroupBy(x => x.OriginalBankId!.Value)
                    .Select(x => x.First())
                    .ToDictionaryAsync(x => x.OriginalBankId!.Value, x => x.BankAccountId, cancellationToken),

                ExistingLogs = await _dbContext.ImportExportLogs
                    .Where(x => originalSeriesNumbers.Contains(x.DocumentNo))
                    .ToListAsync(cancellationToken)
            };
        }

        public async Task<FindCvTradePaymentInDbContextDto> BuildLookupCvTradePaymentContextForAasAsync(
            IEnumerable<CvTradePaymentUploadExcelFileViewModel> rows,
            CancellationToken cancellationToken)
        {
            var receivingReportIds = rows.Select(r => r.DocumentId).Distinct().ToList();
            var checkVoucherIds = rows.Select(r => r.CheckVoucherId).Distinct().ToList();

            return new FindCvTradePaymentInDbContextDto
            {
                ReceivingReportId = await _aasDbContext.ReceivingReports
                    .Where(x => receivingReportIds.Contains(x.OriginalDocumentId!))
                    .GroupBy(x => x.OriginalDocumentId)
                    .Select(x => x.First())
                    .ToDictionaryAsync(x => x.OriginalDocumentId!, x => x.ReceivingReportId, cancellationToken),

                CheckVoucherHeaderId = await _aasDbContext.CheckVoucherHeaders
                    .Where(x => checkVoucherIds.Contains(x.OriginalDocumentId))
                    .GroupBy(x => x.OriginalDocumentId)
                    .Select(x => x.First())
                    .ToDictionaryAsync(x => x.OriginalDocumentId, x => x.CheckVoucherHeaderId, cancellationToken)
            };
        }

        public async Task<FindCvMultiplePaymentInDbContextDto> BuildLookupCvMultiplePaymentContextForAasAsync(
            IEnumerable<CvMultiplePaymentUploadExcelFileViewModel> rows,
            CancellationToken cancellationToken)
        {
            var checkVoucherHeaderPaymentIds = rows.Select(r => r.CheckVoucherHeaderPaymentId).Distinct().ToList();
            var checkVoucherHeaderInvoiceIds = rows.Select(r => r.CheckVoucherHeaderInvoiceId).Distinct().ToList();

            return new FindCvMultiplePaymentInDbContextDto
            {

                CheckVoucherHeaderPaymentId =  await _aasDbContext.CheckVoucherHeaders
                    .Where(x => checkVoucherHeaderPaymentIds.Contains(x.OriginalDocumentId!))
                    .GroupBy(x => x.OriginalDocumentId)
                    .Select(x => x.First())
                    .ToDictionaryAsync(x => x.OriginalDocumentId, x => x.CheckVoucherHeaderId, cancellationToken),

                CheckVoucherHeaderInvoiceId = await _aasDbContext.CheckVoucherHeaders
                    .Where(x => checkVoucherHeaderInvoiceIds.Contains(x.OriginalDocumentId))
                    .GroupBy(x => x.OriginalDocumentId)
                    .Select(x => x.First())
                    .ToDictionaryAsync(x => x.OriginalDocumentId, x => x.CheckVoucherHeaderId, cancellationToken)
            };
        }

        public async Task<FindCheckVoucherDetailsInDbContextDto> BuildLookupCheckVoucherDetailsContextForAasAsync(
            IEnumerable<CheckVoucherDetailsUploadExcelFileViewModel> rows,
            CancellationToken cancellationToken)
        {
            var cvHeaderIds = rows.Select(r => r.CvHeaderId).Distinct().ToList();
            var supplierIds = rows.Select(r => r.SupplierId).Distinct().ToList();
            var originalDocumentId = rows.Select(r => r.OriginalDocumentId).Distinct().ToList();

            return new FindCheckVoucherDetailsInDbContextDto
            {
                ExistingCheckVoucherDetail = await _aasDbContext.CheckVoucherDetails
                    .Where(x => x.OriginalDocumentId.HasValue && originalDocumentId.Contains(x.OriginalDocumentId!.Value))
                    .GroupBy(x => x.OriginalDocumentId!.Value)
                    .Select(x => x.First())
                    .ToDictionaryAsync(x => x.OriginalDocumentId!.Value, cancellationToken),

                SupplierId =  await _aasDbContext.Suppliers
                    .Where(x => x.OriginalSupplierId.HasValue && supplierIds.Contains(x.OriginalSupplierId.Value))
                    .GroupBy(x => x.OriginalSupplierId!.Value)
                    .Select(x => x.First())
                    .ToDictionaryAsync(x => x.OriginalSupplierId!.Value, x => x.SupplierId, cancellationToken),

                CheckVoucherHeader = await _aasDbContext.CheckVoucherHeaders
                    .Where(x => cvHeaderIds.Contains(x.OriginalDocumentId))
                    .GroupBy(x => x.OriginalDocumentId)
                    .Select(x => x.First())
                    .ToDictionaryAsync(x => x.OriginalDocumentId, cancellationToken)
            };
        }
    }
}
