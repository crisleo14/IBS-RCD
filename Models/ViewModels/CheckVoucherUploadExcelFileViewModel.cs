namespace Accounting_System.Models.ViewModels
{
    public class CheckVoucherUploadExcelFileViewModel
    {
        public string CheckVoucherHeaderNo { get; set; }
        public DateOnly Date { get; set; }
        public string[] RRNo { get; set; }
        public string[] SINo { get; set; }
        public string[] PONo { get; set; }
        public string Particulars { get; set; }
        public string CheckNo { get; set; }
        public string Category { get; set; }
        public string Payee { get; set; }
        public DateOnly CheckDate { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public int NumberOfMonths { get; set; }
        public int NumberOfMonthsCreated { get; set; }
        public DateTime LastCreatedDate { get; set; }
        public decimal AmountPerMonth { get; set; }
        public bool IsComplete { get; set; }
        public string AccruedType { get; set; }
        public string Reference { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string PostedBy { get; set; }
        public DateTime PostedDate { get; set; }
        public decimal Total { get; set; }
        public decimal[] Amount { get; set; }
        public decimal CheckAmount { get; set; }
        public string CvType { get; set; }
        public decimal AmountPaid { get; set; }
        public bool IsPaid { get; set; }
        public string CancellationRemarks { get; set; }
        public string CanceledBy { get; set; }
        public DateTime CanceledDate { get; set; }
        public string VoidedBy { get; set; }
        public DateTime VoidedDate { get; set; }
        public string EditedBy { get; set; }
        public DateTime EditedDate { get; set; }
        public int OriginalBankId { get; set; }
        public string OriginalSeriesNumber { get; set; }
        public int OriginalSupplierId { get; set; }
        public int OriginalDocumentId { get; set; }
    }
}
