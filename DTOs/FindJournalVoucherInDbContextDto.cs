using Accounting_System.Models;
using Accounting_System.Models.AccountsPayable;

namespace Accounting_System.DTOs
{
    public class FindJournalVoucherInDbContextDto
    {
        public IReadOnlyDictionary<string, JournalVoucherHeader> ExistingJournalVoucherHeader { get; init; }
            = new Dictionary<string, JournalVoucherHeader>();

        public IReadOnlyDictionary<string, JournalVoucherDetail> ExistingJournalVoucherDetail { get; init; }
            = new Dictionary<string, JournalVoucherDetail>();

        public IReadOnlyDictionary<int, int> CvId { get; init; }
            = new Dictionary<int, int>();

        public IReadOnlyList<ImportExportLog> ExistingLogs { get; init; }
            = new List<ImportExportLog>();
    }
}
