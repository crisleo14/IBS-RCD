using Accounting_System.Models.MasterFile;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Accounting_System.Models.AccountsReceivable
{
    public class SalesInvoice : BaseEntity
    {
        [Display(Name = "SI No")]
        [Column(TypeName = "varchar(12)")]
        public string? SINo { get; set; }

        public long SeriesNumber { get; set; }

        #region-- Customer properties

        [Required]
        [Display(Name = "Customer No")]
        public int CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public Customer? Customer { get; set; }

        [NotMapped]
        public List<SelectListItem>? Customers { get; set; }

        #endregion

        #region-- Product properties

        [NotMapped]
        public List<SelectListItem>? Products { get; set; }

        [Required]
        [Display(Name = "Product No")]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        #endregion

        [Column(TypeName = "varchar(20)")]
        [Display(Name = "Other Ref No")]
        public string OtherRefNo { get; set; }

        [Display(Name = "P.O No")]
        [Column(TypeName = "varchar(20)")]
        public string PoNo { get; set; }

        [Required]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal Quantity { get; set; }

        [Required]
        [Display(Name = "Unit Price")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal UnitPrice { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [Column(TypeName = "varchar(100)")]
        public string Remarks { get; set; }

        [Display(Name = "Vatable Sales")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal VatableSales { get; set; }

        [Display(Name = "VAT Amount")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal VatAmount { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string Status { get; set; } = "Pending";

        [Required]
        [Display(Name = "Transaction Date")]
        [Column(TypeName = "date")]
        public DateOnly TransactionDate { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal Discount { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal NetDiscount { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal VatExempt { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal ZeroRated { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal WithHoldingVatAmount { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal WithHoldingTaxAmount { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal AmountPaid { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal Balance { get; set; }

        public bool IsPaid { get; set; }

        public bool IsTaxAndVatPaid { get; set; }

        [Display(Name = "Due Date")]
        [Column(TypeName = "date")]
        public DateOnly DueDate { get; set; }
    }
}