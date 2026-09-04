namespace Accounting_System.Models.ViewModels
{
    public class JournalVoucherUploadExcelFileViewModel
    {
        public string JournalVoucherHeaderNo { get; set; }
        public DateOnly Date { get; set; }
        public string References { get; set; }
        public string Particulars { get; set; }
        public string CRNo { get; set; }
        public string JVReason { get; set; }
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
        public int OriginalCVId { get; set; }
        public string OriginalSeriesNumber { get; set; }
        public int OriginalDocumentId { get; set; }
    }
}
