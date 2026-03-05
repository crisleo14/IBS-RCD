namespace Accounting_System.Models.ViewModels
{
    public class JournalVoucherDetailsUploadExcelFileViewModel
    {
        public string AccountNo { get; set; } = " ";

        public string AccountName { get; set; } = " ";

        public string TransactionNo { get; set; } = " ";

        public decimal Debit { get; set; }

        public decimal Credit { get; set; }

        public int JournalVoucherHeaderId { get; set; }

        public int OriginalDocumentId { get; set; }
    }
}
