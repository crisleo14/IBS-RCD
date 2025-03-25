using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Accounting_System.Models.AccountsPayable
{
    public class CVTradePayment
    {
        [Key]
        public int Id { get; set; }

        public int DocumentId { get; set; }

        public string DocumentType { get; set; }

        public int CheckVoucherId { get; set; }

        [ForeignKey(nameof(CheckVoucherId))]
        public CheckVoucherHeader CV { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal AmountPaid { get; set; }
    }
}
