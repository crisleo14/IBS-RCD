using System.ComponentModel.DataAnnotations.Schema;

namespace Accounting_System.Models.AccountsPayable
{
    public class MultipleCheckVoucherPayment
    {
        public Guid Id { get; set; }

        [ForeignKey(nameof(CheckVoucherHeaderPaymentId))]
        public CheckVoucherHeader? CheckVoucherHeaderPayment { get; set; } = null;
        public int CheckVoucherHeaderPaymentId{ get; set; }

        [ForeignKey(nameof(CheckVoucherHeaderInvoiceId))]
        public CheckVoucherHeader? CheckVoucherHeaderInvoice { get; set; } = null;
        public int CheckVoucherHeaderInvoiceId { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal AmountPaid { get; set; }
    }
}
