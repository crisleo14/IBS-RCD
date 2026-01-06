namespace Accounting_System.Models.ViewModels
{
    public class ServiceInvoiceUploadExcelFileViewModel
    {
        public string ServiceInvoiceNo { get; set; }
        public DateOnly DueDate { get; set; }
        public DateOnly Period { get; set; }
        public decimal Amount { get; set; }
        public decimal Total { get; set; }
        public decimal Discount { get; set; }
        public decimal CurrentAndPreviousAmount { get; set; }
        public decimal UnearnedAmount { get; set; }
        public string Status { get; set; }
        public string Instructions { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string PostedBy { get; set; }
        public DateTime PostedDate { get; set; }
        public string CancellationRemarks { get; set; }
        public int OriginalCustomerId { get; set; }
        public string OriginalSeriesNumber { get; set; }
        public int OriginalServicesId { get; set; }
        public int OriginalDocumentId { get; set; }
    }
}
