using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Accounting_System.Models.Reports
{
    public class PurchaseJournalBook
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PurchaseBookId { get; set; }

        [Column(TypeName = "date")]
        public DateOnly Date { get; set; }

        [Display(Name = "Supplier Name")]
        [StringLength(100)]
        public string SupplierName { get; set; }

        [StringLength(20)]
        [Display(Name = "Supplier TIN")]
        public string SupplierTin { get; set; }

        [StringLength(200)]
        [Display(Name = "Supplier Address")]
        public string SupplierAddress { get; set; }

        [StringLength(13)]
        [Display(Name = "Document No")]
        public string DocumentNo { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Discount { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Display(Name = "VAT Amount")]
        [Column(TypeName = "numeric(18,4)")]
        public decimal VatAmount { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Amount { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Display(Name = "WHT Amount")]
        [Column(TypeName = "numeric(18,4)")]
        public decimal WhtAmount { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Display(Name = "Net Purchases")]
        [Column(TypeName = "numeric(18,4)")]
        public decimal NetPurchases { get; set; }

        [Display(Name = "Created By")]
        [StringLength(50)]
        public string? CreatedBy { get; set; }

        [Display(Name = "Created Date")]
        [Column(TypeName = "timestamp without time zone")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "PO No.")]
        [StringLength(13)]
        public string PONo { get; set; }

        [Display(Name = "Due Date")]
        [Column(TypeName = "date")]
        public DateOnly DueDate { get; set; }
    }
}
