namespace Accounting_System.DTOs
{
    public class FindCvMultiplePaymentInDbContextDto
    {
        public IReadOnlyDictionary<int, int> CheckVoucherHeaderPaymentId { get; init; }
            = new Dictionary<int, int>();

        public IReadOnlyDictionary<int, int> CheckVoucherHeaderInvoiceId { get; init; }
            = new Dictionary<int, int>();
    }
}
