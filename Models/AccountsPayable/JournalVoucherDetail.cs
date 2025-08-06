using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Accounting_System.Models.AccountsPayable
{
    public class JournalVoucherDetail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int JournalVoucherDetailId { get; set; }

        [StringLength(20)]
        public string AccountNo { get; set; } = " ";

        [StringLength(200)]
        public string AccountName { get; set; } = " ";

        [StringLength(13)]
        public string TransactionNo { get; set; } = " ";

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Debit { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Credit { get; set; }

        public int JournalVoucherHeaderId { get; set; }

        [ForeignKey(nameof(JournalVoucherHeaderId))]
        public JournalVoucherHeader JournalVoucherHeader { get; set; }

        public int? OriginalDocumentId { get; set; }
    }
}
