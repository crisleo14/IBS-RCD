using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Accounting_System.Models
{
    public class CollectionReceipt : BaseEntity
    {
        [Required]
        public int SalesInvoiceId { get; set; }

        [ForeignKey("SalesInvoiceId")]
        public SalesInvoice? SalesInvoice { get; set; }

        public string? CRNo { get; set; }

        public long SeriesNumber { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Display(Name = "Reference No")]
        [Required]
        public string ReferenceNo { get; set; }

        [Display(Name = "Form Of Payment")]
        [Required]
        public string FormOfPayment { get; set; }

        [Display(Name = "Check Date")]
        public DateTime CheckDate { get; set; }

        [Display(Name = "Check No")]
        public string? CheckNo { get; set; }

        public string? Bank { get; set; }

        public string? Branch { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal Amount { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal EWT { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal WVAT { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Required]
        public decimal Total { get; set; }

        public bool IsPrinted { get; set; }

        [NotMapped]
        public List<SelectListItem>? Customers { get; set; }

        [Required]
        public int CustomerNo { get; set; }

        [NotMapped]
        public List<SelectListItem>? Invoices { get; set; }

        [Required]
        public string Preference { get; set; }

        [NotMapped]
        public List<SelectListItem>? ChartOfAccounts { get; set; }

        public string? SINo { get; set; }
    }
}