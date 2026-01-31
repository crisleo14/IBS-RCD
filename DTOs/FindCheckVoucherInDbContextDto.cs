using Accounting_System.Models;
using Accounting_System.Models.AccountsPayable;

namespace Accounting_System.DTOs
{
    public class FindCheckVoucherInDbContextDto
    {
        public IReadOnlyDictionary<string, CheckVoucherHeader> ExistingCheckVoucherHeader { get; init; }
            = new Dictionary<string, CheckVoucherHeader>();

        public IReadOnlyDictionary<string, CheckVoucherDetail> ExistingCheckVoucherDetail { get; init; }
            = new Dictionary<string, CheckVoucherDetail>();

        public IReadOnlyDictionary<int, int> SupplierId { get; init; }
                    = new Dictionary<int, int>();

        public IReadOnlyDictionary<int, int> BankId { get; init; }
            = new Dictionary<int, int>();

        public IReadOnlyList<ImportExportLog> ExistingLogs { get; init; }
            = new List<ImportExportLog>();
    }
}
