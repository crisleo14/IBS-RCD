using Accounting_System.Models.AccountsPayable;
using Accounting_System.Models.MasterFile;

namespace Accounting_System.Models.ViewModels
{
    public class CheckVoucherVM
    {
        public CheckVoucherHeader? Header { get; set; }
        public List<CheckVoucherDetail>? Details { get; set; }

        public Supplier? Supplier { get; set; }
    }
}
