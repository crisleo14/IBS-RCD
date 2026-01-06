using Accounting_System.Models;
using Accounting_System.Models.AccountsReceivable;
using Microsoft.AspNetCore.Razor.Language;

namespace Accounting_System.DTOs
{
    public class FindSalesInvoiceInDbContextDto
    {
        public IReadOnlyDictionary<int, SalesInvoice> ExistingInvoices { get; init; }
            = new Dictionary<int, SalesInvoice>();

        public IReadOnlyDictionary<int, int> CustomerId { get; init; }
            = new Dictionary<int, int>();

        public IReadOnlyDictionary<int, int> ProductId { get; init; }
            = new Dictionary<int, int>();

        public IReadOnlyList<ImportExportLog> ExistingLogs { get; init; }
            = new List<ImportExportLog>();
    }
}
