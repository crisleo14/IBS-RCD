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

        [Display(Name = "Collection Receipt No.")]
        public string? CRNo { get; set; }

        public long SeriesNumber { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Display(Name = "Reference No")]
        [Required]
        public string ReferenceNo { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal EWT { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal WVAT { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal Total { get; set; }

        public bool IsPrinted { get; set; }

        [NotMapped]
        public List<SelectListItem>? Customers { get; set; }

        [Required]
        public int CustomerNo { get; set; }

        [NotMapped]
        public List<SelectListItem>? Invoices { get; set; }

        public string Preference { get; set; } = " ";

        [NotMapped]
        public List<SelectListItem>? ChartOfAccounts { get; set; }

        [Display(Name = "Sales Invoice No.")]
        public string? SINo { get; set; }

        //cash
        public decimal CashAmount { get; set; }

        //Check
        public DateTime? CheckDate { get; set; }

        public string? CheckNo { get; set; }

        public string? CheckBank { get; set; }

        public string? CheckBranch { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal CheckAmount { get; set; }

        //Manager's Check
        public DateTime? ManagerCheckDate { get; set; }

        public string? ManagerCheckNo { get; set; }

        public string? ManagerCheckBank { get; set; }

        public string? ManagerCheckBranch { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal ManagerCheckAmount { get; set; }

        public string? Remarks { get; set; }
    }
}