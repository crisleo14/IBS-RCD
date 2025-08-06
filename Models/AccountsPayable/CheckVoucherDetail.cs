using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Accounting_System.Models.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Accounting_System.Models.AccountsPayable
{
    public class CheckVoucherDetail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CheckVoucherDetailId { get; set; }

        [NotMapped]
        public List<SelectListItem>? DefaultExpenses { get; set; }

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

        public int? CheckVoucherHeaderId { get; set; }

        [ForeignKey(nameof(CheckVoucherHeaderId))]
        public CheckVoucherHeader? CheckVoucherHeader { get; set; }

        public int? OriginalDocumentId { get; set; }

        public int? SupplierId { get; set; }

        [ForeignKey(nameof(SupplierId))]
        public Supplier? Supplier { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal Amount { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal AmountPaid { get; set; }

        public bool IsVatable { get; set; }

        [Column(TypeName = "numeric(18,4)")]
        public decimal EwtPercent { get; set; }

        public bool IsUserSelected { get; set; }
    }
}
