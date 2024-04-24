namespace Accounting_System.Models
{
    public class JournalVoucherVM
    {
        public JournalVoucherHeader? Header { get; set; }
        public List<JournalVoucherDetail>? Details { get; set; }
    }
}