using System.Security.Claims;
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
    public class ReceivingReportRepo
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly GeneralRepo _generalRepo;

        private readonly InventoryRepo _inventoryRepo;

        private readonly AasDbContext _aasDbContext;

        public ReceivingReportRepo(ApplicationDbContext dbContext, GeneralRepo generalRepo, InventoryRepo inventoryRepo, AasDbContext aasDbContext)
        {
            _dbContext = dbContext;
            _generalRepo = generalRepo;
            _inventoryRepo = inventoryRepo;
            _aasDbContext = aasDbContext;
        }

        public async Task<string> GenerateRRNo(CancellationToken cancellationToken = default)
        {
            var receivingReport = await _dbContext
                .ReceivingReports
                .Where(rr => !rr.ReceivingReportNo!.StartsWith("RRBEG"))
                .OrderByDescending(s => s.ReceivingReportNo)
                .FirstOrDefaultAsync(cancellationToken);

            if (receivingReport != null)
            {
                string lastSeries = receivingReport.ReceivingReportNo ?? throw new InvalidOperationException("RRNo is null pls Contact MIS Enterprise");
                string numericPart = lastSeries.Substring(2);
                int incrementedNumber = int.Parse(numericPart) + 1;

                return lastSeries.Substring(0,2) + incrementedNumber.ToString("D10");
            }
            else
            {
                return $"RR{1.ToString("D10")}";
            }
        }

        public async Task<string> GetPONoAsync(int? id, CancellationToken cancellationToken = default)
        {
            if (id != 0)
            {
                var po = await _dbContext
                                .PurchaseOrders
                                .FirstOrDefaultAsync(po => po.PurchaseOrderId == id, cancellationToken);
                return po!.PurchaseOrderNo!;
            }
            else
            {
                throw new ArgumentException("No record found in supplier.");
            }
        }

        public async Task UpdatePOAsync(int id, decimal quantityReceived, CancellationToken cancellationToken = default)
        {
            var po = await _dbContext.PurchaseOrders
                    .FirstOrDefaultAsync(po => po.PurchaseOrderId == id, cancellationToken);

            if (po != null)
            {
                po.QuantityReceived += quantityReceived;

                if (po.QuantityReceived == po.Quantity)
                {
                    po.IsReceived = true;
                    po.ReceivedDate = DateTime.Now;

                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
                if (po.QuantityReceived > po.Quantity)
                {
                    throw new ArgumentException("Input is exceed to remaining quantity received");
                }
            }
            else
            {
                throw new ArgumentException("No record found.");
            }
        }

        public async Task<int> RemoveQuantityReceived(int? id, decimal quantityReceived, CancellationToken cancellationToken = default)
        {
            var po = await _dbContext.PurchaseOrders
                    .FirstOrDefaultAsync(po => po.PurchaseOrderId == id, cancellationToken);

            if (po != null)
            {
                po.QuantityReceived -= quantityReceived;

                if (po.IsReceived)
                {
                    po.IsReceived = false;
                    po.ReceivedDate = DateTime.MaxValue;
                }
                if (po.QuantityReceived > po.Quantity)
                {
                    throw new ArgumentException("Input is exceed to remaining quantity received");
                }

                return await _dbContext.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new ArgumentException("No record found.");
            }
        }

        public async Task<DateOnly> ComputeDueDateAsync(int? poId, DateOnly rrDate, CancellationToken cancellationToken = default)
        {
            var po = await _dbContext
                .PurchaseOrders
                .FirstOrDefaultAsync(po => po.PurchaseOrderId == poId, cancellationToken);

            if (po != null)
            {
                DateOnly dueDate;

                switch (po.Terms)
                {
                    case "7D":
                        return rrDate.AddDays(7);

                    case "10D":
                        return rrDate.AddDays(7);

                    case "15D":
                        return rrDate.AddDays(15);

                    case "30D":
                        return rrDate.AddDays(30);

                    case "45D":
                    case "45PDC":
                        return rrDate.AddDays(45);

                    case "60D":
                    case "60PDC":
                        return rrDate.AddDays(60);

                    case "90D":
                        return rrDate.AddDays(90);

                    case "M15":
                        return rrDate.AddMonths(1).AddDays(15 - rrDate.Day);

                    case "M30":
                        if (rrDate.Month == 1)
                        {
                            dueDate = new DateOnly(rrDate.Year, rrDate.Month, 1).AddMonths(2).AddDays(-1);
                        }
                        else
                        {
                            dueDate = new DateOnly(rrDate.Year, rrDate.Month, 1).AddMonths(2).AddDays(-1);

                            if (dueDate.Day == 31)
                            {
                                dueDate = dueDate.AddDays(-1);
                            }
                        }
                        return dueDate;

                    case "M29":
                        if (rrDate.Month == 1)
                        {
                            dueDate = new DateOnly(rrDate.Year, rrDate.Month, 1).AddMonths(2).AddDays(-1);
                        }
                        else
                        {
                            dueDate = new DateOnly(rrDate.Year, rrDate.Month, 1).AddMonths(2).AddDays(-1);

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
                        return rrDate;
                }
            }

            throw new ArgumentException("No record found.");
        }

        public async Task<ReceivingReport> FindRR(int id, CancellationToken cancellationToken = default)
        {
            var rr = await _dbContext
                .ReceivingReports
                .Include(rr => rr.PurchaseOrder)
                .ThenInclude(po => po!.Product)
                .Include(rr => rr.PurchaseOrder)
                .ThenInclude(po => po!.Supplier)
                .FirstOrDefaultAsync(rr => rr.ReceivingReportId == id, cancellationToken);

            if (rr != null)
            {
                return rr;
            }
            else
            {
                throw new ArgumentException("Invalid id value. The id must be greater than 0.");
            }
        }

        public async Task<PurchaseOrder> GetPurchaseOrderAsync(int id, CancellationToken cancellationToken = default)
        {
            var po = await _dbContext
                .PurchaseOrders
                .Include(po => po.Product)
                .Include(po => po.Supplier)
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

        public async Task<List<ReceivingReport>> GetReceivingReportsAsync(CancellationToken cancellationToken = default)
        {
            var rr = await _dbContext.ReceivingReports
                .Include(p => p.PurchaseOrder)
                .ThenInclude(s => s!.Supplier)
                .Include(p => p.PurchaseOrder)
                .ThenInclude(prod => prod!.Product)
                .ToListAsync(cancellationToken);

            if (rr.Any() && rr.Count > 0)
            {
                return rr;
            }

            return new List<ReceivingReport>();
        }

        public async Task LogChangesAsync(int id, Dictionary<string, (string OriginalValue, string NewValue)> changes, string? modifiedBy, string seriesNumber, string databaseName)
        {
            foreach (var change in changes)
            {
                var logReport = new ImportExportLog()
                {
                    Id = Guid.NewGuid(),
                    TableName = nameof(DynamicView.ReceivingReport),
                    DocumentRecordId = id,
                    ColumnName = change.Key,
                    Module = "Receiving Report",
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

        public async Task PostAsync(ReceivingReport model, ClaimsPrincipal user, CancellationToken cancellationToken = default)
        {
            #region --General Ledger Recording

            var ledgers = new List<GeneralLedgerBook>();

            decimal netOfVatAmount;
            decimal vatAmount = 0;
            decimal ewtAmount = 0;
            decimal netOfEwtAmount;

            if (model.PurchaseOrder!.Supplier!.VatType == CS.VatType_Vatable)
            {
                netOfVatAmount = _generalRepo.ComputeNetOfVat(model.Amount);
                vatAmount = _generalRepo.ComputeVatAmount(netOfVatAmount);
            }
            else
            {
                netOfVatAmount = model.Amount;
            }

            if (model.PurchaseOrder.Supplier.TaxType == CS.TaxType_WithTax)
            {
                ewtAmount = _generalRepo.ComputeEwtAmount(netOfVatAmount, 0.01m);
                netOfEwtAmount = _generalRepo.ComputeNetOfEwt(model.Amount, ewtAmount);
            }
            else
            {
                netOfEwtAmount = model.Amount;
            }

            var (inventoryAcctNo, _) = _generalRepo.GetInventoryAccountTitle(model.PurchaseOrder.Product!.ProductCode!);
            var accountTitlesDto = await _generalRepo.GetListOfAccountTitleDto(cancellationToken);
            var vatInputTitle = accountTitlesDto.Find(c => c.AccountNumber == "101060200") ?? throw new ArgumentException("Account title '101060200' not found.");
            var ewtTitle = accountTitlesDto.Find(c => c.AccountNumber == "201030210") ?? throw new ArgumentException("Account title '201030200' not found.");
            var apTradeTitle = accountTitlesDto.Find(c => c.AccountNumber == "202010100") ?? throw new ArgumentException("Account title '202010100' not found.");
            var inventoryTitle = accountTitlesDto.Find(c => c.AccountNumber == inventoryAcctNo) ?? throw new ArgumentException($"Account title '{inventoryAcctNo}' not found.");

            ledgers.Add(new GeneralLedgerBook
            {
                Date = model.Date,
                Reference = model.ReceivingReportNo!,
                Description = "Receipt of Goods",
                AccountNo = inventoryTitle.AccountNumber,
                AccountTitle = inventoryTitle.AccountName,
                Debit = netOfVatAmount,
                Credit = 0,
                CreatedBy = model.CreatedBy,
                CreatedDate = model.CreatedDate,
            });

            if (vatAmount > 0)
            {
                ledgers.Add(new GeneralLedgerBook
                {
                    Date = model.Date,
                    Reference = model.ReceivingReportNo!,
                    Description = "Receipt of Goods",
                    AccountNo = vatInputTitle.AccountNumber,
                    AccountTitle = vatInputTitle.AccountName,
                    Debit = vatAmount,
                    Credit = 0,
                    CreatedBy = model.CreatedBy,
                    CreatedDate = model.CreatedDate,
                });
            }

            ledgers.Add(new GeneralLedgerBook
            {
                Date = model.Date,
                Reference = model.ReceivingReportNo!,
                Description = "Receipt of Goods",
                AccountNo = apTradeTitle.AccountNumber,
                AccountTitle = apTradeTitle.AccountName,
                Debit = 0,
                Credit = netOfEwtAmount,
                CreatedBy = model.CreatedBy,
                CreatedDate = model.CreatedDate,
            });

            if (ewtAmount > 0)
            {
                ledgers.Add(new GeneralLedgerBook
                {
                    Date = model.Date,
                    Reference = model.ReceivingReportNo!,
                    Description = "Receipt of Goods",
                    AccountNo = ewtTitle.AccountNumber,
                    AccountTitle = ewtTitle.AccountName,
                    Debit = 0,
                    Credit = ewtAmount,
                    CreatedBy = model.CreatedBy,
                    CreatedDate = model.CreatedDate,
                });
            }

            if (!_generalRepo.IsJournalEntriesBalanced(ledgers))
            {
                throw new ArgumentException("Debit and Credit is not equal, check your entries.");
            }

            await _dbContext.AddRangeAsync(ledgers, cancellationToken);

            #endregion --General Ledger Recording

            #region--Inventory Recording

            await _inventoryRepo.AddPurchaseToInventoryAsync(model, user, cancellationToken);

            #endregion

            await UpdatePOAsync(model.PurchaseOrder.PurchaseOrderId, model.QuantityReceived, cancellationToken);

            #region --Purchase Book Recording

            PurchaseJournalBook purchaseBook = new()
            {
                Date = model.Date,
                SupplierName = model.PurchaseOrder.Supplier.SupplierName,
                SupplierTin = model.PurchaseOrder.Supplier.SupplierTin,
                SupplierAddress = model.PurchaseOrder.Supplier.SupplierAddress,
                DocumentNo = model.ReceivingReportNo!,
                Description = model.PurchaseOrder.Product.ProductName,
                Amount = model.Amount,
                VatAmount = vatAmount,
                WhtAmount = ewtAmount,
                NetPurchases = netOfVatAmount,
                CreatedBy = model.CreatedBy,
                PONo = model.PurchaseOrder.PurchaseOrderNo!,
                DueDate = model.DueDate,
            };

            await _dbContext.AddAsync(purchaseBook, cancellationToken);
            #endregion --Purchase Book Recording

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public IReadOnlyList<ReceivingReportUploadExcelFileViewModel> ParseWorksheet(
            ExcelWorksheet worksheet)
        {
            var rows = new List<ReceivingReportUploadExcelFileViewModel>();
            var rowCount = worksheet.Dimension.Rows;

            for (var row = 2; row <= rowCount; row++)
            {
                rows.Add(new ReceivingReportUploadExcelFileViewModel
                {
                    ReceivingReportNo = StringHelper.NormalizeString(worksheet.Cells[row, 21].GetValue<string>()),
                    Date = DateOnly.FromDateTime(worksheet.Cells[row, 1].GetValue<DateTime>()),
                    DueDate = DateOnly.FromDateTime(worksheet.Cells[row, 2].GetValue<DateTime>()),
                    SupplierInvoiceNumber = string.IsNullOrWhiteSpace(worksheet.Cells[row, 3].Text)
                        ? string.Empty
                        : StringHelper.NormalizeString(worksheet.Cells[row, 3].GetValue<string?>()),
                    SupplierInvoiceDate = StringHelper.NormalizeString(worksheet.Cells[row, 4].GetValue<string>()),
                    TruckOrVessels = StringHelper.NormalizeString(worksheet.Cells[row, 5].GetValue<string>()),
                    QuantityDelivered = worksheet.Cells[row, 6].GetValue<decimal>(),
                    QuantityReceived = worksheet.Cells[row, 7].GetValue<decimal>(),
                    GainOrLoss = worksheet.Cells[row, 8].GetValue<decimal>(),
                    Amount = worksheet.Cells[row, 9].GetValue<decimal>(),
                    OtherRef = string.IsNullOrWhiteSpace(worksheet.Cells[row, 10].Text)
                        ? string.Empty
                        : StringHelper.NormalizeString(worksheet.Cells[row, 10].GetValue<string?>()),
                    Remarks = StringHelper.NormalizeString(worksheet.Cells[row, 11].GetValue<string>()),
                    CreatedBy = string.IsNullOrWhiteSpace(worksheet.Cells[row, 16].Text)
                        ? string.Empty
                        : StringHelper.NormalizeString(worksheet.Cells[row, 16].GetValue<string?>()),
                    CreatedDate = worksheet.Cells[row, 17].GetValue<DateTime>(),
                    PostedBy = string.IsNullOrWhiteSpace(worksheet.Cells[row, 23].Text)
                        ? string.Empty
                        : StringHelper.NormalizeString(worksheet.Cells[row, 23].GetValue<string?>()),
                    PostedDate = worksheet.Cells[row, 24].GetValue<DateTime>(),
                    CancellationRemarks = string.IsNullOrWhiteSpace(worksheet.Cells[row, 18].Text)
                        ? string.Empty
                        : StringHelper.NormalizeString(worksheet.Cells[row, 18].GetValue<string?>()),
                    ReceivedDate = DateOnly.FromDateTime(worksheet.Cells[row, 19].GetValue<DateTime>()),
                    OriginalPOId = worksheet.Cells[row, 20].GetValue<int>(),
                    OriginalSeriesNumber = StringHelper.NormalizeString(worksheet.Cells[row, 21].GetValue<string>()),
                    OriginalDocumentId = worksheet.Cells[row, 22].GetValue<int>(),
                });
            }

            return rows;
        }

        public async Task<FindReceivingReportInDbContextDto> BuildLookupReceivingReportContextAsync(
            IEnumerable<ReceivingReportUploadExcelFileViewModel> rows,
            CancellationToken cancellationToken)
        {
            var originalPurchaseOrders = rows.Select(r => r.OriginalPOId).Distinct().ToList();
            var originalSeriesNumbers = rows.Select(r => r.OriginalSeriesNumber).Distinct().ToList();

            return new FindReceivingReportInDbContextDto
            {
                ExistingReceivingReport = await _dbContext.ReceivingReports
                    .Where(x => originalSeriesNumbers.Contains(x.OriginalSeriesNumber))
                    .GroupBy(x => x.OriginalSeriesNumber)
                    .Select(x => x.First())
                    .ToDictionaryAsync(x => x.OriginalSeriesNumber, cancellationToken),

                ExistingPurchaseOrder = await _dbContext.PurchaseOrders
                    .Where(x => originalPurchaseOrders.Contains(x.OriginalDocumentId))
                    .GroupBy(x => x.OriginalDocumentId)
                    .Select(x => x.First())
                    .ToDictionaryAsync(
                        x => x.OriginalDocumentId,
                        x => (x.PurchaseOrderId, x.PurchaseOrderNo),
                        cancellationToken),

                ExistingLogs = await _dbContext.ImportExportLogs
                    .Where(x => originalSeriesNumbers.Contains(x.DocumentNo))
                    .ToListAsync(cancellationToken)
            };
        }

        public ReceivingReport MapToReceivingReportEntity(
            ReceivingReportUploadExcelFileViewModel row,
            FindReceivingReportInDbContextDto context)
        {
            if (!context.ExistingPurchaseOrder.TryGetValue(row.OriginalPOId, out var purchaseOrderData))
            {
                throw new InvalidOperationException($"Purchase Order id missing for RR#{row.ReceivingReportNo}.");
            }

            return new ReceivingReport
            {
                ReceivingReportNo = row.ReceivingReportNo,
                Date = row.Date,
                DueDate = row.DueDate,
                SupplierInvoiceNumber = row.SupplierInvoiceNumber,
                SupplierInvoiceDate = row.SupplierInvoiceDate,
                TruckOrVessels = row.TruckOrVessels,
                QuantityDelivered = row.QuantityDelivered,
                QuantityReceived = row.QuantityReceived,
                GainOrLoss = row.GainOrLoss,
                Amount = row.Amount,
                OtherRef = row.OtherRef,
                Remarks = row.Remarks,
                CreatedBy = row.CreatedBy,
                CreatedDate = row.CreatedDate,
                PostedBy = row.PostedBy,
                PostedDate = row.PostedDate,
                CancellationRemarks = row.CancellationRemarks,
                ReceivedDate = row.ReceivedDate,
                OriginalPOId = row.OriginalPOId,
                OriginalSeriesNumber = row.OriginalSeriesNumber,
                OriginalDocumentId = row.OriginalDocumentId,

                POId = purchaseOrderData.PurchaseOrderId,
                PONo = purchaseOrderData.PurchaseOrderNo
            };
        }

        public IEnumerable<AuditTrail> AuditTrails(
            ReceivingReportUploadExcelFileViewModel row,
            string machineName)
        {
            var audits = new List<AuditTrail>();

            if (!string.IsNullOrWhiteSpace(row.CreatedBy))
            {
                audits.Add(new AuditTrail
                {
                    Username = row.CreatedBy,
                    Activity = $"Create new receiving report# {row.ReceivingReportNo}",
                    DocumentType = "Receiving Report",
                    MachineName = machineName,
                    Date = row.CreatedDate
                });
            }

            if (!string.IsNullOrWhiteSpace(row.PostedBy) && row.PostedDate != default)
            {
                audits.Add(new AuditTrail
                {
                    Username = row.PostedBy,
                    Activity = $"Posted receiving report# {row.ReceivingReportNo}",
                    DocumentType = "Receiving Report",
                    MachineName = machineName,
                    Date = row.PostedDate
                });
            }

            return audits;
        }

        public Dictionary<string, (string Original, string New)> Detect(
            ReceivingReport entity,
            ReceivingReportUploadExcelFileViewModel row,
            IReadOnlyList<ImportExportLog> logs)
        {
            var changes = new Dictionary<string, (string, string)>();

            _generalRepo.Compare(changes, logs, "ReceivingReportNo",
                StringHelper.NormalizeString(entity.ReceivingReportNo),
                row.ReceivingReportNo);

            _generalRepo.Compare(changes, logs, "Date",
                entity.Date.ToString(CS.DateOnly_Format_For_Validation),
                row.Date.ToString(CS.DateOnly_Format_For_Validation));

            _generalRepo.Compare(changes, logs, "DueDate",
                entity.DueDate.ToString(CS.DateOnly_Format_For_Validation),
                row.DueDate.ToString(CS.DateOnly_Format_For_Validation));

            _generalRepo.Compare(changes, logs, "SupplierInvoiceNumber",
                StringHelper.NormalizeString(entity.SupplierInvoiceNumber),
                row.SupplierInvoiceNumber);

            _generalRepo.Compare(changes, logs, "SupplierInvoiceDate",
                StringHelper.NormalizeString(entity.SupplierInvoiceDate),
                row.SupplierInvoiceDate);

            _generalRepo.Compare(changes, logs, "TruckOrVessels",
                StringHelper.NormalizeString(entity.TruckOrVessels),
                row.TruckOrVessels);

            _generalRepo.Compare(changes, logs, "QuantityDelivered",
                entity.QuantityDelivered.ToString(CS.Four_Decimal_Format),
                row.QuantityDelivered.ToString(CS.Four_Decimal_Format));

            _generalRepo.Compare(changes, logs, "QuantityReceived",
                entity.QuantityReceived.ToString(CS.Four_Decimal_Format),
                row.QuantityReceived.ToString(CS.Four_Decimal_Format));

            _generalRepo.Compare(changes, logs, "GainOrLoss",
                entity.GainOrLoss.ToString(CS.Four_Decimal_Format),
                row.GainOrLoss.ToString(CS.Four_Decimal_Format));

            _generalRepo.Compare(changes, logs, "Amount",
                entity.Amount.ToString(CS.Four_Decimal_Format),
                row.Amount.ToString(CS.Four_Decimal_Format));

            _generalRepo.Compare(changes, logs, "OtherRef",
                StringHelper.NormalizeString(entity.OtherRef),
                row.OtherRef);

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
                row.PostedDate.ToString(CS.DateTime_Format_For_Validation));

            _generalRepo.Compare(changes, logs, "CancellationRemarks",
                StringHelper.NormalizeString(entity.CancellationRemarks),
                row.CancellationRemarks);

            _generalRepo.Compare(changes, logs, "ReceivedDate",
                (entity.ReceivedDate ?? DateOnly.MinValue).ToString(CS.DateOnly_Format_For_Validation),
                row.ReceivedDate.ToString(CS.DateOnly_Format_For_Validation));

            _generalRepo.Compare(changes, logs, "OriginalPOId",
                entity.OriginalPOId.ToString() ?? "0",
                row.OriginalPOId.ToString());

            _generalRepo.Compare(changes, logs, "OriginalSeriesNumber",
                StringHelper.NormalizeString(entity.OriginalSeriesNumber),
                row.OriginalSeriesNumber);

            _generalRepo.Compare(changes, logs, "OriginalDocumentId",
                entity.OriginalDocumentId.ToString(),
                row.OriginalDocumentId.ToString());

            return changes;
        }

        public async Task<FindReceivingReportInDbContextDto> BuildLookupReceivingReportContextForAasAsync(
            IEnumerable<ReceivingReportUploadExcelFileViewModel> rows,
            CancellationToken cancellationToken)
        {
            var originalPurchaseOrders = rows.Select(r => r.OriginalPOId).Distinct().ToList();
            var originalSeriesNumbers = rows.Select(r => r.OriginalSeriesNumber).Distinct().ToList();

            return new FindReceivingReportInDbContextDto
            {
                ExistingReceivingReport = await _aasDbContext.ReceivingReports
                    .Where(x => originalSeriesNumbers.Contains(x.OriginalSeriesNumber))
                    .GroupBy(x => x.OriginalSeriesNumber)
                    .Select(x => x.First())
                    .ToDictionaryAsync(x => x.OriginalSeriesNumber, cancellationToken),

                ExistingPurchaseOrder = await _aasDbContext.PurchaseOrders
                    .Where(x => originalPurchaseOrders.Contains(x.OriginalDocumentId))
                    .GroupBy(x => x.OriginalDocumentId)
                    .Select(x => x.First())
                    .ToDictionaryAsync(
                        x => x.OriginalDocumentId,
                        x => (x.PurchaseOrderId, x.PurchaseOrderNo),
                        cancellationToken),

                ExistingLogs = await _dbContext.ImportExportLogs
                    .Where(x => originalSeriesNumbers.Contains(x.DocumentNo))
                    .ToListAsync(cancellationToken)
            };
        }
    }
}
