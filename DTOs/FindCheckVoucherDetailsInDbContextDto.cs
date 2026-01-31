using Accounting_System.Models;
using Accounting_System.Models.AccountsPayable;

namespace Accounting_System.DTOs
{
    public class FindCheckVoucherDetailsInDbContextDto
    {
        public IReadOnlyDictionary<int, CheckVoucherDetail> ExistingCheckVoucherDetail { get; init; }
            = new Dictionary<int, CheckVoucherDetail>();

        public IReadOnlyDictionary<int, int> SupplierId { get; init; }
            = new Dictionary<int, int>();

        public IReadOnlyDictionary<int, CheckVoucherHeader> CheckVoucherHeader { get; init; }
            = new Dictionary<int, CheckVoucherHeader>();

        public IReadOnlyList<ImportExportLog> ExistingLogs { get; init; }
            = new List<ImportExportLog>();
    }
}
