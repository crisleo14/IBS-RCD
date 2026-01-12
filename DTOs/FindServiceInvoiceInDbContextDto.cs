using Accounting_System.Models;
using Accounting_System.Models.AccountsReceivable;

namespace Accounting_System.DTOs
{
    public class FindServiceInvoiceInDbContextDto
    {
        public IReadOnlyDictionary<string, ServiceInvoice> ExistingInvoices { get; init; }
            = new Dictionary<string, ServiceInvoice>();

        public IReadOnlyDictionary<int, int> CustomerId { get; init; }
            = new Dictionary<int, int>();

        public IReadOnlyDictionary<int, int> ServicesId { get; init; }
            = new Dictionary<int, int>();

        public IReadOnlyList<ImportExportLog> ExistingLogs { get; init; }
            = new List<ImportExportLog>();
    }
}
