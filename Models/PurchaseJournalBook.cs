using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Accounting_System.Models
{
    public class PurchaseJournalBook
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column(TypeName = "date")]
        public DateOnly Date { get; set; }

        [Display(Name = "Supplier Name")]
        public string SupplierName { get; set; }

        [Display(Name = "Supplier TIN")]
        public string SupplierTin { get; set; }

        [Display(Name = "Supplier Address")]
        public string SupplierAddress { get; set; }

        [Display(Name = "Document No")]
        public string DocumentNo { get; set; }

        public string Description { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal Discount { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Display(Name = "VAT Amount")]
        [Column(TypeName = "numeric(18,2)")]
        public decimal VatAmount { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal Amount { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Display(Name = "WHT Amount")]
        [Column(TypeName = "numeric(18,2)")]
        public decimal WhtAmount { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Display(Name = "Net Purchases")]
        [Column(TypeName = "numeric(18,2)")]
        public decimal NetPurchases { get; set; }

        [Display(Name = "Created By")]
        [Column(TypeName = "varchar(50)")]
        public string? CreatedBy { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "PO No.")]
        [Column(TypeName = "varchar(12)")]
        public string PONo { get; set; }

        [Display(Name = "Due Date")]
        [Column(TypeName = "date")]
        public DateOnly DueDate { get; set; }
    }
}