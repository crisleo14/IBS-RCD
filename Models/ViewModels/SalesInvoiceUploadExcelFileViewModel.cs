namespace Accounting_System.Models.ViewModels
{
    public class SalesInvoiceUploadExcelFileViewModel
    {
        public string SalesInvoiceNo { get; set; }
        public string OtherRefNo { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }
        public string Remarks { get; set; }
        public string Status { get; set; }
        public DateOnly TransactionDate { get; set; }
        public decimal Discount { get; set; }
        public DateOnly DueDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string PostedBy { get; set; }
        public DateTime PostedDate { get; set; }
        public string CancellationRemarks { get; set; }
        public string CanceledBy { get; set; }
        public DateTime CanceledDate { get; set; }
        public string VoidedBy { get; set; }
        public DateTime VoidedDate { get; set; }
        public string EditedBy { get; set; }
        public DateTime EditedDate { get; set; }
        public int OriginalCustomerId { get; set; }
        public int OriginalProductId { get; set; }
        public string OriginalSeriesNumber { get; set; }
        public int OriginalDocumentId { get; set; }
    }
}
