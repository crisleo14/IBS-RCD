using Accounting_System.Models.AccountsPayable;

namespace Accounting_System.Models.ViewModels
{
    public class CheckVoucherVM
    {
        public CheckVoucherHeader? Header { get; set; }
        public List<CheckVoucherDetail>? Details { get; set; }
    }
}