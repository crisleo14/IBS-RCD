using Accounting_System.Models;
using Accounting_System.Models.AccountsReceivable;

namespace Accounting_System.DTOs
{
    public class FindCollectionReceiptInDbContextDto
    {
        public IReadOnlyDictionary<string, CollectionReceipt> ExistingCollectionReceipt { get; init; }
            = new Dictionary<string, CollectionReceipt>();

        public IReadOnlyDictionary<int, int> CustomerId { get; init; }
            = new Dictionary<int, int>();

        public Dictionary<int, (int SalesInvoiceId, string? SalesInvoiceNo)> ExistingSalesInvoice { get; init; }
            = new();

        public Dictionary<int, (int ServiceInvoiceId, string? ServiceInvoiceNo)> ExistingServiceInvoice { get; init; }
            = new();

        public IReadOnlyList<ImportExportLog> ExistingLogs { get; init; }
            = new List<ImportExportLog>();
    }
}
