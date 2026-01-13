namespace Accounting_System.Models.ViewModels
{
    public class ReceivingReportUploadExcelFileViewModel
    {
        public string ReceivingReportNo { get; set; }
        public DateOnly Date { get; set; }
        public DateOnly DueDate { get; set; }
        public string SupplierInvoiceNumber { get; set; }
        public string SupplierInvoiceDate { get; set; }
        public string TruckOrVessels { get; set; }
        public decimal QuantityDelivered { get; set; }
        public decimal QuantityReceived { get; set; }
        public decimal GainOrLoss { get; set; }
        public decimal Amount { get; set; }
        public string OtherRef { get; set; }
        public string Remarks { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string PostedBy { get; set; }
        public DateTime PostedDate { get; set; }
        public string CancellationRemarks { get; set; }
        public DateOnly ReceivedDate { get; set; }
        public int OriginalPOId { get; set; }
        public string OriginalSeriesNumber { get; set; }
        public int OriginalDocumentId { get; set; }
    }
}
