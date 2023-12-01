using System.ComponentModel.DataAnnotations;

namespace Accounting_System.Models
{
    public class CheckVoucherDetail : BaseEntity
    {
        public string AccountNo { get; set; }
        public string AccountName { get; set; }

        public string TransactionNo { get; set; }

        public string Category { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal Amount { get; set; }
    }
}