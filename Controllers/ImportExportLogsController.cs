using Accounting_System.Data;
using Accounting_System.Models;
using Accounting_System.Models.AccountsPayable;
using Accounting_System.Repository;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Accounting_System.Controllers
{
    public class ImportExportLogsController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<IdentityUser> _userManager;

        private readonly GeneralRepo _generalRepo;

        public ImportExportLogsController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, GeneralRepo generalRepo)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _generalRepo = generalRepo;
        }
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var importExportLogs = await _dbContext.ImportExportLogs
                .Where(iel => iel.Action == string.Empty).ToListAsync(cancellationToken);

            return View(importExportLogs);
        }

        public async Task<IActionResult> ImportAction(Guid id, string procedure, string tableName, string columnName, CancellationToken cancellationToken)
        {
            var importRecord = await _dbContext.ImportExportLogs.FindAsync(id, cancellationToken);
            if (procedure == "Modify")
            {
                #region -- Accounts Receivable --

                #region -- Sales Invoice --

                if (tableName == "SalesInvoice")
                {
                    var existingSI = await _dbContext.SalesInvoices.FirstOrDefaultAsync(si => si.OriginalDocumentId == importRecord.DocumentRecordId, cancellationToken);

                    if (columnName == "SINo")
                    {
                        existingSI.SINo = importRecord.AdjustedValue;
                    }
                    if (columnName == "OriginalCustomerId")
                    {
                        existingSI.OriginalCustomerId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "OriginalProductId")
                    {
                        existingSI.OriginalProductId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "OtherRefNo")
                    {
                        existingSI.OtherRefNo = importRecord.AdjustedValue;
                    }
                    if (columnName == "Quantity")
                    {
                        existingSI.Quantity = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "UnitPrice")
                    {
                        existingSI.UnitPrice = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Amount")
                    {
                        existingSI.Amount = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Remarks")
                    {
                        existingSI.Remarks = importRecord.AdjustedValue;
                    }
                    if (columnName == "Status")
                    {
                        existingSI.Status = importRecord.AdjustedValue;
                    }
                    if (columnName == "TransactionDate")
                    {
                        existingSI.TransactionDate = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                    }
                    if (columnName == "Discount")
                    {
                        existingSI.Discount = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "DueDate")
                    {
                        existingSI.DueDate = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                    }
                    if (columnName == "CreatedBy")
                    {
                        existingSI.CreatedBy = importRecord.AdjustedValue;
                    }
                    if (columnName == "CreatedDate")
                    {
                        existingSI.CreatedDate = DateTime.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd hh:mm:ss.ffffff", null);
                    }
                    if (columnName == "CancellationRemarks")
                    {
                        existingSI.CancellationRemarks = importRecord.AdjustedValue;
                    }
                    if (columnName == "OriginalSeriesNumber")
                    {
                        existingSI.OriginalSeriesNumber = importRecord.AdjustedValue;
                    }
                    if (columnName == "OriginalDocumentId")
                    {
                        existingSI.OriginalDocumentId = int.Parse(importRecord.AdjustedValue);
                    }
                }

                #endregion -- Sales Invoice --

                #region -- Service Invoice --

                if (tableName == "ServiceInvoice")
                {
                    var existingSV = await _dbContext.ServiceInvoices.FirstOrDefaultAsync(sv => sv.OriginalDocumentId == importRecord.DocumentRecordId, cancellationToken);

                    if (columnName == "SVNo")
                    {
                        existingSV.SVNo = importRecord.AdjustedValue;
                    }
                    if (columnName == "DueDate")
                    {
                        existingSV.DueDate = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                    }
                    if (columnName == "Period")
                    {
                        existingSV.Period = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                    }
                    if (columnName == "Amount")
                    {
                        existingSV.Amount = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Total")
                    {
                        existingSV.Total = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Discount")
                    {
                        existingSV.Discount = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "CurrentAndPreviousAmount")
                    {
                        existingSV.CurrentAndPreviousAmount = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "UnearnedAmount")
                    {
                        existingSV.UnearnedAmount = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Status")
                    {
                        existingSV.Status = importRecord.AdjustedValue;
                    }
                    if (columnName == "Instructions")
                    {
                        existingSV.Instructions = importRecord.AdjustedValue;
                    }
                    if (columnName == "CreatedBy")
                    {
                        existingSV.CreatedBy = importRecord.AdjustedValue;
                    }
                    if (columnName == "CreatedDate")
                    {
                        existingSV.CreatedDate = DateTime.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd hh:mm:ss.ffffff", null);
                    }
                    if (columnName == "CancellationRemarks")
                    {
                        existingSV.CancellationRemarks = importRecord.AdjustedValue;
                    }
                    if (columnName == "OriginalCustomerId")
                    {
                        existingSV.OriginalCustomerId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "OriginalSeriesNumber")
                    {
                        existingSV.OriginalSeriesNumber = importRecord.AdjustedValue;
                    }
                    if (columnName == "OriginalServicesId")
                    {
                        existingSV.OriginalServicesId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "OriginalDocumentId")
                    {
                        existingSV.OriginalDocumentId = int.Parse(importRecord.AdjustedValue);
                    }
                }

                #endregion -- Service Invoice --

                #region -- Collection Receipt --

                    if (tableName == "CollectionReceipt")
                    {
                        var existingCR = await _dbContext.CollectionReceipts.FirstOrDefaultAsync(cr => cr.OriginalDocumentId == importRecord.DocumentRecordId, cancellationToken);

                        if (columnName == "CRNo")
                        {
                            existingCR.CRNo = importRecord.AdjustedValue;
                        }
                        if (columnName == "TransactionDate")
                        {
                            existingCR.TransactionDate = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                        }
                        if (columnName == "ReferenceNo")
                        {
                            existingCR.ReferenceNo = importRecord.AdjustedValue;
                        }
                        if (columnName == "Remarks")
                        {
                            existingCR.Remarks = importRecord.AdjustedValue;
                        }
                        if (columnName == "CashAmount")
                        {
                            existingCR.CashAmount = decimal.Parse(importRecord.AdjustedValue);
                        }
                        if (columnName == "CheckDate")
                        {
                            existingCR.CheckDate = importRecord.AdjustedValue;
                        }
                        if (columnName == "CheckNo")
                        {
                            existingCR.CheckNo = importRecord.AdjustedValue;
                        }
                        if (columnName == "CheckBank")
                        {
                            existingCR.CheckBank = importRecord.AdjustedValue;
                        }
                        if (columnName == "CheckBranch")
                        {
                            existingCR.CheckBranch = importRecord.AdjustedValue;
                        }
                        if (columnName == "CheckAmount")
                        {
                            existingCR.CheckAmount = decimal.Parse(importRecord.AdjustedValue);
                        }
                        if (columnName == "ManagerCheckDate")
                        {
                            existingCR.ManagerCheckDate = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                        }
                        if (columnName == "ManagerCheckNo")
                        {
                            existingCR.ManagerCheckNo = importRecord.AdjustedValue;
                        }
                        if (columnName == "ManagerCheckBank")
                        {
                            existingCR.ManagerCheckBank = importRecord.AdjustedValue;
                        }
                        if (columnName == "ManagerCheckBranch")
                        {
                            existingCR.ManagerCheckBranch = importRecord.AdjustedValue;
                        }
                        if (columnName == "ManagerCheckAmount")
                        {
                            existingCR.ManagerCheckAmount = decimal.Parse(importRecord.AdjustedValue);
                        }
                        if (columnName == "EWT")
                        {
                            existingCR.EWT = decimal.Parse(importRecord.AdjustedValue);
                        }
                        if (columnName == "WVAT")
                        {
                            existingCR.WVAT = decimal.Parse(importRecord.AdjustedValue);
                        }
                        if (columnName == "Total")
                        {
                            existingCR.Total = decimal.Parse(importRecord.AdjustedValue);
                        }
                        if (columnName == "IsCertificateUpload")
                        {
                            existingCR.IsCertificateUpload = bool.Parse(importRecord.AdjustedValue);
                        }
                        if (columnName == "F2306FilePath")
                        {
                            existingCR.F2306FilePath = importRecord.AdjustedValue;
                        }
                        if (columnName == "F2307FilePath")
                        {
                            existingCR.F2307FilePath = importRecord.AdjustedValue;
                        }
                        if (columnName == "CreatedBy")
                        {
                            existingCR.CreatedBy = importRecord.AdjustedValue;
                        }
                        if (columnName == "CreatedDate")
                        {
                            existingCR.CreatedDate = DateTime.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd hh:mm:ss.ffffff", null);
                        }
                        if (columnName == "CancellationRemarks")
                        {
                            existingCR.CancellationRemarks = importRecord.AdjustedValue;
                        }
                        if (columnName == "MultipleSI")
                        {
                            existingCR.MultipleSI = [importRecord.AdjustedValue];
                        }
                        if (columnName == "MultipleSIId")
                        {
                            existingCR.MultipleSIId = [int.Parse(importRecord.AdjustedValue)];
                        }
                        if (columnName == "SIMultipleAmount")
                        {
                            existingCR.SIMultipleAmount = [decimal.Parse(importRecord.AdjustedValue)];
                        }
                        if (columnName == "MultipleTransactionDate")
                        {
                            existingCR.MultipleTransactionDate = [DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd")];
                        }
                        if (columnName == "OriginalCustomerId")
                        {
                            existingCR.OriginalCustomerId = int.Parse(importRecord.AdjustedValue);
                        }
                        if (columnName == "OriginalSalesInvoiceId")
                        {
                            existingCR.OriginalSalesInvoiceId = int.Parse(importRecord.AdjustedValue);
                        }
                        if (columnName == "OriginalSeriesNumber")
                        {
                            existingCR.OriginalSeriesNumber = importRecord.AdjustedValue;
                        }
                        if (columnName == "OriginalServiceInvoiceId")
                        {
                            existingCR.OriginalServiceInvoiceId = int.Parse(importRecord.AdjustedValue);
                        }
                        if (columnName == "OriginalDocumentId")
                        {
                            existingCR.OriginalDocumentId = int.Parse(importRecord.AdjustedValue);
                        }
                    }

                #endregion -- Collection Receipt --

                #region -- Offsettings --

                if (tableName == "Offsetting")
                {
                    var existingCR = await _dbContext.CollectionReceipts.FirstOrDefaultAsync(cr => cr.OriginalDocumentId == importRecord.DocumentRecordId, cancellationToken);
                    var existingOffset = await _dbContext.Offsettings.FirstOrDefaultAsync(offset => offset.Reference == existingCR.OriginalSeriesNumber, cancellationToken);

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
                    var existingDM = await _dbContext.DebitMemos.FirstOrDefaultAsync(dm => dm.OriginalDocumentId == importRecord.DocumentRecordId, cancellationToken);

                    if (columnName == "DMNo")
                    {
                        existingDM.DMNo = importRecord.AdjustedValue;
                    }
                    if (columnName == "TransactionDate")
                    {
                        existingDM.TransactionDate = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                    }
                    if (columnName == "DebitAmount")
                    {
                        existingDM.DebitAmount = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Description")
                    {
                        existingDM.Description = importRecord.AdjustedValue;
                    }
                    if (columnName == "AdjustedPrice")
                    {
                        existingDM.AdjustedPrice = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Quantity")
                    {
                        existingDM.Quantity = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Source")
                    {
                        existingDM.Source = importRecord.AdjustedValue;
                    }
                    if (columnName == "Remarks")
                    {
                        existingDM.Remarks = importRecord.AdjustedValue;
                    }
                    if (columnName == "Period")
                    {
                        existingDM.Period = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                    }
                    if (columnName == "Amount")
                    {
                        existingDM.Amount = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "CurrentAndPreviousAmount")
                    {
                        existingDM.CurrentAndPreviousAmount = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "UnearnedAmount")
                    {
                        existingDM.UnearnedAmount = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "ServicesId")
                    {
                        existingDM.ServicesId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "CreatedBy")
                    {
                        existingDM.CreatedBy = importRecord.AdjustedValue;
                    }
                    if (columnName == "CreatedDate")
                    {
                        existingDM.CreatedDate = DateTime.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd hh:mm:ss.ffffff", null);
                    }
                    if (columnName == "CancellationRemarks")
                    {
                        existingDM.CancellationRemarks = importRecord.AdjustedValue;
                    }
                    if (columnName == "OriginalSalesInvoiceId")
                    {
                        existingDM.OriginalSalesInvoiceId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "OriginalSeriesNumber")
                    {
                        existingDM.OriginalSeriesNumber = importRecord.AdjustedValue;
                    }
                    if (columnName == "OriginalServiceInvoiceId")
                    {
                        existingDM.OriginalServiceInvoiceId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "OriginalDocumentId")
                    {
                        existingDM.OriginalDocumentId = int.Parse(importRecord.AdjustedValue);
                    }
                }

                #endregion -- Debit Memo --

                #region -- Credit Memo --

                if (tableName == "CreditMemo")
                {
                    var existingCM = await _dbContext.CreditMemos.FirstOrDefaultAsync(cm => cm.OriginalDocumentId == importRecord.DocumentRecordId, cancellationToken);

                    if (columnName == "CMNo")
                    {
                        existingCM.CMNo = importRecord.AdjustedValue;
                    }
                    if (columnName == "TransactionDate")
                    {
                        existingCM.TransactionDate = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                    }
                    if (columnName == "CreditAmount")
                    {
                        existingCM.CreditAmount = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Description")
                    {
                        existingCM.Description = importRecord.AdjustedValue;
                    }
                    if (columnName == "AdjustedPrice")
                    {
                        existingCM.AdjustedPrice = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Quantity")
                    {
                        existingCM.Quantity = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Source")
                    {
                        existingCM.Source = importRecord.AdjustedValue;
                    }
                    if (columnName == "Remarks")
                    {
                        existingCM.Remarks = importRecord.AdjustedValue;
                    }
                    if (columnName == "Period")
                    {
                        existingCM.Period = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                    }
                    if (columnName == "Amount")
                    {
                        existingCM.Amount = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "CurrentAndPreviousAmount")
                    {
                        existingCM.CurrentAndPreviousAmount = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "UnearnedAmount")
                    {
                        existingCM.UnearnedAmount = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "ServicesId")
                    {
                        existingCM.ServicesId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "CreatedBy")
                    {
                        existingCM.CreatedBy = importRecord.AdjustedValue;
                    }
                    if (columnName == "CreatedDate")
                    {
                        existingCM.CreatedDate = DateTime.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd hh:mm:ss.ffffff", null);
                    }
                    if (columnName == "CancellationRemarks")
                    {
                        existingCM.CancellationRemarks = importRecord.AdjustedValue;
                    }
                    if (columnName == "OriginalSalesInvoiceId")
                    {
                        existingCM.OriginalSalesInvoiceId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "OriginalSeriesNumber")
                    {
                        existingCM.OriginalSeriesNumber = importRecord.AdjustedValue;
                    }
                    if (columnName == "OriginalServiceInvoiceId")
                    {
                        existingCM.OriginalServiceInvoiceId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "OriginalDocumentId")
                    {
                        existingCM.OriginalDocumentId = int.Parse(importRecord.AdjustedValue);
                    }
                }

                #endregion -- Credit Memo --

                #endregion -- Accounts Receivable --

                #region -- Accounts Payable --

                #region -- Purchase Order --

                if (tableName == "PurchaseOrder")
                {
                    var existingPO = await _dbContext.PurchaseOrders.FirstOrDefaultAsync(po => po.OriginalDocumentId == importRecord.DocumentRecordId, cancellationToken);

                    if (columnName == "PONo")
                    {
                        existingPO.PONo = importRecord.AdjustedValue;
                    }
                    if (columnName == "Date")
                    {
                        existingPO.Date = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                    }
                    if (columnName == "Terms")
                    {
                        existingPO.Terms = importRecord.AdjustedValue;
                    }
                    if (columnName == "Quantity")
                    {
                        existingPO.Quantity = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Price")
                    {
                        existingPO.Price = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Amount")
                    {
                        existingPO.Amount = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "FinalPrice")
                    {
                        existingPO.FinalPrice = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Remarks")
                    {
                        existingPO.Remarks = importRecord.AdjustedValue;
                    }
                    if (columnName == "CreatedBy")
                    {
                        existingPO.CreatedBy = importRecord.AdjustedValue;
                    }
                    if (columnName == "CreatedDate")
                    {
                        existingPO.CreatedDate = DateTime.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd hh:mm:ss.ffffff", null);
                    }
                    if (columnName == "IsClosed")
                    {
                        existingPO.IsClosed = bool.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "CancellationRemarks")
                    {
                        existingPO.CancellationRemarks = importRecord.AdjustedValue;
                    }
                    if (columnName == "OriginalProductId")
                    {
                        existingPO.OriginalProductId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "OriginalSeriesNumber")
                    {
                        existingPO.OriginalSeriesNumber = importRecord.AdjustedValue;
                    }
                    if (columnName == "OriginalSupplierId")
                    {
                        existingPO.OriginalSupplierId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "OriginalDocumentId")
                    {
                        existingPO.OriginalDocumentId = int.Parse(importRecord.AdjustedValue);
                    }
                }

                #endregion -- Purchase Order --

                #region -- Receiving Report --

                if (tableName == "ReceivingReport")
                {
                    var existingRR = await _dbContext.ReceivingReports.FirstOrDefaultAsync(rr => rr.OriginalDocumentId == importRecord.DocumentRecordId, cancellationToken);

                    if (columnName == "RRNo")
                    {
                        existingRR.RRNo = importRecord.AdjustedValue;
                    }
                    if (columnName == "Date")
                    {
                        existingRR.Date = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                    }
                    if (columnName == "DueDate")
                    {
                        existingRR.DueDate = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                    }
                    if (columnName == "SupplierInvoiceNumber")
                    {
                        existingRR.SupplierInvoiceNumber = importRecord.AdjustedValue;
                    }
                    if (columnName == "SupplierInvoiceDate")
                    {
                        existingRR.SupplierInvoiceDate = importRecord.AdjustedValue;
                    }
                    if (columnName == "TruckOrVessels")
                    {
                        existingRR.TruckOrVessels = importRecord.AdjustedValue;
                    }
                    if (columnName == "QuantityDelivered")
                    {
                        existingRR.QuantityDelivered = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "QuantityReceived")
                    {
                        existingRR.QuantityReceived = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "GainOrLoss")
                    {
                        existingRR.GainOrLoss = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Amount")
                    {
                        existingRR.Amount = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "OtherRef")
                    {
                        existingRR.OtherRef = importRecord.AdjustedValue;
                    }
                    if (columnName == "Remarks")
                    {
                        existingRR.Remarks = importRecord.AdjustedValue;
                    }
                    if (columnName == "CreatedBy")
                    {
                        existingRR.CreatedBy = importRecord.AdjustedValue;
                    }
                    if (columnName == "CreatedDate")
                    {
                        existingRR.CreatedDate = DateTime.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd hh:mm:ss.ffffff", null);
                    }
                    if (columnName == "CancellationRemarks")
                    {
                        existingRR.CancellationRemarks = importRecord.AdjustedValue;
                    }
                    if (columnName == "ReceivedDate")
                    {
                        existingRR.ReceivedDate = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                    }
                    if (columnName == "OriginalPOId")
                    {
                        existingRR.OriginalPOId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "OriginalSeriesNumber")
                    {
                        existingRR.OriginalSeriesNumber = importRecord.AdjustedValue;
                    }
                    if (columnName == "OriginalDocumentId")
                    {
                        existingRR.OriginalDocumentId = int.Parse(importRecord.AdjustedValue);
                    }
                }

                #endregion -- Receiving Report --

                #region -- Check Voucher Header --

                if (tableName == "CheckVoucherHeader")
                {
                    var existingCVH = await _dbContext.CheckVoucherHeaders.FirstOrDefaultAsync(cv => cv.OriginalDocumentId == importRecord.DocumentRecordId, cancellationToken);

                    if (columnName == "CVNo")
                    {
                        existingCVH.CVNo = importRecord.AdjustedValue;
                    }
                    if (columnName == "Date")
                    {
                        existingCVH.Date = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                    }
                    if (columnName == "RRNo")
                    {
                        existingCVH.RRNo = [importRecord.AdjustedValue];
                    }
                    if (columnName == "SINo")
                    {
                        existingCVH.SINo = [importRecord.AdjustedValue];
                    }
                    if (columnName == "PONo")
                    {
                        existingCVH.PONo = [importRecord.AdjustedValue];
                    }
                    if (columnName == "Particulars")
                    {
                        existingCVH.Particulars = importRecord.AdjustedValue;
                    }
                    if (columnName == "CheckNo")
                    {
                        existingCVH.CheckNo = importRecord.AdjustedValue;
                    }
                    if (columnName == "Category")
                    {
                        existingCVH.Category = importRecord.AdjustedValue;
                    }
                    if (columnName == "Payee")
                    {
                        existingCVH.Payee = importRecord.AdjustedValue;
                    }
                    if (columnName == "CheckDate")
                    {
                        existingCVH.CheckDate = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                    }
                    if (columnName == "StartDate")
                    {
                        existingCVH.StartDate = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                    }
                    if (columnName == "EndDate")
                    {
                        existingCVH.EndDate = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                    }
                    if (columnName == "NumberOfMonths")
                    {
                        existingCVH.NumberOfMonths = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "NumberOfMonthsCreated")
                    {
                        existingCVH.NumberOfMonthsCreated = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "LastCreatedDate")
                    {
                        existingCVH.LastCreatedDate = DateTime.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd hh:mm:ss.ffffff", null);
                    }
                    if (columnName == "AmountPerMonth")
                    {
                        existingCVH.AmountPerMonth = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "IsComplete")
                    {
                        existingCVH.IsComplete = bool.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "AccruedType")
                    {
                        existingCVH.AccruedType = importRecord.AdjustedValue;
                    }
                    if (columnName == "Reference" && existingCVH.CvType == "Payment")
                    {
                        existingCVH.Reference = importRecord.AdjustedValue;
                    }
                    if (columnName == "CreatedBy")
                    {
                        existingCVH.CreatedBy = importRecord.AdjustedValue;
                    }
                    if (columnName == "CreatedDate")
                    {
                        existingCVH.CreatedDate = DateTime.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd hh:mm:ss.ffffff", null);
                    }
                    if (columnName == "Total")
                    {
                        existingCVH.Total = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Amount")
                    {
                        existingCVH.Amount = [decimal.Parse(importRecord.AdjustedValue)];
                    }
                    if (columnName == "CheckAmount")
                    {
                        existingCVH.CheckAmount = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "CvType")
                    {
                        existingCVH.CvType = importRecord.AdjustedValue;
                    }
                    if (columnName == "AmountPaid")
                    {
                        existingCVH.AmountPaid = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "IsPaid")
                    {
                        existingCVH.IsPaid = bool.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "CancellationRemarks")
                    {
                        existingCVH.CancellationRemarks = importRecord.AdjustedValue;
                    }
                    if (columnName == "OriginalBankId")
                    {
                        existingCVH.OriginalBankId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "OriginalSeriesNumber")
                    {
                        existingCVH.OriginalSeriesNumber = importRecord.AdjustedValue;
                    }
                    if (columnName == "OriginalSupplierId")
                    {
                        existingCVH.OriginalSupplierId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "OriginalDocumentId")
                    {
                        existingCVH.OriginalDocumentId = int.Parse(importRecord.AdjustedValue);
                    }
                }

                #endregion -- Check Voucher Header --

                #region -- Check Voucher Details --

                if (tableName == "CheckVoucherDetails")
                {
                    var existingCVD = await _dbContext.CheckVoucherDetails.FirstOrDefaultAsync(cv => cv.OriginalDocumentId == importRecord.DocumentRecordId, cancellationToken);

                    if (columnName == "AccountNo")
                    {
                        existingCVD.AccountNo = importRecord.AdjustedValue;
                    }
                    if (columnName == "AccountName")
                    {
                        existingCVD.AccountName = importRecord.AdjustedValue;
                    }
                    if (columnName == "Debit")
                    {
                        existingCVD.Debit = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Credit")
                    {
                        existingCVD.Credit = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "OriginalDocumentId")
                    {
                        existingCVD.OriginalDocumentId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "CVHeaderId")
                    {
                        existingCVD.CVHeaderId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "TransactionNo")
                    {
                        existingCVD.TransactionNo = importRecord.AdjustedValue;
                    }
                }

                #endregion -- Check Voucher Details --

                #region -- Journal Voucher Header --

                if (tableName == "JournalVoucherHeader")
                {
                    var existingJVH = await _dbContext.JournalVoucherHeaders.FirstOrDefaultAsync(jv => jv.OriginalDocumentId == importRecord.DocumentRecordId, cancellationToken);

                    if (columnName == "JVNo")
                    {
                        existingJVH.JVNo = importRecord.AdjustedValue;
                    }
                    if (columnName == "Date")
                    {
                        existingJVH.Date = DateOnly.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd");
                    }
                    if (columnName == "References")
                    {
                        existingJVH.References = importRecord.AdjustedValue;
                    }
                    if (columnName == "Particulars")
                    {
                        existingJVH.Particulars = importRecord.AdjustedValue;
                    }
                    if (columnName == "CRNo")
                    {
                        existingJVH.CRNo = importRecord.AdjustedValue;
                    }
                    if (columnName == "JVReason")
                    {
                        existingJVH.JVReason = importRecord.AdjustedValue;
                    }
                    if (columnName == "CreatedBy")
                    {
                        existingJVH.CreatedBy = importRecord.AdjustedValue;
                    }
                    if (columnName == "CreatedDate")
                    {
                        existingJVH.CreatedDate = DateTime.ParseExact(importRecord.AdjustedValue, "yyyy-MM-dd hh:mm:ss.ffffff", null);
                    }
                    if (columnName == "CancellationRemarks")
                    {
                        existingJVH.CancellationRemarks = importRecord.AdjustedValue;
                    }
                    if (columnName == "OriginalCVId")
                    {
                        existingJVH.OriginalCVId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "OriginalSeriesNumber")
                    {
                        existingJVH.OriginalSeriesNumber = importRecord.AdjustedValue;
                    }
                    if (columnName == "OriginalDocumentId")
                    {
                        existingJVH.OriginalDocumentId = int.Parse(importRecord.AdjustedValue);
                    }
                }

                #endregion -- Journal Voucher Header --

                #region -- Journal Voucher Details --

                if (tableName == "JournalVoucherDetails")
                {
                    var existingJVD = await _dbContext.JournalVoucherDetails.FirstOrDefaultAsync(jv => jv.OriginalDocumentId == importRecord.DocumentRecordId, cancellationToken);

                    if (columnName == "AccountNo")
                    {
                        existingJVD.AccountNo = importRecord.AdjustedValue;
                    }
                    if (columnName == "AccountName")
                    {
                        existingJVD.AccountName = importRecord.AdjustedValue;
                    }
                    if (columnName == "Debit")
                    {
                        existingJVD.Debit = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "Credit")
                    {
                        existingJVD.Credit = decimal.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "OriginalDocumentId")
                    {
                        existingJVD.OriginalDocumentId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "JVHeaderId")
                    {
                        existingJVD.JVHeaderId = int.Parse(importRecord.AdjustedValue);
                    }
                    if (columnName == "TransactionNo")
                    {
                        existingJVD.TransactionNo = importRecord.AdjustedValue;
                    }
                }

                #endregion -- Journal Voucher Details --

                #endregion -- Accounts Payable --
            }

            if (importRecord != null)
            {
                importRecord.Action = procedure;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
