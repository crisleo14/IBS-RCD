namespace Accounting_System.Models.ViewModels
{
    public class CollectionReceiptUploadExcelFileViewModel
    {
        public string CollectionReceiptNo { get; set; }
        public DateOnly TransactionDate { get; set; }
        public string ReferenceNo { get; set; }
        public string Remarks { get; set; }
        public decimal CashAmount { get; set; }
        public DateOnly? CheckDate { get; set; }
        public string? CheckNo { get; set; }
        public string? CheckBank { get; set; }
        public string? CheckBranch { get; set; }
        public decimal CheckAmount { get; set; }
        public DateOnly? ManagerCheckDate { get; set; }
        public string? ManagerCheckNo { get; set; }
        public string? ManagerCheckBank { get; set; }
        public string? ManagerCheckBranch { get; set; }
        public decimal ManagerCheckAmount { get; set; }
        public decimal EWT { get; set; }
        public decimal WVAT { get; set; }
        public decimal Total { get; set; }
        public bool IsCertificateUpload { get; set; }
        public string F2306FilePath { get; set; }
        public string F2307FilePath { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string PostedBy { get; set; }
        public DateTime PostedDate { get; set; }
        public string CancellationRemarks { get; set; }
        public string[]? MultipleSI { get; set; }
        public int[]? MultipleSIId { get; set; }
        public decimal[]? SIMultipleAmount { get; set; }
        public DateOnly[]? MultipleTransactionDate { get; set; }
        public int OriginalCustomerId { get; set; }
        public int OriginalSalesInvoiceId { get; set; }
        public string OriginalSeriesNumber { get; set; }
        public int OriginalServiceInvoiceId { get; set; }
        public int OriginalDocumentId { get; set; }
        public int SalesInvoiceId { get; set; }
        public int SalesInvoiceNo { get; set; }
        public int ServiceInvoiceId { get; set; }
        public int ServiceInvoiceNo { get; set; }
    }
}
