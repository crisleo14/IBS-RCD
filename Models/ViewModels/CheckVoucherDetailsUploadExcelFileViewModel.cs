namespace Accounting_System.Models.ViewModels
{
    public class CheckVoucherDetailsUploadExcelFileViewModel
    {
        public string AccountNo { get; set; }
        public string AccountName { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public int CvHeaderId { get; set; }
        public int OriginalDocumentId { get; set; }
        public decimal Amount { get; set; }
        public decimal AmountPaid { get; set; }
        public int SupplierId { get; set; }
        public decimal EwtPercent { get; set; }
        public bool IsUserSelected { get; set; }
        public bool IsVatable { get; set; }
    }
}
