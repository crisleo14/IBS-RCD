using Accounting_System.Models.AccountsPayable;
using Accounting_System.Models.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Accounting_System.Models.AccountsReceivable
{
    public class SalesInvoice : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SalesInvoiceId { get; set; }

        [Display(Name = "SI No")]
        [StringLength(13)]
        public string? SalesInvoiceNo { get; set; }

        #region-- Customer properties

        [Required]
        [Display(Name = "Customer No")]
        public int? CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public Customer? Customer { get; set; }

        [NotMapped]
        public List<SelectListItem>? Customers { get; set; }

        #endregion

        #region-- Product properties

        [NotMapped]
        public List<SelectListItem>? Products { get; set; }

        [Required]
        [Display(Name = "Product No")]
        public int? ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public Product? Product { get; set; }

        #endregion

        [StringLength(100)]
        [Display(Name = "Other Ref No")]
        public string OtherRefNo { get; set; }

        [Required]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Quantity { get; set; }

        [Required]
        [Display(Name = "Unit Price")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal UnitPrice { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(1000)]
        public string Remarks { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Pending";

        [Required]
        [Display(Name = "Transaction Date")]
        [Column(TypeName = "date")]
        public DateOnly TransactionDate { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Discount { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal AmountPaid { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,4)")]
        public decimal Balance { get; set; }

        public bool IsPaid { get; set; }

        public bool IsTaxAndVatPaid { get; set; }

        [Display(Name = "Due Date")]
        [Column(TypeName = "date")]
        public DateOnly DueDate { get; set; }

        //Ibs records
        public int? OriginalCustomerId { get; set; }
        public int? OriginalProductId { get; set; }
    }
}
