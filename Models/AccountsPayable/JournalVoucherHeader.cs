using Accounting_System.Models.AccountsPayable;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Accounting_System.Models
{
    public class JournalVoucherHeader : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int JournalVoucherHeaderId { get; set; }

        [Display(Name = "JV No")]
        [StringLength(13)]
        public string? JournalVoucherHeaderNo { get; set; }

        [Display(Name = "Transaction Date")]
        [Column(TypeName = "date")]
        public DateOnly Date { get; set; }

        [StringLength(100)]
        public string? References { get; set; }

        [Display(Name = "Check Voucher Id")]
        public int? CVId { get; set; }

        [ForeignKey(nameof(CVId))]
        public CheckVoucherHeader? CheckVoucherHeader { get; set; }

        [NotMapped]
        public List<SelectListItem>? CheckVoucherHeaders { get; set; }

        [StringLength(1000)]
        public string Particulars { get; set; }

        [Display(Name = "CR No")]
        [StringLength(100)]
        public string? CRNo { get; set; }

        [Display(Name = "JV Reason")]
        [StringLength(1000)]
        public string JVReason { get; set; }

        [NotMapped]
        public List<SelectListItem>? COA { get; set; }


        //Ibs records
        public int? OriginalCVId { get; set; }

        //ICollection
        public ICollection<JournalVoucherDetail> Details { get; set; }
    }
}
