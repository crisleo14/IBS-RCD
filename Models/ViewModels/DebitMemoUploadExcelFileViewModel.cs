namespace Accounting_System.Models.ViewModels
{
    public class DebitMemoUploadExcelFileViewModel
    {
        public string DebitMemoNo { get; set; }

        public DateOnly TransactionDate { get; set; }

        public decimal DebitAmount { get; set; }

        public string Description { get; set; }

        public decimal AdjustedPrice { get; set; }

        public decimal Quantity { get; set; }

        public string Source { get; set; }

        public string Remarks { get; set; }

        public DateOnly Period { get; set; }

        public decimal Amount { get; set; }

        public decimal CurrentAndPreviousAmount { get; set; }

        public decimal UnearnedAmount { get; set; }

        public int ServicesId { get; set; }

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

        public int OriginalSalesInvoiceId { get; set; }

        public string OriginalSeriesNumber { get; set; }

        public int OriginalServiceInvoiceId { get; set; }

        public int OriginalDocumentId { get; set; }
    }
}
