using Accounting_System.Models;
using Accounting_System.Models.AccountsReceivable;

namespace Accounting_System.DTOs
{
    public class FindDebitMemoInDbContextDto
    {
        public IReadOnlyDictionary<string, DebitMemo> ExistingDebitMemo { get; init; }
            = new Dictionary<string, DebitMemo>();

        public IReadOnlyDictionary<int, int> SalesInvoiceId { get; init; }
            = new Dictionary<int, int>();

        public IReadOnlyDictionary<int, int> ServiceInvoiceId { get; init; }
            = new Dictionary<int, int>();

        public IReadOnlyList<ImportExportLog> ExistingLogs { get; init; }
            = new List<ImportExportLog>();
    }
}
