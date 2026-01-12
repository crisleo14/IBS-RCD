using Accounting_System.Models;
using Accounting_System.Models.AccountsPayable;

namespace Accounting_System.DTOs
{
    public class FindPurchaseOrderInDbContextDto
    {
        public IReadOnlyDictionary<string, PurchaseOrder> ExistingPurchaseOrder { get; init; }
            = new Dictionary<string, PurchaseOrder>();

        public Dictionary<int, (int ProductId, string? ProductCode)> ExistingProduct { get; init; }
            = new();

        public Dictionary<int, (int SupplierId, int SupplierNo)> ExistingSuppliers { get; init; }
            = new();

        public IReadOnlyList<ImportExportLog> ExistingLogs { get; init; }
            = new List<ImportExportLog>();
    }
}
