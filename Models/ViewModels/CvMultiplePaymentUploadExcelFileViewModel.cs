namespace Accounting_System.Models.ViewModels
{
    public class CvMultiplePaymentUploadExcelFileViewModel
    {
        public Guid Id { get; set; }
        public int CheckVoucherHeaderPaymentId { get; set; }
        public int CheckVoucherHeaderInvoiceId { get; set; }
        public decimal AmountPaid { get; set; }
    }
}
