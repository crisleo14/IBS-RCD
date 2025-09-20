using System.Drawing;
using Accounting_System.Data;
using Accounting_System.Utility;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace Accounting_System.Controllers
{
    public class ImportExportLogsController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly AasDbContext _aasDbContext;

        public ImportExportLogsController(ApplicationDbContext dbContext, AasDbContext aasDbContext)
        {
            _dbContext = dbContext;
            _aasDbContext = aasDbContext;
        }
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var importExportLogs = await _dbContext.ImportExportLogs
                .Where(iel => iel.Action == string.Empty).ToListAsync(cancellationToken);

            return View(importExportLogs);
        }

        public async Task<IActionResult> ImportAction(Guid id, string procedure, string tableName, string columnName, CancellationToken cancellationToken)
        {
            var importRecord = await _dbContext.ImportExportLogs.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (importRecord == null)
            {
                return NotFound();
            }

            if (procedure == "Modify" && importRecord.DatabaseName == "IBS-RCD")
            {
                #region -- Accounts Receivable --

                #region -- Sales Invoice --

                if (tableName == "SalesInvoice")
                {
                    var existingSalesInvoice = await _dbContext.SalesInvoices.FirstOrDefaultAsync(si => si.OriginalDocumentId == importRecord.DocumentRecordId, cancellationToken);

                    if (existingSalesInvoice == null || importRecord.AdjustedValue == null)
                    {
                        return NotFound();
                    }

                    if (columnName == "SINo")
                    {
                        existingSalesInvoice.SalesInvoiceNo = importRecord.AdjustedValue;
                    }
                    if (columnName == "OriginalCustomerId")
                    {

                        existingSalesInvoice.OriginalCustomerId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "OriginalProductId")
                    {
                        existingSalesInvoice.OriginalProductId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "OtherRefNo")
                    {
                        existingSalesInvoice.OtherRefNo = importRecord.AdjustedValue;
                    }
                    if (columnName == "Quantity")
                    {
                        existingSalesInvoice.Quantity = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "UnitPrice")
                    {
                        existingSalesInvoice.UnitPrice = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Amount")
                    {
                        existingSalesInvoice.Amount = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Remarks")
                    {
                        existingSalesInvoice.Remarks = importRecord.AdjustedValue;
                    }
                    if (columnName == "Status")
                    {
                        existingSalesInvoice.Status = importRecord.AdjustedValue;
                    }
                    if (columnName == "TransactionDate")
                    {
                        existingSalesInvoice.TransactionDate = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                    }
                    if (columnName == "Discount")
                    {
                        existingSalesInvoice.Discount = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "DueDate")
                    {
                        existingSalesInvoice.DueDate = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                    }
                    if (columnName == "CreatedBy")
                    {
                        existingSalesInvoice.CreatedBy = importRecord.AdjustedValue;
                    }
                    if (columnName == "CreatedDate")
                    {
                        existingSalesInvoice.CreatedDate = DateTime.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd hh:mm:ss.ffffff", null);
                    }
                    if (columnName == "CancellationRemarks")
                    {
                        existingSalesInvoice.CancellationRemarks = importRecord.AdjustedValue;
                    }
                    if (columnName == "OriginalSeriesNumber")
                    {
                        existingSalesInvoice.OriginalSeriesNumber = importRecord.AdjustedValue;
                    }
                    if (columnName == "OriginalDocumentId")
                    {
                        existingSalesInvoice.OriginalDocumentId = int.Parse(importRecord.AdjustedValue);
                    }
                }

                #endregion -- Sales Invoice --

                #region -- Service Invoice --

                if (tableName == "ServiceInvoice")
                {
                    var existingServiceInvoice = await _dbContext.ServiceInvoices.FirstOrDefaultAsync(sv => sv.OriginalDocumentId == importRecord.DocumentRecordId, cancellationToken);

                    if (existingServiceInvoice == null || importRecord.AdjustedValue == null)
                    {
                        return NotFound();
                    }

                    if (columnName == "SVNo")
                    {
                        existingServiceInvoice.ServiceInvoiceNo = importRecord.AdjustedValue;
                    }
                    if (columnName == "DueDate")
                    {
                        existingServiceInvoice.DueDate = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                    }
                    if (columnName == "Period")
                    {
                        existingServiceInvoice.Period = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                    }
                    if (columnName == "Amount")
                    {
                        existingServiceInvoice.Amount = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Total")
                    {
                        existingServiceInvoice.Total = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Discount")
                    {
                        existingServiceInvoice.Discount = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "CurrentAndPreviousAmount")
                    {
                        existingServiceInvoice.CurrentAndPreviousAmount = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "UnearnedAmount")
                    {
                        existingServiceInvoice.UnearnedAmount = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Status")
                    {
                        existingServiceInvoice.Status = importRecord.AdjustedValue;
                    }
                    if (columnName == "Instructions")
                    {
                        existingServiceInvoice.Instructions = importRecord.AdjustedValue;
                    }
                    if (columnName == "CreatedBy")
                    {
                        existingServiceInvoice.CreatedBy = importRecord.AdjustedValue;
                    }
                    if (columnName == "CreatedDate")
                    {
                        existingServiceInvoice.CreatedDate = DateTime.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd hh:mm:ss.ffffff", null);
                    }
                    if (columnName == "CancellationRemarks")
                    {
                        existingServiceInvoice.CancellationRemarks = importRecord.AdjustedValue;
                    }
                    if (columnName == "OriginalCustomerId")
                    {
                        existingServiceInvoice.OriginalCustomerId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "OriginalSeriesNumber")
                    {
                        existingServiceInvoice.OriginalSeriesNumber = importRecord.AdjustedValue;
                    }
                    if (columnName == "OriginalServicesId")
                    {
                        existingServiceInvoice.OriginalServicesId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "OriginalDocumentId")
                    {
                        existingServiceInvoice.OriginalDocumentId = int.Parse(importRecord.AdjustedValue);
                    }
                }

                #endregion -- Service Invoice --

                #region -- Collection Receipt --

                    if (tableName == "CollectionReceipt")
                    {
                        var existingCollectionReceipt = await _dbContext.CollectionReceipts.FirstOrDefaultAsync(cr => cr.OriginalDocumentId == importRecord.DocumentRecordId, cancellationToken);

                        if (existingCollectionReceipt == null || importRecord.AdjustedValue == null)
                        {
                            return NotFound();
                        }

                        if (columnName == "CRNo")
                        {
                            existingCollectionReceipt.CollectionReceiptNo = importRecord.AdjustedValue;
                        }
                        if (columnName == "TransactionDate")
                        {
                            existingCollectionReceipt.TransactionDate = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                        }
                        if (columnName == "ReferenceNo")
                        {
                            existingCollectionReceipt.ReferenceNo = importRecord.AdjustedValue;
                        }
                        if (columnName == "Remarks")
                        {
                            existingCollectionReceipt.Remarks = importRecord.AdjustedValue;
                        }
                        if (columnName == "CashAmount")
                        {
                            existingCollectionReceipt.CashAmount = decimal.Parse(importRecord.AdjustedValue);
                        }
                        if (columnName == "CheckDate")
                        {
                            existingCollectionReceipt.CheckDate = DateOnly.Parse(importRecord.AdjustedValue);
                        }
                        if (columnName == "CheckNo")
                        {
                            existingCollectionReceipt.CheckNo = importRecord.AdjustedValue;
                        }
                        if (columnName == "CheckBank")
                        {
                            existingCollectionReceipt.CheckBank = importRecord.AdjustedValue;
                        }
                        if (columnName == "CheckBranch")
                        {
                            existingCollectionReceipt.CheckBranch = importRecord.AdjustedValue;
                        }
                        if (columnName == "CheckAmount")
                        {
                            existingCollectionReceipt.CheckAmount = decimal.Parse(importRecord.AdjustedValue);
                        }
                        if (columnName == "ManagerCheckDate")
                        {
                            existingCollectionReceipt.ManagerCheckDate = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                        }
                        if (columnName == "ManagerCheckNo")
                        {
                            existingCollectionReceipt.ManagerCheckNo = importRecord.AdjustedValue;
                        }
                        if (columnName == "ManagerCheckBank")
                        {
                            existingCollectionReceipt.ManagerCheckBank = importRecord.AdjustedValue;
                        }
                        if (columnName == "ManagerCheckBranch")
                        {
                            existingCollectionReceipt.ManagerCheckBranch = importRecord.AdjustedValue;
                        }
                        if (columnName == "ManagerCheckAmount")
                        {
                            existingCollectionReceipt.ManagerCheckAmount = decimal.Parse(importRecord.AdjustedValue);
                        }
                        if (columnName == "EWT")
                        {
                            existingCollectionReceipt.EWT = decimal.Parse(importRecord.AdjustedValue);
                        }
                        if (columnName == "WVAT")
                        {
                            existingCollectionReceipt.WVAT = decimal.Parse(importRecord.AdjustedValue);
                        }
                        if (columnName == "Total")
                        {
                            existingCollectionReceipt.Total = decimal.Parse(importRecord.AdjustedValue);
                        }
                        if (columnName == "IsCertificateUpload")
                        {
                            existingCollectionReceipt.IsCertificateUpload = bool.Parse(importRecord.AdjustedValue);
                        }
                        if (columnName == "F2306FilePath")
                        {
                            existingCollectionReceipt.F2306FilePath = importRecord.AdjustedValue;
                        }
                        if (columnName == "F2307FilePath")
                        {
                            existingCollectionReceipt.F2307FilePath = importRecord.AdjustedValue;
                        }
                        if (columnName == "CreatedBy")
                        {
                            existingCollectionReceipt.CreatedBy = importRecord.AdjustedValue;
                        }
                        if (columnName == "CreatedDate")
                        {
                            existingCollectionReceipt.CreatedDate = DateTime.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd hh:mm:ss.ffffff", null);
                        }
                        if (columnName == "CancellationRemarks")
                        {
                            existingCollectionReceipt.CancellationRemarks = importRecord.AdjustedValue;
                        }
                        if (columnName == "MultipleSI")
                        {
                            existingCollectionReceipt.MultipleSI = [importRecord.AdjustedValue];
                        }
                        if (columnName == "MultipleSIId")
                        {
                            existingCollectionReceipt.MultipleSIId = [int.Parse(importRecord.AdjustedValue)];
                        }
                        if (columnName == "SIMultipleAmount")
                        {
                            existingCollectionReceipt.SIMultipleAmount = [decimal.Parse(importRecord.AdjustedValue)];
                        }
                        if (columnName == "MultipleTransactionDate")
                        {
                            existingCollectionReceipt.MultipleTransactionDate = [DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd")];
                        }
                        if (columnName == "OriginalCustomerId")
                        {
                            existingCollectionReceipt.OriginalCustomerId = int.Parse(importRecord.AdjustedValue);
                        }
                        if (columnName == "OriginalSalesInvoiceId")
                        {
                            existingCollectionReceipt.OriginalSalesInvoiceId = int.Parse(importRecord.AdjustedValue);
                        }
                        if (columnName == "OriginalSeriesNumber")
                        {
                            existingCollectionReceipt.OriginalSeriesNumber = importRecord.AdjustedValue;
                        }
                        if (columnName == "OriginalServiceInvoiceId")
                        {
                            existingCollectionReceipt.OriginalServiceInvoiceId = int.Parse(importRecord.AdjustedValue);
                        }
                        if (columnName == "OriginalDocumentId")
                        {
                            existingCollectionReceipt.OriginalDocumentId = int.Parse(importRecord.AdjustedValue);
                        }
                        if (columnName.Contains("SingleSalesInvoiceAmount"))
                        {
                            if (existingCollectionReceipt.CashAmount != 0)
                            {
                                existingCollectionReceipt.CashAmount = decimal.Parse(importRecord.AdjustedValue) - (existingCollectionReceipt.EWT + existingCollectionReceipt.WVAT);
                            }
                            if (existingCollectionReceipt.CheckAmount != 0)
                            {
                                existingCollectionReceipt.CheckAmount = decimal.Parse(importRecord.AdjustedValue) - (existingCollectionReceipt.EWT + existingCollectionReceipt.WVAT);
                            }
                            if (existingCollectionReceipt.ManagerCheckAmount != 0)
                            {
                                existingCollectionReceipt.ManagerCheckAmount = decimal.Parse(importRecord.AdjustedValue) - (existingCollectionReceipt.EWT + existingCollectionReceipt.WVAT);
                            }
                            existingCollectionReceipt.Total = decimal.Parse(importRecord.AdjustedValue);
                        }
                        if (columnName.Contains("MultipleSalesInvoiceAmount"))
                        {
                            int start = columnName.IndexOf('(') + 1;
                            int end = columnName.IndexOf(')', start);
                            string salesInvoiceNo = columnName.Substring(start, end - start);
                            int index = Array.FindIndex(
                                existingCollectionReceipt.MultipleSI,
                                x => x != null && x.Contains(salesInvoiceNo, StringComparison.OrdinalIgnoreCase)
                            );
                            var MultipleSalesInvoiceAmount = existingCollectionReceipt.SIMultipleAmount[index];
                            var ajustedValue = decimal.Parse(importRecord.AdjustedValue);
                            var siMultipleAmount = MultipleSalesInvoiceAmount - ajustedValue;
                            var totalAmount = existingCollectionReceipt.Total - siMultipleAmount;

                            existingCollectionReceipt.SIMultipleAmount[index] = ajustedValue;
                            existingCollectionReceipt.Total = totalAmount;

                            if (existingCollectionReceipt.CashAmount != 0)
                            {
                                existingCollectionReceipt.CashAmount = totalAmount - (existingCollectionReceipt.EWT + existingCollectionReceipt.WVAT);
                            }
                            if (existingCollectionReceipt.CheckAmount != 0)
                            {
                                existingCollectionReceipt.CheckAmount = totalAmount - (existingCollectionReceipt.EWT + existingCollectionReceipt.WVAT);
                            }
                            if (existingCollectionReceipt.ManagerCheckAmount != 0)
                            {
                                existingCollectionReceipt.ManagerCheckAmount = totalAmount - (existingCollectionReceipt.EWT + existingCollectionReceipt.WVAT);
                            }
                        }
                    }

                #endregion -- Collection Receipt --

                #region -- Offsettings --

                if (tableName == "Offsetting")
                {
                    var existingCollectionReceipt = await _dbContext.CollectionReceipts.FirstOrDefaultAsync(cr => cr.OriginalDocumentId == importRecord.DocumentRecordId, cancellationToken);

                    if (existingCollectionReceipt == null)
                    {
                        return NotFound();
                    }

                    var existingOffset = await _dbContext.Offsettings.FirstOrDefaultAsync(offset => offset.Reference == existingCollectionReceipt.OriginalSeriesNumber, cancellationToken);

                    if (existingOffset == null || importRecord.AdjustedValue == null)
                    {
                        return NotFound();
                    }

                    if (columnName == "AccountNo")
                    {
                        existingOffset.AccountNo = importRecord.AdjustedValue;
                    }
                    if (columnName == "Reference")
                    {
                        existingOffset.Reference = importRecord.AdjustedValue;
                    }
                    if (columnName == "IsRemoved")
                    {
                        existingOffset.IsRemoved = bool.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Amount")
                    {
                        existingOffset.Amount = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "CreatedBy")
                    {
                        existingOffset.CreatedBy = importRecord.AdjustedValue;
                    }
                    if (columnName == "CreatedDate")
                    {
                        existingOffset.CreatedDate = DateTime.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd hh:mm:ss.ffffff", null);
                    }
                    if (columnName == "AccountTitle")
                    {
                        existingOffset.AccountTitle = importRecord.AdjustedValue;
                    }
                    if (columnName == "Source")
                    {
                        existingOffset.Source = importRecord.AdjustedValue;
                    }
                }

                #endregion -- Offsettings --

                #region -- Debit Memo --

                if (tableName == "DebitMemo")
                {
                    var existingDebitMemo = await _dbContext.DebitMemos.FirstOrDefaultAsync(dm => dm.OriginalDocumentId == importRecord.DocumentRecordId, cancellationToken);

                    if (existingDebitMemo == null || importRecord.AdjustedValue == null)
                    {
                        return NotFound();
                    }

                    if (columnName == "DMNo")
                    {
                        existingDebitMemo.DebitMemoNo = importRecord.AdjustedValue;
                    }
                    if (columnName == "TransactionDate")
                    {
                        existingDebitMemo.TransactionDate = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                    }
                    if (columnName == "DebitAmount")
                    {
                        existingDebitMemo.DebitAmount = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Description")
                    {
                        existingDebitMemo.Description = importRecord.AdjustedValue;
                    }
                    if (columnName == "AdjustedPrice")
                    {
                        existingDebitMemo.AdjustedPrice = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Quantity")
                    {
                        existingDebitMemo.Quantity = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Source")
                    {
                        existingDebitMemo.Source = importRecord.AdjustedValue;
                    }
                    if (columnName == "Remarks")
                    {
                        existingDebitMemo.Remarks = importRecord.AdjustedValue;
                    }
                    if (columnName == "Period")
                    {
                        existingDebitMemo.Period = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                    }
                    if (columnName == "Amount")
                    {
                        existingDebitMemo.Amount = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "CurrentAndPreviousAmount")
                    {
                        existingDebitMemo.CurrentAndPreviousAmount = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "UnearnedAmount")
                    {
                        existingDebitMemo.UnearnedAmount = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "ServicesId")
                    {
                        existingDebitMemo.ServicesId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "CreatedBy")
                    {
                        existingDebitMemo.CreatedBy = importRecord.AdjustedValue;
                    }
                    if (columnName == "CreatedDate")
                    {
                        existingDebitMemo.CreatedDate = DateTime.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd hh:mm:ss.ffffff", null);
                    }
                    if (columnName == "CancellationRemarks")
                    {
                        existingDebitMemo.CancellationRemarks = importRecord.AdjustedValue;
                    }
                    if (columnName == "OriginalSalesInvoiceId")
                    {
                        existingDebitMemo.OriginalSalesInvoiceId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "OriginalSeriesNumber")
                    {
                        existingDebitMemo.OriginalSeriesNumber = importRecord.AdjustedValue;
                    }
                    if (columnName == "OriginalServiceInvoiceId")
                    {
                        existingDebitMemo.OriginalServiceInvoiceId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "OriginalDocumentId")
                    {
                        existingDebitMemo.OriginalDocumentId = int.Parse(importRecord.AdjustedValue);
                    }
                }

                #endregion -- Debit Memo --

                #region -- Credit Memo --

                if (tableName == "CreditMemo")
                {
                    var existingCreditMemo = await _dbContext.CreditMemos.FirstOrDefaultAsync(cm => cm.OriginalDocumentId == importRecord.DocumentRecordId, cancellationToken);

                    if (existingCreditMemo == null || importRecord.AdjustedValue == null)
                    {
                        return NotFound();
                    }

                    if (columnName == "CMNo")
                    {
                        existingCreditMemo.CreditMemoNo = importRecord.AdjustedValue;
                    }
                    if (columnName == "TransactionDate")
                    {
                        existingCreditMemo.TransactionDate = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                    }
                    if (columnName == "CreditAmount")
                    {
                        existingCreditMemo.CreditAmount = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Description")
                    {
                        existingCreditMemo.Description = importRecord.AdjustedValue;
                    }
                    if (columnName == "AdjustedPrice")
                    {
                        existingCreditMemo.AdjustedPrice = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Quantity")
                    {
                        existingCreditMemo.Quantity = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Source")
                    {
                        existingCreditMemo.Source = importRecord.AdjustedValue;
                    }
                    if (columnName == "Remarks")
                    {
                        existingCreditMemo.Remarks = importRecord.AdjustedValue;
                    }
                    if (columnName == "Period")
                    {
                        existingCreditMemo.Period = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                    }
                    if (columnName == "Amount")
                    {
                        existingCreditMemo.Amount = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "CurrentAndPreviousAmount")
                    {
                        existingCreditMemo.CurrentAndPreviousAmount = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "UnearnedAmount")
                    {
                        existingCreditMemo.UnearnedAmount = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "ServicesId")
                    {
                        existingCreditMemo.ServicesId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "CreatedBy")
                    {
                        existingCreditMemo.CreatedBy = importRecord.AdjustedValue;
                    }
                    if (columnName == "CreatedDate")
                    {
                        existingCreditMemo.CreatedDate = DateTime.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd hh:mm:ss.ffffff", null);
                    }
                    if (columnName == "CancellationRemarks")
                    {
                        existingCreditMemo.CancellationRemarks = importRecord.AdjustedValue;
                    }
                    if (columnName == "OriginalSalesInvoiceId")
                    {
                        existingCreditMemo.OriginalSalesInvoiceId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "OriginalSeriesNumber")
                    {
                        existingCreditMemo.OriginalSeriesNumber = importRecord.AdjustedValue;
                    }
                    if (columnName == "OriginalServiceInvoiceId")
                    {
                        existingCreditMemo.OriginalServiceInvoiceId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "OriginalDocumentId")
                    {
                        existingCreditMemo.OriginalDocumentId = int.Parse(importRecord.AdjustedValue);
                    }
                }

                #endregion -- Credit Memo --

                #endregion -- Accounts Receivable --

                #region -- Accounts Payable --

                #region -- Purchase Order --

                if (tableName == "PurchaseOrder")
                {
                    var existingPurchaseOrder = await _dbContext.PurchaseOrders.FirstOrDefaultAsync(po => po.OriginalDocumentId == importRecord.DocumentRecordId, cancellationToken);

                    if (existingPurchaseOrder == null || importRecord.AdjustedValue == null)
                    {
                        return NotFound();
                    }

                    if (columnName == "PONo")
                    {
                        existingPurchaseOrder.PurchaseOrderNo = importRecord.AdjustedValue;
                    }
                    if (columnName == "Date")
                    {
                        existingPurchaseOrder.Date = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                    }
                    if (columnName == "Terms")
                    {
                        existingPurchaseOrder.Terms = importRecord.AdjustedValue;
                    }
                    if (columnName == "Quantity")
                    {
                        existingPurchaseOrder.Quantity = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Price")
                    {
                        existingPurchaseOrder.Price = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Amount")
                    {
                        existingPurchaseOrder.Amount = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "FinalPrice")
                    {
                        existingPurchaseOrder.FinalPrice = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Remarks")
                    {
                        existingPurchaseOrder.Remarks = importRecord.AdjustedValue;
                    }
                    if (columnName == "CreatedBy")
                    {
                        existingPurchaseOrder.CreatedBy = importRecord.AdjustedValue;
                    }
                    if (columnName == "CreatedDate")
                    {
                        existingPurchaseOrder.CreatedDate = DateTime.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd hh:mm:ss.ffffff", null);
                    }
                    if (columnName == "IsClosed")
                    {
                        existingPurchaseOrder.IsClosed = bool.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "CancellationRemarks")
                    {
                        existingPurchaseOrder.CancellationRemarks = importRecord.AdjustedValue;
                    }
                    if (columnName == "OriginalProductId")
                    {
                        existingPurchaseOrder.OriginalProductId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "OriginalSeriesNumber")
                    {
                        existingPurchaseOrder.OriginalSeriesNumber = importRecord.AdjustedValue;
                    }
                    if (columnName == "OriginalSupplierId")
                    {
                        existingPurchaseOrder.OriginalSupplierId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "OriginalDocumentId")
                    {
                        existingPurchaseOrder.OriginalDocumentId = int.Parse(importRecord.AdjustedValue);
                    }
                }

                #endregion -- Purchase Order --

                #region -- Receiving Report --

                if (tableName == "ReceivingReport")
                {
                    var existingReceivingReport = await _dbContext.ReceivingReports.FirstOrDefaultAsync(rr => rr.OriginalDocumentId == importRecord.DocumentRecordId, cancellationToken);

                    if (existingReceivingReport == null || importRecord.AdjustedValue == null)
                    {
                        return NotFound();
                    }

                    if (columnName == "RRNo")
                    {
                        existingReceivingReport.ReceivingReportNo = importRecord.AdjustedValue;
                    }
                    if (columnName == "Date")
                    {
                        existingReceivingReport.Date = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                    }
                    if (columnName == "DueDate")
                    {
                        existingReceivingReport.DueDate = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                    }
                    if (columnName == "SupplierInvoiceNumber")
                    {
                        existingReceivingReport.SupplierInvoiceNumber = importRecord.AdjustedValue;
                    }
                    if (columnName == "SupplierInvoiceDate")
                    {
                        existingReceivingReport.SupplierInvoiceDate = importRecord.AdjustedValue;
                    }
                    if (columnName == "TruckOrVessels")
                    {
                        existingReceivingReport.TruckOrVessels = importRecord.AdjustedValue;
                    }
                    if (columnName == "QuantityDelivered")
                    {
                        existingReceivingReport.QuantityDelivered = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "QuantityReceived")
                    {
                        existingReceivingReport.QuantityReceived = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "GainOrLoss")
                    {
                        existingReceivingReport.GainOrLoss = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Amount")
                    {
                        existingReceivingReport.Amount = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "OtherRef")
                    {
                        existingReceivingReport.OtherRef = importRecord.AdjustedValue;
                    }
                    if (columnName == "Remarks")
                    {
                        existingReceivingReport.Remarks = importRecord.AdjustedValue;
                    }
                    if (columnName == "CreatedBy")
                    {
                        existingReceivingReport.CreatedBy = importRecord.AdjustedValue;
                    }
                    if (columnName == "CreatedDate")
                    {
                        existingReceivingReport.CreatedDate = DateTime.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd hh:mm:ss.ffffff", null);
                    }
                    if (columnName == "CancellationRemarks")
                    {
                        existingReceivingReport.CancellationRemarks = importRecord.AdjustedValue;
                    }
                    if (columnName == "ReceivedDate")
                    {
                        existingReceivingReport.ReceivedDate = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                    }
                    if (columnName == "OriginalPOId")
                    {
                        existingReceivingReport.OriginalPOId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "OriginalSeriesNumber")
                    {
                        existingReceivingReport.OriginalSeriesNumber = importRecord.AdjustedValue;
                    }
                    if (columnName == "OriginalDocumentId")
                    {
                        existingReceivingReport.OriginalDocumentId = int.Parse(importRecord.AdjustedValue);
                    }
                }

                #endregion -- Receiving Report --

                #region -- Check Voucher Header --

                if (tableName == "CheckVoucherHeader")
                {
                    var existingCheckVoucherHeader = await _dbContext.CheckVoucherHeaders.FirstOrDefaultAsync(cv => cv.OriginalDocumentId == importRecord.DocumentRecordId, cancellationToken);

                    if (existingCheckVoucherHeader == null || importRecord.AdjustedValue == null)
                    {
                        return NotFound();
                    }

                    if (columnName == "CVNo")
                    {
                        existingCheckVoucherHeader.CheckVoucherHeaderNo = importRecord.AdjustedValue;
                    }
                    if (columnName == "Date")
                    {
                        existingCheckVoucherHeader.Date = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                    }
                    if (columnName == "RRNo")
                    {
                        existingCheckVoucherHeader.RRNo = [importRecord.AdjustedValue];
                    }
                    if (columnName == "SINo")
                    {
                        existingCheckVoucherHeader.SINo = [importRecord.AdjustedValue];
                    }
                    if (columnName == "PONo")
                    {
                        existingCheckVoucherHeader.PONo = [importRecord.AdjustedValue];
                    }
                    if (columnName == "Particulars")
                    {
                        existingCheckVoucherHeader.Particulars = importRecord.AdjustedValue;
                    }
                    if (columnName == "CheckNo")
                    {
                        existingCheckVoucherHeader.CheckNo = importRecord.AdjustedValue;
                    }
                    if (columnName == "Category")
                    {
                        existingCheckVoucherHeader.Category = importRecord.AdjustedValue;
                    }
                    if (columnName == "Payee")
                    {
                        existingCheckVoucherHeader.Payee = importRecord.AdjustedValue;
                    }
                    if (columnName == "CheckDate")
                    {
                        existingCheckVoucherHeader.CheckDate = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                    }
                    if (columnName == "StartDate")
                    {
                        existingCheckVoucherHeader.StartDate = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                    }
                    if (columnName == "EndDate")
                    {
                        existingCheckVoucherHeader.EndDate = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                    }
                    if (columnName == "NumberOfMonths")
                    {
                        existingCheckVoucherHeader.NumberOfMonths = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "NumberOfMonthsCreated")
                    {
                        existingCheckVoucherHeader.NumberOfMonthsCreated = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "LastCreatedDate")
                    {
                        existingCheckVoucherHeader.LastCreatedDate = DateTime.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd hh:mm:ss.ffffff", null);
                    }
                    if (columnName == "AmountPerMonth")
                    {
                        existingCheckVoucherHeader.AmountPerMonth = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "IsComplete")
                    {
                        existingCheckVoucherHeader.IsComplete = bool.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "AccruedType")
                    {
                        existingCheckVoucherHeader.AccruedType = importRecord.AdjustedValue;
                    }
                    if (columnName == "Reference" && existingCheckVoucherHeader.CvType == "Payment")
                    {
                        existingCheckVoucherHeader.Reference = importRecord.AdjustedValue;
                    }
                    if (columnName == "CreatedBy")
                    {
                        existingCheckVoucherHeader.CreatedBy = importRecord.AdjustedValue;
                    }
                    if (columnName == "CreatedDate")
                    {
                        existingCheckVoucherHeader.CreatedDate = DateTime.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd hh:mm:ss.ffffff", null);
                    }
                    if (columnName == "Total")
                    {
                        existingCheckVoucherHeader.Total = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Amount")
                    {
                        existingCheckVoucherHeader.Amount = [decimal.Parse(importRecord.AdjustedValue)];
                    }
                    if (columnName == "CheckAmount")
                    {
                        existingCheckVoucherHeader.CheckAmount = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "CvType")
                    {
                        existingCheckVoucherHeader.CvType = importRecord.AdjustedValue;
                    }
                    if (columnName == "AmountPaid")
                    {
                        existingCheckVoucherHeader.AmountPaid = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "IsPaid")
                    {
                        existingCheckVoucherHeader.IsPaid = bool.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "CancellationRemarks")
                    {
                        existingCheckVoucherHeader.CancellationRemarks = importRecord.AdjustedValue;
                    }
                    if (columnName == "OriginalBankId")
                    {
                        existingCheckVoucherHeader.OriginalBankId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "OriginalSeriesNumber")
                    {
                        existingCheckVoucherHeader.OriginalSeriesNumber = importRecord.AdjustedValue;
                    }
                    if (columnName == "OriginalSupplierId")
                    {
                        existingCheckVoucherHeader.OriginalSupplierId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "OriginalDocumentId")
                    {
                        existingCheckVoucherHeader.OriginalDocumentId = int.Parse(importRecord.AdjustedValue);
                    }
                }

                #endregion -- Check Voucher Header --

                #region -- Check Voucher Details --

                if (tableName == "CheckVoucherDetails")
                {
                    var existingCheckVoucherDetail = await _dbContext.CheckVoucherDetails.FirstOrDefaultAsync(cv => cv.OriginalDocumentId == importRecord.DocumentRecordId, cancellationToken);

                    if (existingCheckVoucherDetail == null || importRecord.AdjustedValue == null)
                    {
                        return NotFound();
                    }

                    if (columnName == "AccountNo")
                    {
                        existingCheckVoucherDetail.AccountNo = importRecord.AdjustedValue;
                    }
                    if (columnName == "AccountName")
                    {
                        existingCheckVoucherDetail.AccountName = importRecord.AdjustedValue;
                    }
                    if (columnName == "Debit")
                    {
                        existingCheckVoucherDetail.Debit = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Credit")
                    {
                        existingCheckVoucherDetail.Credit = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "OriginalDocumentId")
                    {
                        existingCheckVoucherDetail.OriginalDocumentId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "CVHeaderId")
                    {
                        existingCheckVoucherDetail.CheckVoucherHeaderId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "TransactionNo")
                    {
                        existingCheckVoucherDetail.TransactionNo = importRecord.AdjustedValue;
                    }
                }

                #endregion -- Check Voucher Details --

                #region -- Journal Voucher Header --

                if (tableName == "JournalVoucherHeader")
                {
                    var existingJournalVoucherHeader = await _dbContext.JournalVoucherHeaders.FirstOrDefaultAsync(jv => jv.OriginalDocumentId == importRecord.DocumentRecordId, cancellationToken);

                    if (existingJournalVoucherHeader == null || importRecord.AdjustedValue == null)
                    {
                        return NotFound();
                    }

                    if (columnName == "JVNo")
                    {
                        existingJournalVoucherHeader.JournalVoucherHeaderNo = importRecord.AdjustedValue;
                    }
                    if (columnName == "Date")
                    {
                        existingJournalVoucherHeader.Date = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                    }
                    if (columnName == "References")
                    {
                        existingJournalVoucherHeader.References = importRecord.AdjustedValue;
                    }
                    if (columnName == "Particulars")
                    {
                        existingJournalVoucherHeader.Particulars = importRecord.AdjustedValue;
                    }
                    if (columnName == "CRNo")
                    {
                        existingJournalVoucherHeader.CRNo = importRecord.AdjustedValue;
                    }
                    if (columnName == "JVReason")
                    {
                        existingJournalVoucherHeader.JVReason = importRecord.AdjustedValue;
                    }
                    if (columnName == "CreatedBy")
                    {
                        existingJournalVoucherHeader.CreatedBy = importRecord.AdjustedValue;
                    }
                    if (columnName == "CreatedDate")
                    {
                        existingJournalVoucherHeader.CreatedDate = DateTime.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd hh:mm:ss.ffffff", null);
                    }
                    if (columnName == "CancellationRemarks")
                    {
                        existingJournalVoucherHeader.CancellationRemarks = importRecord.AdjustedValue;
                    }
                    if (columnName == "OriginalCVId")
                    {
                        existingJournalVoucherHeader.OriginalCVId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "OriginalSeriesNumber")
                    {
                        existingJournalVoucherHeader.OriginalSeriesNumber = importRecord.AdjustedValue;
                    }
                    if (columnName == "OriginalDocumentId")
                    {
                        existingJournalVoucherHeader.OriginalDocumentId = int.Parse(importRecord.AdjustedValue);
                    }
                }

                #endregion -- Journal Voucher Header --

                #region -- Journal Voucher Details --

                if (tableName == "JournalVoucherDetails")
                {
                    var existingJournalVoucherDetail = await _dbContext.JournalVoucherDetails.FirstOrDefaultAsync(jv => jv.OriginalDocumentId == importRecord.DocumentRecordId, cancellationToken);

                    if (existingJournalVoucherDetail == null || importRecord.AdjustedValue == null)
                    {
                        return NotFound();
                    }

                    if (columnName == "AccountNo")
                    {
                        existingJournalVoucherDetail.AccountNo = importRecord.AdjustedValue;
                    }
                    if (columnName == "AccountName")
                    {
                        existingJournalVoucherDetail.AccountName = importRecord.AdjustedValue;
                    }
                    if (columnName == "Debit")
                    {
                        existingJournalVoucherDetail.Debit = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Credit")
                    {
                        existingJournalVoucherDetail.Credit = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "OriginalDocumentId")
                    {
                        existingJournalVoucherDetail.OriginalDocumentId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "JVHeaderId")
                    {
                        existingJournalVoucherDetail.JournalVoucherHeaderId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "TransactionNo")
                    {
                        existingJournalVoucherDetail.TransactionNo = importRecord.AdjustedValue;
                    }
                }

                #endregion -- Journal Voucher Details --

                #endregion -- Accounts Payable --
            }
            if (procedure == "Modify" && importRecord.DatabaseName == "AAS")
            {
                #region -- Accounts Receivable --

                #region -- Sales Invoice --

                if (tableName == "SalesInvoice")
                {
                    var existingSalesInvoice = await _aasDbContext.SalesInvoices.FirstOrDefaultAsync(si => si.OriginalDocumentId == importRecord.DocumentRecordId, cancellationToken);

                    if (existingSalesInvoice == null || importRecord.AdjustedValue == null)
                    {
                        return NotFound();
                    }

                    if (columnName == "SINo")
                    {
                        existingSalesInvoice.SalesInvoiceNo = importRecord.AdjustedValue;
                    }
                    if (columnName == "OriginalCustomerId")
                    {

                        existingSalesInvoice.OriginalCustomerId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "OriginalProductId")
                    {
                        existingSalesInvoice.OriginalProductId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "OtherRefNo")
                    {
                        existingSalesInvoice.OtherRefNo = importRecord.AdjustedValue;
                    }
                    if (columnName == "Quantity")
                    {
                        existingSalesInvoice.Quantity = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "UnitPrice")
                    {
                        existingSalesInvoice.UnitPrice = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Amount")
                    {
                        existingSalesInvoice.Amount = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Remarks")
                    {
                        existingSalesInvoice.Remarks = importRecord.AdjustedValue;
                    }
                    if (columnName == "Status")
                    {
                        existingSalesInvoice.Status = importRecord.AdjustedValue;
                    }
                    if (columnName == "TransactionDate")
                    {
                        existingSalesInvoice.TransactionDate = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                    }
                    if (columnName == "Discount")
                    {
                        existingSalesInvoice.Discount = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "DueDate")
                    {
                        existingSalesInvoice.DueDate = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                    }
                    if (columnName == "CreatedBy")
                    {
                        existingSalesInvoice.CreatedBy = importRecord.AdjustedValue;
                    }
                    if (columnName == "CreatedDate")
                    {
                        existingSalesInvoice.CreatedDate = DateTime.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd hh:mm:ss.ffffff", null);
                    }
                    if (columnName == "CancellationRemarks")
                    {
                        existingSalesInvoice.CancellationRemarks = importRecord.AdjustedValue;
                    }
                    if (columnName == "OriginalSeriesNumber")
                    {
                        existingSalesInvoice.OriginalSeriesNumber = importRecord.AdjustedValue;
                    }
                    if (columnName == "OriginalDocumentId")
                    {
                        existingSalesInvoice.OriginalDocumentId = int.Parse(importRecord.AdjustedValue);
                    }
                }

                #endregion -- Sales Invoice --

                #region -- Service Invoice --

                if (tableName == "ServiceInvoice")
                {
                    var existingServiceInvoice = await _aasDbContext.ServiceInvoices.FirstOrDefaultAsync(sv => sv.OriginalDocumentId == importRecord.DocumentRecordId, cancellationToken);

                    if (existingServiceInvoice == null || importRecord.AdjustedValue == null)
                    {
                        return NotFound();
                    }

                    if (columnName == "SVNo")
                    {
                        existingServiceInvoice.ServiceInvoiceNo = importRecord.AdjustedValue;
                    }
                    if (columnName == "DueDate")
                    {
                        existingServiceInvoice.DueDate = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                    }
                    if (columnName == "Period")
                    {
                        existingServiceInvoice.Period = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                    }
                    if (columnName == "Amount")
                    {
                        existingServiceInvoice.Amount = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Total")
                    {
                        existingServiceInvoice.Total = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Discount")
                    {
                        existingServiceInvoice.Discount = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "CurrentAndPreviousAmount")
                    {
                        existingServiceInvoice.CurrentAndPreviousAmount = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "UnearnedAmount")
                    {
                        existingServiceInvoice.UnearnedAmount = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Status")
                    {
                        existingServiceInvoice.Status = importRecord.AdjustedValue;
                    }
                    if (columnName == "Instructions")
                    {
                        existingServiceInvoice.Instructions = importRecord.AdjustedValue;
                    }
                    if (columnName == "CreatedBy")
                    {
                        existingServiceInvoice.CreatedBy = importRecord.AdjustedValue;
                    }
                    if (columnName == "CreatedDate")
                    {
                        existingServiceInvoice.CreatedDate = DateTime.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd hh:mm:ss.ffffff", null);
                    }
                    if (columnName == "CancellationRemarks")
                    {
                        existingServiceInvoice.CancellationRemarks = importRecord.AdjustedValue;
                    }
                    if (columnName == "OriginalCustomerId")
                    {
                        existingServiceInvoice.OriginalCustomerId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "OriginalSeriesNumber")
                    {
                        existingServiceInvoice.OriginalSeriesNumber = importRecord.AdjustedValue;
                    }
                    if (columnName == "OriginalServicesId")
                    {
                        existingServiceInvoice.OriginalServicesId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "OriginalDocumentId")
                    {
                        existingServiceInvoice.OriginalDocumentId = int.Parse(importRecord.AdjustedValue);
                    }
                }

                #endregion -- Service Invoice --

                #region -- Collection Receipt --

                    if (tableName == "CollectionReceipt")
                    {
                        var existingCollectionReceipt = await _aasDbContext.CollectionReceipts.FirstOrDefaultAsync(cr => cr.OriginalDocumentId == importRecord.DocumentRecordId, cancellationToken);

                        if (existingCollectionReceipt == null || importRecord.AdjustedValue == null)
                        {
                            return NotFound();
                        }

                        if (columnName == "CRNo")
                        {
                            existingCollectionReceipt.CollectionReceiptNo = importRecord.AdjustedValue;
                        }
                        if (columnName == "TransactionDate")
                        {
                            existingCollectionReceipt.TransactionDate = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                        }
                        if (columnName == "ReferenceNo")
                        {
                            existingCollectionReceipt.ReferenceNo = importRecord.AdjustedValue;
                        }
                        if (columnName == "Remarks")
                        {
                            existingCollectionReceipt.Remarks = importRecord.AdjustedValue;
                        }
                        if (columnName == "CashAmount")
                        {
                            existingCollectionReceipt.CashAmount = decimal.Parse(importRecord.AdjustedValue);
                        }
                        if (columnName == "CheckDate")
                        {
                            existingCollectionReceipt.CheckDate = DateOnly.Parse(importRecord.AdjustedValue);
                        }
                        if (columnName == "CheckNo")
                        {
                            existingCollectionReceipt.CheckNo = importRecord.AdjustedValue;
                        }
                        if (columnName == "CheckBank")
                        {
                            existingCollectionReceipt.CheckBank = importRecord.AdjustedValue;
                        }
                        if (columnName == "CheckBranch")
                        {
                            existingCollectionReceipt.CheckBranch = importRecord.AdjustedValue;
                        }
                        if (columnName == "CheckAmount")
                        {
                            existingCollectionReceipt.CheckAmount = decimal.Parse(importRecord.AdjustedValue);
                        }
                        if (columnName == "ManagerCheckDate")
                        {
                            existingCollectionReceipt.ManagerCheckDate = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                        }
                        if (columnName == "ManagerCheckNo")
                        {
                            existingCollectionReceipt.ManagerCheckNo = importRecord.AdjustedValue;
                        }
                        if (columnName == "ManagerCheckBank")
                        {
                            existingCollectionReceipt.ManagerCheckBank = importRecord.AdjustedValue;
                        }
                        if (columnName == "ManagerCheckBranch")
                        {
                            existingCollectionReceipt.ManagerCheckBranch = importRecord.AdjustedValue;
                        }
                        if (columnName == "ManagerCheckAmount")
                        {
                            existingCollectionReceipt.ManagerCheckAmount = decimal.Parse(importRecord.AdjustedValue);
                        }
                        if (columnName == "EWT")
                        {
                            existingCollectionReceipt.EWT = decimal.Parse(importRecord.AdjustedValue);
                        }
                        if (columnName == "WVAT")
                        {
                            existingCollectionReceipt.WVAT = decimal.Parse(importRecord.AdjustedValue);
                        }
                        if (columnName == "Total")
                        {
                            existingCollectionReceipt.Total = decimal.Parse(importRecord.AdjustedValue);
                        }
                        if (columnName == "IsCertificateUpload")
                        {
                            existingCollectionReceipt.IsCertificateUpload = bool.Parse(importRecord.AdjustedValue);
                        }
                        if (columnName == "F2306FilePath")
                        {
                            existingCollectionReceipt.F2306FilePath = importRecord.AdjustedValue;
                        }
                        if (columnName == "F2307FilePath")
                        {
                            existingCollectionReceipt.F2307FilePath = importRecord.AdjustedValue;
                        }
                        if (columnName == "CreatedBy")
                        {
                            existingCollectionReceipt.CreatedBy = importRecord.AdjustedValue;
                        }
                        if (columnName == "CreatedDate")
                        {
                            existingCollectionReceipt.CreatedDate = DateTime.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd hh:mm:ss.ffffff", null);
                        }
                        if (columnName == "CancellationRemarks")
                        {
                            existingCollectionReceipt.CancellationRemarks = importRecord.AdjustedValue;
                        }
                        if (columnName == "MultipleSI")
                        {
                            existingCollectionReceipt.MultipleSI = [importRecord.AdjustedValue];
                        }
                        if (columnName == "MultipleSIId")
                        {
                            existingCollectionReceipt.MultipleSIId = [int.Parse(importRecord.AdjustedValue)];
                        }
                        if (columnName == "SIMultipleAmount")
                        {
                            existingCollectionReceipt.SIMultipleAmount = [decimal.Parse(importRecord.AdjustedValue)];
                        }
                        if (columnName == "MultipleTransactionDate")
                        {
                            existingCollectionReceipt.MultipleTransactionDate = [DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd")];
                        }
                        if (columnName == "OriginalCustomerId")
                        {
                            existingCollectionReceipt.OriginalCustomerId = int.Parse(importRecord.AdjustedValue);
                        }
                        if (columnName == "OriginalSalesInvoiceId")
                        {
                            existingCollectionReceipt.OriginalSalesInvoiceId = int.Parse(importRecord.AdjustedValue);
                        }
                        if (columnName == "OriginalSeriesNumber")
                        {
                            existingCollectionReceipt.OriginalSeriesNumber = importRecord.AdjustedValue;
                        }
                        if (columnName == "OriginalServiceInvoiceId")
                        {
                            existingCollectionReceipt.OriginalServiceInvoiceId = int.Parse(importRecord.AdjustedValue);
                        }
                        if (columnName == "OriginalDocumentId")
                        {
                            existingCollectionReceipt.OriginalDocumentId = int.Parse(importRecord.AdjustedValue);
                        }
                        if (columnName.Contains("SingleSalesInvoiceAmount"))
                        {
                            if (existingCollectionReceipt.CashAmount != 0)
                            {
                                existingCollectionReceipt.CashAmount = decimal.Parse(importRecord.AdjustedValue) - (existingCollectionReceipt.EWT + existingCollectionReceipt.WVAT);
                            }
                            if (existingCollectionReceipt.CheckAmount != 0)
                            {
                                existingCollectionReceipt.CheckAmount = decimal.Parse(importRecord.AdjustedValue) - (existingCollectionReceipt.EWT + existingCollectionReceipt.WVAT);
                            }
                            if (existingCollectionReceipt.ManagerCheckAmount != 0)
                            {
                                existingCollectionReceipt.ManagerCheckAmount = decimal.Parse(importRecord.AdjustedValue) - (existingCollectionReceipt.EWT + existingCollectionReceipt.WVAT);
                            }
                            existingCollectionReceipt.Total = decimal.Parse(importRecord.AdjustedValue);
                        }
                        if (columnName.Contains("MultipleSalesInvoiceAmount"))
                        {
                            int start = columnName.IndexOf('(') + 1;
                            int end = columnName.IndexOf(')', start);
                            string salesInvoiceNo = columnName.Substring(start, end - start);
                            int index = Array.FindIndex(
                                existingCollectionReceipt.MultipleSI,
                                x => x != null && x.Contains(salesInvoiceNo, StringComparison.OrdinalIgnoreCase)
                            );
                            var MultipleSalesInvoiceAmount = existingCollectionReceipt.SIMultipleAmount[index];
                            var ajustedValue = decimal.Parse(importRecord.AdjustedValue);
                            var siMultipleAmount = MultipleSalesInvoiceAmount - ajustedValue;
                            var totalAmount = existingCollectionReceipt.Total - siMultipleAmount;

                            existingCollectionReceipt.SIMultipleAmount[index] = ajustedValue;
                            existingCollectionReceipt.Total = totalAmount;

                            if (existingCollectionReceipt.CashAmount != 0)
                            {
                                existingCollectionReceipt.CashAmount = totalAmount - (existingCollectionReceipt.EWT + existingCollectionReceipt.WVAT);
                            }
                            if (existingCollectionReceipt.CheckAmount != 0)
                            {
                                existingCollectionReceipt.CheckAmount = totalAmount - (existingCollectionReceipt.EWT + existingCollectionReceipt.WVAT);
                            }
                            if (existingCollectionReceipt.ManagerCheckAmount != 0)
                            {
                                existingCollectionReceipt.ManagerCheckAmount = totalAmount - (existingCollectionReceipt.EWT + existingCollectionReceipt.WVAT);
                            }
                        }
                    }

                #endregion -- Collection Receipt --

                #region -- Offsettings --

                if (tableName == "Offsetting")
                {
                    var existingCollectionReceipt = await _aasDbContext.CollectionReceipts.FirstOrDefaultAsync(cr => cr.OriginalDocumentId == importRecord.DocumentRecordId, cancellationToken);

                    if (existingCollectionReceipt == null)
                    {
                        return NotFound();
                    }

                    var existingOffset = await _aasDbContext.Offsettings.FirstOrDefaultAsync(offset => offset.Reference == existingCollectionReceipt.OriginalSeriesNumber, cancellationToken);

                    if (existingOffset == null || importRecord.AdjustedValue == null)
                    {
                        return NotFound();
                    }

                    if (columnName == "AccountNo")
                    {
                        existingOffset.AccountNo = importRecord.AdjustedValue;
                    }
                    if (columnName == "Reference")
                    {
                        existingOffset.Reference = importRecord.AdjustedValue;
                    }
                    if (columnName == "IsRemoved")
                    {
                        existingOffset.IsRemoved = bool.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Amount")
                    {
                        existingOffset.Amount = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "CreatedBy")
                    {
                        existingOffset.CreatedBy = importRecord.AdjustedValue;
                    }
                    if (columnName == "CreatedDate")
                    {
                        existingOffset.CreatedDate = DateTime.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd hh:mm:ss.ffffff", null);
                    }
                    if (columnName == "AccountTitle")
                    {
                        existingOffset.AccountTitle = importRecord.AdjustedValue;
                    }
                    if (columnName == "Source")
                    {
                        existingOffset.Source = importRecord.AdjustedValue;
                    }
                }

                #endregion -- Offsettings --

                #region -- Debit Memo --

                if (tableName == "DebitMemo")
                {
                    var existingDebitMemo = await _aasDbContext.DebitMemos.FirstOrDefaultAsync(dm => dm.OriginalDocumentId == importRecord.DocumentRecordId, cancellationToken);

                    if (existingDebitMemo == null || importRecord.AdjustedValue == null)
                    {
                        return NotFound();
                    }

                    if (columnName == "DMNo")
                    {
                        existingDebitMemo.DebitMemoNo = importRecord.AdjustedValue;
                    }
                    if (columnName == "TransactionDate")
                    {
                        existingDebitMemo.TransactionDate = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                    }
                    if (columnName == "DebitAmount")
                    {
                        existingDebitMemo.DebitAmount = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Description")
                    {
                        existingDebitMemo.Description = importRecord.AdjustedValue;
                    }
                    if (columnName == "AdjustedPrice")
                    {
                        existingDebitMemo.AdjustedPrice = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Quantity")
                    {
                        existingDebitMemo.Quantity = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Source")
                    {
                        existingDebitMemo.Source = importRecord.AdjustedValue;
                    }
                    if (columnName == "Remarks")
                    {
                        existingDebitMemo.Remarks = importRecord.AdjustedValue;
                    }
                    if (columnName == "Period")
                    {
                        existingDebitMemo.Period = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                    }
                    if (columnName == "Amount")
                    {
                        existingDebitMemo.Amount = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "CurrentAndPreviousAmount")
                    {
                        existingDebitMemo.CurrentAndPreviousAmount = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "UnearnedAmount")
                    {
                        existingDebitMemo.UnearnedAmount = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "ServicesId")
                    {
                        existingDebitMemo.ServicesId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "CreatedBy")
                    {
                        existingDebitMemo.CreatedBy = importRecord.AdjustedValue;
                    }
                    if (columnName == "CreatedDate")
                    {
                        existingDebitMemo.CreatedDate = DateTime.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd hh:mm:ss.ffffff", null);
                    }
                    if (columnName == "CancellationRemarks")
                    {
                        existingDebitMemo.CancellationRemarks = importRecord.AdjustedValue;
                    }
                    if (columnName == "OriginalSalesInvoiceId")
                    {
                        existingDebitMemo.OriginalSalesInvoiceId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "OriginalSeriesNumber")
                    {
                        existingDebitMemo.OriginalSeriesNumber = importRecord.AdjustedValue;
                    }
                    if (columnName == "OriginalServiceInvoiceId")
                    {
                        existingDebitMemo.OriginalServiceInvoiceId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "OriginalDocumentId")
                    {
                        existingDebitMemo.OriginalDocumentId = int.Parse(importRecord.AdjustedValue);
                    }
                }

                #endregion -- Debit Memo --

                #region -- Credit Memo --

                if (tableName == "CreditMemo")
                {
                    var existingCreditMemo = await _aasDbContext.CreditMemos.FirstOrDefaultAsync(cm => cm.OriginalDocumentId == importRecord.DocumentRecordId, cancellationToken);

                    if (existingCreditMemo == null || importRecord.AdjustedValue == null)
                    {
                        return NotFound();
                    }

                    if (columnName == "CMNo")
                    {
                        existingCreditMemo.CreditMemoNo = importRecord.AdjustedValue;
                    }
                    if (columnName == "TransactionDate")
                    {
                        existingCreditMemo.TransactionDate = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                    }
                    if (columnName == "CreditAmount")
                    {
                        existingCreditMemo.CreditAmount = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Description")
                    {
                        existingCreditMemo.Description = importRecord.AdjustedValue;
                    }
                    if (columnName == "AdjustedPrice")
                    {
                        existingCreditMemo.AdjustedPrice = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Quantity")
                    {
                        existingCreditMemo.Quantity = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Source")
                    {
                        existingCreditMemo.Source = importRecord.AdjustedValue;
                    }
                    if (columnName == "Remarks")
                    {
                        existingCreditMemo.Remarks = importRecord.AdjustedValue;
                    }
                    if (columnName == "Period")
                    {
                        existingCreditMemo.Period = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                    }
                    if (columnName == "Amount")
                    {
                        existingCreditMemo.Amount = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "CurrentAndPreviousAmount")
                    {
                        existingCreditMemo.CurrentAndPreviousAmount = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "UnearnedAmount")
                    {
                        existingCreditMemo.UnearnedAmount = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "ServicesId")
                    {
                        existingCreditMemo.ServicesId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "CreatedBy")
                    {
                        existingCreditMemo.CreatedBy = importRecord.AdjustedValue;
                    }
                    if (columnName == "CreatedDate")
                    {
                        existingCreditMemo.CreatedDate = DateTime.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd hh:mm:ss.ffffff", null);
                    }
                    if (columnName == "CancellationRemarks")
                    {
                        existingCreditMemo.CancellationRemarks = importRecord.AdjustedValue;
                    }
                    if (columnName == "OriginalSalesInvoiceId")
                    {
                        existingCreditMemo.OriginalSalesInvoiceId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "OriginalSeriesNumber")
                    {
                        existingCreditMemo.OriginalSeriesNumber = importRecord.AdjustedValue;
                    }
                    if (columnName == "OriginalServiceInvoiceId")
                    {
                        existingCreditMemo.OriginalServiceInvoiceId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "OriginalDocumentId")
                    {
                        existingCreditMemo.OriginalDocumentId = int.Parse(importRecord.AdjustedValue);
                    }
                }

                #endregion -- Credit Memo --

                #endregion -- Accounts Receivable --

                #region -- Accounts Payable --

                #region -- Purchase Order --

                if (tableName == "PurchaseOrder")
                {
                    var existingPurchaseOrder = await _aasDbContext.PurchaseOrders.FirstOrDefaultAsync(po => po.OriginalDocumentId == importRecord.DocumentRecordId, cancellationToken);

                    if (existingPurchaseOrder == null || importRecord.AdjustedValue == null)
                    {
                        return NotFound();
                    }

                    if (columnName == "PONo")
                    {
                        existingPurchaseOrder.PurchaseOrderNo = importRecord.AdjustedValue;
                    }
                    if (columnName == "Date")
                    {
                        existingPurchaseOrder.Date = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                    }
                    if (columnName == "Terms")
                    {
                        existingPurchaseOrder.Terms = importRecord.AdjustedValue;
                    }
                    if (columnName == "Quantity")
                    {
                        existingPurchaseOrder.Quantity = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Price")
                    {
                        existingPurchaseOrder.Price = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Amount")
                    {
                        existingPurchaseOrder.Amount = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "FinalPrice")
                    {
                        existingPurchaseOrder.FinalPrice = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Remarks")
                    {
                        existingPurchaseOrder.Remarks = importRecord.AdjustedValue;
                    }
                    if (columnName == "CreatedBy")
                    {
                        existingPurchaseOrder.CreatedBy = importRecord.AdjustedValue;
                    }
                    if (columnName == "CreatedDate")
                    {
                        existingPurchaseOrder.CreatedDate = DateTime.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd hh:mm:ss.ffffff", null);
                    }
                    if (columnName == "IsClosed")
                    {
                        existingPurchaseOrder.IsClosed = bool.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "CancellationRemarks")
                    {
                        existingPurchaseOrder.CancellationRemarks = importRecord.AdjustedValue;
                    }
                    if (columnName == "OriginalProductId")
                    {
                        existingPurchaseOrder.OriginalProductId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "OriginalSeriesNumber")
                    {
                        existingPurchaseOrder.OriginalSeriesNumber = importRecord.AdjustedValue;
                    }
                    if (columnName == "OriginalSupplierId")
                    {
                        existingPurchaseOrder.OriginalSupplierId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "OriginalDocumentId")
                    {
                        existingPurchaseOrder.OriginalDocumentId = int.Parse(importRecord.AdjustedValue);
                    }
                }

                #endregion -- Purchase Order --

                #region -- Receiving Report --

                if (tableName == "ReceivingReport")
                {
                    var existingReceivingReport = await _aasDbContext.ReceivingReports.FirstOrDefaultAsync(rr => rr.OriginalDocumentId == importRecord.DocumentRecordId, cancellationToken);

                    if (existingReceivingReport == null || importRecord.AdjustedValue == null)
                    {
                        return NotFound();
                    }

                    if (columnName == "RRNo")
                    {
                        existingReceivingReport.ReceivingReportNo = importRecord.AdjustedValue;
                    }
                    if (columnName == "Date")
                    {
                        existingReceivingReport.Date = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                    }
                    if (columnName == "DueDate")
                    {
                        existingReceivingReport.DueDate = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                    }
                    if (columnName == "SupplierInvoiceNumber")
                    {
                        existingReceivingReport.SupplierInvoiceNumber = importRecord.AdjustedValue;
                    }
                    if (columnName == "SupplierInvoiceDate")
                    {
                        existingReceivingReport.SupplierInvoiceDate = importRecord.AdjustedValue;
                    }
                    if (columnName == "TruckOrVessels")
                    {
                        existingReceivingReport.TruckOrVessels = importRecord.AdjustedValue;
                    }
                    if (columnName == "QuantityDelivered")
                    {
                        existingReceivingReport.QuantityDelivered = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "QuantityReceived")
                    {
                        existingReceivingReport.QuantityReceived = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "GainOrLoss")
                    {
                        existingReceivingReport.GainOrLoss = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Amount")
                    {
                        existingReceivingReport.Amount = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "OtherRef")
                    {
                        existingReceivingReport.OtherRef = importRecord.AdjustedValue;
                    }
                    if (columnName == "Remarks")
                    {
                        existingReceivingReport.Remarks = importRecord.AdjustedValue;
                    }
                    if (columnName == "CreatedBy")
                    {
                        existingReceivingReport.CreatedBy = importRecord.AdjustedValue;
                    }
                    if (columnName == "CreatedDate")
                    {
                        existingReceivingReport.CreatedDate = DateTime.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd hh:mm:ss.ffffff", null);
                    }
                    if (columnName == "CancellationRemarks")
                    {
                        existingReceivingReport.CancellationRemarks = importRecord.AdjustedValue;
                    }
                    if (columnName == "ReceivedDate")
                    {
                        existingReceivingReport.ReceivedDate = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                    }
                    if (columnName == "OriginalPOId")
                    {
                        existingReceivingReport.OriginalPOId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "OriginalSeriesNumber")
                    {
                        existingReceivingReport.OriginalSeriesNumber = importRecord.AdjustedValue;
                    }
                    if (columnName == "OriginalDocumentId")
                    {
                        existingReceivingReport.OriginalDocumentId = int.Parse(importRecord.AdjustedValue);
                    }
                }

                #endregion -- Receiving Report --

                #region -- Check Voucher Header --

                if (tableName == "CheckVoucherHeader")
                {
                    var existingCheckVoucherHeader = await _aasDbContext.CheckVoucherHeaders.FirstOrDefaultAsync(cv => cv.OriginalDocumentId == importRecord.DocumentRecordId, cancellationToken);

                    if (existingCheckVoucherHeader == null || importRecord.AdjustedValue == null)
                    {
                        return NotFound();
                    }

                    if (columnName == "CVNo")
                    {
                        existingCheckVoucherHeader.CheckVoucherHeaderNo = importRecord.AdjustedValue;
                    }
                    if (columnName == "Date")
                    {
                        existingCheckVoucherHeader.Date = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                    }
                    if (columnName == "RRNo")
                    {
                        existingCheckVoucherHeader.RRNo = [importRecord.AdjustedValue];
                    }
                    if (columnName == "SINo")
                    {
                        existingCheckVoucherHeader.SINo = [importRecord.AdjustedValue];
                    }
                    if (columnName == "PONo")
                    {
                        existingCheckVoucherHeader.PONo = [importRecord.AdjustedValue];
                    }
                    if (columnName == "Particulars")
                    {
                        existingCheckVoucherHeader.Particulars = importRecord.AdjustedValue;
                    }
                    if (columnName == "CheckNo")
                    {
                        existingCheckVoucherHeader.CheckNo = importRecord.AdjustedValue;
                    }
                    if (columnName == "Category")
                    {
                        existingCheckVoucherHeader.Category = importRecord.AdjustedValue;
                    }
                    if (columnName == "Payee")
                    {
                        existingCheckVoucherHeader.Payee = importRecord.AdjustedValue;
                    }
                    if (columnName == "CheckDate")
                    {
                        existingCheckVoucherHeader.CheckDate = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                    }
                    if (columnName == "StartDate")
                    {
                        existingCheckVoucherHeader.StartDate = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                    }
                    if (columnName == "EndDate")
                    {
                        existingCheckVoucherHeader.EndDate = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                    }
                    if (columnName == "NumberOfMonths")
                    {
                        existingCheckVoucherHeader.NumberOfMonths = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "NumberOfMonthsCreated")
                    {
                        existingCheckVoucherHeader.NumberOfMonthsCreated = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "LastCreatedDate")
                    {
                        existingCheckVoucherHeader.LastCreatedDate = DateTime.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd hh:mm:ss.ffffff", null);
                    }
                    if (columnName == "AmountPerMonth")
                    {
                        existingCheckVoucherHeader.AmountPerMonth = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "IsComplete")
                    {
                        existingCheckVoucherHeader.IsComplete = bool.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "AccruedType")
                    {
                        existingCheckVoucherHeader.AccruedType = importRecord.AdjustedValue;
                    }
                    if (columnName == "Reference" && existingCheckVoucherHeader.CvType == "Payment")
                    {
                        existingCheckVoucherHeader.Reference = importRecord.AdjustedValue;
                    }
                    if (columnName == "CreatedBy")
                    {
                        existingCheckVoucherHeader.CreatedBy = importRecord.AdjustedValue;
                    }
                    if (columnName == "CreatedDate")
                    {
                        existingCheckVoucherHeader.CreatedDate = DateTime.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd hh:mm:ss.ffffff", null);
                    }
                    if (columnName == "Total")
                    {
                        existingCheckVoucherHeader.Total = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Amount")
                    {
                        existingCheckVoucherHeader.Amount = [decimal.Parse(importRecord.AdjustedValue)];
                    }
                    if (columnName == "CheckAmount")
                    {
                        existingCheckVoucherHeader.CheckAmount = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "CvType")
                    {
                        existingCheckVoucherHeader.CvType = importRecord.AdjustedValue;
                    }
                    if (columnName == "AmountPaid")
                    {
                        existingCheckVoucherHeader.AmountPaid = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "IsPaid")
                    {
                        existingCheckVoucherHeader.IsPaid = bool.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "CancellationRemarks")
                    {
                        existingCheckVoucherHeader.CancellationRemarks = importRecord.AdjustedValue;
                    }
                    if (columnName == "OriginalBankId")
                    {
                        existingCheckVoucherHeader.OriginalBankId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "OriginalSeriesNumber")
                    {
                        existingCheckVoucherHeader.OriginalSeriesNumber = importRecord.AdjustedValue;
                    }
                    if (columnName == "OriginalSupplierId")
                    {
                        existingCheckVoucherHeader.OriginalSupplierId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "OriginalDocumentId")
                    {
                        existingCheckVoucherHeader.OriginalDocumentId = int.Parse(importRecord.AdjustedValue);
                    }
                }

                #endregion -- Check Voucher Header --

                #region -- Check Voucher Details --

                if (tableName == "CheckVoucherDetails")
                {
                    var existingCheckVoucherDetail = await _aasDbContext.CheckVoucherDetails.FirstOrDefaultAsync(cv => cv.OriginalDocumentId == importRecord.DocumentRecordId, cancellationToken);

                    if (existingCheckVoucherDetail == null || importRecord.AdjustedValue == null)
                    {
                        return NotFound();
                    }

                    if (columnName == "AccountNo")
                    {
                        existingCheckVoucherDetail.AccountNo = importRecord.AdjustedValue;
                    }
                    if (columnName == "AccountName")
                    {
                        existingCheckVoucherDetail.AccountName = importRecord.AdjustedValue;
                    }
                    if (columnName == "Debit")
                    {
                        existingCheckVoucherDetail.Debit = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Credit")
                    {
                        existingCheckVoucherDetail.Credit = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "OriginalDocumentId")
                    {
                        existingCheckVoucherDetail.OriginalDocumentId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "CVHeaderId")
                    {
                        existingCheckVoucherDetail.CheckVoucherHeaderId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "TransactionNo")
                    {
                        existingCheckVoucherDetail.TransactionNo = importRecord.AdjustedValue;
                    }
                }

                #endregion -- Check Voucher Details --

                #region -- Journal Voucher Header --

                if (tableName == "JournalVoucherHeader")
                {
                    var existingJournalVoucherHeader = await _aasDbContext.JournalVoucherHeaders.FirstOrDefaultAsync(jv => jv.OriginalDocumentId == importRecord.DocumentRecordId, cancellationToken);

                    if (existingJournalVoucherHeader == null || importRecord.AdjustedValue == null)
                    {
                        return NotFound();
                    }

                    if (columnName == "JVNo")
                    {
                        existingJournalVoucherHeader.JournalVoucherHeaderNo = importRecord.AdjustedValue;
                    }
                    if (columnName == "Date")
                    {
                        existingJournalVoucherHeader.Date = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                    }
                    if (columnName == "References")
                    {
                        existingJournalVoucherHeader.References = importRecord.AdjustedValue;
                    }
                    if (columnName == "Particulars")
                    {
                        existingJournalVoucherHeader.Particulars = importRecord.AdjustedValue;
                    }
                    if (columnName == "CRNo")
                    {
                        existingJournalVoucherHeader.CRNo = importRecord.AdjustedValue;
                    }
                    if (columnName == "JVReason")
                    {
                        existingJournalVoucherHeader.JVReason = importRecord.AdjustedValue;
                    }
                    if (columnName == "CreatedBy")
                    {
                        existingJournalVoucherHeader.CreatedBy = importRecord.AdjustedValue;
                    }
                    if (columnName == "CreatedDate")
                    {
                        existingJournalVoucherHeader.CreatedDate = DateTime.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd hh:mm:ss.ffffff", null);
                    }
                    if (columnName == "CancellationRemarks")
                    {
                        existingJournalVoucherHeader.CancellationRemarks = importRecord.AdjustedValue;
                    }
                    if (columnName == "OriginalCVId")
                    {
                        existingJournalVoucherHeader.OriginalCVId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "OriginalSeriesNumber")
                    {
                        existingJournalVoucherHeader.OriginalSeriesNumber = importRecord.AdjustedValue;
                    }
                    if (columnName == "OriginalDocumentId")
                    {
                        existingJournalVoucherHeader.OriginalDocumentId = int.Parse(importRecord.AdjustedValue);
                    }
                }

                #endregion -- Journal Voucher Header --

                #region -- Journal Voucher Details --

                if (tableName == "JournalVoucherDetails")
                {
                    var existingJournalVoucherDetail = await _aasDbContext.JournalVoucherDetails.FirstOrDefaultAsync(jv => jv.OriginalDocumentId == importRecord.DocumentRecordId, cancellationToken);

                    if (existingJournalVoucherDetail == null || importRecord.AdjustedValue == null)
                    {
                        return NotFound();
                    }

                    if (columnName == "AccountNo")
                    {
                        existingJournalVoucherDetail.AccountNo = importRecord.AdjustedValue;
                    }
                    if (columnName == "AccountName")
                    {
                        existingJournalVoucherDetail.AccountName = importRecord.AdjustedValue;
                    }
                    if (columnName == "Debit")
                    {
                        existingJournalVoucherDetail.Debit = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Credit")
                    {
                        existingJournalVoucherDetail.Credit = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "OriginalDocumentId")
                    {
                        existingJournalVoucherDetail.OriginalDocumentId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "JVHeaderId")
                    {
                        existingJournalVoucherDetail.JournalVoucherHeaderId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "TransactionNo")
                    {
                        existingJournalVoucherDetail.TransactionNo = importRecord.AdjustedValue;
                    }
                }

                #endregion -- Journal Voucher Details --

                #endregion -- Accounts Payable --
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                importRecord.Action = procedure;
                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }

            return RedirectToAction(nameof(Index));
        }

        #region -- save as excel sales invoice report --

        public async Task<IActionResult> GenerateLogReport(CancellationToken cancellationToken)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
		    {
                var extractedBy = User.Identity!.Name;
                // Retrieve the selected invoices from the database
                var importExportLogList = await _dbContext.ImportExportLogs
                    .OrderBy(invoice => invoice.TimeStamp)
                    .ToListAsync(cancellationToken: cancellationToken);

                // Create the Excel package
                using var package = new ExcelPackage();
                // Add a new worksheet to the Excel package
                var worksheet = package.Workbook.Worksheets.Add("Import/Export Log");

                var mergedCells = worksheet.Cells["A1:C1"];
                mergedCells.Merge = true;
                    mergedCells.Value = "IMPORT/EXPORT LOG REPORT";
                mergedCells.Style.Font.Size = 13;

                worksheet.Cells["A2"].Value = "Extracted By:";
                worksheet.Cells["A3"].Value = "Company:";

                worksheet.Cells["B2"].Value = $"{extractedBy}";
                worksheet.Cells["B3"].Value = "FILPRIDE";

                worksheet.Cells["A7"].Value = "DOCUMENT RECORD ID";
                worksheet.Cells["B7"].Value = "SERIES NUMBER";
                worksheet.Cells["C7"].Value = "TABLE NAME";
                worksheet.Cells["D7"].Value = "COLUMN NAME";
                worksheet.Cells["E7"].Value = "ORIGINAL VALUE";
                worksheet.Cells["F7"].Value = "ADJUSTED VALUE";
                worksheet.Cells["G7"].Value = "TIME STAMP";
                worksheet.Cells["H7"].Value = "UPLOADED BY";
                worksheet.Cells["I7"].Value = "STATUS";

                // Apply styling to the header row
                using (var range = worksheet.Cells["A7:I7"])
                {
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                int row = 8;

                foreach (var item in importExportLogList)
                {
                    worksheet.Cells[row, 1].Value = item.DocumentRecordId;
                    worksheet.Cells[row, 2].Value = item.DocumentNo;
                    worksheet.Cells[row, 3].Value = item.TableName;
                    worksheet.Cells[row, 4].Value = item.ColumnName;
                    worksheet.Cells[row, 5].Value = item.OriginalValue;
                    worksheet.Cells[row, 6].Value = item.AdjustedValue;
                    worksheet.Cells[row, 7].Value = item.TimeStamp.ToString("MM/dd/yyyy HH:mm:ss tt");
                    worksheet.Cells[row, 8].Value = item.UploadedBy;
                    worksheet.Cells[row, 9].Value = item.Action;

                    row++;
                }

                worksheet.Cells.AutoFitColumns();
                worksheet.View.FreezePanes(8, 1);

                // Convert the Excel package to a byte array
                var excelBytes = await package.GetAsByteArrayAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"ImportExportLogReport_IBS-RCD_{DateTime.Now:yyyyddMMHHmmss}.xlsx");
		    }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index), new { view = DynamicView.BankAccount });
            }

        }

        #endregion
    }
}
