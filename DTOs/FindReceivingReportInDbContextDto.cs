using Accounting_System.Models;
using Accounting_System.Models.AccountsPayable;

namespace Accounting_System.DTOs
{
    public class FindReceivingReportInDbContextDto
    {
        public IReadOnlyDictionary<string, ReceivingReport> ExistingReceivingReport { get; init; }
            = new Dictionary<string, ReceivingReport>();

        public Dictionary<int, (int PurchaseOrderId, string? PurchaseOrderNo)> ExistingPurchaseOrder { get; init; }
            = new();

        public IReadOnlyList<ImportExportLog> ExistingLogs { get; init; }
            = new List<ImportExportLog>();
    }
}
