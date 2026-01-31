namespace Accounting_System.Models.ViewModels
{
    public class CvTradePaymentUploadExcelFileViewModel
    {
        public int DocumentId { get; set; }

        public string DocumentType { get; set; }

        public int CheckVoucherId { get; set; }

        public decimal AmountPaid { get; set; }
    }
}
