using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Accounting_System.Models.Reports
{
    public class DisbursementBook
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int DisbursementBookId { get; set; }

        [Column(TypeName = "date")]
        public DateOnly Date { get; set; }

        [Display(Name = "CV No")]
        [StringLength(13)]
        public string CVNo { get; set; }

        [StringLength(200)]
        public string Payee { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Amount { get; set; }

        [StringLength(1000)]
        public string Particulars { get; set; }

        [StringLength(100)]
        public string Bank { get; set; }

        [StringLength(50)]
        [Display(Name = "Check No")]
        public string CheckNo { get; set; }

        [Display(Name = "Check Date")]
        public string CheckDate { get; set; }

        [StringLength(500)]
        [Display(Name = "Chart Of Account")]
        public string ChartOfAccount { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Debit { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Credit { get; set; }

        [Display(Name = "Created By")]
        [StringLength(50)]
        public string? CreatedBy { get; set; }

        [Display(Name = "Created Date")]
        [Column(TypeName = "timestamp without time zone")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
