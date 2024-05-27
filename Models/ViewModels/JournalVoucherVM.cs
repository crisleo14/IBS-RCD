namespace Accounting_System.Models.ViewModels
{
    public class JournalVoucherVM
    {
        public JournalVoucherHeader? Header { get; set; }
        public List<JournalVoucherDetail>? Details { get; set; }
    }
}