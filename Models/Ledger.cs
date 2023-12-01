using System.ComponentModel.DataAnnotations;

namespace Accounting_System.Models
{
    public class Ledger : BaseEntity
    {
        [Display(Name = "Account No")]
        public int AccountNo { get; set; }

        [Display(Name = "Transaction No")]
        public string TransactionNo { get; set; }

        [Display(Name = "Transaction Date")]
        public string TransactionDate { get; set; }

        public string Category { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal Amount { get; set; }
    }
}