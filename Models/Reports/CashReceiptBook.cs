using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Accounting_System.Models.Reports
{
    public class CashReceiptBook
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CashReceiptBookId { get; set; }

        [Display(Name = "OR Date")]
        [Column(TypeName = "date")]
        public DateOnly Date { get; set; }

        [Display(Name = "Ref No")]
        [StringLength(50)]
        public string RefNo { get; set; }

        [StringLength(200)]
        [Display(Name = "Customer Name")]
        public string CustomerName { get; set; }

        [StringLength(100)]
        public string? Bank { get; set; }

        [StringLength(50)]
        [Display(Name = "Check No.")]
        public string? CheckNo { get; set; }

        [StringLength(250)]
        [Display(Name = "Chart of Account")]
        public string COA { get; set; }

        [StringLength(1000)]
        public string Particulars { get; set; }

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
