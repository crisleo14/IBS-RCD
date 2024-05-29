using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Accounting_System.Models
{
    public class DebitMemo : BaseEntity
    {
        [Display(Name = "SI No")]
        public int? SalesInvoiceId { get; set; }

        [ForeignKey("SalesInvoiceId")]
        public SalesInvoice? SalesInvoice { get; set; }

        [NotMapped]
        public List<SelectListItem>? SalesInvoices { get; set; }

        [Display(Name = "SV No")]
        public int? ServiceInvoiceId { get; set; }

        [ForeignKey("ServiceInvoiceId")]
        public ServiceInvoice? ServiceInvoice { get; set; }

        [NotMapped]
        public List<SelectListItem>? ServiceInvoices { get; set; }

        public string? DMNo { get; set; }
        public long SeriesNumber { get; set; }

        [Column(TypeName = "date")]
        public DateOnly Date { get; set; }

        [Display(Name = "Debit Amount")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal DebitAmount { get; set; }

        public string Description { get; set; }

        [Display(Name = "Vatable Sales")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal VatableSales { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal VatAmount { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal TotalSales { get; set; }

        [Display(Name = "Price Adjustment")]
        [Range(1, int.MaxValue, ErrorMessage = "Adjusted Price should be greater than 0")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal? AdjustedPrice { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Quantity should be greater than 0")]
        [Column(TypeName = "numeric(18,2)")]
        public int? Quantity { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal WithHoldingVatAmount { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal WithHoldingTaxAmount { get; set; }

        public string Source { get; set; }

        public string? Remarks { get; set; }

        [Column(TypeName = "date")]
        public DateOnly Period { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal Amount { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal CurrentAndPreviousAmount { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal UnearnedAmount { get; set; }

        public int ServicesId { get; set; }
    }
}