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
    public class ReceiptRepo
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly GeneralRepo _generalRepo;

        private readonly AasDbContext _aasDbContext;

        public ReceiptRepo(ApplicationDbContext dbContext, GeneralRepo generalRepo, AasDbContext aasDbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _generalRepo = generalRepo;
            _aasDbContext = aasDbContext;
        }

        public async Task<string> GenerateCRNo(CancellationToken cancellationToken = default)
        {
            var collectionReceipt = await _dbContext
                .CollectionReceipts
                .OrderByDescending(s => s.CollectionReceiptNo)
                .FirstOrDefaultAsync(cancellationToken);

            if (collectionReceipt != null)
            {
                string lastSeries = collectionReceipt.CollectionReceiptNo ?? throw new InvalidOperationException("CRNo is null pls Contact MIS Enterprise");
                string numericPart = lastSeries.Substring(2);
                int incrementedNumber = int.Parse(numericPart) + 1;

                return lastSeries.Substring(0,2) + incrementedNumber.ToString("D10");
            }
            else
            {
                return $"CR{1.ToString("D10")}";
            }
        }

        public async Task<List<CollectionReceipt>> GetCollectionReceiptsAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext
                .CollectionReceipts
                .Include(cr => cr.Customer)
                .Include(cr => cr.SalesInvoice)
                .ThenInclude(s => s!.Customer)
                .Include(cr => cr.SalesInvoice)
                .ThenInclude(s => s!.Product)
                .Include(cr => cr.ServiceInvoice)
                .ThenInclude(sv => sv!.Customer)
                .Include(cr => cr.ServiceInvoice)
                .ThenInclude(sv => sv!.Service)
                .ToListAsync(cancellationToken);
        }

        public async Task<CollectionReceipt> FindCR(int id, CancellationToken cancellationToken = default)
        {
            var collectionReceipt = await _dbContext
                .CollectionReceipts
                .Include(cr => cr.Customer)
                .Include(cr => cr.SalesInvoice)
                .ThenInclude(s => s!.Customer)
                .Include(cr => cr.SalesInvoice)
                .ThenInclude(s => s!.Product)
                .Include(cr => cr.ServiceInvoice)
                .ThenInclude(sv => sv!.Customer)
                .Include(cr => cr.ServiceInvoice)
                .ThenInclude(sv => sv!.Service)
                .FirstOrDefaultAsync(collectionReceipt => collectionReceipt.CollectionReceiptId == id, cancellationToken);

            if (collectionReceipt != null)
            {
                return collectionReceipt;
            }
            else
            {
                throw new ArgumentException("Invalid id value. The id must be greater than 0.");
            }
        }

        public async Task<int> UpdateInvoice(int id, decimal paidAmount, decimal offsetAmount, CancellationToken cancellationToken = default)
        {
            var si = await _dbContext
                .SalesInvoices
                .FirstOrDefaultAsync(si => si.SalesInvoiceId == id, cancellationToken);

            if (si != null)
            {
                decimal netDiscount = si.Amount - si.Discount;

                var total = paidAmount + offsetAmount;
                si.AmountPaid += total;
                si.Balance = netDiscount - si.AmountPaid;

                if (si.Balance == 0 && si.AmountPaid == netDiscount)
                {
                    si.IsPaid = true;
                    si.Status = "Paid";
                }
                else if (si.AmountPaid > netDiscount)
                {
                    si.IsPaid = true;
                    si.Status = "OverPaid";
                }

                return await _dbContext.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new ArgumentException("", "No record found");
            }
        }
        public async Task<int> UpdateMultipleInvoice(string[] siNo, decimal[] paidAmount, decimal offsetAmount, CancellationToken cancellationToken = default)
        {
            if (siNo.IsNullOrEmpty())
            {
                var salesInvoice = new SalesInvoice();
                for (int i = 0; i < siNo.Length; i++)
                {
                    decimal netDiscount = salesInvoice.Amount - salesInvoice.Discount;

                    var siValue = siNo[i];
                    salesInvoice = await _dbContext.SalesInvoices
                                .FirstOrDefaultAsync(p => p.SalesInvoiceNo == siValue);

                    var amountPaid = salesInvoice!.AmountPaid + paidAmount[i] + offsetAmount;

                    if (!salesInvoice.IsPaid)
                    {
                        salesInvoice.AmountPaid += salesInvoice.Amount >= amountPaid ? paidAmount[i] + offsetAmount : paidAmount[i];

                        salesInvoice.Balance = netDiscount - salesInvoice.AmountPaid;

                        if (salesInvoice.Balance == 0 && salesInvoice.AmountPaid == netDiscount)
                        {
                            salesInvoice.IsPaid = true;
                            salesInvoice.Status = "Paid";
                        }
                        else if (salesInvoice.AmountPaid > netDiscount)
                        {
                            salesInvoice.IsPaid = true;
                            salesInvoice.Status = "OverPaid";
                        }
                    }
                    else
                    {
                        continue;
                    }
                    if (salesInvoice.Amount >= amountPaid)
                    {
                        offsetAmount = 0;
                    }
                }

                return await _dbContext.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new ArgumentException("", "No record found");
            }
        }

        public async Task<int> RemoveSIPayment(int id, decimal paidAmount, decimal offsetAmount, CancellationToken cancellationToken = default)
        {
            var si = await _dbContext
                .SalesInvoices
                .FirstOrDefaultAsync(si => si.SalesInvoiceId == id, cancellationToken);

            if (si != null)
            {
                decimal netDiscount = si.Amount - si.Discount;

                var total = paidAmount + offsetAmount;
                si.AmountPaid -= total;
                si.Balance -= netDiscount - total;

                if (si.IsPaid && si.Status == "Paid" || si.IsPaid && si.Status == "OverPaid")
                {
                    si.IsPaid = false;
                    si.Status = "Pending";
                }

                return await _dbContext.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new ArgumentException("", "No record found");
            }
        }
        public async Task<int> RemoveSVPayment(int? id, decimal paidAmount, decimal offsetAmount, CancellationToken cancellationToken = default)
        {
            var sv = await _dbContext
                .ServiceInvoices
                .FirstOrDefaultAsync(si => si.ServiceInvoiceId == id, cancellationToken);

            if (sv != null)
            {
                var total = paidAmount + offsetAmount;
                sv.AmountPaid -= total;
                sv.Balance -= (sv.Total - sv.Discount) - total;

                if (sv.IsPaid && sv.Status == "Paid" || sv.IsPaid && sv.Status == "OverPaid")
                {
                    sv.IsPaid = false;
                    sv.Status = "Pending";
                }

                return await _dbContext.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new ArgumentException("", "No record found");
            }
        }

        public async Task<int> UpdateSv(int id, decimal paidAmount, decimal offsetAmount, CancellationToken cancellationToken = default)
        {
            var sv = await _dbContext
                .ServiceInvoices
                .FirstOrDefaultAsync(si => si.ServiceInvoiceId == id, cancellationToken);

            if (sv != null)
            {
                var total = paidAmount + offsetAmount;
                sv.AmountPaid += total;
                sv.Balance = (sv.Total - sv.Discount) - sv.AmountPaid;

                if (sv.Balance == 0 && sv.AmountPaid == (sv.Total - sv.Discount))
                {
                    sv.IsPaid = true;
                    sv.Status = "Paid";
                }
                else if (sv.AmountPaid > (sv.Total - sv.Discount))
                {
                    sv.IsPaid = true;
                    sv.Status = "OverPaid";
                }

                return await _dbContext.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new ArgumentException("", "No record found");
            }
        }

        public async Task<List<Offsetting>> GetOffsettingAsync(string source, string reference, CancellationToken cancellationToken = default)
        {
            var result = await _dbContext
                .Offsettings
                .Where(o => o.Source == source && o.Reference == reference)
                .ToListAsync(cancellationToken);

            if (result.Any())
            {
                return result;
            }

            return new List<Offsetting>();
        }

        public async Task RemoveMultipleSIPayment(int[] id, decimal[] paidAmount, decimal offsetAmount, CancellationToken cancellationToken = default)
        {
            var salesInvoices = await _dbContext
                .SalesInvoices
                .Where(si => id.Contains(si.SalesInvoiceId))
                .ToListAsync(cancellationToken);

            if (salesInvoices.Any())
            {
                for (int i = 0; i < paidAmount.Length; i++)
                {
                    var total = paidAmount[i] + offsetAmount;
                    salesInvoices[i].AmountPaid -= total;
                    salesInvoices[i].Balance += total;

                    if (salesInvoices[i].IsPaid && salesInvoices[i].Status == "Paid" || salesInvoices[i].IsPaid && salesInvoices[i].Status == "OverPaid")
                    {
                        salesInvoices[i].IsPaid = false;
                        salesInvoices[i].Status = "Pending";
                    }
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task LogChangesAsync(int id, Dictionary<string, (string OriginalValue, string NewValue)> changes, string? modifiedBy, string seriesNumber, string databaseName)
        {
            foreach (var change in changes)
            {
                var logReport = new ImportExportLog()
                {
                    Id = Guid.NewGuid(),
                    TableName = nameof(DynamicView.CollectionReceipt),
                    DocumentRecordId = id,
                    ColumnName = change.Key,
                    Module = "Collection Receipt",
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

        public async Task PostAsync(CollectionReceipt collectionReceipt, List<Offsetting> offsettings, CancellationToken cancellationToken = default)
        {
            var ledgers = new List<GeneralLedgerBook>();
            var accountTitlesDto = await _generalRepo.GetListOfAccountTitleDto(cancellationToken);
            var cashInBankTitle = accountTitlesDto.Find(c => c.AccountNumber == "101010100") ?? throw new ArgumentException("Account title '101010100' not found.");
            var arTradeTitle = accountTitlesDto.Find(c => c.AccountNumber == "101020100") ?? throw new ArgumentException("Account title '101020100' not found.");
            var arTradeCwt = accountTitlesDto.Find(c => c.AccountNumber == "101020200") ?? throw new ArgumentException("Account title '101020200' not found.");
            var arTradeCwv = accountTitlesDto.Find(c => c.AccountNumber == "101020300") ?? throw new ArgumentException("Account title '101020300' not found.");
            var cwt = accountTitlesDto.Find(c => c.AccountNumber == "101060400") ?? throw new ArgumentException("Account title '101060400' not found.");
            var cwv = accountTitlesDto.Find(c => c.AccountNumber == "101060600") ?? throw new ArgumentException("Account title '101060600' not found.");
            var offsetAmount = 0m;

            if (collectionReceipt.CashAmount > 0 || collectionReceipt.CheckAmount > 0 || collectionReceipt.ManagerCheckAmount > 0)
            {
                ledgers.Add(
                    new GeneralLedgerBook
                    {
                        Date = collectionReceipt.TransactionDate,
                        Reference = collectionReceipt.CollectionReceiptNo!,
                        Description = "Collection for Receivable",
                        AccountNo = cashInBankTitle.AccountNumber,
                        AccountTitle = cashInBankTitle.AccountName,
                        Debit = collectionReceipt.CashAmount + collectionReceipt.CheckAmount + collectionReceipt.ManagerCheckAmount,
                        Credit = 0,
                        CreatedBy = collectionReceipt.CreatedBy,
                        CreatedDate = collectionReceipt.CreatedDate
                    }
                );
            }

            if (collectionReceipt.EWT > 0)
            {
                ledgers.Add(
                    new GeneralLedgerBook
                    {
                        Date = collectionReceipt.TransactionDate,
                        Reference = collectionReceipt.CollectionReceiptNo!,
                        Description = "Collection for Receivable",
                        AccountNo = cwt.AccountNumber,
                        AccountTitle = cwt.AccountName,
                        Debit = collectionReceipt.EWT,
                        Credit = 0,
                        CreatedBy = collectionReceipt.CreatedBy,
                        CreatedDate = collectionReceipt.CreatedDate
                    }
                );
            }

            if (collectionReceipt.WVAT > 0)
            {
                ledgers.Add(
                    new GeneralLedgerBook
                    {
                        Date = collectionReceipt.TransactionDate,
                        Reference = collectionReceipt.CollectionReceiptNo!,
                        Description = "Collection for Receivable",
                        AccountNo = cwv.AccountNumber,
                        AccountTitle = cwv.AccountName,
                        Debit = collectionReceipt.WVAT,
                        Credit = 0,
                        CreatedBy = collectionReceipt.CreatedBy,
                        CreatedDate = collectionReceipt.CreatedDate
                    }
                );
            }

            foreach (var item in offsettings)
            {
                var account = accountTitlesDto.Find(c => c.AccountNumber == item.AccountNo) ??
                              throw new ArgumentException($"Account title '{item.AccountNo}' not found.");

                ledgers.Add(
                    new GeneralLedgerBook
                    {
                        Date = collectionReceipt.TransactionDate,
                        Reference = collectionReceipt.CollectionReceiptNo!,
                        Description = "Collection for Receivable",
                        AccountNo = account.AccountNumber,
                        AccountTitle = account.AccountName,
                        Debit = item.Amount,
                        Credit = 0,
                        CreatedBy = collectionReceipt.CreatedBy,
                        CreatedDate = collectionReceipt.CreatedDate
                    }
                );

                offsetAmount += item.Amount;
            }

            if (collectionReceipt.CashAmount > 0 || collectionReceipt.CheckAmount > 0 || collectionReceipt.ManagerCheckAmount > 0 || offsetAmount > 0)
            {
                ledgers.Add(
                    new GeneralLedgerBook
                    {
                        Date = collectionReceipt.TransactionDate,
                        Reference = collectionReceipt.CollectionReceiptNo!,
                        Description = "Collection for Receivable",
                        AccountNo = arTradeTitle.AccountNumber,
                        AccountTitle = arTradeTitle.AccountName,
                        Debit = 0,
                        Credit = collectionReceipt.CashAmount + collectionReceipt.CheckAmount + collectionReceipt.ManagerCheckAmount + offsetAmount,
                        CreatedBy = collectionReceipt.CreatedBy,
                        CreatedDate = collectionReceipt.CreatedDate,
                    }
                );
            }

            if (collectionReceipt.EWT > 0)
            {
                ledgers.Add(
                    new GeneralLedgerBook
                    {
                        Date = collectionReceipt.TransactionDate,
                        Reference = collectionReceipt.CollectionReceiptNo!,
                        Description = "Collection for Receivable",
                        AccountNo = arTradeCwt.AccountNumber,
                        AccountTitle = arTradeCwt.AccountName,
                        Debit = 0,
                        Credit = collectionReceipt.EWT,
                        CreatedBy = collectionReceipt.CreatedBy,
                        CreatedDate = collectionReceipt.CreatedDate
                    }
                );
            }

            if (collectionReceipt.WVAT > 0)
            {
                ledgers.Add(
                    new GeneralLedgerBook
                    {
                        Date = collectionReceipt.TransactionDate,
                        Reference = collectionReceipt.CollectionReceiptNo!,
                        Description = "Collection for Receivable",
                        AccountNo = arTradeCwv.AccountNumber,
                        AccountTitle = arTradeCwv.AccountName,
                        Debit = 0,
                        Credit = collectionReceipt.WVAT,
                        CreatedBy = collectionReceipt.CreatedBy,
                        CreatedDate = collectionReceipt.CreatedDate
                    }
                );
            }

            await _dbContext.GeneralLedgerBooks.AddRangeAsync(ledgers, cancellationToken);

            #region Cash Receipt Book Recording

            var crb = new List<CashReceiptBook>();

            crb.Add(
                new CashReceiptBook
                {
                    Date = collectionReceipt.TransactionDate,
                    RefNo = collectionReceipt.CollectionReceiptNo!,
                    CustomerName = collectionReceipt.SalesInvoiceId != null ? collectionReceipt.SalesInvoice!.Customer!.CustomerName : collectionReceipt.MultipleSIId != null ? collectionReceipt.Customer!.CustomerName : collectionReceipt.ServiceInvoice!.Customer!.CustomerName,
                    Bank = collectionReceipt.CheckBank ?? (collectionReceipt.ManagerCheckBank ?? "--"),
                    CheckNo = collectionReceipt.CheckNo ?? (collectionReceipt.ManagerCheckNo ?? "--"),
                    COA = $"{cashInBankTitle.AccountNumber} {cashInBankTitle.AccountName}",
                    Particulars = (collectionReceipt.SalesInvoiceId != null ? collectionReceipt.SalesInvoice!.SalesInvoiceNo : collectionReceipt.MultipleSIId != null ? string.Join(", ", collectionReceipt.MultipleSI!.Select(si => si.ToString())) : collectionReceipt.ServiceInvoice!.ServiceInvoiceNo)!,
                    Debit = collectionReceipt.CashAmount + collectionReceipt.CheckAmount + collectionReceipt.ManagerCheckAmount,
                    Credit = 0,
                    CreatedBy = collectionReceipt.CreatedBy,
                    CreatedDate = collectionReceipt.CreatedDate
                }

            );

            if (collectionReceipt.EWT > 0)
            {
                crb.Add(
                    new CashReceiptBook
                    {
                        Date = collectionReceipt.TransactionDate,
                        RefNo = collectionReceipt.CollectionReceiptNo!,
                        CustomerName = collectionReceipt.SalesInvoiceId != null ? collectionReceipt.SalesInvoice!.Customer!.CustomerName : collectionReceipt.MultipleSIId != null ? collectionReceipt.Customer!.CustomerName : collectionReceipt.ServiceInvoice!.Customer!.CustomerName,
                        Bank = collectionReceipt.CheckBank ?? (collectionReceipt.ManagerCheckBank ?? "--"),
                        CheckNo = collectionReceipt.CheckNo ?? (collectionReceipt.ManagerCheckNo ?? "--"),
                        COA = $"{cwt.AccountNumber} {cwt.AccountName}",
                        Particulars = (collectionReceipt.SalesInvoiceId != null ? collectionReceipt.SalesInvoice!.SalesInvoiceNo : collectionReceipt.MultipleSIId != null ? string.Join(", ", collectionReceipt.MultipleSI!.Select(si => si.ToString())) : collectionReceipt.ServiceInvoice!.ServiceInvoiceNo)!,
                        Debit = collectionReceipt.EWT,
                        Credit = 0,
                        CreatedBy = collectionReceipt.CreatedBy,
                        CreatedDate = collectionReceipt.CreatedDate
                    }
                );
            }

            if (collectionReceipt.WVAT > 0)
            {
                crb.Add(
                    new CashReceiptBook
                    {
                        Date = collectionReceipt.TransactionDate,
                        RefNo = collectionReceipt.CollectionReceiptNo!,
                        CustomerName = collectionReceipt.SalesInvoiceId != null ? collectionReceipt.SalesInvoice!.Customer!.CustomerName : collectionReceipt.MultipleSIId != null ? collectionReceipt.Customer!.CustomerName : collectionReceipt.ServiceInvoice!.Customer!.CustomerName,
                        Bank = collectionReceipt.CheckBank ?? (collectionReceipt.ManagerCheckBank ?? "--"),
                        CheckNo = collectionReceipt.CheckNo ?? (collectionReceipt.ManagerCheckNo ?? "--"),
                        COA = $"{cwv.AccountNumber} {cwv.AccountName}",
                        Particulars = (collectionReceipt.SalesInvoiceId != null ? collectionReceipt.SalesInvoice!.SalesInvoiceNo : collectionReceipt.MultipleSIId != null ? string.Join(", ", collectionReceipt.MultipleSI!.Select(si => si.ToString())) : collectionReceipt.ServiceInvoice!.ServiceInvoiceNo)!,
                        Debit = collectionReceipt.WVAT,
                        Credit = 0,
                        CreatedBy = collectionReceipt.CreatedBy,
                        CreatedDate = collectionReceipt.CreatedDate
                    }
                );
            }

            foreach (var item in offsettings)
            {
                var account = accountTitlesDto.Find(c => c.AccountNumber == item.AccountNo) ??
                              throw new ArgumentException($"Account title '{item.AccountNo}' not found.");

                crb.Add(
                    new CashReceiptBook
                    {
                        Date = collectionReceipt.TransactionDate,
                        RefNo = collectionReceipt.CollectionReceiptNo!,
                        CustomerName = collectionReceipt.SalesInvoiceId != null ? collectionReceipt.SalesInvoice!.Customer!.CustomerName : collectionReceipt.MultipleSIId != null ? collectionReceipt.Customer!.CustomerName : collectionReceipt.ServiceInvoice!.Customer!.CustomerName,
                        Bank = collectionReceipt.CheckBank ?? (collectionReceipt.ManagerCheckBank ?? "--"),
                        CheckNo = collectionReceipt.CheckNo ?? (collectionReceipt.ManagerCheckNo ?? "--"),
                        COA = $"{account.AccountNumber} {account.AccountName}",
                        Particulars = (collectionReceipt.SalesInvoiceId != null ? collectionReceipt.SalesInvoice!.SalesInvoiceNo : collectionReceipt.MultipleSIId != null ? string.Join(", ", collectionReceipt.MultipleSI!.Select(si => si.ToString())) : collectionReceipt.ServiceInvoice!.ServiceInvoiceNo)!,
                        Debit = item.Amount,
                        Credit = 0,
                        CreatedBy = collectionReceipt.CreatedBy,
                        CreatedDate = collectionReceipt.CreatedDate
                    }
                );
            }

            crb.Add(
                new CashReceiptBook
                {
                    Date = collectionReceipt.TransactionDate,
                    RefNo = collectionReceipt.CollectionReceiptNo!,
                    CustomerName = collectionReceipt.SalesInvoiceId != null ? collectionReceipt.SalesInvoice!.Customer!.CustomerName : collectionReceipt.MultipleSIId != null ? collectionReceipt.Customer!.CustomerName : collectionReceipt.ServiceInvoice!.Customer!.CustomerName,
                    Bank = collectionReceipt.CheckBank ?? (collectionReceipt.ManagerCheckBank ?? "--"),
                    CheckNo = collectionReceipt.CheckNo ?? (collectionReceipt.ManagerCheckNo ?? "--"),
                    COA = $"{arTradeTitle.AccountNumber} {arTradeTitle.AccountName}",
                    Particulars = (collectionReceipt.SalesInvoiceId != null ? collectionReceipt.SalesInvoice!.SalesInvoiceNo : collectionReceipt.MultipleSIId != null ? string.Join(", ", collectionReceipt.MultipleSI!.Select(si => si.ToString())) : collectionReceipt.ServiceInvoice!.ServiceInvoiceNo)!,
                    Debit = 0,
                    Credit = collectionReceipt.CashAmount + collectionReceipt.CheckAmount + collectionReceipt.ManagerCheckAmount + offsetAmount,
                    CreatedBy = collectionReceipt.CreatedBy,
                    CreatedDate = collectionReceipt.CreatedDate
                }
            );

            if (collectionReceipt.EWT > 0)
            {
                crb.Add(
                    new CashReceiptBook
                    {
                        Date = collectionReceipt.TransactionDate,
                        RefNo = collectionReceipt.CollectionReceiptNo!,
                        CustomerName = collectionReceipt.SalesInvoiceId != null ? collectionReceipt.SalesInvoice!.Customer!.CustomerName : collectionReceipt.MultipleSIId != null ? collectionReceipt.Customer!.CustomerName : collectionReceipt.ServiceInvoice!.Customer!.CustomerName,
                        Bank = collectionReceipt.CheckBank ?? (collectionReceipt.ManagerCheckBank ?? "--"),
                        CheckNo = collectionReceipt.CheckNo ?? (collectionReceipt.ManagerCheckNo ?? "--"),
                        COA = $"{arTradeCwt.AccountNumber} {arTradeCwt.AccountName}",
                        Particulars = (collectionReceipt.SalesInvoiceId != null ? collectionReceipt.SalesInvoice!.SalesInvoiceNo : collectionReceipt.MultipleSIId != null ? string.Join(", ", collectionReceipt.MultipleSI!.Select(si => si.ToString())) : collectionReceipt.ServiceInvoice!.ServiceInvoiceNo)!,
                        Debit = 0,
                        Credit = collectionReceipt.EWT,
                        CreatedBy = collectionReceipt.CreatedBy,
                        CreatedDate = collectionReceipt.CreatedDate
                    }
                );
            }

            if (collectionReceipt.WVAT > 0)
            {
                crb.Add(
                    new CashReceiptBook
                    {
                        Date = collectionReceipt.TransactionDate,
                        RefNo = collectionReceipt.CollectionReceiptNo!,
                        CustomerName = collectionReceipt.SalesInvoiceId != null ? collectionReceipt.SalesInvoice!.Customer!.CustomerName : collectionReceipt.MultipleSIId != null ? collectionReceipt.Customer!.CustomerName : collectionReceipt.ServiceInvoice!.Customer!.CustomerName,
                        Bank = collectionReceipt.CheckBank ?? (collectionReceipt.ManagerCheckBank ?? "--"),
                        CheckNo = collectionReceipt.CheckNo ?? (collectionReceipt.ManagerCheckNo ?? "--"),
                        COA = $"{arTradeCwv.AccountNumber} {arTradeCwv.AccountName}",
                        Particulars = (collectionReceipt.SalesInvoiceId != null ? collectionReceipt.SalesInvoice!.SalesInvoiceNo : collectionReceipt.MultipleSIId != null ? string.Join(", ", collectionReceipt.MultipleSI!.Select(si => si.ToString())) : collectionReceipt.ServiceInvoice!.ServiceInvoiceNo)!,
                        Debit = 0,
                        Credit = collectionReceipt.WVAT,
                        CreatedBy = collectionReceipt.CreatedBy,
                        CreatedDate = collectionReceipt.CreatedDate
                    }
                );
            }

            await _dbContext.AddRangeAsync(crb, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            #endregion

        }

        public IReadOnlyList<CollectionReceiptUploadExcelFileViewModel> ParseWorksheet(
            ExcelWorksheet worksheet)
        {
            var rows = new List<CollectionReceiptUploadExcelFileViewModel>();
            var rowCount = worksheet.Dimension.Rows;

            for (var row = 2; row <= rowCount; row++)
            {
                rows.Add(new CollectionReceiptUploadExcelFileViewModel
                {
                    CollectionReceiptNo = StringHelper.NormalizeString(worksheet.Cells[row, 30].GetValue<string>()),
                    TransactionDate = DateOnly.FromDateTime(worksheet.Cells[row, 1].GetValue<DateTime>()),
                    ReferenceNo = StringHelper.NormalizeString(worksheet.Cells[row, 2].GetValue<string>()),
                    Remarks = string.IsNullOrWhiteSpace(worksheet.Cells[row, 3].Text)
                        ? string.Empty
                        : StringHelper.NormalizeString(worksheet.Cells[row, 3].GetValue<string>()),
                    CashAmount = worksheet.Cells[row, 4].GetValue<decimal>(),
                    CheckDate = string.IsNullOrWhiteSpace(worksheet.Cells[row, 5].Text)
                        ? null
                        : DateOnly.FromDateTime(worksheet.Cells[row, 5].GetValue<DateTime>()),
                    CheckNo = string.IsNullOrWhiteSpace(worksheet.Cells[row, 6].Text)
                        ? null
                        : StringHelper.NormalizeString(worksheet.Cells[row, 6].GetValue<string>()),
                    CheckBank = string.IsNullOrWhiteSpace(worksheet.Cells[row, 7].Text)
                        ? null
                        : StringHelper.NormalizeString(worksheet.Cells[row, 7].GetValue<string>()),
                    CheckBranch = string.IsNullOrWhiteSpace(worksheet.Cells[row, 8].Text)
                        ? null
                        : StringHelper.NormalizeString(worksheet.Cells[row, 8].GetValue<string>()),
                    CheckAmount = worksheet.Cells[row, 9].GetValue<decimal>(),
                    ManagerCheckDate = string.IsNullOrWhiteSpace(worksheet.Cells[row, 10].Text)
                        ? null
                        : DateOnly.FromDateTime(worksheet.Cells[row, 10].GetValue<DateTime>()),
                    ManagerCheckNo = string.IsNullOrWhiteSpace(worksheet.Cells[row, 11].Text)
                        ? null
                        : StringHelper.NormalizeString(worksheet.Cells[row, 11].GetValue<string>()),
                    ManagerCheckBank = string.IsNullOrWhiteSpace(worksheet.Cells[row, 12].Text)
                        ? null
                        : StringHelper.NormalizeString(worksheet.Cells[row, 12].GetValue<string>()),
                    ManagerCheckBranch = string.IsNullOrWhiteSpace(worksheet.Cells[row, 13].Text)
                        ? null
                        : StringHelper.NormalizeString(worksheet.Cells[row, 13].GetValue<string>()),
                    ManagerCheckAmount = worksheet.Cells[row, 14].GetValue<decimal>(),
                    EWT = worksheet.Cells[row, 15].GetValue<decimal>(),
                    WVAT = worksheet.Cells[row, 16].GetValue<decimal>(),
                    Total = worksheet.Cells[row, 17].GetValue<decimal>(),
                    IsCertificateUpload = worksheet.Cells[row, 18].GetValue<bool>(),
                    F2306FilePath = string.IsNullOrWhiteSpace(worksheet.Cells[row, 19].Text)
                        ? string.Empty
                        : StringHelper.NormalizeString(worksheet.Cells[row, 19].GetValue<string>()),
                    F2307FilePath = string.IsNullOrWhiteSpace(worksheet.Cells[row, 20].Text)
                        ? string.Empty
                        : StringHelper.NormalizeString(worksheet.Cells[row, 20].GetValue<string>()),
                    CreatedBy = string.IsNullOrWhiteSpace(worksheet.Cells[row, 21].Text)
                        ? string.Empty
                        : StringHelper.NormalizeString(worksheet.Cells[row, 21].GetValue<string>()),
                    CreatedDate = worksheet.Cells[row, 22].GetValue<DateTime>(),
                    PostedBy = string.IsNullOrWhiteSpace(worksheet.Cells[row, 33].Text)
                        ? string.Empty
                        : StringHelper.NormalizeString(worksheet.Cells[row, 33].GetValue<string>()),
                    PostedDate = worksheet.Cells[row, 34].GetValue<DateTime>(),
                    CancellationRemarks = string.IsNullOrWhiteSpace(worksheet.Cells[row, 23].Text)
                        ? string.Empty
                        : StringHelper.NormalizeString(worksheet.Cells[row, 23].GetValue<string>()),
                    MultipleSI = string.IsNullOrWhiteSpace(worksheet.Cells[row, 24].Text)
                        ? null
                        : worksheet.Cells[row, 24].Text
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(si => si.Trim())
                            .ToArray(),

                    MultipleSIId = string.IsNullOrWhiteSpace(worksheet.Cells[row, 25].Text)
                        ? null
                        : worksheet.Cells[row, 25].Text
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(x => int.TryParse(x.Trim(), out var id) ? id : 0)
                            .ToArray(),

                    SIMultipleAmount = string.IsNullOrWhiteSpace(worksheet.Cells[row, 26].Text)
                        ? null
                        : worksheet.Cells[row, 26].Text
                            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                            .Select(x => decimal.TryParse(x.Trim(), out var amt) ? amt : 0m)
                            .ToArray(),

                    MultipleTransactionDate = string.IsNullOrWhiteSpace(worksheet.Cells[row, 27].Text)
                        ? null
                        : worksheet.Cells[row, 27].Text
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(x => DateOnly.TryParse(x.Trim(), out var d) ? d : default)
                            .ToArray(),
                    OriginalCustomerId = worksheet.Cells[row, 28].GetValue<int>(),
                    OriginalSalesInvoiceId = worksheet.Cells[row, 29].GetValue<int>(),
                    OriginalSeriesNumber = StringHelper.NormalizeString(worksheet.Cells[row, 30].GetValue<string>()),
                    OriginalServiceInvoiceId = worksheet.Cells[row, 31].GetValue<int>(),
                    OriginalDocumentId = worksheet.Cells[row, 32].GetValue<int>(),
                });
            }

            return rows;
        }

        public async Task<FindCollectionReceiptInDbContextDto> BuildLookupCollectionReceiptContextAsync(
            IEnumerable<CollectionReceiptUploadExcelFileViewModel> rows,
            CancellationToken cancellationToken)
        {

            var originalCustomerIds = rows
                .Select(r => r.OriginalCustomerId)
                .Where(x => x != 0)
                .Distinct()
                .ToList();

            var originalSalesInvoiceIds = rows
                .Select(r => r.OriginalSalesInvoiceId)
                .Where(x => x != 0)
                .Distinct()
                .ToList();

            var originalServiceInvoiceIds = rows
                .Select(r => r.OriginalServiceInvoiceId)
                .Where(x => x != 0)
                .Distinct()
                .ToList();

            var originalSeriesNumbers = rows
                .Select(r => r.OriginalSeriesNumber)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            return new FindCollectionReceiptInDbContextDto
            {
                ExistingCollectionReceipt = await _dbContext.CollectionReceipts
                    .Where(x => originalSeriesNumbers.Contains(x.OriginalSeriesNumber))
                    .GroupBy(x => x.OriginalSeriesNumber)
                    .Select(x => x.First())
                    .ToDictionaryAsync(x => x.OriginalSeriesNumber, cancellationToken),

                CustomerId = await _dbContext.Customers
                    .Where(x => originalCustomerIds.Contains(x.OriginalCustomerId))
                    .GroupBy(x => x.OriginalCustomerId)
                    .Select(x => x.First())
                    .ToDictionaryAsync(x => x.OriginalCustomerId, x => x.CustomerId, cancellationToken),

                ExistingSalesInvoice = await _dbContext.SalesInvoices
                    .Where(x => originalSalesInvoiceIds.Contains(x.OriginalDocumentId))
                    .GroupBy(x => x.OriginalDocumentId)
                    .Select(x => x.First())
                    .ToDictionaryAsync(
                        x => x.OriginalDocumentId,
                        x => (x.SalesInvoiceId, x.SalesInvoiceNo),
                        cancellationToken),

                ExistingServiceInvoice = await _dbContext.ServiceInvoices
                    .Where(x => originalServiceInvoiceIds.Contains(x.OriginalDocumentId))
                    .GroupBy(x => x.OriginalDocumentId)
                    .Select(x => x.First())
                    .ToDictionaryAsync(
                        x => x.OriginalDocumentId,
                        x => (x.ServiceInvoiceId, x.ServiceInvoiceNo),
                        cancellationToken),

                ExistingLogs = await _dbContext.ImportExportLogs
                    .Where(x => originalSeriesNumbers.Contains(x.DocumentNo))
                    .ToListAsync(cancellationToken)
            };
        }

        public async Task<FindCollectionReceiptInDbContextDto> BuildLookupCollectionReceiptContextForAasAsync(
            IEnumerable<CollectionReceiptUploadExcelFileViewModel> rows,
            CancellationToken cancellationToken)
        {
            var originalCustomerIds = rows
                .Select(r => r.OriginalCustomerId)
                .Where(x => x != 0)
                .Distinct()
                .ToList();

            var originalSalesInvoiceIds = rows
                .Select(r => r.OriginalSalesInvoiceId)
                .Where(x => x != 0)
                .Distinct()
                .ToList();

            var originalServiceInvoiceIds = rows
                .Select(r => r.OriginalServiceInvoiceId)
                .Where(x => x != 0)
                .Distinct()
                .ToList();

            var originalSeriesNumbers = rows
                .Select(r => r.OriginalSeriesNumber)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            return new FindCollectionReceiptInDbContextDto
            {
                ExistingCollectionReceipt = await _aasDbContext.CollectionReceipts
                    .Where(x => originalSeriesNumbers.Contains(x.OriginalSeriesNumber))
                    .GroupBy(x => x.OriginalSeriesNumber)
                    .Select(x => x.First())
                    .ToDictionaryAsync(x => x.OriginalSeriesNumber, cancellationToken),

                CustomerId = await _aasDbContext.Customers
                    .Where(x => originalCustomerIds.Contains(x.OriginalCustomerId))
                    .GroupBy(x => x.OriginalCustomerId)
                    .Select(x => x.First())
                    .ToDictionaryAsync(x => x.OriginalCustomerId, x => x.CustomerId, cancellationToken),

                ExistingSalesInvoice = await _aasDbContext.SalesInvoices
                    .Where(x => originalSalesInvoiceIds.Contains(x.OriginalDocumentId))
                    .GroupBy(x => x.OriginalDocumentId)
                    .Select(x => x.First())
                    .ToDictionaryAsync(
                        x => x.OriginalDocumentId,
                        x => (x.SalesInvoiceId, x.SalesInvoiceNo),
                        cancellationToken),

                ExistingServiceInvoice = await _aasDbContext.ServiceInvoices
                    .Where(x => originalServiceInvoiceIds.Contains(x.OriginalDocumentId))
                    .GroupBy(x => x.OriginalDocumentId)
                    .Select(x => x.First())
                    .ToDictionaryAsync(
                        x => x.OriginalDocumentId,
                        x => (x.ServiceInvoiceId, x.ServiceInvoiceNo),
                        cancellationToken),

                ExistingLogs = await _dbContext.ImportExportLogs
                    .Where(x => originalSeriesNumbers.Contains(x.DocumentNo))
                    .ToListAsync(cancellationToken)
            };
        }

        public CollectionReceipt MapToCollectionReceiptEntity(
            CollectionReceiptUploadExcelFileViewModel row,
            FindCollectionReceiptInDbContextDto context)
        {
            if (!context.CustomerId.TryGetValue(row.OriginalCustomerId, out var customerId))
            {
                throw new InvalidOperationException($"Customer id missing for CR#{row.CollectionReceiptNo}.");
            }

            context.ExistingSalesInvoice.TryGetValue(row.OriginalSalesInvoiceId, out var salesInvoiceData);
            context.ExistingServiceInvoice.TryGetValue(row.OriginalServiceInvoiceId, out var serviceInvoiceData);

            var hasSalesInvoice = salesInvoiceData.SalesInvoiceId > 0;
            var hasServiceInvoice = serviceInvoiceData.ServiceInvoiceId > 0;

            if (!hasSalesInvoice && !hasServiceInvoice && row.MultipleSIId?.Length < 0)
            {
                throw new InvalidOperationException($"Id is missing for CR#{row.CollectionReceiptNo}. No selected services, single or multiple invoices.");
            }

            return new CollectionReceipt
            {
                CollectionReceiptNo = row.CollectionReceiptNo,
                TransactionDate = row.TransactionDate,
                ReferenceNo = row.ReferenceNo,
                Remarks = row.Remarks,
                CashAmount = row.CashAmount,
                CheckDate = row.CheckDate,
                CheckNo = row.CheckNo,
                CheckBank = row.CheckBank,
                CheckBranch = row.CheckBranch,
                CheckAmount = row.CheckAmount,
                ManagerCheckDate = row.ManagerCheckDate,
                ManagerCheckNo = row.ManagerCheckNo,
                ManagerCheckBank = row.ManagerCheckBank,
                ManagerCheckBranch = row.ManagerCheckBranch,
                ManagerCheckAmount = row.ManagerCheckAmount,
                EWT = row.EWT,
                WVAT = row.WVAT,
                Total = row.Total,
                IsCertificateUpload = row.IsCertificateUpload,
                F2306FilePath = row.F2306FilePath,
                F2307FilePath = row.F2307FilePath,
                CreatedBy = row.CreatedBy,
                CreatedDate = row.CreatedDate,
                PostedBy = row.PostedBy,
                PostedDate = row.PostedDate,
                CancellationRemarks = row.CancellationRemarks,
                MultipleSI = row.MultipleSI,
                MultipleSIId = row.MultipleSIId,
                SIMultipleAmount = row.SIMultipleAmount,
                MultipleTransactionDate = row.MultipleTransactionDate,
                OriginalCustomerId = row.OriginalCustomerId,
                OriginalSalesInvoiceId = row.OriginalSalesInvoiceId,
                OriginalSeriesNumber = row.OriginalSeriesNumber,
                OriginalServiceInvoiceId = row.OriginalServiceInvoiceId,
                OriginalDocumentId = row.OriginalDocumentId,

                CustomerId = customerId,
                SalesInvoiceId = salesInvoiceData.SalesInvoiceId == 0 ? null : salesInvoiceData.SalesInvoiceId,
                SINo = salesInvoiceData.SalesInvoiceNo == string.Empty ? null : salesInvoiceData.SalesInvoiceNo,
                ServiceInvoiceId = serviceInvoiceData.ServiceInvoiceId == 0 ? null : serviceInvoiceData.ServiceInvoiceId,
                SVNo = serviceInvoiceData.ServiceInvoiceNo == string.Empty ? null : serviceInvoiceData.ServiceInvoiceNo
            };
        }

        public IEnumerable<AuditTrail> AuditTrails(
            CollectionReceiptUploadExcelFileViewModel row,
            string machineName)
        {
            var audits = new List<AuditTrail>();

            if (!string.IsNullOrWhiteSpace(row.CreatedBy))
            {
                audits.Add(new AuditTrail
                {
                    Username = row.CreatedBy,
                    Activity = $"Create new invoice# {row.CollectionReceiptNo}",
                    DocumentType = "Collection Receipt",
                    MachineName = machineName,
                    Date = row.CreatedDate
                });
            }

            if (!string.IsNullOrWhiteSpace(row.PostedBy) && row.PostedDate != default)
            {
                audits.Add(new AuditTrail
                {
                    Username = row.PostedBy,
                    Activity = $"Posted invoice# {row.CollectionReceiptNo}",
                    DocumentType = "Collection Receipt",
                    MachineName = machineName,
                    Date = row.PostedDate
                });
            }

            return audits;
        }

        public Dictionary<string, (string Original, string New)> Detect(
            CollectionReceipt entity,
            CollectionReceiptUploadExcelFileViewModel row,
            IReadOnlyList<ImportExportLog> logs)
        {
            var changes = new Dictionary<string, (string, string)>();

            _generalRepo.Compare(changes, logs, "CollectionReceiptNo",
                StringHelper.NormalizeString(entity.CollectionReceiptNo),
                row.CollectionReceiptNo);

            _generalRepo.Compare(changes, logs, "TransactionDate",
                entity.TransactionDate.ToString(CS.DateOnly_Format_For_Validation),
                row.TransactionDate.ToString(CS.DateOnly_Format_For_Validation));

            _generalRepo.Compare(changes, logs, "ReferenceNo",
                StringHelper.NormalizeString(entity.ReferenceNo),
                row.ReferenceNo);

            _generalRepo.Compare(changes, logs, "Remarks",
                StringHelper.NormalizeString(entity.Remarks),
                row.Remarks);

            _generalRepo.Compare(changes, logs, "CashAmount",
                entity.CashAmount.ToString(CS.Four_Decimal_Format),
                row.CashAmount.ToString(CS.Four_Decimal_Format));

            _generalRepo.Compare(changes, logs, "CheckDate",
                entity.CheckDate?.ToString(CS.DateOnly_Format_For_Validation)  ?? string.Empty,
                row.CheckDate?.ToString(CS.DateOnly_Format_For_Validation)  ?? string.Empty);

            _generalRepo.Compare(changes, logs, "CheckNo",
                StringHelper.NormalizeString(entity.CheckNo),
                StringHelper.NormalizeString(row.CheckNo));

            _generalRepo.Compare(changes, logs, "CheckBank",
                StringHelper.NormalizeString(entity.CheckBank),
                StringHelper.NormalizeString(row.CheckBank));

            _generalRepo.Compare(changes, logs, "CheckBranch",
                StringHelper.NormalizeString(entity.CheckBranch),
                StringHelper.NormalizeString(row.CheckBranch));

            _generalRepo.Compare(changes, logs, "CheckAmount",
                entity.CheckAmount.ToString(CS.Four_Decimal_Format),
                row.CheckAmount.ToString(CS.Four_Decimal_Format));

            _generalRepo.Compare(changes, logs, "ManagerCheckDate",
                entity.ManagerCheckDate?.ToString(CS.DateOnly_Format_For_Validation) ?? string.Empty,
                row.ManagerCheckDate?.ToString(CS.DateOnly_Format_For_Validation)  ?? string.Empty);

            _generalRepo.Compare(changes, logs, "ManagerCheckNo",
                StringHelper.NormalizeString(entity.ManagerCheckNo),
                StringHelper.NormalizeString(row.ManagerCheckNo));

            _generalRepo.Compare(changes, logs, "ManagerCheckBank",
                StringHelper.NormalizeString(entity.ManagerCheckBank),
                StringHelper.NormalizeString(row.ManagerCheckBank));

            _generalRepo.Compare(changes, logs, "ManagerCheckBranch",
                StringHelper.NormalizeString(entity.ManagerCheckBranch),
                StringHelper.NormalizeString(row.ManagerCheckBranch));

            _generalRepo.Compare(changes, logs, "ManagerCheckAmount",
                entity.ManagerCheckAmount.ToString(CS.Four_Decimal_Format),
                row.ManagerCheckAmount.ToString(CS.Four_Decimal_Format));

            _generalRepo.Compare(changes, logs, "EWT",
                entity.EWT.ToString(CS.Four_Decimal_Format),
                row.EWT.ToString(CS.Four_Decimal_Format));

            _generalRepo.Compare(changes, logs, "WVAT",
                entity.WVAT.ToString(CS.Four_Decimal_Format),
                row.WVAT.ToString(CS.Four_Decimal_Format));

            _generalRepo.Compare(changes, logs, "Total",
                entity.Total.ToString(CS.Four_Decimal_Format),
                row.Total.ToString(CS.Four_Decimal_Format));

            _generalRepo.Compare(changes, logs, "IsCertificateUpload",
                StringHelper.NormalizeString(entity.IsCertificateUpload.ToString()),
                StringHelper.NormalizeString(row.IsCertificateUpload.ToString()));

            _generalRepo.Compare(changes, logs, "F2306FilePath",
                StringHelper.NormalizeString(entity.F2306FilePath),
                row.F2306FilePath);

            _generalRepo.Compare(changes, logs, "F2307FilePath",
                StringHelper.NormalizeString(entity.F2307FilePath),
                row.F2307FilePath);

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
                entity.PostedDate?.ToString(CS.DateTime_Format_For_Validation)  ?? string.Empty,
                row.PostedDate.ToString(CS.DateTime_Format_For_Validation));

            _generalRepo.Compare(changes, logs, "CancellationRemarks",
                StringHelper.NormalizeString(entity.CancellationRemarks),
                row.CancellationRemarks);

            _generalRepo.Compare(changes, logs, "MultipleSI",
                StringHelper.NormalizeString(entity.MultipleSI?.ToString()),
                StringHelper.NormalizeString(row.MultipleSI?.ToString()));

            _generalRepo.Compare(changes, logs, "MultipleSIId",
                StringHelper.NormalizeString(entity.MultipleSIId?.ToString()),
                StringHelper.NormalizeString(row.MultipleSIId?.ToString()));

            _generalRepo.Compare(changes, logs, "SIMultipleAmount",
                StringHelper.NormalizeString(entity.SIMultipleAmount?.ToString()),
                StringHelper.NormalizeString(row.SIMultipleAmount?.ToString()));

            _generalRepo.Compare(changes, logs, "MultipleTransactionDate",
                StringHelper.NormalizeString(entity.MultipleTransactionDate?.ToString()),
                StringHelper.NormalizeString(row.MultipleTransactionDate?.ToString()));

            _generalRepo.Compare(changes, logs, "OriginalCustomerId",
                StringHelper.NormalizeString(entity.OriginalCustomerId.ToString()),
                StringHelper.NormalizeString(row.OriginalCustomerId.ToString()));

            _generalRepo.Compare(changes, logs, "OriginalSalesInvoiceId",
                StringHelper.NormalizeString(entity.OriginalSalesInvoiceId.ToString()),
                StringHelper.NormalizeString(row.OriginalSalesInvoiceId.ToString()));

            _generalRepo.Compare(changes, logs, "OriginalSeriesNumber",
                StringHelper.NormalizeString(entity.OriginalSeriesNumber),
                row.OriginalSeriesNumber);

            _generalRepo.Compare(changes, logs, "OriginalServiceInvoiceId",
                StringHelper.NormalizeString(entity.OriginalServiceInvoiceId.ToString()),
                StringHelper.NormalizeString(row.OriginalServiceInvoiceId.ToString()));

            _generalRepo.Compare(changes, logs, "OriginalDocumentId",
                StringHelper.NormalizeString(entity.OriginalDocumentId.ToString()),
                StringHelper.NormalizeString(row.OriginalDocumentId.ToString()));

            return changes;
        }

        public async Task CheckSalesInvoiceAmountsAsync(
            CollectionReceiptUploadExcelFileViewModel row,
            IReadOnlyList<ImportExportLog> logs,
            CancellationToken cancellationToken)
        {
            var changes = new Dictionary<string, (string, string)>();

            // ===== MULTIPLE SI CHECK =====
            if (row.MultipleSI?.Length > 0 &&
                row.SIMultipleAmount?.Length > 0 &&
                row.MultipleSI != null &&
                row.SIMultipleAmount != null)
            {
                var max = Math.Min(row.MultipleSI.Length, row.SIMultipleAmount.Length);

                for (int i = 0; i < max; i++)
                {
                    var salesInvoiceNo = row.MultipleSI[i];

                    if (string.IsNullOrWhiteSpace(salesInvoiceNo))
                    {
                        continue;
                    }

                    var originalDecimal = row.SIMultipleAmount[i];

                    var salesInvoice = await _dbContext.SalesInvoices
                        .FirstOrDefaultAsync(x => x.OriginalSeriesNumber == salesInvoiceNo, cancellationToken);

                    if (salesInvoice == null)
                    {
                        continue;
                    }

                    var salesInvoiceAmount = salesInvoice.Amount;

                    if (Math.Round(originalDecimal, 2) > Math.Round(salesInvoiceAmount, 2))
                    {
                        _generalRepo.Compare(
                            changes,
                            logs,
                            $"MultipleSalesInvoiceAmount({salesInvoice.SalesInvoiceNo})",
                            originalDecimal.ToString(CS.Two_Decimal_Format),
                            salesInvoiceAmount.ToString(CS.Two_Decimal_Format));
                    }
                }
            }

            // ===== SINGLE SI CHECK =====
            if (row.OriginalSalesInvoiceId != 0)
            {
                var originalValue =
                    row.CashAmount != 0 ? row.CashAmount :
                    row.CheckAmount != 0 ? row.CheckAmount :
                    row.ManagerCheckAmount;

                var salesInvoice = await _dbContext.SalesInvoices
                    .FirstOrDefaultAsync(x => x.OriginalDocumentId == row.OriginalSalesInvoiceId, cancellationToken);

                if (salesInvoice != null)
                {
                    var salesInvoiceAmount = salesInvoice.Amount;

                    if (Math.Round(originalValue, 2) > Math.Round(salesInvoiceAmount, 2))
                    {
                        _generalRepo.Compare(
                            changes,
                            logs,
                            $"SingleSalesInvoiceAmount({salesInvoice.SalesInvoiceNo})",
                            originalValue.ToString(CS.Two_Decimal_Format),
                            salesInvoiceAmount.ToString(CS.Two_Decimal_Format));
                    }
                }
            }

            if (changes.Any())
            {
                await LogChangesAsync(
                    row.OriginalDocumentId,
                    changes,
                    row.CreatedBy,
                    row.OriginalSeriesNumber,
                    "IBS-RCD");
            }
        }
    }
}
