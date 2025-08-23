using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Accounting_System.Models.Reports
{
    public class GeneralLedgerBook
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int GeneralLedgerBookId { get; set; }

        [Column(TypeName = "date")]
        public DateOnly Date { get; set; }

        [StringLength(13)]
        public string Reference { get; set; }

        [StringLength(20)]
        [Display(Name = "Account Number")]
        public string AccountNo { get; set; }

        [StringLength(300)]
        [Display(Name = "Account Title")]
        public string AccountTitle { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

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

        public bool IsPosted { get; set; } = true;
    }
}
