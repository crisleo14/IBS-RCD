namespace Accounting_System.DTOs
{
    public class FindCvTradePaymentInDbContextDto
    {
        public IReadOnlyDictionary<int, int> ReceivingReportId { get; init; }
            = new Dictionary<int, int>();

        public IReadOnlyDictionary<int, int> CheckVoucherHeaderId { get; init; }
            = new Dictionary<int, int>();
    }
}
