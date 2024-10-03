using Accounting_System.Models.AccountsPayable;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Accounting_System.Models
{
    public class JournalVoucherHeader : BaseEntity
    {
        [Display(Name = "JV No")]
        public string? JVNo { get; set; }

        public long SeriesNumber { get; set; }

        [Display(Name = "Transaction Date")]
        [Column(TypeName = "date")]
        public DateOnly Date { get; set; }

        public string? References { get; set; }

        [Display(Name = "Check Voucher Id")]
        public int? CVId { get; set; }

        [ForeignKey("CVId")]
        public CheckVoucherHeader? CheckVoucherHeader { get; set; }

        [NotMapped]
        public List<SelectListItem>? CheckVoucherHeaders { get; set; }

        public string Particulars { get; set; }

        [Display(Name = "CR No")]
        public string? CRNo { get; set; }

        [Display(Name = "JV Reason")]
        public string JVReason { get; set; }

        [NotMapped]
        public List<SelectListItem>? COA { get; set; }


        //Ibs records
        public int? OriginalCVId { get; set; }

        //ICollection
        public ICollection<JournalVoucherDetail> Details { get; set; }
    }
}