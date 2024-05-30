using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Accounting_System.Models.AccountsReceivable
{
    public class CollectionReceipt : BaseEntity
    {
        [Display(Name = "Collection Receipt No.")]
        [Column(TypeName = "varchar(12)")]
        public string? CRNo { get; set; }

        //Sales Invoice Property

        public int? SalesInvoiceId { get; set; }

        [Display(Name = "Sales Invoice No.")]
        [Column(TypeName = "varchar(12)")]
        public string? SINo { get; set; }

        [ForeignKey("SalesInvoiceId")]
        public SalesInvoice? SalesInvoice { get; set; }

        [NotMapped]
        public List<SelectListItem>? SalesInvoices { get; set; }

        //Service Invoice Property

        public int? ServiceInvoiceId { get; set; }

        [Display(Name = "Sales Invoice No.")]
        [Column(TypeName = "varchar(12)")]
        public string? SVNo { get; set; }

        [ForeignKey("ServiceInvoiceId")]
        public ServiceInvoice? ServiceInvoice { get; set; }

        [NotMapped]
        public List<SelectListItem>? ServiceInvoices { get; set; }

        //Customer Property

        [NotMapped]
        public List<SelectListItem>? Customers { get; set; }

        [Required(ErrorMessage = "Customer is required.")]
        public int CustomerId { get; set; }

        //COA Property

        [NotMapped]
        public List<SelectListItem>? ChartOfAccounts { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateOnly Date { get; set; }

        public long SeriesNumber { get; set; }

        [Display(Name = "Reference No")]
        [Required]
        [Column(TypeName = "varchar(20)")]
        public string ReferenceNo { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string? Remarks { get; set; }

        //Cash
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal CashAmount { get; set; }

        //Check
        [Column(TypeName = "date")]
        public DateOnly? CheckDate { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? CheckNo { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? CheckBank { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? CheckBranch { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal CheckAmount { get; set; }

        //Manager's Check
        [Column(TypeName = "date")]
        public DateOnly? ManagerCheckDate { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? ManagerCheckNo { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? ManagerCheckBank { get; set; }

        [Column(TypeName = "varchar(20)")]
        public string? ManagerCheckBranch { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal ManagerCheckAmount { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal EWT { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal WVAT { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        [Column(TypeName = "numeric(18,2)")]
        public decimal Total { get; set; }

        public bool IsCertificateUpload { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string? F2306FilePath { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string? F2307FilePath { get; set; }
    }
}