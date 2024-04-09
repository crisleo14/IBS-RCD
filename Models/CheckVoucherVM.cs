namespace Accounting_System.Models
{
    public class CheckVoucherVM
    {
        public CheckVoucherHeader? Header { get; set; }
        public List<CheckVoucherDetail>? Details { get; set; }
    }
}