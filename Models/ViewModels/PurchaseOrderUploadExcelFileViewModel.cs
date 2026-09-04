namespace Accounting_System.Models.ViewModels
{
    public class PurchaseOrderUploadExcelFileViewModel
    {
        public string PurchaseOrderNo { get; set; }
        public DateOnly Date { get; set; }
        public string Terms { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Amount { get; set; }
        public decimal? FinalPrice { get; set; }
        public string Remarks { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string PostedBy { get; set; }
        public DateTime PostedDate { get; set; }
        public bool IsClosed { get; set; }
        public string CancellationRemarks { get; set; }
        public string CanceledBy { get; set; }
        public DateTime CanceledDate { get; set; }
        public string VoidedBy { get; set; }
        public DateTime VoidedDate { get; set; }
        public string EditedBy { get; set; }
        public DateTime EditedDate { get; set; }
        public int OriginalProductId { get; set; }
        public string OriginalSeriesNumber { get; set; }
        public int OriginalSupplierId { get; set; }
        public int OriginalDocumentId { get; set; }
    }
}
