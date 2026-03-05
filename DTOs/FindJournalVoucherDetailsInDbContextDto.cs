using Accounting_System.Models;
using Accounting_System.Models.AccountsPayable;

namespace Accounting_System.DTOs
{
    public class FindJournalVoucherDetailsInDbContextDto
    {
        public IReadOnlyDictionary<int, JournalVoucherDetail> ExistingJournalVoucherDetail { get; init; }
            = new Dictionary<int, JournalVoucherDetail>();

        public IReadOnlyDictionary<int, JournalVoucherHeader> JournalVoucherHeader { get; init; }
            = new Dictionary<int, JournalVoucherHeader>();

        public IReadOnlyList<ImportExportLog> ExistingLogs { get; init; }
            = new List<ImportExportLog>();
    }
}
