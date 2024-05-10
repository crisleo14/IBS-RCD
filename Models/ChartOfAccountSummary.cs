namespace Accounting_System.Models
{
    public class ChartOfAccountSummary
    {
        public string AccountNumber { get; set; }

        public string AccountName { get; set; }

        public string AccountType { get; set; }

        public string? Parent { get; set; }

        public int? Level { get; set; }

        public decimal Debit { get; set; }

        public decimal Credit { get; set; }

        public decimal Balance { get; set; }

        public List<ChartOfAccountSummary>? Children { get; set; }
    }
}
